/*-----------------------------------------------------------------------
<copyright file="FileResourceController.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
Copyright (c) Tahoe Regional Planning Agency and Sitka Technology Group. All rights reserved.
<author>Sitka Technology Group</author>
</copyright>

<license>
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License <http://www.gnu.org/licenses/> for more details.

Source code is available upon request via <support@sitkatech.com>.
</license>
-----------------------------------------------------------------------*/

using Neptune.WebMvc.Security;
using Neptune.Common.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Neptune.EFModels.Entities;
using Neptune.Common.Services;
using Microsoft.Extensions.Options;
using Neptune.WebMvc.Common;
using Neptune.Common.Services;
using Neptune.WebMvc.Services;
using Neptune.Common.Services;
using Neptune.WebMvc.Services.Filters;
using Neptune.Common.Services;

namespace Neptune.WebMvc.Controllers
{
    public class FileResourceController(
        NeptuneDbContext dbContext,
        ILogger<FileResourceController> logger,
        IOptions<WebConfiguration> webConfiguration,
        LinkGenerator linkGenerator,
        AzureBlobStorageService azureBlobStorageService)
        : NeptuneBaseController<FileResourceController>(dbContext, logger, linkGenerator, webConfiguration)
    {
        [HttpGet("{fileResourceGuidAsString}")]
        public async Task<IActionResult> DisplayResource([FromRoute] string fileResourceGuidAsString)
        {
            var isStringAGuid = Guid.TryParse(fileResourceGuidAsString, out var fileResourceGuid);
            if (isStringAGuid)
            {
                var fileResource = _dbContext.FileResources.AsNoTracking().SingleOrDefault(x => x.FileResourceGUID == fileResourceGuid);
                if (fileResource != null)
                {
                    return await DisplayFileResource(fileResource);
                }
            }
            // Unhappy path - return an HTTP 404
            // ---------------------------------
            var message = $"File Resource {fileResourceGuidAsString} Not Found in database. It may have been deleted.";
            _logger.LogError(message);
            return new NotFoundResult();
        }

        private async Task<IActionResult> DisplayFileResource(FileResource fileResource)
        {
            var contentDisposition = new System.Net.Mime.ContentDisposition
            {
                FileName = fileResource.GetOriginalCompleteFileName(),
                Inline = false
            };
            Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

            var blobDownloadResult =
                await azureBlobStorageService.DownloadFileResourceFromBlobStorage(fileResource.FileResourceGUID);

            return File(blobDownloadResult.Content.ToArray(), blobDownloadResult.Details.ContentType);
        }

        [HttpGet("{fileResourcePrimaryKey}")]
        [LoggedInUnclassifiedFeature]
        [ValidateEntityExistsAndPopulateParameterFilter("fileResourcePrimaryKey")]
        public async Task<IActionResult> DisplayResourceByID([FromRoute] FileResourcePrimaryKey fileResourcePrimaryKey)
        {
            var fileResource = fileResourcePrimaryKey.EntityObject;
            return await DisplayFileResource(fileResource);
        }
    }
}
