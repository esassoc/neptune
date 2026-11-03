using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Helpers;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.Common;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptune.API.Controllers;

[ApiController]
[Route("trash-results-by-jurisdiction/{jurisdictionID}")]
public class TrashGeneratingUnitByStormwaterJurisdictionController(
    NeptuneDbContext dbContext,
    ILogger<TrashGeneratingUnitController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<TrashGeneratingUnitController>(dbContext, logger, neptuneConfiguration)
{

    [HttpGet("area-based-results-calculations")]
    [AllowAnonymous]
    [EntityNotFound(typeof(StormwaterJurisdiction), "jurisdictionID")]
    public async Task<ActionResult<AreaBasedAcreCalculationsDto>> GetAreaBasedResultsCalculations([FromRoute] int jurisdictionID)
    {
        var trashGeneratingUnits = GetRelevantTrashGeneratingUnitsForCalculations(jurisdictionID);
        var landUseAcres = await GetPhaseIMS4LandUseAcresByTypeAsync(jurisdictionID);

        var fullTrashCapturePLU = trashGeneratingUnits.FullTrashCaptureAcreage(true);
        var partialTrashCapturePLU = trashGeneratingUnits.PartialTrashCaptureAcreage(true);

        var totalAcresCapturedPLU = fullTrashCapturePLU + partialTrashCapturePLU;

        var totalPLUAcres = landUseAcres.Where(x => x.PriorityLandUseTypeID != (int)PriorityLandUseTypeEnum.ALU).Sum(x => x.Acres);

        var untreatedPLU = totalPLUAcres != 0 ?  totalPLUAcres - totalAcresCapturedPLU : 0;
        // Round after used in calculation
        fullTrashCapturePLU = Math.Round(fullTrashCapturePLU, 0);
        partialTrashCapturePLU = Math.Round(partialTrashCapturePLU, 0);



        var fullTrashCaptureALU = trashGeneratingUnits.FullTrashCaptureAcreage(false);
        var partialTrashCaptureALU = trashGeneratingUnits.PartialTrashCaptureAcreage(false);

        var totalAcresCapturedALU = fullTrashCaptureALU + partialTrashCaptureALU;

        var totalALUAcres = landUseAcres.Where(x => x.PriorityLandUseTypeID == (int)PriorityLandUseTypeEnum.ALU).Sum(x => x.Acres);
        var untreatedALU = totalALUAcres != 0 ? totalALUAcres - totalAcresCapturedALU : 0;
        // Round after used in calculation
        fullTrashCaptureALU = Math.Round(fullTrashCaptureALU, 0);
        partialTrashCaptureALU = Math.Round(partialTrashCaptureALU, 0);

        var areaBasedAcreCalculationsDto = new AreaBasedAcreCalculationsDto
        {
            FullTrashCaptureAcreagePLU = fullTrashCapturePLU,
            PartialTrashCaptureAcreagePLU = partialTrashCapturePLU,
            UntreatedAcreagePLU = untreatedPLU,
            FullTrashCaptureAcreageALU = fullTrashCaptureALU,
            PartialTrashCaptureAcreageALU = partialTrashCaptureALU,
            UntreatedAcreageALU = untreatedALU
        };
        return Ok(areaBasedAcreCalculationsDto);
    }

    private List<TrashGeneratingUnit> GetRelevantTrashGeneratingUnitsForCalculations(int stormwaterJurisdictionID)
    {
        var trashGeneratingUnits = DbContext.TrashGeneratingUnits
            .Include(x => x.LandUseBlock)
            .Include(x => x.OnlandVisualTrashAssessmentArea).ThenInclude(x => x.OnlandVisualTrashAssessments)
            .Include(x => x.Delineation)
            .ThenInclude(x => x.TreatmentBMP)
            .Include(x => x.WaterQualityManagementPlan)
            .AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID && x.LandUseBlock != null && x.LandUseBlock.PermitTypeID == (int)PermitTypeEnum.PhaseIMS4)
            .ToList();
        return trashGeneratingUnits;
    }


    // NPT-1126: Phase I MS4 Land Use Block acres by land use type, summed in SQL. Shared by the Area-Based and
    // OVTA-Based results so their PLU/ALU totals reconcile. Blocks with no Priority Land Use Type are left out,
    // as the original "PriorityLandUseTypeID != ALU" SQL filter always did.
    private async Task<List<OVTAResultsTableBuilder.LandUseAcres>> GetPhaseIMS4LandUseAcresByTypeAsync(int stormwaterJurisdictionID)
    {
        var landUseAcres = await DbContext.LandUseBlocks.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID && x.PriorityLandUseTypeID != null && x.PermitTypeID == (int)PermitTypeEnum.PhaseIMS4)
            .GroupBy(x => new { x.PriorityLandUseTypeID, x.LandUseForTGR })
            .Select(g => new { g.Key.PriorityLandUseTypeID, g.Key.LandUseForTGR, Area = g.Sum(x => x.LandUseBlockGeometry.Area) })
            .ToListAsync();
        return landUseAcres.Select(x => new OVTAResultsTableBuilder.LandUseAcres(x.PriorityLandUseTypeID, x.LandUseForTGR, x.Area * Constants.SquareMetersToAcres)).ToList();
    }

    // NPT-1128 rework / NPT-1126: the OVTA score columns only need each TGU's land use, its OVTA Area's scores and
    // its area, so it projects those scalars in SQL instead of loading entities.
    // GetRelevantTrashGeneratingUnitsForCalculations pulls every assessment, delineation, BMP and WQMP per TGU,
    // which fans out badly for jurisdictions with many OVTA areas (City of Irvine never finished loading in production).
    private async Task<List<OVTAResultsTableBuilder.TrashGeneratingUnitAcres>> GetTrashGeneratingUnitAcresForOVTACalculationsAsync(int stormwaterJurisdictionID)
    {
        // Untyped blocks are left out here too, matching GetPhaseIMS4LandUseAcresByTypeAsync, so no row has assessed acres over a 0 total.
        var trashGeneratingUnits = await DbContext.TrashGeneratingUnits.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID && x.LandUseBlock != null && x.LandUseBlock.PriorityLandUseTypeID != null
                        && x.LandUseBlock.PermitTypeID == (int)PermitTypeEnum.PhaseIMS4)
            .Select(x => new
            {
                x.LandUseBlock.PriorityLandUseTypeID,
                x.LandUseBlock.LandUseForTGR,
                BaselineScoreID = x.OnlandVisualTrashAssessmentArea == null ? null : x.OnlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentBaselineScoreID,
                ProgressScoreID = x.OnlandVisualTrashAssessmentArea == null ? null : x.OnlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentProgressScoreID,
                Area = x.TrashGeneratingUnitGeometry.Area
            })
            .ToListAsync();
        return trashGeneratingUnits.Select(x => new OVTAResultsTableBuilder.TrashGeneratingUnitAcres(x.PriorityLandUseTypeID, x.LandUseForTGR, x.BaselineScoreID,
            x.ProgressScoreID, x.Area * Constants.SquareMetersToAcres)).ToList();
    }

    // NPT-1126 rework: the assessments view reads Land Use Block geometry intersected with OVTA Areas, not TGUs,
    // which only link areas with 2 or more assessments. Same blocks and filter as GetPhaseIMS4LandUseAcresByTypeAsync,
    // so each row's four columns sum to its Total Mapped Land Use.
    private async Task<List<OVTAResultsTableBuilder.LandUseAssessmentAcres>> GetLandUseAssessmentAcresAsync(int stormwaterJurisdictionID)
    {
        var landUseAssessmentAcres = await DbContext.vLandUseBlockOVTAAssessmentCoverages.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID && x.PriorityLandUseTypeID != null && x.PermitTypeID == (int)PermitTypeEnum.PhaseIMS4)
            .GroupBy(x => new { x.PriorityLandUseTypeID, x.LandUseForTGR })
            .Select(g => new
            {
                g.Key.PriorityLandUseTypeID,
                g.Key.LandUseForTGR,
                OnceOrMore = g.Sum(x => x.AreaAssessedOnceOrMore),
                TwiceOrMore = g.Sum(x => x.AreaAssessedTwiceOrMore),
                ThreeOrMore = g.Sum(x => x.AreaAssessedThreeOrMore)
            })
            .ToListAsync();
        return landUseAssessmentAcres.Select(x => new OVTAResultsTableBuilder.LandUseAssessmentAcres(x.PriorityLandUseTypeID, x.LandUseForTGR,
            x.OnceOrMore * Constants.SquareMetersToAcres, x.TwiceOrMore * Constants.SquareMetersToAcres, x.ThreeOrMore * Constants.SquareMetersToAcres)).ToList();
    }

    [HttpGet("ovta-based-results-calculations")]
    [AllowAnonymous]
    [EntityNotFound(typeof(StormwaterJurisdiction), "jurisdictionID")]
    public async Task<ActionResult<OVTAResultsDto>> GetOVTABasedResultsCalculations([FromRoute] int jurisdictionID)
    {
        var hasLandUseBlocks = await DbContext.LandUseBlocks.AsNoTracking().AnyAsync(x => x.StormwaterJurisdictionID == jurisdictionID);
        var hasOVTAAreas = await DbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().AnyAsync(x => x.StormwaterJurisdictionID == jurisdictionID);
        // Keyed off scored OVTA Areas, not off the Phase I MS4 TGUs below, so the "no OVTA Areas with 2 completed
        // baseline assessments" message is literally true. A jurisdiction whose scored areas fall only on
        // non-Phase-I blocks gets an honest all-zero table instead of that message.
        var hasOVTAResults = await DbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
            .AnyAsync(x => x.StormwaterJurisdictionID == jurisdictionID && x.OnlandVisualTrashAssessmentBaselineScoreID != null);
        var landUseAcres = await GetPhaseIMS4LandUseAcresByTypeAsync(jurisdictionID);
        var trashGeneratingUnitAcres = await GetTrashGeneratingUnitAcresForOVTACalculationsAsync(jurisdictionID);
        var landUseAssessmentAcres = await GetLandUseAssessmentAcresAsync(jurisdictionID);
        var rows = OVTAResultsTableBuilder.BuildRows(landUseAcres, trashGeneratingUnitAcres, landUseAssessmentAcres);

        var areasWithProgressButNoBaseline = await DbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == jurisdictionID && x.OnlandVisualTrashAssessmentProgressScoreID != null && x.OnlandVisualTrashAssessmentBaselineScoreID == null)
            .OrderBy(x => x.OnlandVisualTrashAssessmentAreaName)
            .Select(x => new OVTAResultsAreaDataIssueDto
            {
                OnlandVisualTrashAssessmentAreaID = x.OnlandVisualTrashAssessmentAreaID,
                OnlandVisualTrashAssessmentAreaName = x.OnlandVisualTrashAssessmentAreaName
            })
            .ToListAsync();

        var ovtaResultsDto = new OVTAResultsDto
        {
            Rows = rows,
            AreasWithProgressButNoBaseline = areasWithProgressButNoBaseline,
            HasLandUseBlocks = hasLandUseBlocks,
            HasOVTAAreas = hasOVTAAreas,
            HasOVTAResults = hasOVTAResults,
        };
        return Ok(ovtaResultsDto);
    }


    [HttpGet("load-based-results-calculations")]
    [AllowAnonymous]
    [EntityNotFound(typeof(StormwaterJurisdiction), "jurisdictionID")]
    public async Task<ActionResult<LoadResultsDto>> GetLoadBasedResultsCalculations([FromRoute] int jurisdictionID)
    {
        var vTrashGeneratingUnitLoadStatistics =
            DbContext.vTrashGeneratingUnitLoadStatistics.Where(x => x.StormwaterJurisdictionID == jurisdictionID);

        // Single DB round-trip: compute all three conditional sums in one aggregate
        var aggregates = await vTrashGeneratingUnitLoadStatistics
            .GroupBy(x => 1)
            .Select(g => new
            {
                Full = g.Where(x => x.IsFullTrashCapture && x.BaselineLoadingRate.HasValue)
                    .Sum(x => x.Area * (double)(x.BaselineLoadingRate - TrashGeneratingUnitHelper.FullTrashCaptureLoading) * Constants.SquareMetersToAcres),
                Partial = g.Where(x => x.IsPartialTrashCapture && x.BaselineLoadingRate.HasValue)
                    .Sum(x => x.Area * (double)(x.BaselineLoadingRate - x.CurrentLoadingRate) * Constants.SquareMetersToAcres),
                Ovta = g.Where(x => x.HasBaselineScore == true && x.HasProgressScore == true && x.BaselineLoadingRate.HasValue)
                    .Sum(x => x.Area * (double)(x.BaselineLoadingRate - x.ProgressLoadingRate) * Constants.SquareMetersToAcres)
            })
            .SingleOrDefaultAsync();

        var viaFullCapture = aggregates?.Full ?? 0;
        var viaPartialCapture = aggregates?.Partial ?? 0;
        var viaOVTAs = aggregates?.Ovta ?? 0;

        var totalAchieved = viaFullCapture + viaPartialCapture + viaOVTAs;
        var targetLoadReduction = TrashGeneratingUnitHelper.TargetLoadReduction(DbContext, jurisdictionID, vTrashGeneratingUnitLoadStatistics);

        var loadResultsDto = new LoadResultsDto
        {
            LoadFullCapture = viaFullCapture,
            LoadPartialCapture = viaPartialCapture,
            LoadOVTA = viaOVTAs,
            TotalAchieved = totalAchieved,
            TargetLoadReduction = targetLoadReduction
        };
        return Ok(loadResultsDto);
    }
}
