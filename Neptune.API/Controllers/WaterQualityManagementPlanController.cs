using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.EFModels.Nereid;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("water-quality-management-plans")]
    public class WaterQualityManagementPlanController(
        NeptuneDbContext dbContext,
        ILogger<WaterQualityManagementPlanController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration)
        : SitkaController<WaterQualityManagementPlanController>(dbContext, logger,
            neptuneConfiguration)
    {
        [HttpGet]
        [AdminFeature]
        public async Task<ActionResult<IEnumerable<WaterQualityManagementPlanDto>>> List()
        {
            var plans = await WaterQualityManagementPlans.ListAsDtoAsync(DbContext);
            return Ok(plans);
        }

        [HttpGet("grid")]
        [AllowAnonymous]
        [OptionalAuth]
        public async Task<ActionResult<List<WaterQualityManagementPlanGridDto>>> ListAsGridDto()
        {
            var entities = await vWaterQualityManagementPlanDetaileds.ListViewableByPersonDtoAsync(DbContext, CallingUser);
            var gridDtos = entities.Select(x => x.AsGridDto()).ToList();
            return Ok(gridDtos);
        }

        [HttpGet("display-dtos")]
        [AdminFeature]
        public async Task<ActionResult<List<WaterQualityManagementPlanDisplayDto>>> ListAsDisplayDtos()
        {
            var plans = await WaterQualityManagementPlans.ListAsDisplayDtoAsync(DbContext);
            return Ok(plans);
        }

        [HttpGet("{waterQualityManagementPlanID}")]
        [AllowAnonymous]
        [OptionalAuth]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanDto>> Get([FromRoute] int waterQualityManagementPlanID)
        {
            var entity = await WaterQualityManagementPlans.GetByIDAsDtoAsync(DbContext, waterQualityManagementPlanID);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        [HttpPost]
        [AdminFeature]
        public async Task<ActionResult<WaterQualityManagementPlanDto>> Create([FromBody] WaterQualityManagementPlanUpsertDto dto)
        {
            var created = await WaterQualityManagementPlans.CreateAsync(DbContext, dto);
            return CreatedAtAction(nameof(Get), new { waterQualityManagementPlanID = created.WaterQualityManagementPlanID }, created);
        }

        [HttpPut("{waterQualityManagementPlanID}")]
        [AdminFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanDto>> Update([FromRoute] int waterQualityManagementPlanID, [FromBody] WaterQualityManagementPlanUpsertDto dto)
        {
            var updated = await WaterQualityManagementPlans.UpdateAsync(DbContext, waterQualityManagementPlanID, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{waterQualityManagementPlanID}")]
        [AdminFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<IActionResult> Delete([FromRoute] int waterQualityManagementPlanID)
        {
            var deleted = await WaterQualityManagementPlans.DeleteAsync(DbContext, waterQualityManagementPlanID);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpGet("{waterQualityManagementPlanID}/documents")]
        [AllowAnonymous]
        [OptionalAuth]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<IEnumerable<WaterQualityManagementPlanDocumentDto>>> ListDocuments([FromRoute] int waterQualityManagementPlanID)
        {
            if (CallingUser == null || CallingUser.RoleID == (int)RoleEnum.Unassigned)
            {
                return Ok(new List<WaterQualityManagementPlanDocumentDto>());
            }
            var waterQualityManagementPlanDocumentDtos = await WaterQualityManagementPlanDocuments.ListByWaterQualityManagementPlanIDAsDtoAsync(DbContext, waterQualityManagementPlanID);
            return Ok(waterQualityManagementPlanDocumentDtos);
        }

        [HttpGet("{waterQualityManagementPlanID}/quick-bmps")]
        [AllowAnonymous]
        [OptionalAuth]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<List<QuickBMPDto>>> ListQuickBMPs([FromRoute] int waterQualityManagementPlanID)
        {
            var quickBMPs = await QuickBMPs.ListByWaterQualityManagementPlanIDAsDtoAsync(DbContext, waterQualityManagementPlanID);
            return Ok(quickBMPs);
        }

        [HttpGet("{waterQualityManagementPlanID}/source-control-bmps")]
        [AllowAnonymous]
        [OptionalAuth]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<List<SourceControlBMPDto>>> ListSourceControlBMPs([FromRoute] int waterQualityManagementPlanID)
        {
            var sourceControlBMPs = await SourceControlBMPs.ListByWaterQualityManagementPlanIDAsDtoAsync(DbContext, waterQualityManagementPlanID);
            return Ok(sourceControlBMPs);
        }

        [HttpGet("{waterQualityManagementPlanID}/verifications")]
        [AllowAnonymous]
        [OptionalAuth]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<List<WaterQualityManagementPlanVerifyGridDto>>> ListVerifications([FromRoute] int waterQualityManagementPlanID)
        {
            var verifications = await WaterQualityManagementPlanVerifies.ListByWaterQualityManagementPlanIDAsDtoAsync(DbContext, waterQualityManagementPlanID);
            return Ok(verifications);
        }

        [HttpGet("{waterQualityManagementPlanID}/modeled-performance")]
        [AllowAnonymous]
        [OptionalAuth]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<ProjectLoadReducingResultDto>> GetModeledPerformance([FromRoute] int waterQualityManagementPlanID)
        {
            var result = await WaterQualityManagementPlanModeledPerformance.GetModeledPerformanceAsync(DbContext, waterQualityManagementPlanID);
            if (result == null) return Ok(null);
            return Ok(result);
        }

        [HttpGet("{waterQualityManagementPlanID}/hru-characteristics")]
        [AllowAnonymous]
        [OptionalAuth]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<List<HRUCharacteristicDto>>> ListHRUCharacteristics([FromRoute] int waterQualityManagementPlanID)
        {
            var hruCharacteristics = await vHRUCharacteristics.ListByWaterQualityManagementPlanIDAsDtoAsync(DbContext, waterQualityManagementPlanID);
            return Ok(hruCharacteristics);
        }

        [HttpGet("hydrologic-subareas")]
        [AdminFeature]
        public async Task<ActionResult<List<HydrologicSubareaSimpleDto>>> ListHydrologicSubareas()
        {
            var subareas = await DbContext.HydrologicSubareas
                .Select(x => new HydrologicSubareaSimpleDto { HydrologicSubareaID = x.HydrologicSubareaID, HydrologicSubareaName = x.HydrologicSubareaName })
                .OrderBy(x => x.HydrologicSubareaName)
                .ToListAsync();
            return Ok(subareas);
        }

        [HttpGet("with-final-document")]
        [AdminFeature]
        public async Task<ActionResult<IEnumerable<WaterQualityManagementPlanDto>>> ListWithFinalWQMPDocument()
        {
            var filteredPlans = await WaterQualityManagementPlans.ListWithFinalWQMPDocumentAsync(DbContext);
            return Ok(filteredPlans);
        }

        [HttpPut("{waterQualityManagementPlanID}/modeling-approach")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult> UpdateModelingApproach(
            [FromRoute] int waterQualityManagementPlanID, [FromBody] int modelingApproachID)
        {
            if (modelingApproachID != 1 && modelingApproachID != 2)
            {
                return BadRequest("Modeling Approach must be Detailed (1) or Simplified (2).");
            }

            var wqmp = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
            wqmp.WaterQualityManagementPlanModelingApproachID = modelingApproachID;
            await DbContext.SaveChangesAsync();
            await NereidUtilities.MarkWqmpDirty(wqmp, DbContext);
            return NoContent();
        }

        [HttpPut("{waterQualityManagementPlanID}/treatment-bmps")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult> UpdateTreatmentBMPs(
            [FromRoute] int waterQualityManagementPlanID, [FromBody] List<int> treatmentBMPIDs)
        {
            await TreatmentBMPs.UpdateWaterQualityManagementPlanAssociationsAsync(DbContext, waterQualityManagementPlanID, treatmentBMPIDs);
            var wqmp = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
            await NereidUtilities.MarkWqmpDirty(wqmp, DbContext);
            return NoContent();
        }

        [HttpGet("{waterQualityManagementPlanID}/available-treatment-bmps")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<List<TreatmentBMPMinimalDto>>> ListAvailableTreatmentBMPs(
            [FromRoute] int waterQualityManagementPlanID)
        {
            var wqmp = WaterQualityManagementPlans.GetByID(DbContext, waterQualityManagementPlanID);
            var availableBMPs = await TreatmentBMPs.ListAvailableForWaterQualityManagementPlanAsync(DbContext, wqmp.StormwaterJurisdictionID, waterQualityManagementPlanID);
            return Ok(availableBMPs);
        }

        [HttpPut("{waterQualityManagementPlanID}/quick-bmps")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult> MergeQuickBMPs(
            [FromRoute] int waterQualityManagementPlanID, [FromBody] List<QuickBMPUpsertDto> quickBMPs)
        {
            var quickBMPNoteMaxLength = QuickBMP.FieldLengths.QuickBMPNote;
            var duplicateNames = quickBMPs?.GroupBy(x => x.QuickBMPName).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateNames?.Any() == true)
            {
                return BadRequest($"Duplicate BMP names found: {string.Join(", ", duplicateNames)}. All names must be unique.");
            }

            var bmps = quickBMPs ?? new List<QuickBMPUpsertDto>();
            foreach (var bmp in bmps)
            {
                if (bmp.QuickBMPNote?.Length > quickBMPNoteMaxLength)
                {
                    return BadRequest($"\"{bmp.QuickBMPName}\"'s note exceeds the maximum of {quickBMPNoteMaxLength} characters.");
                }
            }

            if (bmps.Any(x => x.PercentRetained > x.PercentCaptured))
            {
                return BadRequest("Percent Captured needs to be greater than or equal to Percent Retained.");
            }

            if (bmps.Any(x => x.PercentOfSiteTreated < 0 || x.PercentOfSiteTreated > 100))
            {
                return BadRequest("Percent of Site Treated needs to be between 0 and 100.");
            }

            if (bmps.Any(x => x.PercentCaptured < 0 || x.PercentCaptured > 100))
            {
                return BadRequest("Percent Captured needs to be between 0 and 100.");
            }

            if (bmps.Any(x => x.PercentRetained < 0 || x.PercentRetained > 100))
            {
                return BadRequest("Percent Retained needs to be between 0 and 100.");
            }

            if (bmps.Any(x => x.PercentOfSiteTreated.HasValue) && bmps.Sum(x => x.PercentOfSiteTreated ?? 0) > 100)
            {
                return BadRequest("The Percent of Site Treated exceeds 100 percent, please correct any errors before saving.");
            }

            await QuickBMPs.MergeAsync(DbContext, waterQualityManagementPlanID, quickBMPs);
            var wqmp = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
            await NereidUtilities.MarkWqmpDirty(wqmp, DbContext);
            return NoContent();
        }

        [HttpPut("{waterQualityManagementPlanID}/source-control-bmps")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult> MergeSourceControlBMPs(
            [FromRoute] int waterQualityManagementPlanID, [FromBody] List<SourceControlBMPUpsertDto> sourceControlBMPs)
        {
            var sourceControlBMPNoteMaxLength = SourceControlBMP.FieldLengths.SourceControlBMPNote;
            foreach (var bmp in sourceControlBMPs)
            {
                if (bmp.SourceControlBMPNote?.Length > sourceControlBMPNoteMaxLength)
                {
                    return BadRequest($"\"{bmp.SourceControlBMPAttributeName}\"'s note exceeds the maximum of {sourceControlBMPNoteMaxLength} characters.");
                }
            }

            await SourceControlBMPs.MergeAsync(DbContext, waterQualityManagementPlanID, sourceControlBMPs);
            var wqmp = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
            await NereidUtilities.MarkWqmpDirty(wqmp, DbContext);
            return NoContent();
        }

        [HttpGet("source-control-bmp-attributes")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<SourceControlBMPUpsertDto>>> ListSourceControlBMPAttributes()
        {
            var attributes = await SourceControlBMPAttributes.ListAsUpsertDtoAsync(DbContext);
            return Ok(attributes);
        }
    }
}
