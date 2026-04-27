using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Anthropic.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Common;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.API.Services.AI;
using Neptune.EFModels.Nereid;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("water-quality-management-plans")]
    public class WaterQualityManagementPlanController(
        NeptuneDbContext dbContext,
        ILogger<WaterQualityManagementPlanController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration,
        AzureBlobStorageService azureBlobStorageService,
        WqmpExtractionService wqmpExtractionService)
        : SitkaController<WaterQualityManagementPlanController>(dbContext, logger,
            neptuneConfiguration)
    {
        // PDF size cap for AI extraction. Configurable via NeptuneConfiguration.MaxExtractablePdfSizeBytes
        // (defaults to 200 MB) — was 32 MB while we used the URL-source document block, raised in
        // NPT-1044 when we switched extraction + chat to Anthropic's Files API path. Enforced at
        // upload (so users don't park unusable PDFs) and again at extract-time as a safety net.

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
        [JurisdictionEditFeature]
        public async Task<ActionResult<WaterQualityManagementPlanDto>> Create([FromBody] WaterQualityManagementPlanUpsertDto dto)
        {
            var created = await WaterQualityManagementPlans.CreateAsync(DbContext, dto);
            return CreatedAtAction(nameof(Get), new { waterQualityManagementPlanID = created.WaterQualityManagementPlanID }, created);
        }

        [HttpPut("{waterQualityManagementPlanID}")]
        [JurisdictionEditFeature]
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

        [HttpGet("{waterQualityManagementPlanID}/verifications/{waterQualityManagementPlanVerifyID}")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        [EntityNotFound(typeof(WaterQualityManagementPlanVerify), "waterQualityManagementPlanVerifyID")]
        public async Task<ActionResult<WaterQualityManagementPlanVerifyDetailDto>> GetVerification(
            [FromRoute] int waterQualityManagementPlanID, [FromRoute] int waterQualityManagementPlanVerifyID)
        {
            var dto = await WaterQualityManagementPlanVerifies.GetByIDAsDtoAsync(DbContext, waterQualityManagementPlanVerifyID);
            if (dto == null) return NotFound();
            return Ok(dto);
        }

        [HttpPost("{waterQualityManagementPlanID}/verifications")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanVerifyDetailDto>> CreateVerification(
            [FromRoute] int waterQualityManagementPlanID, [FromBody] WaterQualityManagementPlanVerifyUpsertDto dto)
        {
            var result = await WaterQualityManagementPlanVerifies.CreateAsync(DbContext, waterQualityManagementPlanID, dto, CallingUser.PersonID);
            return Ok(result);
        }

        [HttpPut("{waterQualityManagementPlanID}/verifications/{waterQualityManagementPlanVerifyID}")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        [EntityNotFound(typeof(WaterQualityManagementPlanVerify), "waterQualityManagementPlanVerifyID")]
        public async Task<ActionResult<WaterQualityManagementPlanVerifyDetailDto>> UpdateVerification(
            [FromRoute] int waterQualityManagementPlanID, [FromRoute] int waterQualityManagementPlanVerifyID,
            [FromBody] WaterQualityManagementPlanVerifyUpsertDto dto)
        {
            var result = await WaterQualityManagementPlanVerifies.UpdateAsync(DbContext, waterQualityManagementPlanVerifyID, dto, CallingUser.PersonID);
            return Ok(result);
        }

        [HttpDelete("{waterQualityManagementPlanID}/verifications/{waterQualityManagementPlanVerifyID}")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        [EntityNotFound(typeof(WaterQualityManagementPlanVerify), "waterQualityManagementPlanVerifyID")]
        public async Task<IActionResult> DeleteVerification(
            [FromRoute] int waterQualityManagementPlanID, [FromRoute] int waterQualityManagementPlanVerifyID)
        {
            await WaterQualityManagementPlanVerifies.DeleteAsync(DbContext, waterQualityManagementPlanVerifyID);
            return NoContent();
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
        [UserViewFeature]
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

        [HttpGet("{waterQualityManagementPlanID}/boundary")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public ActionResult<WaterQualityManagementPlanBoundaryResponseDto> GetBoundary([FromRoute] int waterQualityManagementPlanID)
        {
            var boundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanID(DbContext, waterQualityManagementPlanID);
            var response = new WaterQualityManagementPlanBoundaryResponseDto
            {
                BoundaryAsFeatureCollection = WaterQualityManagementPlanBoundaries.GetBoundaryAsFeatureCollection(DbContext, waterQualityManagementPlanID),
                Parcels = WaterQualityManagementPlanParcels.ListAsParcelDisplayDtos(DbContext, waterQualityManagementPlanID),
                CalculatedWQMPAcreage = WaterQualityManagementPlanBoundaries.CalculateAcreage(DbContext, waterQualityManagementPlanID),
                BoundingBox = boundary?.Geometry4326 != null ? new BoundingBoxDto(boundary.Geometry4326) : new BoundingBoxDto()
            };
            return Ok(response);
        }

        [HttpPut("{waterQualityManagementPlanID}/boundary")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanBoundaryResponseDto>> UpdateBoundary(
            [FromRoute] int waterQualityManagementPlanID, [FromBody] WaterQualityManagementPlanBoundaryUpsertDto dto)
        {
            var oldGeometryNative = await WaterQualityManagementPlanBoundaries.UpdateBoundary(DbContext, waterQualityManagementPlanID, dto.GeometryAsGeoJson);
            await WaterQualityManagementPlanParcels.RebuildForWaterQualityManagementPlan(DbContext, waterQualityManagementPlanID);

            var newBoundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanID(DbContext, waterQualityManagementPlanID);
            var newGeometryNative = newBoundary?.GeometryNative;

            if (!(oldGeometryNative == null && newGeometryNative == null))
            {
                await ModelingEngineUtilities.QueueLGURefreshForArea(oldGeometryNative, newGeometryNative, DbContext);
            }

            var wqmp = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
            await NereidUtilities.MarkWqmpDirty(wqmp, DbContext);

            var response = new WaterQualityManagementPlanBoundaryResponseDto
            {
                BoundaryAsFeatureCollection = WaterQualityManagementPlanBoundaries.GetBoundaryAsFeatureCollection(DbContext, waterQualityManagementPlanID),
                Parcels = WaterQualityManagementPlanParcels.ListAsParcelDisplayDtos(DbContext, waterQualityManagementPlanID),
                CalculatedWQMPAcreage = WaterQualityManagementPlanBoundaries.CalculateAcreage(DbContext, waterQualityManagementPlanID),
                BoundingBox = newBoundary?.Geometry4326 != null ? new BoundingBoxDto(newBoundary.Geometry4326) : new BoundingBoxDto()
            };
            return Ok(response);
        }

        [HttpGet("{waterQualityManagementPlanID}/parcel-ids")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public ActionResult<List<int>> GetParcelIDs([FromRoute] int waterQualityManagementPlanID)
        {
            var parcelIDs = WaterQualityManagementPlanParcels.ListParcelIDsByWaterQualityManagementPlanID(DbContext, waterQualityManagementPlanID);
            return Ok(parcelIDs);
        }

        [HttpPut("{waterQualityManagementPlanID}/parcels")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanBoundaryResponseDto>> UpdateParcels(
            [FromRoute] int waterQualityManagementPlanID, [FromBody] List<int> parcelIDs)
        {
            var oldGeometryNative = await WaterQualityManagementPlanParcels.UpdateParcelsAndRecomputeBoundary(DbContext, waterQualityManagementPlanID, parcelIDs);

            var newBoundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanID(DbContext, waterQualityManagementPlanID);
            var newGeometryNative = newBoundary?.GeometryNative;

            if (!(oldGeometryNative == null && newGeometryNative == null))
            {
                await ModelingEngineUtilities.QueueLGURefreshForArea(oldGeometryNative, newGeometryNative, DbContext);
            }

            var wqmp = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
            await NereidUtilities.MarkWqmpDirty(wqmp, DbContext);

            var response = new WaterQualityManagementPlanBoundaryResponseDto
            {
                BoundaryAsFeatureCollection = WaterQualityManagementPlanBoundaries.GetBoundaryAsFeatureCollection(DbContext, waterQualityManagementPlanID),
                Parcels = WaterQualityManagementPlanParcels.ListAsParcelDisplayDtos(DbContext, waterQualityManagementPlanID),
                CalculatedWQMPAcreage = WaterQualityManagementPlanBoundaries.CalculateAcreage(DbContext, waterQualityManagementPlanID),
                BoundingBox = newBoundary?.Geometry4326 != null ? new BoundingBoxDto(newBoundary.Geometry4326) : new BoundingBoxDto()
            };
            return Ok(response);
        }

        [HttpPost("upload")]
        [AdminFeature]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(200 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 200 * 1024 * 1024)]
        public async Task<ActionResult<WqmpUploadResultDto>> UploadDocument(
            IFormFile file, [FromForm] int stormwaterJurisdictionID, [FromForm] string wqmpName, [FromForm] bool overwrite = false)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file provided.");
            }

            if (string.IsNullOrWhiteSpace(wqmpName))
            {
                return BadRequest("WQMP name is required.");
            }

            wqmpName = wqmpName.Trim();
            if (wqmpName.Length > 100)
            {
                return BadRequest("WQMP name must be 100 characters or fewer.");
            }

            // Reject oversize PDFs up front rather than letting the user upload a file Claude
            // will later refuse during extraction.
            var maxBytes = neptuneConfiguration.Value.MaxExtractablePdfSizeBytes;
            if (file.Length > maxBytes)
            {
                var sizeMB = file.Length / (1024.0 * 1024.0);
                var maxMB = maxBytes / (1024.0 * 1024.0);
                return BadRequest(new { message = $"PDF is {sizeMB:F0} MB. AI extraction supports PDFs up to {maxMB:F0} MB — please re-export or re-scan the document at a lower resolution (recommended: 150 DPI for scanned pages)." });
            }

            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (extension != ".pdf")
            {
                return BadRequest("Only PDF files are accepted.");
            }

            // Check for existing WQMP with the same name in this jurisdiction
            var existing = await WaterQualityManagementPlans.GetByNameAndJurisdiction(DbContext, wqmpName, stormwaterJurisdictionID);
            if (existing != null)
            {
                var isActive = existing.WaterQualityManagementPlanStatusID == (int)WaterQualityManagementPlanStatusEnum.Active;
                if (isActive)
                {
                    return Conflict(new WaterQualityManagementPlanConflictDto
                    {
                        ExistingWaterQualityManagementPlanID = existing.WaterQualityManagementPlanID,
                        ExistingStatus = "Active",
                        CanOverwrite = false,
                        Message = $"An active WQMP named \"{wqmpName}\" already exists in this jurisdiction. It must be deactivated before re-uploading."
                    });
                }

                if (!overwrite)
                {
                    var statusName = existing.WaterQualityManagementPlanStatusID == (int)WaterQualityManagementPlanStatusEnum.Draft ? "Draft" : "Inactive";
                    return Conflict(new WaterQualityManagementPlanConflictDto
                    {
                        ExistingWaterQualityManagementPlanID = existing.WaterQualityManagementPlanID,
                        ExistingStatus = statusName,
                        CanOverwrite = true,
                        Message = $"A WQMP named \"{wqmpName}\" already exists in this jurisdiction (Status: {statusName}). Would you like to overwrite it?"
                    });
                }

                // User confirmed overwrite — delete the existing WQMP
                await WaterQualityManagementPlans.DeleteAsync(DbContext, existing.WaterQualityManagementPlanID);
            }

            // Create Draft WQMP
            var dto = new WaterQualityManagementPlanUpsertDto
            {
                WaterQualityManagementPlanName = wqmpName,
                StormwaterJurisdictionID = stormwaterJurisdictionID,
                WaterQualityManagementPlanStatusID = (int)WaterQualityManagementPlanStatusEnum.Draft,
                WaterQualityManagementPlanModelingApproachID = (int)WaterQualityManagementPlanModelingApproachEnum.Detailed,
                TrashCaptureStatusTypeID = (int)TrashCaptureStatusTypeEnum.NotProvided,
            };
            var wqmpDto = await WaterQualityManagementPlans.CreateAsync(DbContext, dto);

            // Upload file to Azure Blob Storage and create document record.
            // No AI call — extraction is now a second, explicit step triggered from the review page.
            var fileResource = await HttpUtilities.MakeFileResourceFromFormFileAsync(DbContext, HttpContext, azureBlobStorageService, file);
            var document = await WaterQualityManagementPlanDocuments.CreateFromFileResourceAsync(
                DbContext, wqmpDto.WaterQualityManagementPlanID, fileResource.FileResourceID,
                file.FileName, (int)WaterQualityManagementPlanDocumentTypeEnum.FinalWQMP);

            return Ok(new WqmpUploadResultDto
            {
                WaterQualityManagementPlanID = wqmpDto.WaterQualityManagementPlanID,
                WaterQualityManagementPlanDocumentID = document.WaterQualityManagementPlanDocumentID,
            });
        }

        [HttpPost("{waterQualityManagementPlanID}/extract")]
        [AdminFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanExtractionResultDto>> RunExtraction(
            [FromRoute] int waterQualityManagementPlanID)
        {
            // Primary document for the WQMP — uploaded in Step 1 (UploadDocument).
            var document = WaterQualityManagementPlanDocuments.ListByWaterQualityManagementPlanID(DbContext, waterQualityManagementPlanID)
                .FirstOrDefault();
            if (document == null)
            {
                return BadRequest(new { message = "No uploaded document found for this WQMP. Upload a PDF before running extraction." });
            }

            // Proactive size check — bounded to NeptuneConfiguration.MaxExtractablePdfSizeBytes.
            var extractMaxBytes = neptuneConfiguration.Value.MaxExtractablePdfSizeBytes;
            if (document.FileResource.ContentLength > extractMaxBytes)
            {
                var sizeMB = document.FileResource.ContentLength / (1024.0 * 1024.0);
                var maxMB = extractMaxBytes / (1024.0 * 1024.0);
                return BadRequest(new { message = $"This PDF is {sizeMB:F0} MB. AI extraction supports PDFs up to {maxMB:F0} MB — please re-upload a smaller version (re-export or re-scan at a lower resolution, recommended: 150 DPI for scanned pages)." });
            }

            // If a previous extraction (plus any draft overlay) exists, drop it cleanly — the
            // frontend already surfaced a confirm dialog warning that draft edits will be lost.
            await WaterQualityManagementPlanExtractionResults.DeleteByWqmpIDAsync(DbContext, waterQualityManagementPlanID);

            try
            {
                var extractionResult = await wqmpExtractionService.ExtractFromDocument(
                    document.WaterQualityManagementPlanDocumentID, CallingUser.PersonID, HttpContext.RequestAborted);

                var storedResult = new WaterQualityManagementPlanExtractionResult
                {
                    WaterQualityManagementPlanID = waterQualityManagementPlanID,
                    WaterQualityManagementPlanDocumentID = document.WaterQualityManagementPlanDocumentID,
                    ExtractionResultJson = extractionResult.FinalOutput,
                    ExtractedAt = DateTime.UtcNow,
                };
                DbContext.WaterQualityManagementPlanExtractionResults.Add(storedResult);
                await DbContext.SaveChangesAsync();

                var dto = await WaterQualityManagementPlanExtractionResults.GetByWqmpIDAsDtoAsync(DbContext, waterQualityManagementPlanID);
                return Ok(dto);
            }
            catch (Exception ex) when (ex is InvalidOperationException or Anthropic4xxException)
            {
                // Surface actionable Claude / validation errors as 400s. The WQMP + document
                // stay intact so the reviewer can retry once the underlying issue is addressed
                // (smaller re-scan, different file, etc.).
                return BadRequest(new { message = ExtractReadableErrorMessage(ex.Message) });
            }
        }

        // Anthropic SDK errors embed the API response JSON in Message — pull the human-readable
        // `error.message` out so the frontend alert shows "PDF exceeds 32 MB" instead of a wall of JSON.
        private static string ExtractReadableErrorMessage(string rawMessage)
        {
            if (string.IsNullOrEmpty(rawMessage) || !rawMessage.Contains("\"message\""))
            {
                return rawMessage;
            }

            try
            {
                var jsonStart = rawMessage.IndexOf('{');
                if (jsonStart >= 0)
                {
                    using var json = System.Text.Json.JsonDocument.Parse(rawMessage[jsonStart..]);
                    return json.RootElement.GetProperty("error").GetProperty("message").GetString() ?? rawMessage;
                }
            }
            catch
            {
                // Fall through — return the original on any parse failure.
            }
            return rawMessage;
        }

        [HttpGet("{waterQualityManagementPlanID}/extraction-result")]
        [AdminFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanExtractionResultDto>> GetExtractionResult(
            [FromRoute] int waterQualityManagementPlanID)
        {
            // A null result is a valid state (document uploaded but extraction not run yet) —
            // return 200 with a null body so the frontend doesn't need exception-driven flow.
            var dto = await WaterQualityManagementPlanExtractionResults.GetByWqmpIDAsDtoAsync(DbContext, waterQualityManagementPlanID);
            return Ok(dto);
        }

        [HttpPut("{waterQualityManagementPlanID}/extraction-result/draft")]
        [AdminFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult> SaveExtractionResultDraft(
            [FromRoute] int waterQualityManagementPlanID,
            [FromBody] WaterQualityManagementPlanExtractionDraftUpsertDto dto)
        {
            // Validate JSON shape so a malformed payload fails here with a clear error
            // rather than silently storing garbage that breaks draft restoration on reload.
            if (!TryParseJson(dto.DraftOverlayJson))
            {
                return BadRequest("DraftOverlayJson must be valid JSON.");
            }

            try
            {
                await WaterQualityManagementPlanExtractionResults.SaveDraftAsync(
                    DbContext, waterQualityManagementPlanID, dto.DraftOverlayJson, CallingUser.PersonID);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            return NoContent();
        }

        [HttpDelete("{waterQualityManagementPlanID}/extraction-result/draft")]
        [AdminFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult> ClearExtractionResultDraft(
            [FromRoute] int waterQualityManagementPlanID)
        {
            await WaterQualityManagementPlanExtractionResults.ClearDraftAsync(DbContext, waterQualityManagementPlanID);
            return NoContent();
        }

        [HttpPost("{waterQualityManagementPlanID}/extraction-result/approve")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanDto>> ApproveExtractionResult(
            [FromRoute] int waterQualityManagementPlanID,
            [FromBody] WaterQualityManagementPlanUpsertDto dto)
        {
            // Pre-check the extraction result exists and is not already approved BEFORE touching
            // the live WQMP, so we fail fast with no partial writes.
            var existing = await WaterQualityManagementPlanExtractionResults.GetByWqmpIDAsync(DbContext, waterQualityManagementPlanID);
            if (existing == null) return NotFound("No extraction result found for this WQMP.");
            if (existing.ApprovedDate.HasValue) return BadRequest("Extraction result has already been approved.");

            // Wrap the live-WQMP update and the approval stamp in a single transaction so a partial
            // failure can't leave the WQMP modified without the approval being recorded.
            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                var updated = await WaterQualityManagementPlans.UpdateAsync(DbContext, waterQualityManagementPlanID, dto);
                if (updated == null) return NotFound();

                await WaterQualityManagementPlanExtractionResults.MarkApprovedAsync(DbContext, waterQualityManagementPlanID, CallingUser.PersonID);
                await transaction.CommitAsync();
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }

        private static bool TryParseJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                return true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }
    }
}
