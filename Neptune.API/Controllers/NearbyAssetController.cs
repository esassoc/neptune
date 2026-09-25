using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects.NearbyAsset;

namespace Neptune.API.Controllers;

[ApiController]
[Route("nearby-assets")]
public class NearbyAssetController(NeptuneDbContext dbContext, ILogger<NearbyAssetController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<NearbyAssetController>(dbContext, logger, neptuneConfiguration)
{
    // Homepage Field Actions panel (NPT-1123). JurisdictionEditFeature is role-only, which is exactly
    // "every signed-in role except Unassigned"; jurisdiction scoping happens inside the query.
    [HttpGet]
    [JurisdictionEditFeature]
    public async Task<ActionResult<NearbyAssetsResultDto>> List([FromQuery] double latitude, [FromQuery] double longitude)
    {
        if (!NearbyAssets.AreValidCoordinates(latitude, longitude))
        {
            return BadRequest("Latitude must be a number between -90 and 90 and longitude a number between -180 and 180.");
        }

        var person = People.GetByID(DbContext, CallingUser.PersonID);
        var result = await NearbyAssets.ListWithinRadiusAsync(DbContext, person, latitude, longitude);
        return Ok(result);
    }
}
