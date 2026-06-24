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
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptune.API.Controllers;

[ApiController]
[Route("treatment-bmps")]
public class TreatmentBMPController(
    NeptuneDbContext dbContext,
    ILogger<TreatmentBMPController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration,
    GDALAPIService gdalApiService)
    : SitkaController<TreatmentBMPController>(dbContext, logger, neptuneConfiguration)
{
    [HttpPost]
    [JurisdictionEditFeature]
    public async Task<ActionResult<TreatmentBMPDto>> Create([FromBody] TreatmentBMPCreateDto treatmentBMPCreateDto)
    {
        var errors = await TreatmentBMPs.ValidateCreateAsync(DbContext, treatmentBMPCreateDto);
        errors.ForEach(e => ModelState.AddModelError(e.Type, e.Message));

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var treatmentBMPDto = await TreatmentBMPs.CreateAsync(DbContext, treatmentBMPCreateDto, CallingUser);
        return CreatedAtAction(nameof(GetByID), new { treatmentBMPID = treatmentBMPDto.TreatmentBMPID }, treatmentBMPDto);
    }

    [HttpGet]
    [AllowAnonymous]
    [OptionalAuth]
    public async Task<ActionResult<List<TreatmentBMPGridDto>>> List()
    {
        var stormwaterJurisdictionIDsPersonCanView = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);

        // Public (anonymous) and unassigned users may only see verified BMPs, matching the legacy
        // Find-a-BMP behavior (PersonModelExtensions.GetTreatmentBmpsPersonCanView). The jurisdiction
        // filter above already excludes jurisdictions whose public BMP visibility is None; this adds
        // the per-BMP verified gate the SPA list endpoint was missing. Anonymous callers are an
        // Unassigned-role PersonDto sentinel (see UserContext), so this one check covers both. (NPT-1079)
        var publicUser = CallingUser.RoleID == (int)RoleEnum.Unassigned;

        var entities = await DbContext.vTreatmentBMPDetaileds.AsNoTracking()
            .Where(x => stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID))
            .Where(x => !publicUser || x.InventoryIsVerified)
            .ToListAsync();

        var treatmentBMPGridDtos = entities.Select(x => x.AsGridDto()).ToList();
        return Ok(treatmentBMPGridDtos);
    }

    [HttpGet("verified/feature-collection")]
    [AllowAnonymous]
    [OptionalAuth]
    public async Task<ActionResult<FeatureCollection>> ListInventoryVerifiedTreatmentBMPsAsFeatureCollection()
    {
        var featureCollection = await TreatmentBMPs.ListInventoryIsVerifiedByPersonAsFeatureCollectionAsync(DbContext, CallingUser);
        return Ok(featureCollection);
    }

    [HttpGet("jurisdictions/{jurisdictionID}/verified/feature-collection")]
    [AllowAnonymous]
    [OptionalAuth]
    public async Task<ActionResult<FeatureCollection>> ListInventoryVerifiedTreatmentBMPsByJurisdictionIDAsFeatureCollection([FromRoute] int jurisdictionID)
    {
        var featureCollection = await TreatmentBMPs.ListInventoryIsVerifiedByPersonAndJurisdictionIDAsFeatureCollectionAsync(DbContext, CallingUser, jurisdictionID);
        return Ok(featureCollection);
    }

    [HttpGet("planned-projects")]
    [JurisdictionEditFeature]
    public async Task<ActionResult<List<TreatmentBMPDisplayDto>>> ListPlannedProjects()
    {
        var treatmentBMPDisplayDtos = await TreatmentBMPs.ListWithProjectByPersonAsDisplayDtoAsync(DbContext, CallingUser);
        return Ok(treatmentBMPDisplayDtos);
    }

    [HttpGet("for-delineation-map")]
    [UserViewFeature]
    public async Task<ActionResult<List<TreatmentBMPDelineationMapDto>>> ListForDelineationMap()
    {
        var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
        var dtos = await TreatmentBMPs.ListForDelineationMapAsync(DbContext, currentPerson);
        return Ok(dtos);
    }

    [HttpGet("octa-m2-tier2-grant-program")]
    [JurisdictionEditFeature]
    public async Task<ActionResult<List<TreatmentBMPDisplayDto>>> ListOCTAM2Tier2GrantProgramTreatmentBMPs()
    {
        var featureCollection = await TreatmentBMPs.ListWithOCTAM2Tier2GrantProgramByPersonAsDisplayDtoAsync(DbContext, CallingUser);
        return Ok(featureCollection);
    }

    [HttpPut("{treatmentBMPID}/treatment-bmp-types/{treatmentBMPTypeID}")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public ActionResult<int> ChangeTreatmentBMPType([FromRoute] int treatmentBMPID, int treatmentBMPTypeID)
    {
        var updatedTreatmentBMPModelingTypeID = TreatmentBMPs.ChangeTreatmentBMPType(DbContext, treatmentBMPID, treatmentBMPTypeID);
        var projectID = TreatmentBMPs.GetByTreatmentBMPID(DbContext, treatmentBMPID)!.ProjectID;
        if (projectID != null)
        {
            Projects.SetUpdatePersonAndDate(DbContext, (int)projectID, CallingUser.PersonID);
        }

        return Ok(updatedTreatmentBMPModelingTypeID);
    }

    [HttpGet("{treatmentBMPID}/upstreamRSBCatchmentGeoJSON")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public ActionResult<GeometryGeoJSONAndAreaDto> GetUpstreamRSBCatchmentGeoJSONForTreatmentBMP([FromRoute] int treatmentBMPID)
    {
        var treatmentBMP = TreatmentBMPs.GetByTreatmentBMPID(DbContext, treatmentBMPID);
        var delineation = Delineations.GetByTreatmentBMPID(DbContext, treatmentBMPID);
        var regionalSubbasin = RegionalSubbasins.GetFirstByContainsGeometry(DbContext, treatmentBMP.LocationPoint);
        var geometries = RegionalSubbasins.GetUpstreamCatchmentGeometry4326GeoJSONAndArea(DbContext, regionalSubbasin.RegionalSubbasinID, treatmentBMPID, delineation?.DelineationID);
        return Ok(geometries);
    }

    [HttpGet("{treatmentBMPID}")]
    [AllowAnonymous]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<TreatmentBMPDto>> GetByID([FromRoute] int treatmentBMPID)
    {
        var treatmentBMPDto = await TreatmentBMPs.GetByIDAsDtoAsync(DbContext, treatmentBMPID);
        return Ok(treatmentBMPDto);
    }

    /// <summary>
    /// NPT-1068: Modeled BMP Performance panel on the SPA detail page. Returns the per-BMP
    /// Nereid load-reducing result summed from <c>vLoadReducingResults</c>; returns 200 with a
    /// null body when Nereid hasn't produced a non-baseline result yet so the SPA can fall back
    /// to the "missing fields" / "not modeled" message without a console-spamming 404.
    /// </summary>
    [HttpGet("{treatmentBMPID}/load-reducing-result")]
    [AllowAnonymous]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<ProjectLoadReducingResultDto?>> GetLoadReducingResult([FromRoute] int treatmentBMPID)
    {
        var dto = await TreatmentBMPModeledPerformance.GetByBMPIDAsync(DbContext, treatmentBMPID);
        return Ok(dto);
    }

    /// <summary>
    /// NPT-1068: Sitka-admin-only "Latest Nereid Request / Response" download links on the
    /// Modeled BMP Performance panel. Returns the BMP's most recent NereidLog row's raw
    /// request/response JSON strings so the SPA can wrap them in a blob and trigger download
    /// (legacy MVC inlined the JSON into a script tag and did the same thing client-side).
    /// Returns 200 with a null body when the BMP has no NereidLog yet — the SPA suppresses the
    /// download links in that case (avoids a noisy 404 in devtools for the common new-BMP case).
    /// </summary>
    [HttpGet("{treatmentBMPID}/latest-nereid-log")]
    [SitkaAdminFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<TreatmentBMPNereidLogContentDto?>> GetLatestNereidLog([FromRoute] int treatmentBMPID)
    {
        var dto = await NereidLogs.GetLatestForTreatmentBMPAsDtoAsync(DbContext, treatmentBMPID);
        return Ok(dto);
    }

    [HttpPut("{treatmentBMPID}/basic-info")]
    [AllowAnonymous]
    [OptionalAuth]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<TreatmentBMPDto>> UpdateBasicInfo([FromRoute] int treatmentBMPID, [FromBody] TreatmentBMPBasicInfoUpdateDto updateDto)
    {
        var errors = await TreatmentBMPs.ValidateUpdateBasicInfoAsync(DbContext, treatmentBMPID, updateDto);
        errors.ForEach(e => ModelState.AddModelError(e.Type, e.Message));

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var treatmentBMPDto = await TreatmentBMPs.UpdateBasicInfoAsync(DbContext, treatmentBMPID, updateDto, CallingUser);
        return Ok(treatmentBMPDto);
    }

    [HttpPut("{treatmentBMPID}/type")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<TreatmentBMPDto>> UpdateType([FromRoute] int treatmentBMPID, [FromBody] TreatmentBMPTypeUpdateDto typeUpdateDto)
    {
        var errors = await TreatmentBMPs.ValidateUpdateTypeAsync(DbContext, treatmentBMPID, typeUpdateDto);
        errors.ForEach(e => ModelState.AddModelError(e.Type, e.Message));

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var treatmentBMPDto = await TreatmentBMPs.UpdateTypeAsync(DbContext, treatmentBMPID, typeUpdateDto, CallingUser);
        return Ok(treatmentBMPDto);
    }

    [HttpPut("{treatmentBMPID}/location")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<TreatmentBMPDto>> UpdateLocation([FromRoute] int treatmentBMPID, [FromBody] TreatmentBMPLocationUpdateDto locationUpdateDto)
    {
        var errors = await TreatmentBMPs.ValidateUpdateLocationAsync(DbContext, treatmentBMPID, locationUpdateDto);
        errors.ForEach(e => ModelState.AddModelError(e.Type, e.Message));
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var treatmentBMPDto = await TreatmentBMPs.UpdateLocationAsync(DbContext, treatmentBMPID, locationUpdateDto, CallingUser);
        return Ok(treatmentBMPDto);
    }

    [HttpPut("{treatmentBMPID}/custom-attribute-type-purposes/{customAttributeTypePurposeID}/custom-attributes")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<List<CustomAttributeDto>>> UpdateCustomAttributes([FromRoute] int treatmentBMPID, [FromRoute] int customAttributeTypePurposeID, [FromBody] List<CustomAttributeUpsertDto> customAttributes)
    {
        var customAttributePurposeType = CustomAttributeTypePurpose.All.FirstOrDefault(x => x.CustomAttributeTypePurposeID == customAttributeTypePurposeID);
        if (customAttributePurposeType == null)
        {
            return NotFound();
        }

        var errors = await CustomAttributes.ValidateUpdateCustomAttributesAsync(DbContext, treatmentBMPID, customAttributeTypePurposeID, customAttributes);
        errors.ForEach(e => ModelState.AddModelError(e.Type, e.Message));
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updatedCustomAttributes = await CustomAttributes.UpdateCustomAttributesAsync(DbContext, treatmentBMPID, customAttributeTypePurposeID, customAttributes, CallingUser);
        return Ok(updatedCustomAttributes);
    }

    [HttpPut("{treatmentBMPID}/upstream-bmp")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<TreatmentBMPDto>> UpdateUpstreamBMP([FromRoute] int treatmentBMPID, [FromBody] TreatmentBMPUpstreamBMPUpdateDto upstreamBMPUpdateDto)
    {
        var errors = await TreatmentBMPs.ValidateUpdateUpstreamBMPAsync(DbContext, treatmentBMPID, upstreamBMPUpdateDto);
        errors.ForEach(e => ModelState.AddModelError(e.Type, e.Message));

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var treatmentBMPDto = await TreatmentBMPs.UpdateUpstreamBMPAsync(DbContext, treatmentBMPID, upstreamBMPUpdateDto);
        return Ok(treatmentBMPDto);
    }

    [HttpDelete("{treatmentBMPID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<IActionResult> Delete([FromRoute] int treatmentBMPID)
    {
        var treatmentBMP = TreatmentBMPs.GetByIDWithChangeTracking(DbContext, treatmentBMPID);

        var delineation = Delineations.GetByTreatmentBMPIDWithChangeTracking(DbContext, treatmentBMP.TreatmentBMPID);
        var delineationGeometry = delineation?.DelineationGeometry;
        var isDelineationDistributed = delineation != null && delineation.DelineationTypeID == (int)DelineationTypeEnum.Distributed;

        await EFModels.Nereid.NereidUtilities.MarkDownstreamNodeDirty(treatmentBMP, DbContext);

        // Remove upstream references
        foreach (var downstreamBMP in treatmentBMP.InverseUpstreamBMP)
        {
            downstreamBMP.UpstreamBMPID = null;
        }
        await DbContext.SaveChangesAsync();

        await treatmentBMP.DeleteFull(DbContext);

        // Queue LGU refresh if needed
        if (isDelineationDistributed && delineationGeometry != null)
        {
            await ModelingEngineUtilities.QueueLGURefreshForArea(delineationGeometry, null, DbContext);
        }

        return NoContent();
    }

    [HttpGet("{treatmentBMPID}/hru-characteristics")]
    [SitkaAdminFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<List<HRUCharacteristicDto>>> ListHRUCharacteristics([FromRoute] int treatmentBMPID)
    {
        var treatmentBMP = TreatmentBMPs.GetByID(DbContext, treatmentBMPID);
        var treatmentBMPTree = DbContext.vTreatmentBMPUpstreams.AsNoTracking()
            .Single(x => x.TreatmentBMPID == treatmentBMP.TreatmentBMPID);
        var upstreamestBMP = treatmentBMPTree.UpstreamBMPID.HasValue ? TreatmentBMPs.GetByID(DbContext, treatmentBMPTree.UpstreamBMPID) : null;
        var delineation = Delineations.GetByTreatmentBMPID(DbContext, upstreamestBMP?.TreatmentBMPID ?? treatmentBMP.TreatmentBMPID);
        var hruCharacteristics = await vHRUCharacteristics.ListByTreatmentBMPAsDtoAsync(DbContext, upstreamestBMP ?? treatmentBMP, delineation);
        return Ok(hruCharacteristics);
    }

    [HttpGet("{treatmentBMPID}/custom-attributes")]
    [AllowAnonymous]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public ActionResult<List<CustomAttributeDto>> ListCustomAttributes([FromRoute] int treatmentBMPID)
    {
        var customAttributes = CustomAttributes.ListByTreatmentBMPIDAsDto(DbContext, treatmentBMPID);
        return Ok(customAttributes);
    }

    [HttpGet("{treatmentBMPID}/field-visits")]
    [AllowAnonymous]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public ActionResult<List<FieldVisitDto>> FieldVisitGridJsonData([FromRoute] int treatmentBMPID)
    {
        var fieldVisits = vFieldVisitDetaileds.ListAsDtoByTreatmentBMPID(DbContext, treatmentBMPID);
        return Ok(fieldVisits);
    }

    [HttpGet("modeling-attributes")]
    [AllowAnonymous]
    [OptionalAuth]
    public async Task<ActionResult<List<TreatmentBMPModelingAttributesDto>>> ListWithModelingAttributes()
    {
        var stormwaterJurisdictionIDsPersonCanView = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var dtos = await TreatmentBMPs.ListWithModelingAttributesAsync(DbContext, stormwaterJurisdictionIDsPersonCanView);
        return Ok(dtos);
    }

    [HttpGet("{treatmentBMPID}/delineation-errors")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public ActionResult<TreatmentBMPDelineationErrorsDto> GetDelineationErrors([FromRoute] int treatmentBMPID)
    {
        var treatmentBMP = DbContext.TreatmentBMPs
            .Include(x => x.Delineation).ThenInclude(delineation => delineation.DelineationOverlapDelineations).ThenInclude(overlap => overlap.OverlappingDelineation).ThenInclude(d => d.TreatmentBMP)
            .Single(x => x.TreatmentBMPID == treatmentBMPID);

        var upstreamestBMP = TreatmentBMPs.GetUpstreamestTreatmentBMP(DbContext, treatmentBMPID);
        var delineation = upstreamestBMP?.Delineation ?? treatmentBMP.Delineation;

        var dto = new TreatmentBMPDelineationErrorsDto
        {
            HasDiscrepancies = delineation != null && delineation.HasDiscrepancies,
            OverlappingTreatmentBMPs = new List<TreatmentBMPDisplayDto>()
        };

        if (delineation != null && delineation.DelineationOverlapDelineations.Any())
        {
            dto.OverlappingTreatmentBMPs = delineation.DelineationOverlapDelineations
                .Select(x => x.OverlappingDelineation.TreatmentBMP.AsDisplayDto(null))
                .ToList();
        }

        return Ok(dto);
    }

    [HttpGet("{treatmentBMPID}/parameterization-errors")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public ActionResult<TreatmentBMPParameterizationErrorsDto> GetParameterizationErrors([FromRoute] int treatmentBMPID)
    {
        var treatmentBMP = DbContext.TreatmentBMPs
            .Include(x => x.WaterQualityManagementPlan)
            .Include(x => x.TreatmentBMPType)
            .Single(x => x.TreatmentBMPID == treatmentBMPID);

        // A downstream BMP inherits its upstream BMP's delineation (resolved via vTreatmentBMPUpstreams, the same
        // upstream lookup TreatmentBMPs.GetByIDAsync uses). Check whether a delineation row EXISTS for that effective
        // BMP — existence only, matching the prior `delineation != null` behavior; verification status is not part of
        // this alert. This stops a BMP whose upstream already has one (e.g. BMP 318 -> upstream 354) from wrongly
        // showing the "delineation required" alert.
        var upstreamRow = DbContext.vTreatmentBMPUpstreams.AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPID == treatmentBMPID);
        var delineationBMPID = upstreamRow?.UpstreamBMPID ?? treatmentBMPID;
        var hasDelineation = DbContext.Delineations.AsNoTracking().Any(x => x.TreatmentBMPID == delineationBMPID);
        var linkToDelineationMap = !hasDelineation && (treatmentBMP.UpstreamBMPID == null);
        WaterQualityManagementPlanDisplayDto? simplifiedWQMP = null;
        if (treatmentBMP.WaterQualityManagementPlan != null && treatmentBMP.WaterQualityManagementPlan.WaterQualityManagementPlanModelingApproach == WaterQualityManagementPlanModelingApproach.Simplified)
        {
            simplifiedWQMP = new WaterQualityManagementPlanDisplayDto
            {
                WaterQualityManagementPlanID = treatmentBMP.WaterQualityManagementPlan.WaterQualityManagementPlanID,
                WaterQualityManagementPlanName = treatmentBMP.WaterQualityManagementPlan.WaterQualityManagementPlanName
            };
        }

        var missingModelAttributes = false;
        if (!linkToDelineationMap && simplifiedWQMP == null)
        {
            var treatmentBMPModelingAttribute = vTreatmentBMPModelingAttributes.GetByTreatmentBMPID(DbContext, treatmentBMP.TreatmentBMPID);
            missingModelAttributes = treatmentBMP.TreatmentBMPType.MissingModelingAttributes(treatmentBMPModelingAttribute).Any();
        }

        var dto = new TreatmentBMPParameterizationErrorsDto
        {
            HasDelineation = hasDelineation,
            LinkToDelineationMap = linkToDelineationMap,
            SimplifiedWQMP = simplifiedWQMP,
            MissingModelAttributes = missingModelAttributes
        };

        return Ok(dto);
    }

    [HttpGet("{treatmentBMPID}/upstreamest-errors")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public ActionResult<TreatmentBMPUpstreamestErrorsDto> GetUpstreamestErrors([FromRoute] int treatmentBMPID)
    {
        var treatmentBMPTree = DbContext.vTreatmentBMPUpstreams.AsNoTracking()
            .Single(x => x.TreatmentBMPID == treatmentBMPID);

        var upstreamestBMP = treatmentBMPTree.UpstreamBMPID.HasValue ? TreatmentBMPs.GetByID(DbContext, treatmentBMPTree.UpstreamBMPID) : null;
        var isUpstreamestBMPAnalyzedInModelingModule = upstreamestBMP != null && upstreamestBMP.TreatmentBMPType.IsAnalyzedInModelingModule;
        
        var dto = new TreatmentBMPUpstreamestErrorsDto
        {
            UpstreamestBMP = upstreamestBMP?.AsDisplayDto(null),
            IsUpstreamestBMPAnalyzedInModelingModule = isUpstreamestBMPAnalyzedInModelingModule
        };

        return Ok(dto);
    }

    [HttpGet("{treatmentBMPID}/other-treatment-bmps-in-regional-subbasin")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public ActionResult<List<TreatmentBMPDisplayDto>> ListOtherTreatmentBMPsInRegionalSubbasin([FromRoute] int treatmentBMPID)
    {
        var treatmentBMP = TreatmentBMPs.GetByID(DbContext, treatmentBMPID);
        var subbasin = treatmentBMP.GetRegionalSubbasin(DbContext);
        if (subbasin != null)
        {
            var otherTreatmentBMPs = subbasin.GetTreatmentBMPs(DbContext)
                .Where(x => x.TreatmentBMPID != treatmentBMP.TreatmentBMPID)
                .Select(x => x.AsDisplayDto(null))
                .OrderBy(x => x.DisplayName)
                .ToList();

            return Ok(otherTreatmentBMPs);
        }

        return Ok(new List<TreatmentBMPDisplayDto>());
    }

    [HttpPut("{treatmentBMPID}/queue-refresh-land-use")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<List<TreatmentBMPDisplayDto>>> QueueRefreshLandUse([FromRoute] int treatmentBMPID)
    {
        var delineation = Delineations.GetByTreatmentBMPID(DbContext, treatmentBMPID);
        if (delineation == null)
        {
            ModelState.AddModelError("Delineation Required", "Treatment BMPs require a delineation in order to refresh their Land Use.");
            return BadRequest(ModelState);
        }

        if (delineation.DelineationTypeID != DelineationType.Distributed.DelineationTypeID)
        {
            ModelState.AddModelError("Delineation is Distributed", "This delineation cannot be refreshed because it is not distributed.");
            return BadRequest(ModelState);
        }

        await ModelingEngineUtilities.QueueLGURefreshForArea(delineation.DelineationGeometry, null, DbContext);

        return Ok();
    }

    [HttpPost("bulk-upload")]
    [AdminFeature]
    [RequestSizeLimit(100_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<TreatmentBMPCsvUploadResultDto>> BulkUpload([FromForm] TreatmentBMPCsvUploadFormDto form)
    {
        var result = new TreatmentBMPCsvUploadResultDto();

        if (form.File == null || form.File.Length == 0)
        {
            result.Errors.Add("Please select a CSV file to upload.");
            return Ok(result);
        }

        var treatmentBMPType = TreatmentBMPTypes.GetByID(DbContext, form.TreatmentBMPTypeID);
        if (treatmentBMPType == null)
        {
            result.Errors.Add("The selected Treatment BMP Type was not found.");
            return Ok(result);
        }

        await using var stream = form.File.OpenReadStream();
        var treatmentBMPs = TreatmentBMPCsvParserHelper.CSVUpload(DbContext, stream, treatmentBMPType,
            out var errorList, out var customAttributes, out var customAttributeValues);

        if (errorList.Any())
        {
            result.Errors = errorList;
            return Ok(result);
        }

        var treatmentBmpsAdded = treatmentBMPs.Where(x => x.TreatmentBMPID <= 0).ToList();
        var treatmentBmpsUpdated = treatmentBMPs.Where(x => x.TreatmentBMPID > 0).ToList();

        // NPT-1069: seed default Benchmark & Threshold rows on each newly-added BMP. Templates
        // are built once for the type and shared across all new BMPs in this upload; existing
        // BMPs (matched on name + jurisdiction by the parser) are deliberately left untouched so
        // we don't clobber user-edited values. Skip the seed-template query entirely on
        // update-only uploads.
        if (treatmentBmpsAdded.Count > 0)
        {
            var seedTemplates = await TreatmentBMPBenchmarkAndThresholds.BuildSeedTemplatesAsync(DbContext, form.TreatmentBMPTypeID);
            foreach (var newBmp in treatmentBmpsAdded)
            {
                TreatmentBMPBenchmarkAndThresholds.AttachSeedsToBMP(newBmp, seedTemplates);
            }
        }

        await DbContext.TreatmentBMPs.AddRangeAsync(treatmentBmpsAdded);
        await DbContext.CustomAttributes.AddRangeAsync(customAttributes.Where(x => x.CustomAttributeID <= 0));
        await DbContext.CustomAttributeValues.AddRangeAsync(customAttributeValues.Where(x => x.CustomAttributeValueID <= 0));
        await DbContext.SaveChangesAsync();

        // Re-execute model for updated BMPs since they may have been re-parameterized;
        // new BMPs are skipped because they don't have delineations yet.
        await EFModels.Nereid.NereidUtilities.MarkTreatmentBMPDirty(treatmentBmpsUpdated, DbContext);

        result.AddedCount = treatmentBmpsAdded.Count;
        result.UpdatedCount = treatmentBmpsUpdated.Count;
        return Ok(result);
    }

    [HttpGet("download-gdb")]
    [UserViewFeature]
    [Produces("application/zip")]
    public async Task<FileResult> DownloadGdb()
    {
        var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
        var (bytes, fileName) = await TreatmentBMPGdbExport.BuildBMPInventoryGdbExportAsync(DbContext, gdalApiService, currentPerson);
        return File(bytes, "application/zip", fileName);
    }
}