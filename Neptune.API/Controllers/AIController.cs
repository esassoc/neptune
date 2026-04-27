using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Exceptions;
using Anthropic.Models.Beta.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BetaRole = Anthropic.Models.Beta.Messages.Role;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.AI;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("ai")]
public class AIController(
    NeptuneDbContext dbContext,
    ILogger<AIController> logger,
    IOptions<NeptuneConfiguration> appConfiguration,
    AnthropicClient anthropicClient,
    AnthropicFileService anthropicFileService,
    WqmpExtractionService wqmpExtractionService)
    : SitkaController<AIController>(dbContext, logger, appConfiguration)
{
    [HttpPost("water-quality-management-plan-documents/{waterQualityManagementPlanDocumentID}/extract-all")]
    [AdminFeature]
    public async Task<ActionResult<WaterQualityManagementPlanDocumentExtractionResultDto>> ExtractAll([FromRoute] int waterQualityManagementPlanDocumentID)
    {
        var docDto = await WaterQualityManagementPlanDocuments.GetByIDAsDtoAsync(DbContext, waterQualityManagementPlanDocumentID);
        if (docDto == null) return NotFound();

        var extractionResult = await wqmpExtractionService.ExtractFromDocument(
            waterQualityManagementPlanDocumentID, CallingUser.PersonID, HttpContext.RequestAborted);
        return Ok(extractionResult);
    }

    [HttpPost("water-quality-management-plan-documents/{waterQualityManagementPlanDocumentID}/ask")]
    [AdminFeature]
    public async Task Ask([FromRoute] int waterQualityManagementPlanDocumentID, [FromBody] ChatRequestDto chatRequestDto)
    {
        var docDto = await WaterQualityManagementPlanDocuments.GetByIDAsDtoAsync(DbContext, waterQualityManagementPlanDocumentID);
        if (docDto == null) { Response.StatusCode = 404; return; }

        // Proactive size check — mirrors the upload + extract endpoints so chat fails
        // fast with a friendly 400 instead of pulling the blob into memory just to be
        // rejected at Anthropic. AnthropicFileService also enforces this defensively.
        var maxBytes = appConfiguration.Value.MaxExtractablePdfSizeBytes;
        if (docDto.FileResource.ContentLength > maxBytes)
        {
            var sizeMB = docDto.FileResource.ContentLength / (1024.0 * 1024.0);
            var maxMB = maxBytes / (1024.0 * 1024.0);
            Response.StatusCode = 400;
            await Response.WriteAsJsonAsync(new
            {
                message = $"This PDF is {sizeMB:F0} MB. AI chat supports PDFs up to {maxMB:F0} MB — please re-upload a smaller version (re-export or re-scan at a lower resolution, recommended: 150 DPI for scanned pages)."
            });
            return;
        }

        // NPT-1044: reference the PDF by Anthropic file_id (Files API). Same shared
        // upload-and-cache helper used by extraction — first chat on a never-extracted
        // document uploads on demand; subsequent calls reuse the cached id. Lifts the
        // 32 MB ceiling that the URL-source path enforced.
        var fileID = await anthropicFileService.EnsureUploadedFileIDAsync(
            waterQualityManagementPlanDocumentID, HttpContext.RequestAborted);

        var domainContext = await wqmpExtractionService.BuildDomainContext();

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("X-Accel-Buffering", "no");

        // Builds the per-attempt messages list bound to a specific file_id. Pulled out
        // so the stale-file_id retry below can rebuild it with a fresh id without
        // duplicating the chat-history mapping.
        List<BetaMessageParam> BuildMessages(string attemptFileID)
        {
            var messages = new List<BetaMessageParam>();
            foreach (var msg in chatRequestDto.Messages)
            {
                if (msg.Role?.Equals("assistant", StringComparison.OrdinalIgnoreCase) == true)
                {
                    messages.Add(new() { Role = BetaRole.Assistant, Content = msg.Content });
                }
                else
                {
                    // First user message includes the PDF; subsequent user messages are plain text
                    if (messages.Count == 0)
                    {
                        messages.Add(new()
                        {
                            Role = BetaRole.User,
                            Content = new List<BetaContentBlockParam>
                            {
                                new BetaRequestDocumentBlock { Source = new BetaFileDocumentSource { FileID = attemptFileID } },
                                new BetaTextBlockParam { Text = domainContext, CacheControl = new BetaCacheControlEphemeral() },
                                new BetaTextBlockParam { Text = msg.Content },
                            },
                        });
                    }
                    else
                    {
                        messages.Add(new() { Role = BetaRole.User, Content = msg.Content });
                    }
                }
            }
            return messages;
        }

        async Task StreamAsync(string attemptFileID)
        {
            var parameters = new MessageCreateParams
            {
                Model = appConfiguration.Value.ClaudeModelId,
                MaxTokens = 4096,
                // Required for BetaFileDocumentSource — see WqmpExtractionService for the
                // full reasoning. Without this header the API rejects the file-source variant.
                Betas = ["files-api-2025-04-14"],
                Messages = BuildMessages(attemptFileID),
            };

            // Stream the response via SSE — same format the frontend expects.
            // Observing RequestAborted lets Claude streaming stop as soon as the client disconnects.
            await foreach (var streamEvent in anthropicClient.Beta.Messages.CreateStreaming(parameters, HttpContext.RequestAborted))
            {
                if (streamEvent.TryPickContentBlockDelta(out var delta) &&
                    delta.Delta.TryPickText(out var text))
                {
                    var chunk = text.Text.Replace("\n", "<br>").Replace("\r", "");
                    await Response.WriteAsync($"data: {chunk}\n\n");
                    await Response.Body.FlushAsync();
                }
            }
        }

        // Retry once on a stale-file_id 404, but only if we haven't already started
        // streaming bytes back to the client — once SSE writes have flushed, the response
        // is committed and we can't restart it cleanly.
        try
        {
            await StreamAsync(fileID);
        }
        catch (AnthropicNotFoundException ex) when (!Response.HasStarted)
        {
            Logger.LogWarning(ex, "Anthropic 404 on chat for documentID={DocumentID} (likely stale file_id); refreshing and retrying once.",
                waterQualityManagementPlanDocumentID);
            var refreshedFileID = await anthropicFileService.RefreshFileIDAsync(
                waterQualityManagementPlanDocumentID, fileID, HttpContext.RequestAborted);
            await StreamAsync(refreshedFileID);
        }

        await Response.WriteAsync("data: ---MessageCompleted---\n\n");
    }
}
