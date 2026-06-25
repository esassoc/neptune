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
[Route("octa-prioritizations")]
public class OCTAPrioritizationController(
    NeptuneDbContext dbContext,
    ILogger<OCTAPrioritizationController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<OCTAPrioritizationController>(dbContext, logger, neptuneConfiguration)
{
    [HttpPost("enqueue-refresh")]
    [AdminFeature]
    public IActionResult EnqueueRefresh()
    {
        BackgroundJob.Enqueue<OCGISService>(x => x.RefreshOCTAPrioritizations());
        return Ok("OCTA Prioritization refresh has been queued.");
    }
}
