using System.Collections.Generic;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Jobs.Hangfire;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("hru-characteristics")]
public class HRUCharacteristicController(
    NeptuneDbContext dbContext,
    ILogger<HRUCharacteristicController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<HRUCharacteristicController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [SitkaAdminFeature]
    public async Task<ActionResult<List<HRUCharacteristicDto>>> List()
    {
        var hruCharacteristics = await vHRUCharacteristics.ListAsDtoAsync(DbContext);
        return Ok(hruCharacteristics);
    }

    [HttpPost("enqueue-refresh")]
    [SitkaAdminFeature]
    public IActionResult EnqueueRefresh()
    {
        BackgroundJob.Enqueue<LoadGeneratingUnitRefreshJob>(x => x.RunJob(null));
        return Ok("HRU Characteristic refresh has been queued.");
    }
}