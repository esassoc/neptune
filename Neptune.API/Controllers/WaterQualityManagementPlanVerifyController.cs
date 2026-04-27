using System.Collections.Generic;
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
[Route("water-quality-management-plan-verifies")]
public class WaterQualityManagementPlanVerifyController(
    NeptuneDbContext dbContext,
    ILogger<WaterQualityManagementPlanVerifyController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<WaterQualityManagementPlanVerifyController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [JurisdictionEditFeature]
    public async Task<ActionResult<List<WaterQualityManagementPlanVerifyIndexGridDto>>> ListAllAsIndexGrid()
    {
        var dtos = await WaterQualityManagementPlanVerifies.ListAllAsIndexGridDtoAsync(DbContext, CallingUser);
        return Ok(dtos);
    }
}
