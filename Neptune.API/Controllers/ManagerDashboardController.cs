using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Common;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.Common.GeoSpatial;
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
            // NPT-1155: only verified delineations are Load Generating Unit inputs, so each one this
            // call verifies becomes an input and the LGUs under it have to be rebuilt. Note which of
            // the requested ones are provisional now, then refresh those that came out verified (the
            // helper skips delineations outside the caller's jurisdictions).
            var provisionalIDs = (await Delineations.ListLoadGeneratingUnitInputGeometriesAsync(DbContext, request.IDs ?? [], false)).Keys;
            var count = await Delineations.BulkMarkAsVerifiedAsync(DbContext, request.IDs, person);
            var newlyVerifiedGeometries = await Delineations.ListLoadGeneratingUnitInputGeometriesAsync(DbContext, provisionalIDs, true);
            var newlyVerifiedArea = newlyVerifiedGeometries.Values.ToList().UnionListGeometries();
            if (newlyVerifiedArea != null)
            {
                await ModelingEngineUtilities.QueueLGURefreshForArea(newlyVerifiedArea, null, DbContext);
            }
            return Ok(new BulkVerifyResponseDto { VerifiedCount = count });
        }
    }
}
