using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anthropic.Exceptions;
using Hangfire;
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
using Neptune.Jobs.Hangfire;
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
            // NPT-1051: Status transitions across the Active boundary need cleanup + re-solve; the
            // SPA Basics modal hits this endpoint and can flip Active <-> Inactive.
            var oldStatusID = await DbContext.WaterQualityManagementPlans.AsNoTracking()
                .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
                .Select(x => x.WaterQualityManagementPlanStatusID)
                .FirstAsync();
            var updated = await WaterQualityManagementPlans.UpdateAsync(DbContext, waterQualityManagementPlanID, dto);
            if (updated == null) return NotFound();
            if (await WaterQualityManagementPlans.HandleStatusTransitionAsync(DbContext, waterQualityManagementPlanID, oldStatusID))
            {
                BackgroundJob.Enqueue<DeltaSolveJob>(x => x.RunJob());
            }
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
            // Cross-WQMP guard — see Copilot PR #502 review feedback. A caller authorized for
            // wqmp A shouldn't be able to read B's verify by smuggling B's verify ID through
            // A's route.
            if (dto.WaterQualityManagementPlanID != waterQualityManagementPlanID) return NotFound();
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
            var verify = WaterQualityManagementPlanVerifies.GetByID(DbContext, waterQualityManagementPlanVerifyID);
            // Cross-WQMP guard — see Copilot PR #502 review feedback.
            if (verify.WaterQualityManagementPlanID != waterQualityManagementPlanID) return NotFound();
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
            var verify = WaterQualityManagementPlanVerifies.GetByID(DbContext, waterQualityManagementPlanVerifyID);
            // Cross-WQMP guard — see Copilot PR #502 review feedback.
            if (verify.WaterQualityManagementPlanID != waterQualityManagementPlanID) return NotFound();
            await WaterQualityManagementPlanVerifies.DeleteAsync(DbContext, waterQualityManagementPlanVerifyID);
            return NoContent();
        }

        // NPT-995 Round 5: Supporting Documentation upload/delete on a verification. Mirrors the
        // legacy MVC SupportingDocumentation panel (single FileResource per verify). Draft-only —
        // finalized verifications are locked.
        [HttpPost("{waterQualityManagementPlanID}/verifications/{waterQualityManagementPlanVerifyID}/supporting-documentation")]
        [JurisdictionEditFeature]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(500 * 1024 * 1024)]
        [RequestFormLimits(MultipartBodyLengthLimit = 500 * 1024 * 1024)]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        [EntityNotFound(typeof(WaterQualityManagementPlanVerify), "waterQualityManagementPlanVerifyID")]
        public async Task<ActionResult<WaterQualityManagementPlanVerifyDetailDto>> UploadVerificationSupportingDocumentation(
            [FromRoute] int waterQualityManagementPlanID, [FromRoute] int waterQualityManagementPlanVerifyID, IFormFile file)
        {
            var verify = WaterQualityManagementPlanVerifies.GetByID(DbContext, waterQualityManagementPlanVerifyID);
            // Guard against cross-WQMP modification — a caller with edit rights on WQMP A
            // shouldn't be able to mutate WQMP B's verifications by smuggling B's verify ID
            // through A's route. Copilot review on PR #502.
            if (verify.WaterQualityManagementPlanID != waterQualityManagementPlanID)
            {
                return NotFound();
            }
            if (!verify.IsDraft)
            {
                return BadRequest("Supporting Documentation cannot be modified after the verification has been finalized.");
            }
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file was supplied.");
            }
            var validationErrors = FileResources.ValidateFileUpload(file);
            if (validationErrors.Any())
            {
                return BadRequest(string.Join(" ", validationErrors.Select(x => x.Message)));
            }

            var fileResource = await HttpUtilities.MakeFileResourceFromFormFileAsync(DbContext, HttpContext, azureBlobStorageService, file);
            var dto = await WaterQualityManagementPlanVerifies.SetSupportingDocumentationFileResourceAsync(
                DbContext, waterQualityManagementPlanVerifyID, fileResource.FileResourceID);
            return Ok(dto);
        }

        [HttpDelete("{waterQualityManagementPlanID}/verifications/{waterQualityManagementPlanVerifyID}/supporting-documentation")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        [EntityNotFound(typeof(WaterQualityManagementPlanVerify), "waterQualityManagementPlanVerifyID")]
        public async Task<IActionResult> DeleteVerificationSupportingDocumentation(
            [FromRoute] int waterQualityManagementPlanID, [FromRoute] int waterQualityManagementPlanVerifyID)
        {
            var verify = WaterQualityManagementPlanVerifies.GetByID(DbContext, waterQualityManagementPlanVerifyID);
            // Guard against cross-WQMP modification — see upload endpoint above.
            if (verify.WaterQualityManagementPlanID != waterQualityManagementPlanID)
            {
                return NotFound();
            }
            if (!verify.IsDraft)
            {
                return BadRequest("Supporting Documentation cannot be modified after the verification has been finalized.");
            }
            await WaterQualityManagementPlanVerifies.ClearSupportingDocumentationAsync(DbContext, waterQualityManagementPlanVerifyID);
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
            var validationError = QuickBMPs.Validate(quickBMPs);
            if (validationError != null)
            {
                return BadRequest(validationError);
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

        // NPT-984: Create-via-AI is a Manager-level entry point. Previously [AdminFeature]
        // (Admin + SitkaAdmin only) which locked out Jurisdiction Managers even though they
        // own WQMP records for their jurisdictions. Editors stay excluded — creating a new
        // WQMP record is an attestation action above performing field work on an existing one.
        [HttpPost("upload")]
        [JurisdictionManageFeature]
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
                // KE 5/13/26 decision: default to Simplified — see wqmp-modal.component.ts for context.
                WaterQualityManagementPlanModelingApproachID = (int)WaterQualityManagementPlanModelingApproachEnum.Simplified,
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
            catch (OperationCanceledException) when (HttpContext.RequestAborted.IsCancellationRequested)
            {
                // The client disconnected mid-extraction. No diagnostic value in persisting a
                // failure row (the "failure" is the user navigating away), and no response
                // shape matters because the caller is gone. Rethrow so the framework returns
                // ClientClosedRequest semantics.
                throw;
            }
            catch (Exception ex)
            {
                // NPT-1051 rework: catch broadly so any failure leaves a diagnostic trail —
                // the wizard's "Last extraction failed: ..." alert reads this row on next load.
                // Map known user-actionable failures (Anthropic 4xx, our InvalidOperationException
                // pre-checks) to 400; everything else (timeouts, upstream 5xx, network/SSL,
                // JSON parse) is a server-side problem and returns 500 so monitoring/clients
                // don't conflate transient infra issues with validation errors.
                Logger.LogError(ex, "WQMP extraction failed for WQMP={WaterQualityManagementPlanID}", waterQualityManagementPlanID);

                // Store the readable form of the error rather than the raw exception message
                // (Anthropic SDK exceptions embed full JSON dumps in .Message). The toast and
                // the persistent "Last extraction failed: ..." alert both render this directly.
                var readableMessage = ExtractReadableErrorMessage(ex.Message);
                var failureRow = new WaterQualityManagementPlanExtractionResult
                {
                    WaterQualityManagementPlanID = waterQualityManagementPlanID,
                    WaterQualityManagementPlanDocumentID = document.WaterQualityManagementPlanDocumentID,
                    ExtractionResultJson = null,
                    ExtractedAt = DateTime.UtcNow,
                    ErrorMessage = readableMessage,
                    ErrorCode = ex.GetType().Name,
                };
                DbContext.WaterQualityManagementPlanExtractionResults.Add(failureRow);
                await DbContext.SaveChangesAsync();

                var isUserActionable = ex is InvalidOperationException or Anthropic4xxException;
                return isUserActionable
                    ? BadRequest(new { message = readableMessage })
                    : StatusCode(StatusCodes.Status500InternalServerError, new { message = readableMessage });
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


        // NPT-1051: Section-save endpoints for the AI wizard (Location / Basics / BMPs).
        //
        // Reframes the AI flow as another data-entry method peer to the modal CRUD editors —
        // overwrite semantics, no draft-overlay round-trip, no per-field auto-save. The wizard
        // builds a complete WaterQualityManagementPlanUpsertDto by overlaying its per-field state
        // (pending → AI value, accepted → AI value, edited → user value, rejected → live value)
        // onto the live WQMP. Server-side it's just UpdateAsync (and helpers for parcels / BMPs).
        //
        // Status is intentionally pinned server-side to the live entity's current status —
        // promotion (Draft → Active) goes through the dedicated /promote endpoint, not through
        // section saves.

        [HttpPost("{waterQualityManagementPlanID}/save-location")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanSectionSaveResponseDto>> SaveLocation(
            [FromRoute] int waterQualityManagementPlanID,
            [FromBody] WaterQualityManagementPlanSectionSaveLocationDto dto)
        {
            if (dto?.WaterQualityManagementPlan == null)
            {
                return BadRequest("Save payload missing the WQMP root upsert.");
            }

            var current = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
            dto.WaterQualityManagementPlan.WaterQualityManagementPlanStatusID = current.WaterQualityManagementPlanStatusID;

            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                var updated = await WaterQualityManagementPlans.UpdateAsync(DbContext, waterQualityManagementPlanID, dto.WaterQualityManagementPlan);
                if (updated == null) return NotFound();

                var oldGeometryNative = await WaterQualityManagementPlanParcels.UpdateParcelsAndRecomputeBoundary(DbContext, waterQualityManagementPlanID, dto.ParcelIDs ?? new List<int>());

                var newBoundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanID(DbContext, waterQualityManagementPlanID);
                var newGeometryNative = newBoundary?.GeometryNative;
                if (!(oldGeometryNative == null && newGeometryNative == null))
                {
                    await ModelingEngineUtilities.QueueLGURefreshForArea(oldGeometryNative, newGeometryNative, DbContext);
                }

                var wqmp = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
                await NereidUtilities.MarkWqmpDirty(wqmp, DbContext);

                await transaction.CommitAsync();
                return Ok(new WaterQualityManagementPlanSectionSaveResponseDto
                {
                    WaterQualityManagementPlan = updated,
                });
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("{waterQualityManagementPlanID}/save-basics")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanSectionSaveResponseDto>> SaveBasics(
            [FromRoute] int waterQualityManagementPlanID,
            [FromBody] WaterQualityManagementPlanUpsertDto dto)
        {
            if (dto == null)
            {
                return BadRequest("Save payload missing the WQMP root upsert.");
            }

            var current = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
            dto.WaterQualityManagementPlanStatusID = current.WaterQualityManagementPlanStatusID;

            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                var updated = await WaterQualityManagementPlans.UpdateAsync(DbContext, waterQualityManagementPlanID, dto);
                if (updated == null) return NotFound();

                var wqmp = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
                await NereidUtilities.MarkWqmpDirty(wqmp, DbContext);

                await transaction.CommitAsync();
                return Ok(new WaterQualityManagementPlanSectionSaveResponseDto
                {
                    WaterQualityManagementPlan = updated,
                });
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }

        // NPT-1051: Promote a Draft WQMP to Active. Active is the binding-legal-record state —
        // promotion is the act of declaring "this transcription faithfully represents the
        // agreement." Validates required fields up-front; returns 400 with the missing-field
        // list so the SPA can render an actionable error toast.
        [HttpPost("{waterQualityManagementPlanID}/promote")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanDto>> Promote([FromRoute] int waterQualityManagementPlanID)
        {
            var entity = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
            if (entity.WaterQualityManagementPlanStatusID != (int)WaterQualityManagementPlanStatusEnum.Draft)
            {
                return BadRequest("WQMP must be in Draft status to promote to Active.");
            }

            var missingFields = WaterQualityManagementPlans.ValidateForPromote(entity);
            if (missingFields.Count > 0)
            {
                return BadRequest(new { MissingFields = missingFields });
            }

            var oldStatusID = entity.WaterQualityManagementPlanStatusID;
            entity.WaterQualityManagementPlanStatusID = (int)WaterQualityManagementPlanStatusEnum.Active;
            await DbContext.SaveChangesAsync();

            // Mark the WQMP dirty + kick off the incremental solve immediately. Without the
            // enqueue, the dirty marker would only be consumed when HRURefreshJob next runs
            // (hours away on the schedule), so a freshly-promoted WQMP would show no modeling
            // results until then.
            if (await WaterQualityManagementPlans.HandleStatusTransitionAsync(DbContext, waterQualityManagementPlanID, oldStatusID))
            {
                BackgroundJob.Enqueue<DeltaSolveJob>(x => x.RunJob());
            }

            var dto = await WaterQualityManagementPlans.GetByIDAsDtoAsync(DbContext, waterQualityManagementPlanID);
            return Ok(dto);
        }

        [HttpPost("{waterQualityManagementPlanID}/save-bmps")]
        [JurisdictionEditFeature]
        [EntityNotFound(typeof(WaterQualityManagementPlan), "waterQualityManagementPlanID")]
        public async Task<ActionResult<WaterQualityManagementPlanSectionSaveResponseDto>> SaveBmps(
            [FromRoute] int waterQualityManagementPlanID,
            [FromBody] List<QuickBMPUpsertDto> quickBMPs)
        {
            // Mirror ApproveExtractionResult: rows missing required fields land in report.Skipped
            // and surface as a warning toast on the SPA — they don't hard-fail the section save.
            // Real validation errors (% out of range, duplicate names, etc.) still reject up-front
            // so a bad payload doesn't leave the WQMP partially updated.
            var (quickBMPsForValidation, _) = QuickBMPs.PartitionForMerge(quickBMPs);
            var quickBMPValidationError = QuickBMPs.Validate(quickBMPsForValidation);
            if (quickBMPValidationError != null)
            {
                return BadRequest(quickBMPValidationError);
            }

            await using var transaction = await DbContext.Database.BeginTransactionAsync();
            try
            {
                var report = await QuickBMPs.MergeWithReportAsync(DbContext, waterQualityManagementPlanID, quickBMPs ?? new List<QuickBMPUpsertDto>());

                var wqmp = WaterQualityManagementPlans.GetByIDWithChangeTracking(DbContext, waterQualityManagementPlanID);
                await NereidUtilities.MarkWqmpDirty(wqmp, DbContext);
                var updated = await WaterQualityManagementPlans.GetByIDAsDtoAsync(DbContext, waterQualityManagementPlanID);

                await transaction.CommitAsync();
                return Ok(new WaterQualityManagementPlanSectionSaveResponseDto
                {
                    WaterQualityManagementPlan = updated,
                    SkippedBMPs = report.Skipped,
                });
            }
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }

    }
}
