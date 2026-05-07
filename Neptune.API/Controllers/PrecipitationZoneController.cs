using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Jobs.Services;

namespace Neptune.API.Controllers;

[ApiController]
[Route("precipitation-zones")]
public class PrecipitationZoneController(
    NeptuneDbContext dbContext,
    ILogger<PrecipitationZoneController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<PrecipitationZoneController>(dbContext, logger, neptuneConfiguration)
{
    [HttpPost("enqueue-refresh")]
    [AdminFeature]
    public IActionResult EnqueueRefresh()
    {
        BackgroundJob.Enqueue<OCGISService>(x => x.RefreshPrecipitationZones());
        return Ok("Precipitation Zones refresh has been queued.");
    }
}
