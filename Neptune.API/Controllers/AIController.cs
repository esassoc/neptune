using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anthropic;
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

        // NPT-1044: reference the PDF by Anthropic file_id (Files API). Same shared
        // upload-and-cache helper used by extraction — first chat on a never-extracted
        // document uploads on demand; subsequent calls reuse the cached id. Lifts the
        // 32 MB ceiling that the URL-source path enforced.
        var fileID = await anthropicFileService.EnsureUploadedFileIDAsync(
            waterQualityManagementPlanDocumentID, HttpContext.RequestAborted);

        var domainContext = await wqmpExtractionService.BuildDomainContext();

        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("X-Accel-Buffering", "no");

        // Build messages from the chat history
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
                            new BetaRequestDocumentBlock { Source = new BetaFileDocumentSource { FileID = fileID } },
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

        var parameters = new MessageCreateParams
        {
            Model = appConfiguration.Value.ClaudeModelId,
            MaxTokens = 4096,
            // Required for BetaFileDocumentSource — see WqmpExtractionService for the
            // full reasoning. Without this header the API rejects the file-source variant.
            Betas = ["files-api-2025-04-14"],
            Messages = messages,
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
        await Response.WriteAsync("data: ---MessageCompleted---\n\n");
    }
}
