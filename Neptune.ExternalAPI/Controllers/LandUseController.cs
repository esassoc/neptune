using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neptune.Common.Services;

namespace Neptune.ExternalAPI.Controllers;

[Authorize]
[ApiController]
[Tags("Land Use")]
[Route("land-use")]
public class LandUseController(AzureBlobStorageService blobStorageService) : ControllerBase
{
    [HttpGet("statistics")]
    [EndpointSummary("Land Use")]
    [EndpointDescription("This table is the result of a spatial overlay analysis (union) between the Regional Subbasins managed by OC Survey, WQMP project boundaries entered into OCST, and distributed delineations also entered into OCST. The result is a **non-overlapping** account of how the land surface is classified in the OCST system. These OCST classes are further subdivided by additional spatial analysis (provided by a web service build by OC Survey) to identify hydrologic soil group (HSG), slope category (0, 5, 10+), land use, and to compute a % imperviousness for each resulting 'sliver' of the landscape. This generates a very tall table in which each row is a sliver that identifies precisely how each sliver is treated, and how each sliver might assert influence on the hydrology of the system.\n\nThe PowerBI file uses the ID columns(TreatmentBMPID, WaterQualityManagementPlanID) to group these slivers into pivot tables to report total area treated, land use composition and % impervious.")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Produces("application/json")]
    public Task<IActionResult> Statistics()
    {
        return BlobFileDownloadHelper.DownloadIfExistsAsync(blobStorageService, BlobFileNames.LandUseStatistics, Response);
    }
}
