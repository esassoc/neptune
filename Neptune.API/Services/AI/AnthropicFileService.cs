using System;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Models.Beta.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neptune.EFModels.Entities;

namespace Neptune.API.Services.AI;

/// <summary>
/// Uploads PDFs to Anthropic's Files API and caches the resulting <c>file_id</c> on the
/// owning <see cref="WaterQualityManagementPlanDocument"/> so subsequent extraction and
/// chat calls can reference the same uploaded document without re-uploading.
///
/// The Files-API source path on the Beta Messages API has a 500 MB ceiling per Anthropic
/// (vs. 32 MB on the URL-source path we used previously), which lifts the constraint that
/// rejected ~50% of real-world scanned WQMPs. NPT-1044.
/// </summary>
public class AnthropicFileService
{
    private readonly AnthropicClient _anthropic;
    private readonly AzureBlobStorageService _blobService;
    private readonly NeptuneDbContext _dbContext;
    private readonly ILogger<AnthropicFileService> _logger;

    public AnthropicFileService(
        AnthropicClient anthropic,
        AzureBlobStorageService blobService,
        NeptuneDbContext dbContext,
        ILogger<AnthropicFileService> logger)
    {
        _anthropic = anthropic;
        _blobService = blobService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Returns the cached Anthropic <c>file_id</c> for a document, uploading the PDF
    /// once if not already cached. Idempotent — repeated calls for the same document
    /// return the same id without re-uploading.
    /// </summary>
    public async Task<string> EnsureUploadedFileIDAsync(
        int waterQualityManagementPlanDocumentID, CancellationToken cancellationToken)
    {
        var document = await _dbContext.WaterQualityManagementPlanDocuments
            .Include(x => x.FileResource)
            .SingleAsync(x => x.WaterQualityManagementPlanDocumentID == waterQualityManagementPlanDocumentID, cancellationToken);

        if (!string.IsNullOrEmpty(document.AnthropicFileID))
        {
            _logger.LogInformation("Anthropic file cache hit for documentID={DocumentID}, fileID={FileID}",
                waterQualityManagementPlanDocumentID, document.AnthropicFileID);
            return document.AnthropicFileID;
        }

        _logger.LogInformation("Anthropic file cache miss for documentID={DocumentID} — uploading to Files API...",
            waterQualityManagementPlanDocumentID);

        var blobGuid = document.FileResource.GetFileResourceGUIDAsString().ToLower();
        var streamingResult = await _blobService.DownloadBlobFromBlobStorageAsStream(blobGuid);

        FileMetadata fileMetadata;
        await using (streamingResult.Content)
        {
            // BinaryContent has implicit conversion from Stream — pass directly.
            fileMetadata = await _anthropic.Beta.Files.Upload(
                new FileUploadParams { File = streamingResult.Content },
                cancellationToken);
        }

        document.AnthropicFileID = fileMetadata.ID;
        document.AnthropicFileUploadedDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded documentID={DocumentID} to Anthropic Files API, cached fileID={FileID}",
            waterQualityManagementPlanDocumentID, fileMetadata.ID);

        return fileMetadata.ID;
    }

    /// <summary>
    /// Clear the cached file_id for a document, e.g., after Anthropic returned a 404
    /// for a stale id. The next <see cref="EnsureUploadedFileIDAsync"/> call will
    /// re-upload.
    /// </summary>
    public async Task InvalidateFileIDAsync(
        int waterQualityManagementPlanDocumentID, CancellationToken cancellationToken)
    {
        var document = await _dbContext.WaterQualityManagementPlanDocuments
            .SingleAsync(x => x.WaterQualityManagementPlanDocumentID == waterQualityManagementPlanDocumentID, cancellationToken);

        document.AnthropicFileID = null;
        document.AnthropicFileUploadedDate = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogWarning("Invalidated Anthropic file cache for documentID={DocumentID}",
            waterQualityManagementPlanDocumentID);
    }
}
