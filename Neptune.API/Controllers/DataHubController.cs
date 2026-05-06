using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("data-hub")]
public class DataHubController(
    NeptuneDbContext dbContext,
    ILogger<DataHubController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
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
        };
        return Ok(dto);
    }
}
