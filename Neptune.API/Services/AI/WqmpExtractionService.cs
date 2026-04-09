using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using OpenAI;
using OpenAI.Responses;

namespace Neptune.API.Services.AI;

#pragma warning disable OPENAI001

public class WqmpExtractionService
{
    private const string ModelId = "gpt-5.2";
    private const string SchemaVersion = "v1.0";
    private static readonly Lazy<string> ExtractedValueSchema = new(BuildExtractedValueJsonSchema);
    private static readonly Lazy<string> WqmpSchema = new(BuildWqmpSchemaJson);
    private static readonly Lazy<string> ParcelSchema = new(BuildParcelSchemaJson);
    private static readonly Lazy<string> TreatmentBmpSchema = new(BuildTreatmentBmpSchemaJson);
    private static readonly Lazy<string> SourceControlBmpSchema = new(BuildSourceControlBmpSchemaJson);

    private readonly OpenAIClient _openAIClient;
    private readonly AzureBlobStorageService _blobService;
    private readonly NeptuneDbContext _dbContext;
    private readonly IPromptTemplateService _promptTemplateService;
    private readonly ILogger<WqmpExtractionService> _logger;
    private readonly string _apiKey;

    public WqmpExtractionService(
        OpenAIClient openAIClient,
        AzureBlobStorageService blobService,
        NeptuneDbContext dbContext,
        IPromptTemplateService promptTemplateService,
        ILogger<WqmpExtractionService> logger,
        IOptions<NeptuneConfiguration> configuration)
    {
        _openAIClient = openAIClient;
        _blobService = blobService;
        _dbContext = dbContext;
        _promptTemplateService = promptTemplateService;
        _logger = logger;
        _apiKey = configuration.Value.OpenAIApiKey;
    }

    public async Task<WaterQualityManagementPlanDocumentExtractionResultDto> ExtractFromDocument(
        int waterQualityManagementPlanDocumentID, int personID, CancellationToken cancellationToken)
    {
        var docDto = await WaterQualityManagementPlanDocuments.GetByIDAsDtoAsync(_dbContext, waterQualityManagementPlanDocumentID);
        if (docDto == null) throw new InvalidOperationException($"Document {waterQualityManagementPlanDocumentID} not found.");

        var vectorStoreId = await EnsureVectorStoreWithFileAsync(waterQualityManagementPlanDocumentID, docDto);
        var domainContext = await BuildDomainContext();

        var evidenceInstructions =
            $"SchemaVersion: {SchemaVersion}. Use ONLY the provided WQMP PDF. Each attribute object MUST match ExtractedValueSchema. " +
            "Value = raw extracted string or null; ExtractionEvidence = source snippet (preceding sentence, target sentence, following sentence OR nearby table text); DocumentSource = page reference (e.g. 'Page 12'). " +
            "If not found set Value, ExtractionEvidence, DocumentSource to null. Do not add or rename properties.\n" +
            $"ExtractedValueSchema: {ExtractedValueSchema.Value}";

        var prompts = new Dictionary<string, (PromptTemplate template, string schema, bool expectArray)>
        {
            ["WQMP"] = (PromptTemplate.ExtractWqmpFields, WqmpSchema.Value, false),
            ["Parcels"] = (PromptTemplate.ExtractParcels, ParcelSchema.Value, true),
            ["TreatmentBMPs"] = (PromptTemplate.ExtractTreatmentBMPs, TreatmentBmpSchema.Value, true),
            ["SourceControlBMPs"] = (PromptTemplate.ExtractSourceControlBMPs, SourceControlBmpSchema.Value, true),
        };

        var responseClient = CreateResponseClient();
        var fileSearchTool = CreateFileSearchTool(vectorStoreId);

        async Task<string> ExtractCategoryAsync(string key, PromptTemplate template, string schema, bool expectArray)
        {
            var templateModel = new
            {
                EvidenceInstructions = evidenceInstructions,
                DomainContext = domainContext,
                ExtractedValueSchema = ExtractedValueSchema.Value,
                Schema = schema,
            };
            var prompt = _promptTemplateService.Render(template, templateModel);

            // Use structured output for guaranteed valid JSON
            var textOptions = new ResponseTextOptions();
            textOptions.TextFormat = ResponseTextFormat.CreateJsonSchemaFormat($"extraction_{key}", BinaryData.FromString(schema));

            var options = new ResponseCreationOptions
            {
                Tools = { fileSearchTool },
                TextOptions = textOptions,
            };
            var response = await responseClient.CreateResponseAsync(prompt, options, cancellationToken);
            await LogTokenUsage(personID, response.Value, $"WQMP Extraction - {key}");

            var output = ExtractMessageText(response.Value.OutputItems);
            if (!IsValidJson(output))
            {
                _logger.LogError("Structured output returned invalid JSON for {Category}. Using empty fallback.", key);
                output = expectArray ? "[]" : "{}";
            }
            return output;
        }

        var tasks = prompts.Select(kvp => ExtractCategoryAsync(kvp.Key, kvp.Value.template, kvp.Value.schema, kvp.Value.expectArray)).ToList();
        var results = await Task.WhenAll(tasks);
        var keys = prompts.Keys.ToList();
        var map = new Dictionary<string, string>();
        for (var i = 0; i < keys.Count; i++) map[keys[i]] = results[i];

        var finalOutput = $"{{ \"SchemaVersion\": \"{SchemaVersion}\", \"WQMP\": {map["WQMP"]}, \"Parcels\": {map["Parcels"]}, \"TreatmentBMPs\": {map["TreatmentBMPs"]}, \"SourceControlBMPs\": {map["SourceControlBMPs"]} }}";

        if (!IsValidJson(finalOutput))
        {
            _logger.LogError("Final consolidated JSON is invalid. Using raw parts.");
        }

        return new WaterQualityManagementPlanDocumentExtractionResultDto
        {
            FinalOutput = finalOutput,
            RawResults = string.Join("\n", map.Select(kvp => $"{kvp.Key}: {kvp.Value}")),
            ExtractedAt = DateTime.UtcNow
        };
    }

    private async Task LogTokenUsage(int personID, OpenAIResponse response, string context)
    {
        try
        {
            var usage = response.Usage;
            if (usage != null)
            {
                _dbContext.AITokenUsages.Add(new AITokenUsage
                {
                    PersonID = personID,
                    Model = ModelId,
                    InputTokens = usage.InputTokenCount,
                    CachedInputTokens = 0,
                    OutputTokens = usage.OutputTokenCount,
                    RequestDate = DateTime.UtcNow,
                    RequestContext = context
                });
                await _dbContext.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Failed to log token usage for {Context}", context);
        }
    }

    private async Task<string> EnsureVectorStoreWithFileAsync(int documentId, WaterQualityManagementPlanDocumentDto docDto)
    {
        var existingVectorStoreId = await WaterQualityManagementPlanDocumentVectorStores.GetByWaterQualityManagementPlanDocumentIDAsDtoAsync(_dbContext, documentId);
        if (!string.IsNullOrWhiteSpace(existingVectorStoreId)) return existingVectorStoreId;

        var vectorStoreClient = _openAIClient.GetVectorStoreClient();
        var fileClient = _openAIClient.GetOpenAIFileClient();
        var fileStream = await _blobService.DownloadBlobFromBlobStorageAsStream(docDto.FileResource.FileResourceGUID.ToString());
        var fileUploadResult = await fileClient.UploadFileAsync(fileStream.Content, docDto.FileResource.OriginalFilename, OpenAI.Files.FileUploadPurpose.Assistants);
        if (fileUploadResult?.Value == null) throw new Exception($"OpenAI file upload failed for documentId={documentId}");

        var options = new OpenAI.VectorStores.VectorStoreCreationOptions { Name = $"WQMP_{documentId}" };
        options.FileIds.Add(fileUploadResult.Value.Id);
        var vectorStoreCreateResult = await vectorStoreClient.CreateVectorStoreAsync(options);
        if (vectorStoreCreateResult?.Value == null) throw new Exception($"OpenAI vector store creation failed for documentId={documentId}");

        var vectorStoreId = vectorStoreCreateResult.Value.Id;
        var fileId = fileUploadResult.Value.Id;

        // Poll until vector store is ready
        while (vectorStoreCreateResult.Value.Status != OpenAI.VectorStores.VectorStoreStatus.Completed)
        {
            vectorStoreCreateResult = await vectorStoreClient.GetVectorStoreAsync(vectorStoreId);
            if (vectorStoreCreateResult?.Value == null) throw new Exception($"OpenAI vector store status check failed for vectorStoreId={vectorStoreId}");
            if (vectorStoreCreateResult.Value.Status == OpenAI.VectorStores.VectorStoreStatus.Expired)
                throw new Exception($"OpenAI vector store expired for vectorStoreId={vectorStoreId}");
            await Task.Delay(200);
        }

        // Poll until the file within the vector store is fully indexed
        OpenAI.VectorStores.VectorStoreFile vectorStoreFile = null;
        do
        {
            try
            {
                var fileResult = await vectorStoreClient.GetVectorStoreFileAsync(vectorStoreId, fileId);
                vectorStoreFile = fileResult?.Value;
                if (vectorStoreFile == null) throw new Exception($"OpenAI file status check failed for fileId={fileId}");
                if (vectorStoreFile.Status == OpenAI.VectorStores.VectorStoreFileStatus.Completed) break;
                if (vectorStoreFile.Status == OpenAI.VectorStores.VectorStoreFileStatus.Failed)
                {
                    var lastError = vectorStoreFile.LastError;
                    var errorDetail = lastError != null ? $"Code={lastError.Code}, Message={lastError.Message}" : "no details";
                    _logger.LogError("OpenAI file indexing failed for fileId={FileId}: {Error}", fileId, errorDetail);
                    throw new Exception($"OpenAI file indexing failed for fileId={fileId}: {errorDetail}");
                }
            }
            catch (System.ClientModel.ClientResultException)
            {
                // File not ready yet, continue polling
            }
            await Task.Delay(200);
        } while (vectorStoreFile == null || vectorStoreFile.Status != OpenAI.VectorStores.VectorStoreFileStatus.Completed);

        _logger.LogInformation("Vector store {VectorStoreId} ready with file {FileId} indexed.", vectorStoreId, fileId);
        await WaterQualityManagementPlanDocumentVectorStores.UpsertAsync(_dbContext, documentId, vectorStoreId);
        return vectorStoreId;
    }

    private async Task<string> BuildDomainContext()
    {
        var domainTables = new
        {
            Jurisdictions = await _dbContext.StormwaterJurisdictions.Include(x => x.Organization).Select(x => x.Organization.OrganizationName).AsNoTracking().ToListAsync(),
            TreatmentBMPTypes = await _dbContext.TreatmentBMPTypes.Select(x => x.TreatmentBMPTypeName).AsNoTracking().ToListAsync(),
            HydrologicSubareas = await _dbContext.HydrologicSubareas.Select(x => x.HydrologicSubareaName).AsNoTracking().ToListAsync(),
            WaterQualityManagementPlanLandUse = WaterQualityManagementPlanLandUse.All.Select(x => x.WaterQualityManagementPlanLandUseDisplayName),
            WaterQualityManagementPlanPriority = WaterQualityManagementPlanPriority.All.Select(x => x.WaterQualityManagementPlanPriorityDisplayName),
            WaterQualityManagementPlanStatus = WaterQualityManagementPlanStatus.All.Select(x => x.WaterQualityManagementPlanStatusDisplayName),
            WaterQualityManagementPlanDevelopmentType = WaterQualityManagementPlanDevelopmentType.All.Select(x => x.WaterQualityManagementPlanDevelopmentTypeDisplayName),
            WaterQualityManagementPlanPermitTerm = WaterQualityManagementPlanPermitTerm.All.Select(x => x.WaterQualityManagementPlanPermitTermDisplayName),
            TrashCaptureStatusType = TrashCaptureStatusType.All.Select(x => x.TrashCaptureStatusTypeDisplayName),
            SourceControlBMPAttributes = await _dbContext.SourceControlBMPAttributes.AsNoTracking().Select(x => x.SourceControlBMPAttributeName).ToListAsync(),
        };
        return $"SCHEMA_VERSION: {SchemaVersion}\nDOMAIN TABLES: {JsonSerializer.Serialize(domainTables)}\n";
    }

    [Experimental("OPENAI001")]
    private OpenAIResponseClient CreateResponseClient() => new(ModelId, apiKey: _apiKey);

    [Experimental("OPENAI001")]
    private static ResponseTool CreateFileSearchTool(string fileId) => ResponseTool.CreateFileSearchTool([fileId]);

    [Experimental("OPENAI001")]
    private static string ExtractMessageText(IEnumerable<ResponseItem> items)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var outputItem in items)
        {
            if (outputItem is MessageResponseItem m)
            {
                var text = m.Content?.FirstOrDefault()?.Text;
                if (!string.IsNullOrEmpty(text)) sb.Append(Regex.Replace(text, "【.*?】", string.Empty));
            }
        }
        return sb.ToString();
    }

    private static bool IsValidJson(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return false;
        try { using var _ = JsonDocument.Parse(candidate); return true; } catch { return false; }
    }

    private static bool MatchesExtractionSchema(string json, bool expectArray)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (expectArray)
            {
                if (root.ValueKind != JsonValueKind.Array) return false;
                foreach (var item in root.EnumerateArray())
                    if (item.ValueKind != JsonValueKind.Object || !AllChildPropertiesMatchTripleSchema(item)) return false;
            }
            else
            {
                if (root.ValueKind != JsonValueKind.Object || !AllChildPropertiesMatchTripleSchema(root)) return false;
            }
            return true;
        }
        catch { return false; }
    }

    private static bool AllChildPropertiesMatchTripleSchema(JsonElement obj)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            var v = prop.Value;
            if (v.ValueKind == JsonValueKind.Null) continue;
            if (v.ValueKind != JsonValueKind.Object) return false;
            if (!(v.TryGetProperty("Value", out _) && v.TryGetProperty("ExtractionEvidence", out _) && v.TryGetProperty("DocumentSource", out _))) return false;
        }
        return true;
    }

    private static object ExtractedValueProp(string description) => new
    {
        type = "object",
        description,
        properties = new
        {
            Value = new { type = new[] { "string", "null" }, description = "Raw extracted value or null." },
            ExtractionEvidence = new { type = new[] { "string", "null" }, description = "Source snippet or null." },
            DocumentSource = new { type = new[] { "string", "null" }, description = "Page reference or null." }
        },
        required = new[] { "Value", "ExtractionEvidence", "DocumentSource" },
        additionalProperties = false
    };

    // Schema builders

    private static string BuildExtractedValueJsonSchema()
    {
        var schema = new
        {
            type = "object",
            description = "ExtractedValue schema. Attribute with evidence.",
            properties = new
            {
                Value = new { type = "string", description = "Raw extracted value or null." },
                ExtractionEvidence = new { type = "string", description = "Snippet: preceding, target, following sentence OR nearby table text." },
                DocumentSource = new { type = "string", description = "Page reference (e.g. 'Page 12')." }
            },
            required = new[] { "Value", "ExtractionEvidence", "DocumentSource" },
            additionalProperties = false
        };
        return JsonSerializer.Serialize(schema);
    }

    private static string BuildWqmpSchemaJson()
    {
        var properties = new Dictionary<string, object>
        {
            ["WaterQualityManagementPlanName"] = ExtractedValueProp("Title of the WQMP."),
            ["Jurisdiction"] = ExtractedValueProp("Jurisdiction responsible."),
            ["WaterQualityManagementPlanLandUse"] = ExtractedValueProp("Land use classification."),
            ["WaterQualityManagementPlanPriority"] = ExtractedValueProp("Priority category."),
            ["WaterQualityManagementPlanStatus"] = ExtractedValueProp("Current status."),
            ["WaterQualityManagementPlanDevelopmentType"] = ExtractedValueProp("Development type."),
            ["ApprovalDate"] = ExtractedValueProp("Approval date."),
            ["MaintenanceContactName"] = ExtractedValueProp("Maintenance contact or owner name."),
            ["MaintenanceContactOrganization"] = ExtractedValueProp("Maintenance contact or owner organization."),
            ["MaintenanceContactPhone"] = ExtractedValueProp("Maintenance contact phone."),
            ["MaintenanceContactAddress1"] = ExtractedValueProp("Address line 1."),
            ["MaintenanceContactAddress2"] = ExtractedValueProp("Address line 2."),
            ["MaintenanceContactCity"] = ExtractedValueProp("Address city."),
            ["MaintenanceContactState"] = ExtractedValueProp("Address state."),
            ["MaintenanceContactZip"] = ExtractedValueProp("Address ZIP."),
            ["WaterQualityManagementPlanPermitTerm"] = ExtractedValueProp("Permit term."),
            ["DateOfConstruction"] = ExtractedValueProp("Construction completion date."),
            ["HydrologicSubarea"] = ExtractedValueProp("Hydrologic subarea."),
            ["RecordNumber"] = ExtractedValueProp("Agency record number."),
            ["RecordedWQMPAreaInAcres"] = ExtractedValueProp("Area in acres."),
            ["TrashCaptureStatusType"] = ExtractedValueProp("Trash capture status.")
        };
        var schema = new
        {
            type = "object",
            description = "WQMP root schema (uses ExtractedValue objects).",
            properties,
            required = properties.Keys.ToArray(),
            additionalProperties = false
        };
        return JsonSerializer.Serialize(schema);
    }

    private static string BuildParcelSchemaJson()
    {
        var schema = new
        {
            type = "object",
            description = "Parcel schema (ExtractedValue objects).",
            properties = new Dictionary<string, object>
            {
                ["ParcelNumber"] = ExtractedValueProp("APN (e.g. XXX-XX-XXX or XXX-XXX-XX)")
            },
            required = new[] { "ParcelNumber" },
            additionalProperties = false
        };
        return JsonSerializer.Serialize(schema);
    }

    private static string BuildTreatmentBmpSchemaJson()
    {
        var properties = new Dictionary<string, object>
        {
            ["TreatmentBMPName"] = ExtractedValueProp("BMP name."),
            ["TreatmentBMPType"] = ExtractedValueProp("BMP type/classification."),
            ["Area"] = ExtractedValueProp("Area in acres."),
            ["LocationPointAsWellKnownText"] = ExtractedValueProp("Location WKT point."),
            ["Jurisdiction"] = ExtractedValueProp("Responsible jurisdiction."),
            ["Notes"] = ExtractedValueProp("Notes/comments."),
            ["SystemOfRecordID"] = ExtractedValueProp("External identifier."),
            ["YearBuilt"] = ExtractedValueProp("Year built."),
            ["OwnerOrganization"] = ExtractedValueProp("Owning organization."),
            ["TreatmentBMPLifespanType"] = ExtractedValueProp("Lifespan category."),
            ["TreatmentBMPLifespanEndDate"] = ExtractedValueProp("Lifespan end date."),
            ["RequiredFieldVisitsPerYear"] = ExtractedValueProp("Routine visits/year."),
            ["RequiredPostStormFieldVisitsPerYear"] = ExtractedValueProp("Post-storm visits/year."),
            ["TrashCaptureStatusType"] = ExtractedValueProp("Trash capture status."),
            ["SizingBasisType"] = ExtractedValueProp("Sizing basis."),
            ["TrashCaptureEffectiveness"] = ExtractedValueProp("Trash capture effectiveness.")
        };
        var schema = new
        {
            type = "object",
            description = "Treatment BMP schema (ExtractedValue objects).",
            properties,
            required = properties.Keys.ToArray(),
            additionalProperties = false
        };
        return JsonSerializer.Serialize(schema);
    }

    private static string BuildSourceControlBmpSchemaJson()
    {
        var properties = new Dictionary<string, object>
        {
            ["SourceControlBMPAttribute"] = ExtractedValueProp("Source control attribute name."),
            ["IsPresent"] = ExtractedValueProp("Indicates presence (Yes/No)."),
            ["SourceControlBMPNote"] = ExtractedValueProp("Attribute notes.")
        };
        var schema = new
        {
            type = "object",
            description = "Source Control BMP schema (ExtractedValue objects).",
            properties,
            required = properties.Keys.ToArray(),
            additionalProperties = false
        };
        return JsonSerializer.Serialize(schema);
    }
}
