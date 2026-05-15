using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using BetaRole = Anthropic.Models.Beta.Messages.Role;
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
    private static readonly Lazy<string> QuickBmpSchema = new(BuildQuickBmpSchemaJson);
    private static readonly Lazy<string> SourceControlBmpSchema = new(BuildSourceControlBmpSchemaJson);

    private readonly AnthropicClient _anthropic;
    private readonly AnthropicFileService _anthropicFileService;
    private readonly NeptuneDbContext _dbContext;
    private readonly IPromptTemplateService _promptTemplateService;
    private readonly ILogger<WqmpExtractionService> _logger;
    private readonly NeptuneConfiguration _configuration;

    public WqmpExtractionService(
        AnthropicClient anthropic,
        AnthropicFileService anthropicFileService,
        NeptuneDbContext dbContext,
        IPromptTemplateService promptTemplateService,
        ILogger<WqmpExtractionService> logger,
        IOptions<NeptuneConfiguration> configuration)
    {
        _anthropic = anthropic;
        _anthropicFileService = anthropicFileService;
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

        // NPT-1044: upload to Anthropic Files API once and cache the file_id on the document.
        // The Files-API source path on the Beta Messages API supports up to 500 MB per file
        // — vs. 32 MB on the URL-source path we used previously — and dodges the SAS-URL
        // expiry race we used to see on long-running extractions.
        var fileID = await _anthropicFileService.EnsureUploadedFileIDAsync(waterQualityManagementPlanDocumentID, cancellationToken);
        _logger.LogInformation("Anthropic file ID resolved for documentID={DocumentID}: {FileID}. Building domain context...",
            waterQualityManagementPlanDocumentID, fileID);

        var domainContext = await BuildDomainContext();

        var evidenceInstructions =
            $"SchemaVersion: {SchemaVersion}. Use ONLY the provided WQMP PDF. Each attribute object MUST match ExtractedValueSchema. " +
            "Value = raw extracted string or null; ExtractionEvidence = source snippet (preceding sentence, target sentence, following sentence OR nearby table text); DocumentSource = page reference (e.g. 'Page 12'). " +
            "BoundingBox = {PageNumber, X, Y, Width, Height} locating the evidence on the page. X/Y/Width/Height are 0-1 fractions of page size where (0,0) is top-left and (1,1) is bottom-right. " +
            "ALWAYS emit BoundingBox whenever Value is not null. The rectangle should tightly cover the ACTUAL TEXT of ExtractionEvidence — measure from the top of the character baselines to the bottom, and from the first character to the last. Do NOT center the box on surrounding whitespace, adjacent paragraphs, or 'the general area'; point directly at the text characters themselves. " +
            "For scanned/rasterized pages, look at the page image and estimate from the ink positions. Use a typical line height of ~0.02–0.04 (2–4% of page height) for a single-line field value; taller for multi-line evidence. " +
            "Only set BoundingBox to null if Value is also null (field not found in the document). " +
            "If not found, set Value, ExtractionEvidence, DocumentSource, BoundingBox to null. Do not add or rename properties.\n" +
            $"ExtractedValueSchema: {ExtractedValueSchema.Value}";

        // Build all 4 tools upfront — identical across all parallel calls so the tools-level cache is shared.
        var categoryConfigs = new Dictionary<string, (PromptTemplate template, string schema, bool expectArray)>
        {
            ["WQMP"] = (PromptTemplate.ExtractWqmpFields, WqmpSchema.Value, false),
            ["Parcels"] = (PromptTemplate.ExtractParcels, ParcelSchema.Value, true),
            ["QuickBMPs"] = (PromptTemplate.ExtractQuickBMPs, QuickBmpSchema.Value, true),
            ["SourceControlBMPs"] = (PromptTemplate.ExtractSourceControlBMPs, SourceControlBmpSchema.Value, true),
        };

        var allTools = categoryConfigs.Select(kvp => BuildToolForCategory(kvp.Key, kvp.Value.schema)).ToList();

        _logger.LogInformation("Domain context ready (elapsed {ElapsedMs}ms); invoking 4 parallel category extractions via Claude...",
            totalSw.ElapsedMilliseconds);

        // Per-category extraction — forces the category-specific tool via ToolChoice.
        // A 4-minute per-category timeout prevents a single stalled call from hanging the whole extraction.
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
            var systemBlocks = new List<BetaTextBlockParam>
            {
                new() { Text = evidenceInstructions, CacheControl = new BetaCacheControlEphemeral() },
            };

            // Single attempt: build params bound to a specific file_id and stream the response.
            // Factored out so the outer retry can rebuild the message with a refreshed id
            // when Anthropic 404s on a stale cached file_id.
            async Task<(string output, long inputTokens, long outputTokens, long cachedTokens)> AttemptAsync(string attemptFileID)
            {
                using var categoryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                categoryCts.CancelAfter(TimeSpan.FromMinutes(4));

                // User message: PDF document (referenced by file_id, cached) + domain context (cached) + per-category prompt (varies)
                var messageContent = new List<BetaContentBlockParam>
                {
                    new BetaRequestDocumentBlock { Source = new BetaFileDocumentSource { FileID = attemptFileID } },
                    new BetaTextBlockParam { Text = $"DomainContext:\n{domainContext}", CacheControl = new BetaCacheControlEphemeral() },
                    new BetaTextBlockParam { Text = prompt },
                };

                var parameters = new MessageCreateParams
                {
                    Model = _configuration.ClaudeModelId,
                    // NPT-1054: bumped from 8192 → 16384. Sonnet 4.5+ uses interleaved thinking
                    // by default when tool_use is in play; thinking tokens count toward the same
                    // output budget. On the SourceControlBMPs call with a longer prompt + a
                    // 65-page PDF, Claude consumed all 8192 tokens on thinking and never reached
                    // the tool call (stop_reason=max_tokens, no text or input_json captured).
                    MaxTokens = 16384,
                    // The Files-API source variant (BetaFileDocumentSource) is gated behind
                    // this beta header on the Messages endpoint. Without it the request gets
                    // routed to the standard validator and rejected with
                    // "Input tag 'file' found using 'type' does not match any of the
                    // expected tags: 'base64', 'content', 'text', 'url'".
                    Betas = ["files-api-2025-04-14"],
                    System = systemBlocks,
                    Messages = [new() { Role = BetaRole.User, Content = messageContent }],
                    Tools = allTools.Select(t => (BetaToolUnion)t).ToList(),
                    ToolChoice = new BetaToolChoiceTool { Name = toolName },
                };

                // Stream the response — keeps the HTTP connection alive via SSE so there's no
                // HttpClient.Timeout to worry about, even for large PDFs on a cold cache.
                var toolInputJson = new System.Text.StringBuilder();
                long cachedTokens = 0;
                long inputTokens = 0;
                long outputTokens = 0;

                await foreach (var evt in _anthropic.Beta.Messages.CreateStreaming(parameters, categoryCts.Token))
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

            // Retry once on a stale-file_id 404 — invalidate the cached id and re-upload
            // (serialized across the 4 parallel calls inside RefreshFileIDAsync), then retry.
            try
            {
                return await AttemptAsync(fileID);
            }
            catch (AnthropicNotFoundException ex)
            {
                _logger.LogWarning(ex, "Anthropic 404 on {Category} for documentID={DocumentID} (likely stale file_id); refreshing and retrying once.",
                    key, waterQualityManagementPlanDocumentID);
                var refreshedFileID = await _anthropicFileService.RefreshFileIDAsync(
                    waterQualityManagementPlanDocumentID, fileID, cancellationToken);
                return await AttemptAsync(refreshedFileID);
            }
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

        // Array categories return { "items": [...] } — unwrap to just the array.
        // NPT-1054: Claude sometimes JSON-encodes `items` as a string instead of emitting a real
        // array (observed on the SourceControlBMPs call with longer prompts + multi-line array
        // examples). Detect that shape and parse it back to an array before consolidating;
        // otherwise the downstream JSON_QUERY consumer sees a string literal where an array
        // should be.
        string UnwrapItems(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("items", out var items))
                {
                    if (items.ValueKind == JsonValueKind.String)
                    {
                        var innerJson = items.GetString();
                        if (string.IsNullOrEmpty(innerJson))
                        {
                            _logger.LogWarning("Tool output `items` was an empty string — emitting empty array.");
                            return "[]";
                        }
                        try
                        {
                            using var innerDoc = JsonDocument.Parse(innerJson);
                            _logger.LogWarning("Tool output had `items` as a JSON-encoded string ({Len} chars) — unwrapped successfully.", innerJson.Length);
                            return innerJson;
                        }
                        catch (JsonException ex)
                        {
                            // The string content failed to parse as JSON. Try a best-effort cleanup:
                            // strip a stray trailing comma + newline before the closing bracket,
                            // which is a common Claude foible on long array outputs.
                            var cleaned = System.Text.RegularExpressions.Regex.Replace(
                                innerJson, @",\s*([\]\}])", "$1");
                            try
                            {
                                using var cleanedDoc = JsonDocument.Parse(cleaned);
                                _logger.LogWarning("Tool output `items` was a JSON-encoded string with trailing-comma issues — repaired and unwrapped ({Len} chars).", cleaned.Length);
                                return cleaned;
                            }
                            catch (JsonException ex2)
                            {
                                _logger.LogError(ex2, "Tool output `items` was a JSON-encoded string but failed to parse even after cleanup. First error: {FirstErr}. Cleanup error: {CleanupErr}. Inner head: {Head}",
                                    ex.Message, ex2.Message, innerJson.Length > 500 ? innerJson.Substring(0, 500) : innerJson);
                                return "[]";
                            }
                        }
                    }
                    return items.GetRawText();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UnwrapItems failed to parse tool output JSON; using raw output.");
            }
            return json;
        }

        var finalOutput = $"{{ \"SchemaVersion\": \"{SchemaVersion}\", \"WQMP\": {map["WQMP"]}, \"Parcels\": {UnwrapItems(map["Parcels"])}, \"QuickBMPs\": {UnwrapItems(map["QuickBMPs"])}, \"SourceControlBMPs\": {UnwrapItems(map["SourceControlBMPs"])} }}";

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

    private static BetaTool BuildToolForCategory(string categoryKey, string jsonSchema)
    {
        var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonSchema);
        return new BetaTool
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
            // NPT-1054: send SC attribute names grouped by their category so Claude has the
            // three-bucket taxonomy when matching extracted attribute wording. The v3 prompt
            // explicitly enumerates the three categories with examples — the categorized
            // DomainContext lets Claude resolve fuzzy PDF wording against the right bucket.
            SourceControlBMPAttributes = (await _dbContext.SourceControlBMPAttributes
                .AsNoTracking()
                .Select(x => new { x.SourceControlBMPAttributeName, x.SourceControlBMPAttributeCategoryID })
                .ToListAsync())
                .GroupBy(x => x.SourceControlBMPAttributeCategoryID)
                .Select(g => new
                {
                    Category = SourceControlBMPAttributeCategory.AllLookupDictionary[g.Key].SourceControlBMPAttributeCategoryName,
                    Attributes = g.Select(a => a.SourceControlBMPAttributeName).OrderBy(n => n).ToList(),
                })
                .OrderBy(g => g.Category)
                .ToList(),
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
            DocumentSource = new { type = new[] { "string", "null" }, description = "Page reference or null." },
            BoundingBox = BoundingBoxProp()
        },
        required = new[] { "Value", "ExtractionEvidence", "DocumentSource", "BoundingBox" },
        additionalProperties = false
    };

    // Spatial hint for the evidence region on the page. Normalized 0-1 fractions relative
    // to the page's own width/height, top-left origin. Claude should emit this whenever
    // Value is non-null — a rough estimate is more useful than null. Only null when the
    // field wasn't found in the document.
    private static object BoundingBoxProp() => new
    {
        type = new[] { "object", "null" },
        description = "Required {PageNumber, X, Y, Width, Height} locating the evidence on its page. X/Y/Width/Height are 0-1 fractions of page size (top-left origin). Null only when Value is null.",
        properties = new
        {
            PageNumber = new { type = "integer", description = "1-based page index." },
            X = new { type = "number", description = "Left edge, fraction of page width." },
            Y = new { type = "number", description = "Top edge, fraction of page height." },
            Width = new { type = "number", description = "Width, fraction of page width." },
            Height = new { type = "number", description = "Height, fraction of page height." }
        },
        required = new[] { "PageNumber", "X", "Y", "Width", "Height" },
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
                DocumentSource = new { type = new[] { "string", "null" }, description = "Page reference (e.g. 'Page 12')." },
                BoundingBox = BoundingBoxProp()
            },
            required = new[] { "Value", "ExtractionEvidence", "DocumentSource", "BoundingBox" },
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

    // Targets QuickBMP (Simplified Structural BMP) records, which are jurisdiction-scoped
    // to a single WQMP — distinct from the global TreatmentBMP inventory. Field names match
    // QuickBMPUpsertDto so the approve endpoint can drop accepted entries straight into the
    // Merge call. Extraction of the global TreatmentBMP inventory is a future story.
    private static object BuildQuickBmpItemSchema()
    {
        var properties = new Dictionary<string, object>
        {
            ["QuickBMPName"] = ExtractedValueProp("Simple BMP name as written in the document."),
            ["TreatmentBMPType"] = ExtractedValueProp("BMP type/classification name; should match a TreatmentBMPType from DomainContext when possible."),
            ["NumberOfIndividualBMPs"] = ExtractedValueProp("Count of individual physical BMP units this row represents (default 1 if not stated)."),
            ["PercentOfSiteTreated"] = ExtractedValueProp("% of the WQMP site this BMP treats (0-100)."),
            ["PercentCaptured"] = ExtractedValueProp("% of design storm captured by this BMP (0-100)."),
            ["PercentRetained"] = ExtractedValueProp("% of design storm retained on-site (0-100; must be <= PercentCaptured)."),
            ["QuickBMPNote"] = ExtractedValueProp("Free-form note about this BMP (≤200 chars).")
        };
        return new
        {
            type = "object",
            properties,
            required = properties.Keys.ToArray(),
            additionalProperties = false
        };
    }

    private static string BuildQuickBmpSchemaJson() =>
        WrapAsArraySchema("Array of QuickBMPs (Simplified Structural BMPs, ExtractedValue objects).", BuildQuickBmpItemSchema());

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
