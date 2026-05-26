using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects.ManagerDashboard;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("manager-dashboard")]
    public class ManagerDashboardController(
        NeptuneDbContext dbContext,
        ILogger<ManagerDashboardController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration)
        : SitkaController<ManagerDashboardController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpGet("field-visits/provisional")]
        [JurisdictionManageFeature]
        public async Task<ActionResult<List<FieldVisitProvisionalGridDto>>> ListProvisionalFieldVisits()
        {
            var person = UserContext.GetUserFromHttpContext(DbContext, HttpContext);
            var rows = await vFieldVisitDetaileds.GetProvisionalFieldVisitsAsGridDtoAsync(DbContext, person);
            return Ok(rows);
        }

        [HttpGet("bmps/provisional")]
        [JurisdictionManageFeature]
        public async Task<ActionResult<List<TreatmentBMPProvisionalGridDto>>> ListProvisionalTreatmentBMPs()
        {
            var person = UserContext.GetUserFromHttpContext(DbContext, HttpContext);
            var rows = await TreatmentBMPs.GetProvisionalTreatmentBMPsAsGridDtoAsync(DbContext, person);
            return Ok(rows);
        }

        [HttpGet("delineations/provisional")]
        [JurisdictionManageFeature]
        public async Task<ActionResult<List<DelineationProvisionalGridDto>>> ListProvisionalDelineations()
        {
            var person = UserContext.GetUserFromHttpContext(DbContext, HttpContext);
            var rows = await Delineations.GetProvisionalBMPDelineationsAsGridDtoAsync(DbContext, person);
            return Ok(rows);
        }

        [HttpPost("field-visits/bulk-verify")]
        [JurisdictionManageFeature]
        public async Task<ActionResult<BulkVerifyResponseDto>> BulkVerifyFieldVisits([FromBody] BulkVerifyRequestDto request)
        {
            var person = UserContext.GetUserFromHttpContext(DbContext, HttpContext);
            var count = await FieldVisits.BulkMarkAsVerifiedAsync(DbContext, request.IDs, person);
            return Ok(new BulkVerifyResponseDto { VerifiedCount = count });
        }

        [HttpPost("bmps/bulk-verify")]
        [JurisdictionManageFeature]
        public async Task<ActionResult<BulkVerifyResponseDto>> BulkVerifyTreatmentBMPs([FromBody] BulkVerifyRequestDto request)
        {
            var person = UserContext.GetUserFromHttpContext(DbContext, HttpContext);
            var count = await TreatmentBMPs.BulkMarkAsVerifiedAsync(DbContext, request.IDs, person);
            return Ok(new BulkVerifyResponseDto { VerifiedCount = count });
        }

        [HttpPost("delineations/bulk-verify")]
        [JurisdictionManageFeature]
        public async Task<ActionResult<BulkVerifyResponseDto>> BulkVerifyDelineations([FromBody] BulkVerifyRequestDto request)
        {
            var person = UserContext.GetUserFromHttpContext(DbContext, HttpContext);
            var count = await Delineations.BulkMarkAsVerifiedAsync(DbContext, request.IDs, person);
            return Ok(new BulkVerifyResponseDto { VerifiedCount = count });
        }
    }
}
