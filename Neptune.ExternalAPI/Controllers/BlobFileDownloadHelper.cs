using Microsoft.AspNetCore.Mvc;
using Neptune.Common.Services;

namespace Neptune.ExternalAPI.Controllers;

internal static class BlobFileDownloadHelper
{
    public static async Task<IActionResult> DownloadIfExistsAsync(AzureBlobStorageService blobStorageService, string fileName, HttpResponse response)
    {
        if (!await blobStorageService.ExistsFromBlobStorage(fileName))
        {
            return new NotFoundResult();
        }

        var contentDisposition = new System.Net.Mime.ContentDisposition
        {
            FileName = fileName,
            Inline = false
        };
        response.Headers.Append("Content-Disposition", contentDisposition.ToString());

        var blobDownloadResult = await blobStorageService.DownloadBlobFromBlobStorage(fileName);
        return new FileContentResult(blobDownloadResult.Content.ToArray(), blobDownloadResult.Details.ContentType);
    }
}
