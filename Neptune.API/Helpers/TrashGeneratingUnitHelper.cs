using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.EFModels.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neptune.API.Helpers;


public static class TrashGeneratingUnitHelper
{
    public const decimal FullTrashCaptureLoading = 0m;

    public static double TargetLoadReduction(NeptuneDbContext dbContext, int stormwaterJurisdictionID, IQueryable<vTrashGeneratingUnitLoadStatistic> vTrashGeneratingUnitLoadStatistics)
    {
        // Get only the IDs for land use blocks in the target jurisdiction and permit/priority filters.
        var lubIds = dbContext.LandUseBlocks.AsNoTracking()
            .Where(x => x.PermitTypeID == (int)PermitTypeEnum.PhaseIMS4 &&
                        x.StormwaterJurisdictionID == stormwaterJurisdictionID &&
                        x.PriorityLandUseTypeID != (int)PriorityLandUseTypeEnum.ALU)
            .Select(x => x.LandUseBlockID)
            .ToList();

        if (!lubIds.Any()) return 0;

        // Aggregate vTrashGeneratingUnitLoadStatistics on the DB side for only the relevant land use block IDs
        var lubSums = vTrashGeneratingUnitLoadStatistics
            .Where(x => x.LandUseBlockID.HasValue && lubIds.Contains(x.LandUseBlockID.Value))
            .GroupBy(x => x.LandUseBlockID.Value)
            .Select(g => new
            {
                LandUseBlockID = g.Key,
                Sum = g.Sum(y => y.Area * (double)y.BaselineLoadingRate * Constants.SquareMetersToAcres)
            })
            .ToDictionary(x => x.LandUseBlockID, x => x.Sum);

        double total = 0;

        // Add sums for LandUseBlocks that have aggregated vTGU stats
        if (lubSums.Any())
        {
            total += lubSums.Values.Sum();
        }

        // For LandUseBlocks without aggregated stats, fetch only those IDs and compute fallback locally.
        var missingLubIds = lubIds.Except(lubSums.Keys).ToList();
        if (missingLubIds.Any())
        {
            var missingLubs = dbContext.LandUseBlocks.AsNoTracking()
                .Where(x => missingLubIds.Contains(x.LandUseBlockID))
                .Select(x => new { x.LandUseBlockID, x.LandUseBlockGeometry, x.TrashGenerationRate })
                .ToList();

            foreach (var lub in missingLubs)
            {
                // LandUseBlockGeometry.Area may not be translatable; compute client-side
                var area = lub.LandUseBlockGeometry?.Area ?? 0;
                var trashGenRate = lub.TrashGenerationRate ?? 0;
                total += area * (double)(trashGenRate - FullTrashCaptureLoading) * Constants.SquareMetersToAcres;
            }
        }

        return total;
    }

    public static double EquivalentAreaAcreageFromAssessments(this List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return trashGeneratingUnits.Where(x => x.OnlandVisualTrashAssessmentArea != null &&
            x.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessments.Count >= 2 &&
            x.OnlandVisualTrashAssessmentArea.OnlandVisualTrashAssessments.OrderByDescending(y => y.CompletedDate).Take(2).All(y => y.OnlandVisualTrashAssessmentScoreID == (int)OnlandVisualTrashAssessmentScoreEnum.A) &&
            !x.IsFullTrashCapture() &&
            x.IsPLU()
        ).GetArea();
    }

    public static double FullTrashCaptureAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits, bool isPLU)
    {
        return trashGeneratingUnits.Where(x =>
            x.IsFullTrashCapture() &&
            x.IsPLU() == isPLU
        ).GetArea();
    }

    public static double PartialTrashCaptureAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits, bool isPLU)
    {
        return trashGeneratingUnits.Where(x =>
            x.IsPartialTrashCapture() &&
            x.IsPLU() == isPLU
        ).GetArea();
    }

    public static double GetArea(this IEnumerable<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return trashGeneratingUnits
            .Select(x => x.TrashGeneratingUnitGeometry.Area * Constants.SquareMetersToAcres).Sum(); // will never be null
    }

    public static bool IsFullTrashCapture(this TrashGeneratingUnit trashGeneratingUnit)
    {
        return (trashGeneratingUnit.Delineation?.TreatmentBMP.TrashCaptureStatusTypeID ==
            (int)TrashCaptureStatusTypeEnum.Full ||
            trashGeneratingUnit.ActiveWaterQualityManagementPlan()?.TrashCaptureStatusTypeID ==
            (int)TrashCaptureStatusTypeEnum.Full);
    }

    public static bool IsPartialTrashCapture(this TrashGeneratingUnit trashGeneratingUnit)
    {
        return (trashGeneratingUnit.Delineation?.TreatmentBMP.TrashCaptureStatusTypeID ==
                (int)TrashCaptureStatusTypeEnum.Partial ||
                trashGeneratingUnit.ActiveWaterQualityManagementPlan()?.TrashCaptureStatusTypeID ==
                (int)TrashCaptureStatusTypeEnum.Partial) && !trashGeneratingUnit.IsFullTrashCapture();
    }

    // NPT-1051: Only Active WQMPs contribute to trash result calculations. Treats Draft/Inactive WQMPs as if the TGU has no WQMP association.
    private static WaterQualityManagementPlan ActiveWaterQualityManagementPlan(this TrashGeneratingUnit trashGeneratingUnit)
    {
        return trashGeneratingUnit.WaterQualityManagementPlan?.WaterQualityManagementPlanStatusID == (int)WaterQualityManagementPlanStatusEnum.Active
            ? trashGeneratingUnit.WaterQualityManagementPlan
            : null;
    }

    public static bool IsPLU(this TrashGeneratingUnit trashGeneratingUnit)
    {
        // This is how to check "PLU == true"
        return trashGeneratingUnit.LandUseBlock?.PriorityLandUseTypeID != (int)PriorityLandUseTypeEnum.ALU;
    }

    // OVTA-based calculations

    public static double AlternateOVTAScoreDAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return GetAlternativeOVTAScoreAcreageImpl(trashGeneratingUnits, OnlandVisualTrashAssessmentScore.D);
    }

    public static double AlternateOVTAScoreBAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return GetAlternativeOVTAScoreAcreageImpl(trashGeneratingUnits, OnlandVisualTrashAssessmentScore.B);
    }

    public static double PriorityOVTAScoreDAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return GetPriorityOVTAScoreAcreageImpl(trashGeneratingUnits, OnlandVisualTrashAssessmentScore.D);
    }

    public static double PriorityOVTAScoreBAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return GetPriorityOVTAScoreAcreageImpl(trashGeneratingUnits, OnlandVisualTrashAssessmentScore.B);
    }

    public static double AlternateOVTAScoreCAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return GetAlternativeOVTAScoreAcreageImpl(trashGeneratingUnits, OnlandVisualTrashAssessmentScore.C);
    }

    public static double AlternateOVTAScoreAAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return GetAlternativeOVTAScoreAcreageImpl(trashGeneratingUnits, OnlandVisualTrashAssessmentScore.A);
    }

    public static double PriorityOVTAScoreCAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return GetPriorityOVTAScoreAcreageImpl(trashGeneratingUnits, OnlandVisualTrashAssessmentScore.C);
    }

    public static double PriorityOVTAScoreAAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
    {
        return GetPriorityOVTAScoreAcreageImpl(trashGeneratingUnits, OnlandVisualTrashAssessmentScore.A);
    }

    private static double GetAlternativeOVTAScoreAcreageImpl(List<TrashGeneratingUnit> trashGeneratingUnits,
        OnlandVisualTrashAssessmentScore onlandVisualTrashAssessmentScore)
    {
        return Math.Round(trashGeneratingUnits.Where(x =>
            x.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentBaselineScoreID ==
            onlandVisualTrashAssessmentScore.OnlandVisualTrashAssessmentScoreID &&
            x.IsPLU()).GetArea(), 0);
    }

    private static double GetPriorityOVTAScoreAcreageImpl(List<TrashGeneratingUnit> trashGeneratingUnits,
        OnlandVisualTrashAssessmentScore onlandVisualTrashAssessmentScore)
    {
        return Math.Round(trashGeneratingUnits.Where(x =>
            x.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentBaselineScoreID ==
            onlandVisualTrashAssessmentScore.OnlandVisualTrashAssessmentScoreID &&
            x.IsPLU()).GetArea(), 0);
    }
}