using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Neptune.API.Controllers;

[ApiController]
[Route("file-resources")]
public class FileResourceController(NeptuneDbContext dbContext, ILogger<FileResourceController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration, AzureBlobStorageService azureBlobStorageService)
    : SitkaController<FileResourceController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet("{fileResourceGuidAsString}")]
    [ProducesResponseType(typeof(FileStreamResult), (int)HttpStatusCode.OK)]
    [AllowAnonymous]
    public async Task<IActionResult> DisplayResource(string fileResourceGuidAsString)
    {
        var isStringAGuid = Guid.TryParse(fileResourceGuidAsString, out var fileResourceGuid);
        if (isStringAGuid)
        {
            var fileResource = DbContext.FileResources.AsNoTracking().SingleOrDefault(x => x.FileResourceGUID == fileResourceGuid);
            if (fileResource != null)
            {
                return await DisplayFileResource(fileResource);
            }
        }
        
        // Unhappy path - return an HTTP 404
        // ---------------------------------
        var message = $"File Resource {fileResourceGuidAsString} Not Found in database. It may have been deleted.";
        Logger.LogError(message);
        return new NotFoundResult();
    }

    private async Task<IActionResult> DisplayFileResource(FileResource fileResource)
    {
        var blobDownloadResult =
            await azureBlobStorageService.DownloadFileResourceFromBlobStorage(fileResource.FileResourceGUID);

        var fileName = fileResource.GetOriginalCompleteFileName();
        var isInlineable = blobDownloadResult.Details.ContentType == "application/pdf"
            || blobDownloadResult.Details.ContentType?.StartsWith("image/") == true;

        var contentDisposition = new System.Net.Mime.ContentDisposition
        {
            FileName = fileName,
            Inline = isInlineable
        };
        Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

        if (isInlineable)
            return File(blobDownloadResult.Content.ToArray(), blobDownloadResult.Details.ContentType);
        return File(blobDownloadResult.Content.ToArray(), blobDownloadResult.Details.ContentType, fileName);
    }
}