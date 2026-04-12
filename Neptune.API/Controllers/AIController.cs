using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis; // Added for Experimental attribute
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.AI;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using OpenAI;
using OpenAI.Files;
using OpenAI.Responses;

namespace Neptune.API.Controllers;

[ApiController]
[Route("ai")]
public class AIController(
    NeptuneDbContext dbContext,
    ILogger<AIController> logger,
    IOptions<NeptuneConfiguration> appConfiguration,
    AzureBlobStorageService azureBlobStorageService,
    OpenAIClient openAIClient,
    WqmpExtractionService wqmpExtractionService)
    : SitkaController<AIController>(dbContext, logger, appConfiguration)
{
#pragma warning disable OPENAI001 // Suppress experimental OpenAI SDK warnings for evaluation usage

    private static readonly FileUploadPurpose VectorStoreFileUploadPurpose = FileUploadPurpose.UserData;

    [HttpPost("water-quality-management-plan-documents/{waterQualityManagementPlanDocumentID}/extract-all")]
    [AdminFeature]
    [Experimental("OPENAI001")]
    public async Task<ActionResult<WaterQualityManagementPlanDocumentExtractionResultDto>> ExtractAll([FromRoute] int waterQualityManagementPlanDocumentID)
    {
        var docDto = await WaterQualityManagementPlanDocuments.GetByIDAsDtoAsync(DbContext, waterQualityManagementPlanDocumentID);
        if (docDto == null) return NotFound();

        var extractionResult = await wqmpExtractionService.ExtractFromDocument(
            waterQualityManagementPlanDocumentID, CallingUser.PersonID, HttpContext.RequestAborted);
        return Ok(extractionResult);
    }

    private const string SchemaVersion = "v1.0";

    private async Task<string> EnsureVectorStoreWithFileAsync(int documentId, WaterQualityManagementPlanDocumentDto docDto)
    {
        var existingVectorStoreId = await WaterQualityManagementPlanDocumentVectorStores.GetByWaterQualityManagementPlanDocumentIDAsDtoAsync(DbContext, documentId);
        if (!string.IsNullOrWhiteSpace(existingVectorStoreId)) return existingVectorStoreId;
        var vectorStoreClient = openAIClient.GetVectorStoreClient();
        var fileClient = openAIClient.GetOpenAIFileClient();
        var fileStream = await azureBlobStorageService.DownloadBlobFromBlobStorageAsStream(docDto.FileResource.FileResourceGUID.ToString());
        var fileUploadResult = await fileClient.UploadFileAsync(fileStream.Content, docDto.FileResource.OriginalFilename, VectorStoreFileUploadPurpose);
        if (fileUploadResult?.Value == null) throw new Exception($"OpenAI file upload failed for documentId={documentId}");
        var options = new OpenAI.VectorStores.VectorStoreCreationOptions { Name = $"WQMP_{documentId}" };
        options.FileIds.Add(fileUploadResult.Value.Id);
        var vectorStoreCreateResult = await vectorStoreClient.CreateVectorStoreAsync(options);
        if (vectorStoreCreateResult?.Value == null) throw new Exception($"OpenAI vector store creation failed for documentId={documentId}");
        var vectorStoreId = vectorStoreCreateResult.Value.Id;
        await WaterQualityManagementPlanDocumentVectorStores.UpsertAsync(DbContext, documentId, vectorStoreId);
        return vectorStoreId;
    }

    private async Task<string> BuildSchemaAndDomainContext()
    {
        var domainTables = new
        {
            Jurisdictions = await DbContext.StormwaterJurisdictions.Include(x => x.Organization).Select(x => x.Organization.OrganizationName).AsNoTracking().ToListAsync(),
            TreatmentBMPTypes = await DbContext.TreatmentBMPTypes.Select(x => x.TreatmentBMPTypeName).AsNoTracking().ToListAsync(),
            HydrologicSubareas = await DbContext.HydrologicSubareas.Select(x => x.HydrologicSubareaName).AsNoTracking().ToListAsync(),
            WaterQualityManagementPlanLandUse = WaterQualityManagementPlanLandUse.All.Select(x => x.WaterQualityManagementPlanLandUseDisplayName),
            WaterQualityManagementPlanPriority = WaterQualityManagementPlanPriority.All.Select(x => x.WaterQualityManagementPlanPriorityDisplayName),
            WaterQualityManagementPlanStatus = WaterQualityManagementPlanStatus.All.Select(x => x.WaterQualityManagementPlanStatusDisplayName),
            WaterQualityManagementPlanDevelopmentType = WaterQualityManagementPlanDevelopmentType.All.Select(x => x.WaterQualityManagementPlanDevelopmentTypeDisplayName),
            WaterQualityManagementPlanPermitTerm = WaterQualityManagementPlanPermitTerm.All.Select(x => x.WaterQualityManagementPlanPermitTermDisplayName),
            WaterQualityManagementPlanModelingApproach = WaterQualityManagementPlanModelingApproach.All.Select(x => x.WaterQualityManagementPlanModelingApproachDisplayName),
            TrashCaptureStatusType = TrashCaptureStatusType.All.Select(x => x.TrashCaptureStatusTypeDisplayName),
            TreatmentBMPLifespanType = TreatmentBMPLifespanType.All.Select(x => x.TreatmentBMPLifespanTypeDisplayName),
            SizingBasisType = SizingBasisType.All.Select(x => x.SizingBasisTypeDisplayName),
            DryWeatherFlowOverride = DryWeatherFlowOverride.All.Select(x => x.DryWeatherFlowOverrideDisplayName),
            SourceControlBMPAttributes = await DbContext.SourceControlBMPAttributes.AsNoTracking().Select(x => x.SourceControlBMPAttributeName).ToListAsync(),
        };
        var domainTablesJson = JsonSerializer.Serialize(domainTables);
        return $"SCHEMA_VERSION: {SchemaVersion}\nDOMAIN TABLES: {domainTablesJson}\n";
    }

    [Experimental("OPENAI001")]
    private OpenAIResponseClient CreateResponseClient() => new("gpt-4.1", apiKey: appConfiguration.Value.OpenAIApiKey);

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

    [HttpPost("water-quality-management-plan-documents/{waterQualityManagementPlanDocumentID}/ask")]
    [AdminFeature]
    [Experimental("OPENAI001")]
    public async Task Ask([FromRoute] int waterQualityManagementPlanDocumentID, [FromBody] ChatRequestDto chatRequestDto)
    {
        var docDto = await WaterQualityManagementPlanDocuments.GetByIDAsDtoAsync(DbContext, waterQualityManagementPlanDocumentID);
        if (docDto == null) { Response.StatusCode = 404; return; }
        var vectorStoreId = await EnsureVectorStoreWithFileAsync(waterQualityManagementPlanDocumentID, docDto);
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("X-Accel-Buffering", "no");
        var domainContext = await BuildSchemaAndDomainContext();
        var client = CreateResponseClient();
        var tool = CreateFileSearchTool(vectorStoreId);
        var userPrompt = domainContext + "\n\n" + string.Join("\n\n", chatRequestDto.Messages.Select(m => m.Content));
        var response = await client.CreateResponseAsync(userPrompt, new ResponseCreationOptions { Tools = { tool } });
        var outputText = ExtractMessageText(response.Value.OutputItems).Replace("\n", "<br>").Replace("\r", "");
        if (!string.IsNullOrWhiteSpace(outputText)) await Response.WriteAsync($"data: {outputText}\n\n");
        await Response.WriteAsync("data: ---MessageCompleted---\n\n");
    }

    [HttpPost("clean-up")]
    [AdminFeature]
    [Experimental("OPENAI001")]
    public async Task PostChatCompletions()
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("X-Accel-Buffering", "no");
        var fileClient = openAIClient.GetOpenAIFileClient();
        var vectorStoreClient = openAIClient.GetVectorStoreClient();
        var assistantClient = openAIClient.GetAssistantClient();
        var assistants = assistantClient.GetAssistantsAsync();
        await Response.WriteAsync("data: ---STARTING CLEANUP---\n\n");
        await foreach (var assistant in assistants)
        {
            await Response.WriteAsync($"data: DELETING ASSISTANT {assistant.Id}\n\n");
            await assistantClient.DeleteAssistantAsync(assistant.Id);
        }
        var files = await fileClient.GetFilesAsync();
        foreach (var file in files.Value)
        {
            await Response.WriteAsync($"data: DELETING FILE {file.Id}\n\n");
            await fileClient.DeleteFileAsync(file.Id);
        }
        var vectorStores = vectorStoreClient.GetVectorStoresAsync();
        await foreach (var store in vectorStores)
        {
            await Response.WriteAsync($"data: DELETING VECTORSTORE {store.Id}\n\n");
            await vectorStoreClient.DeleteVectorStoreAsync(store.Id);
        }
        await Response.WriteAsync("data: ---DONE---\n\n");
    }

    [HttpGet("vector-stores")]
    [AdminFeature]
    [Experimental("OPENAI001")]
    public async Task<ActionResult<List<object>>> GetVectorStores()
    {
        var client = openAIClient.GetVectorStoreClient();
        var vectorStores = new List<object>();
        await foreach (var store in client.GetVectorStoresAsync())
            vectorStores.Add(new { store.Id, store.Name, store.FileCounts, store.UsageBytes, store.CreatedAt, store.Status });
        return Ok(vectorStores);
    }

#pragma warning restore OPENAI001
}
