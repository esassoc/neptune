using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.Common.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("data-hub")]
public class DataHubController(
    NeptuneDbContext dbContext,
    ILogger<DataHubController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration,
    AzureBlobStorageService azureBlobStorageService)
    : SitkaController<DataHubController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet("last-updated")]
    [JurisdictionEditFeature]
    public async Task<ActionResult<DataHubLastUpdatedDto>> GetLastUpdated()
    {
        var dto = new DataHubLastUpdatedDto
        {
            Parcels = await Parcels.GetLatestUpdateAsync(DbContext),
            RegionalSubbasins = await RegionalSubbasins.GetLatestUpdateAsync(DbContext),
            HRUCharacteristics = await HRUCharacteristics.GetLatestUpdateAsync(DbContext),
            ModelBasins = await ModelBasins.GetLatestUpdateAsync(DbContext),
            PrecipitationZones = await PrecipitationZones.GetLatestUpdateAsync(DbContext),
            OCTAPrioritizations = await OCTAPrioritizations.GetLatestUpdateAsync(DbContext),
        };
        return Ok(dto);
    }

    /// <summary>
    /// NPT-998: streams a Data Hub bulk-upload template stored in Azure Blob Storage.
    /// One endpoint covers all five templates (WQMP, Simplified BMP, WQMP Locations, Trash
    /// Screen Field Visit, OVTA) so the SPA upload pages can offer a "Download Template"
    /// button without bouncing users back to the MVC site. Each template's blob path is
    /// configured on NeptuneConfiguration with values that match the legacy MVC's
    /// WebConfiguration so both surfaces can serve the same files during the retirement
    /// transition.
    /// </summary>
    [HttpGet("upload-templates/{templateKey}")]
    [JurisdictionEditFeature]
    public async Task<IActionResult> DownloadUploadTemplate([FromRoute] string templateKey)
    {
        var config = neptuneConfiguration.Value;
        var (blobPath, downloadFileName) = templateKey switch
        {
            "wqmp" => (config.PathToBulkUploadWQMPTemplate, $"UploadWQMPTemplate_{CallingUser.LastName}{CallingUser.FirstName}.xlsx"),
            "simplified-bmp" => (config.PathToSimplifiedBMPTemplate, $"SimplifiedBMPBulkUploadTemplate_{CallingUser.LastName}{CallingUser.FirstName}.xlsx"),
            "wqmp-locations" => (config.PathToUploadWQMPBoundaryTemplate, $"UploadWQMPBoundaryTemplate_{CallingUser.LastName}{CallingUser.FirstName}.csv"),
            "trash-screen-field-visit" => (config.PathToFieldVisitUploadTemplate, $"TrashScreenBulkUploadTemplate_{CallingUser.LastName}{CallingUser.FirstName}.xlsx"),
            "ovta" => (config.PathToOVTAUploadTemplate, $"OVTABulkUploadTemplate_{CallingUser.LastName}{CallingUser.FirstName}.xlsx"),
            _ => (null, null),
        };

        if (blobPath == null)
        {
            return NotFound($"Unknown upload template key '{templateKey}'.");
        }

        if (string.IsNullOrWhiteSpace(blobPath))
        {
            return Problem($"Upload template path for '{templateKey}' is not configured.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var contentType = downloadFileName!.EndsWith(".csv")
            ? "text/csv"
            : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        var download = await azureBlobStorageService.DownloadBlobFromBlobStorageAsStream(blobPath);
        return File(download.Content, contentType, downloadFileName);
    }
}
