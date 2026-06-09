using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Neptune.Common.Services;

namespace Neptune.ExternalAPI.Controllers;

internal static class BlobFileDownloadHelper
{
    // All callers serve JSON blobs (LandUseStatistics.json, ModelResults.json,
    // BaselineModelResults.json). The blobs were originally uploaded with
    // Content-Type application/octet-stream by the Hangfire jobs, which PowerBI's
    // data source dialog refuses to auto-detect as JSON. Force application/json on
    // the response so consumers don't have to hand-configure parsers.
    //
    // Content-Disposition stays "attachment" — programmatic consumers (PowerBI, curl,
    // fetch) ignore the header and just read the body, while browsers honor it and
    // download the file instead of trying to render it inline. ModelResults.json is
    // ~185MB, which is enough to OOM Chrome's JSON viewer if rendered in a tab.
    //
    // Uses the streaming download variant + FileStreamResult so ASP.NET pipes the blob
    // straight to the response without buffering ~185MB in pod memory per request.
    // Content-Length is set explicitly from the blob metadata — the Azure SDK response
    // stream isn't reliably seekable, so FileStreamResult on its own may emit a
    // chunked response without Content-Length, which breaks PowerBI's progress UI.
    public static async Task<IActionResult> DownloadIfExistsAsync(AzureBlobStorageService blobStorageService, string fileName, HttpResponse response)
    {
        if (!await blobStorageService.ExistsFromBlobStorage(fileName))
        {
            return new NotFoundResult();
        }

        var contentDisposition = new ContentDispositionHeaderValue("attachment");
        contentDisposition.SetHttpFileName(fileName);
        response.Headers[HeaderNames.ContentDisposition] = contentDisposition.ToString();

        var blobStream = await blobStorageService.DownloadBlobFromBlobStorageAsStream(fileName);
        response.ContentLength = blobStream.Details.ContentLength;
        return new FileStreamResult(blobStream.Content, "application/json")
        {
            FileDownloadName = fileName,
            LastModified = blobStream.Details.LastModified,
        };
    }
}
