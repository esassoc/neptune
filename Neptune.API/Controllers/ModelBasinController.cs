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
[Route("model-basins")]
public class ModelBasinController(
    NeptuneDbContext dbContext,
    ILogger<ModelBasinController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<ModelBasinController>(dbContext, logger, neptuneConfiguration)
{
    [HttpPost("enqueue-refresh")]
    [AdminFeature]
    public IActionResult EnqueueRefresh()
    {
        BackgroundJob.Enqueue<OCGISService>(x => x.RefreshModelBasins());
        return Ok("Model Basins refresh has been queued.");
    }
}
