using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
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
    private const string AnthropicFilesUrl = "https://api.anthropic.com/v1/files";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AzureBlobStorageService _blobService;
    private readonly NeptuneDbContext _dbContext;
    private readonly ILogger<AnthropicFileService> _logger;
    private readonly NeptuneConfiguration _configuration;

    public AnthropicFileService(
        IHttpClientFactory httpClientFactory,
        AzureBlobStorageService blobService,
        NeptuneDbContext dbContext,
        ILogger<AnthropicFileService> logger,
        IOptions<NeptuneConfiguration> configuration)
    {
        _httpClientFactory = httpClientFactory;
        _blobService = blobService;
        _dbContext = dbContext;
        _logger = logger;
        _configuration = configuration.Value;
    }

    private sealed class FileUploadResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("filename")] public string Filename { get; set; }
        [JsonPropertyName("size_bytes")] public long SizeBytes { get; set; }
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

        // Bypass the SDK for upload. Anthropic.SDK 12.17.0's Beta.Files.Upload still
        // throws AnthropicIOException with inner ObjectDisposedException on real
        // payloads — we hit it with both byte[] and Azure streaming inputs. The bug
        // is internal SDK body-handling; documented in the NPT-1044 card with the
        // raw HttpClient fallback we're using here. Extraction and chat (which
        // reference the file_id, not upload it) keep using the SDK normally.
        var downloadResult = await _blobService.DownloadFileResourceFromBlobStorage(document.FileResource);
        var pdfBytes = downloadResult.Content.ToArray();
        var filename = document.FileResource.GetOriginalCompleteFileName();
        if (string.IsNullOrWhiteSpace(filename))
        {
            filename = $"{document.WaterQualityManagementPlanDocumentID}.pdf";
        }

        var fileID = await UploadViaHttpClientAsync(pdfBytes, filename, cancellationToken);

        document.AnthropicFileID = fileID;
        document.AnthropicFileUploadedDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded documentID={DocumentID} to Anthropic Files API, cached fileID={FileID}",
            waterQualityManagementPlanDocumentID, fileID);

        return fileID;
    }

    private async Task<string> UploadViaHttpClientAsync(byte[] bytes, string filename, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "file", filename);

        using var request = new HttpRequestMessage(HttpMethod.Post, AnthropicFilesUrl) { Content = form };
        request.Headers.Add("x-api-key", _configuration.AnthropicApiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Headers.Add("anthropic-beta", "files-api-2025-04-14");

        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Anthropic Files API upload failed (status={Status}): {Body}", (int)response.StatusCode, body);
            throw new InvalidOperationException(
                $"Anthropic Files API upload returned {(int)response.StatusCode}: {body}");
        }

        var parsed = JsonSerializer.Deserialize<FileUploadResponse>(body)
                     ?? throw new InvalidOperationException("Anthropic Files API returned an empty response body.");
        if (string.IsNullOrEmpty(parsed.Id))
        {
            throw new InvalidOperationException(
                $"Anthropic Files API response missing 'id' field: {body}");
        }
        return parsed.Id;
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
