using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Models.Messages;
using AnthropicRole = Anthropic.Models.Messages.Role;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Services.AI;

public class WqmpExtractionService
{
    private const string SchemaVersion = "v1.0";
    private static readonly Lazy<string> ExtractedValueSchema = new(BuildExtractedValueJsonSchema);
    private static readonly Lazy<string> WqmpSchema = new(BuildWqmpSchemaJson);
    private static readonly Lazy<string> ParcelSchema = new(BuildParcelSchemaJson);
    private static readonly Lazy<string> TreatmentBmpSchema = new(BuildTreatmentBmpSchemaJson);
    private static readonly Lazy<string> SourceControlBmpSchema = new(BuildSourceControlBmpSchemaJson);

    private readonly AnthropicClient _anthropic;
    private readonly AzureBlobStorageService _blobService;
    private readonly NeptuneDbContext _dbContext;
    private readonly IPromptTemplateService _promptTemplateService;
    private readonly ILogger<WqmpExtractionService> _logger;
    private readonly NeptuneConfiguration _configuration;

    public WqmpExtractionService(
        AnthropicClient anthropic,
        AzureBlobStorageService blobService,
        NeptuneDbContext dbContext,
        IPromptTemplateService promptTemplateService,
        ILogger<WqmpExtractionService> logger,
        IOptions<NeptuneConfiguration> configuration)
    {
        _anthropic = anthropic;
        _blobService = blobService;
        _dbContext = dbContext;
        _promptTemplateService = promptTemplateService;
        _logger = logger;
        _configuration = configuration.Value;
    }

    public async Task<WaterQualityManagementPlanDocumentExtractionResultDto> ExtractFromDocument(
        int waterQualityManagementPlanDocumentID, int personID, CancellationToken cancellationToken)
    {
        var totalSw = Stopwatch.StartNew();
        _logger.LogInformation("Starting WQMP extraction for documentID={DocumentID}, personID={PersonID}, model={Model}",
            waterQualityManagementPlanDocumentID, personID, _configuration.ClaudeModelId);

        var docDto = await WaterQualityManagementPlanDocuments.GetByIDAsDtoAsync(_dbContext, waterQualityManagementPlanDocumentID);
        if (docDto == null) throw new InvalidOperationException($"Document {waterQualityManagementPlanDocumentID} not found.");

        // Download PDF bytes and base64-encode for inline document blocks.
        // Claude's Messages API accepts PDFs directly as document content blocks.
        var blobStream = await _blobService.DownloadBlobFromBlobStorageAsStream(docDto.FileResource.FileResourceGUID.ToString());
        using var ms = new MemoryStream();
        await blobStream.Content.CopyToAsync(ms, cancellationToken);
        var pdfBytes = ms.ToArray();
        var pdfSizeMB = pdfBytes.Length / (1024.0 * 1024.0);
        _logger.LogInformation("PDF downloaded ({SizeMB:F1} MB). Building domain context...", pdfSizeMB);

        const int maxPdfSizeBytes = 25 * 1024 * 1024; // ~33 MB base64 ≈ safe request body
        if (pdfBytes.Length > maxPdfSizeBytes)
        {
            throw new InvalidOperationException(
                $"PDF is {pdfSizeMB:F1} MB, which exceeds the 25 MB limit for AI extraction. " +
                "Consider re-exporting the document at a lower scan resolution.");
        }

        var pdfBase64 = Convert.ToBase64String(pdfBytes);

        var domainContext = await BuildDomainContext();

        var evidenceInstructions =
            $"SchemaVersion: {SchemaVersion}. Use ONLY the provided WQMP PDF. Each attribute object MUST match ExtractedValueSchema. " +
            "Value = raw extracted string or null; ExtractionEvidence = source snippet (preceding sentence, target sentence, following sentence OR nearby table text); DocumentSource = page reference (e.g. 'Page 12'). " +
            "If not found set Value, ExtractionEvidence, DocumentSource to null. Do not add or rename properties.\n" +
            $"ExtractedValueSchema: {ExtractedValueSchema.Value}";

        // Build all 4 tools upfront — identical across all parallel calls so the tools-level cache is shared.
        var categoryConfigs = new Dictionary<string, (PromptTemplate template, string schema, bool expectArray)>
        {
            ["WQMP"] = (PromptTemplate.ExtractWqmpFields, WqmpSchema.Value, false),
            ["Parcels"] = (PromptTemplate.ExtractParcels, ParcelSchema.Value, true),
            ["TreatmentBMPs"] = (PromptTemplate.ExtractTreatmentBMPs, TreatmentBmpSchema.Value, true),
            ["SourceControlBMPs"] = (PromptTemplate.ExtractSourceControlBMPs, SourceControlBmpSchema.Value, true),
        };

        var allTools = categoryConfigs.Select(kvp => BuildToolForCategory(kvp.Key, kvp.Value.schema)).ToList();

        _logger.LogInformation("Domain context ready (elapsed {ElapsedMs}ms); invoking 4 parallel category extractions via Claude...",
            totalSw.ElapsedMilliseconds);

        // Per-category extraction — forces the category-specific tool via ToolChoice
        async Task<(string output, long inputTokens, long outputTokens, long cachedTokens)> ExtractCategoryAsync(string key, PromptTemplate template, string schema, bool expectArray)
        {
            var catSw = Stopwatch.StartNew();
            _logger.LogInformation("Starting extraction category: {Category}", key);

            var templateModel = new
            {
                EvidenceInstructions = evidenceInstructions,
                DomainContext = domainContext,
                ExtractedValueSchema = ExtractedValueSchema.Value,
                Schema = schema,
            };
            var prompt = _promptTemplateService.Render(template, templateModel);
            var toolName = $"emit_{key.ToLower()}_extraction";

            // System prompt: evidence instructions (cached — stable across all 4 calls)
            var systemBlocks = new List<TextBlockParam>
            {
                new() { Text = evidenceInstructions, CacheControl = new CacheControlEphemeral() },
            };

            // User message: PDF document (cached) + domain context (cached) + per-category prompt (varies)
            var messageContent = new List<ContentBlockParam>
            {
                new DocumentBlockParam { Source = new Base64PdfSource { Data = pdfBase64 } },
                new TextBlockParam { Text = $"DomainContext:\n{domainContext}", CacheControl = new CacheControlEphemeral() },
                new TextBlockParam { Text = prompt },
            };

            var parameters = new MessageCreateParams
            {
                Model = _configuration.ClaudeModelId,
                MaxTokens = 8192,
                System = systemBlocks,
                Messages = [new() { Role = AnthropicRole.User, Content = messageContent }],
                Tools = allTools.Select(t => (ToolUnion)t).ToList(),
                ToolChoice = new ToolChoiceTool { Name = toolName },
            };

            // Stream the response — keeps the HTTP connection alive via SSE so there's no
            // HttpClient.Timeout to worry about, even for large PDFs on a cold cache.
            var toolInputJson = new System.Text.StringBuilder();
            long cachedTokens = 0;
            long inputTokens = 0;
            long outputTokens = 0;

            await foreach (var evt in _anthropic.Messages.CreateStreaming(parameters))
            {
                if (evt.TryPickContentBlockDelta(out var delta))
                {
                    // Tool-use input arrives as input_json_delta chunks
                    if (delta.Delta.TryPickInputJson(out var jsonDelta))
                    {
                        toolInputJson.Append(jsonDelta.PartialJson);
                    }
                }
                else if (evt.TryPickDelta(out var msgDelta))
                {
                    // message_delta carries usage info
                    if (msgDelta.Usage != null)
                    {
                        outputTokens = msgDelta.Usage.OutputTokens;
                    }
                }
                else if (evt.TryPickStart(out var msgStart))
                {
                    if (msgStart.Message?.Usage != null)
                    {
                        inputTokens = msgStart.Message.Usage.InputTokens;
                        cachedTokens = msgStart.Message.Usage.CacheReadInputTokens ?? 0;
                    }
                }
            }

            var output = toolInputJson.Length > 0 ? toolInputJson.ToString() : (expectArray ? "[]" : "{}");

            if (!IsValidJson(output))
            {
                _logger.LogError("Streamed tool output returned invalid JSON for {Category} after {ElapsedMs}ms. Using empty fallback.",
                    key, catSw.ElapsedMilliseconds);
                output = expectArray ? "[]" : "{}";
            }
            else
            {
                _logger.LogInformation("Finished extraction category: {Category} in {ElapsedMs}ms ({OutputChars} chars, cached={CachedTokens})",
                    key, catSw.ElapsedMilliseconds, output.Length, cachedTokens);
            }

            return (output, inputTokens, outputTokens, cachedTokens);
        }

        var tasks = categoryConfigs.Select(kvp =>
            ExtractCategoryAsync(kvp.Key, kvp.Value.template, kvp.Value.schema, kvp.Value.expectArray)).ToList();
        var results = await Task.WhenAll(tasks);
        var keys = categoryConfigs.Keys.ToList();
        var map = new Dictionary<string, string>();
        for (var i = 0; i < keys.Count; i++)
        {
            map[keys[i]] = results[i].output;
            await LogTokenUsage(personID, results[i].inputTokens, results[i].outputTokens,
                results[i].cachedTokens, $"WQMP Extraction - {keys[i]}");
        }

        // Array categories return { "items": [...] } — unwrap to just the array
        string UnwrapItems(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("items", out var items)) return items.GetRawText();
            }
            catch { /* fall through */ }
            return json;
        }

        var finalOutput = $"{{ \"SchemaVersion\": \"{SchemaVersion}\", \"WQMP\": {map["WQMP"]}, \"Parcels\": {UnwrapItems(map["Parcels"])}, \"TreatmentBMPs\": {UnwrapItems(map["TreatmentBMPs"])}, \"SourceControlBMPs\": {UnwrapItems(map["SourceControlBMPs"])} }}";

        if (!IsValidJson(finalOutput))
        {
            _logger.LogError("Final consolidated JSON is invalid. Using raw parts.");
        }

        _logger.LogInformation("Completed WQMP extraction for documentID={DocumentID} in {ElapsedMs}ms",
            waterQualityManagementPlanDocumentID, totalSw.ElapsedMilliseconds);

        return new WaterQualityManagementPlanDocumentExtractionResultDto
        {
            FinalOutput = finalOutput,
            RawResults = string.Join("\n", map.Select(kvp => $"{kvp.Key}: {kvp.Value}")),
            ExtractedAt = DateTime.UtcNow
        };
    }

    private async Task LogTokenUsage(int personID, long inputTokens, long outputTokens, long cachedTokens, string context)
    {
        try
        {
            _dbContext.AITokenUsages.Add(new AITokenUsage
            {
                PersonID = personID,
                Model = _configuration.ClaudeModelId,
                InputTokens = (int)inputTokens,
                CachedInputTokens = (int)cachedTokens,
                OutputTokens = (int)outputTokens,
                RequestDate = DateTime.UtcNow,
                RequestContext = context
            });
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to log token usage for {Context}", context);
        }
    }

    private static Tool BuildToolForCategory(string categoryKey, string jsonSchema)
    {
        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonSchema);
        return new Tool
        {
            Name = $"emit_{categoryKey.ToLower()}_extraction",
            Description = $"Emit the extracted {categoryKey} fields as structured JSON matching the required schema.",
            InputSchema = new()
            {
                Properties = parsed?.Where(kvp => kvp.Key == "properties")
                    .SelectMany(kvp => kvp.Value.EnumerateObject())
                    .ToDictionary(prop => prop.Name, prop => JsonSerializer.SerializeToElement(JsonSerializer.Deserialize<object>(prop.Value.GetRawText())))
                    ?? new Dictionary<string, JsonElement>(),
                Required = parsed != null && parsed.TryGetValue("required", out var req)
                    ? JsonSerializer.Deserialize<List<string>>(req.GetRawText()) ?? []
                    : [],
            },
        };
    }

    public async Task<string> BuildDomainContext()
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

    private static bool IsValidJson(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return false;
        try { using var _ = JsonDocument.Parse(candidate); return true; } catch { return false; }
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

    // Schema builders — unchanged from OpenAI implementation (same JSON shape)

    private static string BuildExtractedValueJsonSchema()
    {
        var schema = new
        {
            type = "object",
            description = "ExtractedValue schema. Attribute with evidence.",
            properties = new
            {
                Value = new { type = new[] { "string", "null" }, description = "Raw extracted value or null." },
                ExtractionEvidence = new { type = new[] { "string", "null" }, description = "Snippet: preceding, target, following sentence OR nearby table text." },
                DocumentSource = new { type = new[] { "string", "null" }, description = "Page reference (e.g. 'Page 12')." }
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
            ["TrashCaptureStatusType"] = ExtractedValueProp("Trash capture status."),
            ["HydromodificationAppliesType"] = ExtractedValueProp("Hydromodification applies status.")
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

    private static string WrapAsArraySchema(string description, object itemSchema)
    {
        var schema = new
        {
            type = "object",
            description,
            properties = new Dictionary<string, object>
            {
                ["items"] = new { type = "array", items = itemSchema }
            },
            required = new[] { "items" },
            additionalProperties = false
        };
        return JsonSerializer.Serialize(schema);
    }

    private static object BuildParcelItemSchema()
    {
        var properties = new Dictionary<string, object>
        {
            ["ParcelNumber"] = ExtractedValueProp("APN (e.g. XXX-XX-XXX or XXX-XXX-XX)")
        };
        return new
        {
            type = "object",
            properties,
            required = properties.Keys.ToArray(),
            additionalProperties = false
        };
    }

    private static string BuildParcelSchemaJson() =>
        WrapAsArraySchema("Array of parcels (ExtractedValue objects).", BuildParcelItemSchema());

    private static object BuildTreatmentBmpItemSchema()
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
        return new
        {
            type = "object",
            properties,
            required = properties.Keys.ToArray(),
            additionalProperties = false
        };
    }

    private static string BuildTreatmentBmpSchemaJson() =>
        WrapAsArraySchema("Array of treatment BMPs (ExtractedValue objects).", BuildTreatmentBmpItemSchema());

    private static object BuildSourceControlBmpItemSchema()
    {
        var properties = new Dictionary<string, object>
        {
            ["SourceControlBMPAttribute"] = ExtractedValueProp("Source control attribute name."),
            ["IsPresent"] = ExtractedValueProp("Indicates presence (Yes/No)."),
            ["SourceControlBMPNote"] = ExtractedValueProp("Attribute notes.")
        };
        return new
        {
            type = "object",
            properties,
            required = properties.Keys.ToArray(),
            additionalProperties = false
        };
    }

    private static string BuildSourceControlBmpSchemaJson() =>
        WrapAsArraySchema("Array of source control BMPs (ExtractedValue objects).", BuildSourceControlBmpItemSchema());
}
