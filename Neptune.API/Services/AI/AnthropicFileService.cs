using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
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
using Neptune.Common.Services;
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

    // Serializes upload attempts per documentID so concurrent callers (extract+chat
    // hitting the same doc, or two users on the same doc) don't both upload the same
    // PDF when the cache is empty. Same gate covers the stale-id refresh path so
    // refresh and ensure can't deadlock waiting on each other. Static — the file
    // service is scoped, but races span requests/replicas (within a replica, anyway).
    private static readonly ConcurrentDictionary<int, SemaphoreSlim> UploadGates = new();

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
    /// Thrown by <see cref="EnsureUploadedFileIDAsync"/> when the document's PDF exceeds
    /// <see cref="NeptuneConfiguration.MaxExtractablePdfSizeBytes"/>. Controllers should
    /// pre-check size for a clean 400 response; this exception is the defense-in-depth
    /// guard so we never spend bandwidth pushing an oversized blob to Anthropic only
    /// to be rejected at the upstream.
    /// </summary>
    public sealed class PdfTooLargeForUploadException : InvalidOperationException
    {
        public long ContentLengthBytes { get; }
        public long MaxBytes { get; }
        public PdfTooLargeForUploadException(long contentLengthBytes, long maxBytes)
            : base($"PDF is {contentLengthBytes / (1024.0 * 1024.0):F0} MB; the AI extraction upload cap is {maxBytes / (1024.0 * 1024.0):F0} MB.")
        {
            ContentLengthBytes = contentLengthBytes;
            MaxBytes = maxBytes;
        }
    }

    /// <summary>
    /// Returns the cached Anthropic <c>file_id</c> for a document, uploading the PDF
    /// once if not already cached. Idempotent — repeated calls for the same document
    /// return the same id without re-uploading. Concurrent callers for the same doc
    /// are serialized so only one upload occurs.
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

        // Defense-in-depth: refuse to spend memory + bandwidth uploading a PDF that
        // Anthropic will reject anyway. Controllers pre-check this for a friendly 400;
        // this guard catches any caller that bypasses the controller pre-check.
        if (document.FileResource.ContentLength > _configuration.MaxExtractablePdfSizeBytes)
        {
            throw new PdfTooLargeForUploadException(
                document.FileResource.ContentLength, _configuration.MaxExtractablePdfSizeBytes);
        }

        // Serialize concurrent uploads of the same document. Without this, two callers
        // observing AnthropicFileID == null both proceed to upload, resulting in two
        // file_ids on Anthropic (last-write-wins persisted, the loser orphans).
        var gate = UploadGates.GetOrAdd(waterQualityManagementPlanDocumentID, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await UploadAndCacheAsync(document, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Uploads the document and persists the resulting file_id. Caller is responsible
    /// for holding the per-document gate from <see cref="UploadGates"/>. Re-checks the
    /// cached id on entry so a concurrent caller that won the race doesn't trigger a
    /// duplicate upload.
    /// </summary>
    private async Task<string> UploadAndCacheAsync(
        WaterQualityManagementPlanDocument document, CancellationToken cancellationToken)
    {
        // Re-check inside the gate. The cheap-path read in the public entry point
        // happens outside the lock, so by the time we acquire it another caller may
        // have already populated the id.
        await _dbContext.Entry(document).ReloadAsync(cancellationToken);
        if (!string.IsNullOrEmpty(document.AnthropicFileID))
        {
            _logger.LogInformation("Anthropic file populated by concurrent caller for documentID={DocumentID}, fileID={FileID}",
                document.WaterQualityManagementPlanDocumentID, document.AnthropicFileID);
            return document.AnthropicFileID;
        }

        _logger.LogInformation("Anthropic file cache miss for documentID={DocumentID} — uploading to Files API...",
            document.WaterQualityManagementPlanDocumentID);

        // Bypass the SDK for upload. Anthropic.SDK 12.17.0's Beta.Files.Upload still
        // throws AnthropicIOException with inner ObjectDisposedException on real
        // payloads — we hit it with both byte[] and Azure streaming inputs. The bug
        // is internal SDK body-handling; documented in the NPT-1044 card with the
        // raw HttpClient fallback we're using here. Extraction and chat (which
        // reference the file_id, not upload it) keep using the SDK normally.
        //
        // Stream straight from blob storage instead of materializing the PDF as
        // byte[]. The earlier byte[] buffer was a workaround for the SDK encoder
        // refusing chunked length-unknown streams; on raw HttpClient we set the
        // inner part's Content-Length explicitly from FileResource.ContentLength,
        // so the multipart envelope is well-formed without a server-side buffer.
        // Cuts peak memory from O(file size) to a small read window per upload.
        var canonicalName = document.FileResource.GetFileResourceGUIDAsString().ToLower();

        // Skip any junk ahead of the %PDF signature — see FindPdfHeaderOffsetAsync.
        var headerOffset = await FindPdfHeaderOffsetAsync(
            canonicalName, document.WaterQualityManagementPlanDocumentID, cancellationToken);

        using var blobDownload = headerOffset == 0
            ? await _blobService.DownloadBlobFromBlobStorageAsStream(canonicalName, cancellationToken)
            : await _blobService.DownloadBlobRangeFromBlobStorageAsStream(
                canonicalName, headerOffset, cancellationToken: cancellationToken);

        var filename = document.FileResource.GetOriginalCompleteFileName();
        if (string.IsNullOrWhiteSpace(filename))
        {
            filename = $"{document.WaterQualityManagementPlanDocumentID}.pdf";
        }

        var fileID = await UploadViaHttpClientAsync(
            blobDownload.Content, document.FileResource.ContentLength - headerOffset, filename, cancellationToken);

        document.AnthropicFileID = fileID;
        document.AnthropicFileUploadedDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Uploaded documentID={DocumentID} to Anthropic Files API, cached fileID={FileID}",
            document.WaterQualityManagementPlanDocumentID, fileID);

        return fileID;
    }

    /// <summary>
    /// Number of leading bytes scanned for the %PDF signature. Matches the tolerance window
    /// used by mainstream readers, which is where these files pick up their reputation for
    /// being fine.
    /// </summary>
    private const int PdfHeaderScanBytes = 1024;

    private static readonly byte[] PdfSignature = "%PDF"u8.ToArray();

    /// <summary>
    /// Returns the offset of the %PDF signature within the first <see cref="PdfHeaderScanBytes"/>
    /// bytes of the blob, or 0 if it is already at byte 0 (or cannot be found).
    ///
    /// A valid PDF starts with %PDF at byte 0, but real-world files sometimes carry junk ahead
    /// of it — one WQMP arrived with a single stray 0x01 byte. Acrobat and browsers scan the
    /// leading kilobyte for the signature and open such files without complaint, so they look
    /// healthy to the uploader. Anthropic's parser requires the signature at byte 0; without it
    /// the document is sniffed as application/octet-stream and the extraction call fails with
    /// "Unsupported document file format", which names the wrong problem entirely and sends you
    /// hunting for a MIME-type bug. Trimming the leading bytes makes those documents extractable.
    /// </summary>
    private async Task<int> FindPdfHeaderOffsetAsync(
        string canonicalName, int documentID, CancellationToken cancellationToken)
    {
        using var head = await _blobService.DownloadBlobRangeFromBlobStorageAsStream(
            canonicalName, 0, PdfHeaderScanBytes, cancellationToken);
        using var buffer = new MemoryStream();
        await head.Content.CopyToAsync(buffer, cancellationToken);
        var bytes = buffer.ToArray();

        var offset = IndexOfSignature(bytes);
        if (offset < 0)
        {
            // Not a PDF as far as we can tell. Upload unchanged rather than inventing a new
            // failure mode — Anthropic will reject it, and that rejection is the honest answer.
            _logger.LogWarning(
                "No %PDF signature in the first {ScanBytes} bytes for documentID={DocumentID}; uploading unchanged. Extraction will likely fail — the file may not be a PDF.",
                PdfHeaderScanBytes, documentID);
            return 0;
        }

        if (offset > 0)
        {
            _logger.LogWarning(
                "PDF for documentID={DocumentID} has {Offset} junk byte(s) before the %PDF signature; skipping them so Anthropic can parse the document.",
                documentID, offset);
        }

        return offset;
    }

    private static int IndexOfSignature(byte[] bytes)
    {
        for (var i = 0; i + PdfSignature.Length <= bytes.Length; i++)
        {
            var matched = true;
            for (var j = 0; j < PdfSignature.Length; j++)
            {
                if (bytes[i + j] != PdfSignature[j])
                {
                    matched = false;
                    break;
                }
            }
            if (matched)
            {
                return i;
            }
        }
        return -1;
    }

    private async Task<string> UploadViaHttpClientAsync(
        Stream pdfStream, long contentLength, string filename, CancellationToken cancellationToken)
    {
        using var client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromMinutes(5);

        using var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(pdfStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        // Explicit Content-Length on the inner part lets the multipart encoder finalize
        // a deterministic outer Content-Length — the previous failure mode with chunked
        // Azure streams was the encoder couldn't compute total length without it.
        fileContent.Headers.ContentLength = contentLength;
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
    /// Recovers from a stale-<c>file_id</c> 404 by invalidating the cached id and
    /// re-uploading. Pass the id you observed the 404 against as <paramref name="staleFileID"/>;
    /// if another concurrent caller already refreshed past it (different id now in the DB),
    /// this returns that fresher id without re-uploading. Serialized per documentID via
    /// the same gate as <see cref="EnsureUploadedFileIDAsync"/> so the 4 parallel
    /// extraction calls don't each upload their own copy and so refresh and ensure
    /// can't deadlock on each other.
    /// </summary>
    public async Task<string> RefreshFileIDAsync(
        int waterQualityManagementPlanDocumentID, string staleFileID, CancellationToken cancellationToken)
    {
        var gate = UploadGates.GetOrAdd(waterQualityManagementPlanDocumentID, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var document = await _dbContext.WaterQualityManagementPlanDocuments
                .Include(x => x.FileResource)
                .SingleAsync(x => x.WaterQualityManagementPlanDocumentID == waterQualityManagementPlanDocumentID, cancellationToken);

            // Already-refreshed check: another caller may have re-uploaded while we
            // were queued on the gate. If the persisted id has moved on from the one
            // we 404'd against, just use it.
            if (!string.IsNullOrEmpty(document.AnthropicFileID) && document.AnthropicFileID != staleFileID)
            {
                _logger.LogInformation("Anthropic file cache already refreshed for documentID={DocumentID} (was {Stale}, now {Current}); skipping re-upload.",
                    waterQualityManagementPlanDocumentID, staleFileID, document.AnthropicFileID);
                return document.AnthropicFileID;
            }

            // Clear the stale id and force the upload path. UploadAndCacheAsync expects
            // the caller to hold the gate, which we do.
            document.AnthropicFileID = null;
            document.AnthropicFileUploadedDate = null;
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Invalidated Anthropic file cache for documentID={DocumentID} prior to refresh.",
                waterQualityManagementPlanDocumentID);

            return await UploadAndCacheAsync(document, cancellationToken);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Best-effort delete of a remote Anthropic upload. Call this whenever a cached
    /// <c>file_id</c> stops being referenced — the document row is deleted, or its
    /// PDF is replaced — otherwise the upload is orphaned on the account permanently
    /// and nothing ever reclaims it (NPT-1121).
    ///
    /// Never throws. A leftover remote file is not worth failing (or masking) the
    /// caller's operation over — same posture as
    /// <c>GDALAPIService.DeleteStagedBlobs</c>. Callers should invoke this *after*
    /// the database change commits, so a cleanup failure can't leave the row
    /// pointing at a file we already deleted.
    ///
    /// Do not call this on the stale-id 404 path in <see cref="RefreshFileIDAsync"/>:
    /// upstream has already reported the file missing, so there is nothing to delete
    /// and the extra round trip only adds latency to an error path.
    /// </summary>
    public async Task DeleteRemoteFileAsync(string fileID, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(fileID))
        {
            return;
        }

        try
        {
            // Raw HttpClient rather than the SDK, matching the upload path — see the
            // NPT-1044 note in UploadAndCacheAsync.
            using var client = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{AnthropicFilesUrl}/{fileID}");
            request.Headers.Add("x-api-key", _configuration.AnthropicApiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Headers.Add("anthropic-beta", "files-api-2025-04-14");

            using var response = await client.SendAsync(request, cancellationToken);

            // Already gone upstream is the outcome we wanted, not a failure.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Anthropic file {FileID} was already absent upstream; nothing to delete.", fileID);
                return;
            }

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("Failed to delete Anthropic file {FileID} (status={Status}): {Body}",
                    fileID, (int)response.StatusCode, body);
                return;
            }

            _logger.LogInformation("Deleted Anthropic file {FileID}.", fileID);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete Anthropic file {FileID}", fileID);
        }
    }
}
