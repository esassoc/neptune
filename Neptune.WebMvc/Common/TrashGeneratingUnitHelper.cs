using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.EFModels.Entities;

namespace Neptune.WebMvc.Common
{
    public static class TrashGeneratingUnitHelper
    {
        private const decimal FullTrashCaptureLoading = 2.5m;

        public static double LoadBasedFullCapture(NeptuneDbContext dbContext, StormwaterJurisdiction jurisdiction)
        {
            var vTrashGeneratingUnitLoadStatistics = dbContext.vTrashGeneratingUnitLoadStatistics.Where(x =>
                x.StormwaterJurisdictionID == jurisdiction.StormwaterJurisdictionID
                && x.IsFullTrashCapture );

            return vTrashGeneratingUnitLoadStatistics.Any()
                ? vTrashGeneratingUnitLoadStatistics.Sum(x =>
                    x.Area * (double) (x.BaselineLoadingRate - FullTrashCaptureLoading) *
                    Constants.SquareMetersToAcres)
                : 0;
        }

        public static double LoadBasedPartialCapture(NeptuneDbContext dbContext, StormwaterJurisdiction jurisdiction)
        {
            var vTrashGeneratingUnitLoadStatistics = dbContext.vTrashGeneratingUnitLoadStatistics.Where(x =>
                x.StormwaterJurisdictionID == jurisdiction.StormwaterJurisdictionID && x.IsPartialTrashCapture);

            return vTrashGeneratingUnitLoadStatistics.Any()
                ? vTrashGeneratingUnitLoadStatistics.Sum(x =>
                    x.Area * (double) (x.BaselineLoadingRate - x.CurrentLoadingRate) *
                    Constants.SquareMetersToAcres)
                : 0;
        }

        public static double LoadBasedOVTAProgressScores(NeptuneDbContext dbContext, StormwaterJurisdiction jurisdiction)
        {
            var vTrashGeneratingUnitLoadStatistics =
                dbContext.vTrashGeneratingUnitLoadStatistics.Where(x =>
                    x.StormwaterJurisdictionID == jurisdiction.StormwaterJurisdictionID
                    && x.HasBaselineScore == true && x.HasProgressScore == true);

            return vTrashGeneratingUnitLoadStatistics.Any()
                ? vTrashGeneratingUnitLoadStatistics.Sum(x =>
                    x.Area * (double) (x.BaselineLoadingRate - x.ProgressLoadingRate) *
                    Constants.SquareMetersToAcres)
                : 0;
        }

        // done
        public static double TargetLoadReduction(NeptuneDbContext dbContext, StormwaterJurisdiction jurisdiction)
        {
            var landUseBlocks = dbContext.LandUseBlocks.AsNoTracking().Where(x => x.PermitTypeID == (int) PermitTypeEnum.PhaseIMS4 &&
                x.StormwaterJurisdictionID == jurisdiction.StormwaterJurisdictionID && x.PriorityLandUseTypeID != (int)PriorityLandUseTypeEnum.ALU);

            return landUseBlocks.Any()
                ? landUseBlocks.Sum(x =>
                    x.LandUseBlockGeometry.Area * (double) (x.TrashGenerationRate - FullTrashCaptureLoading) *
                    Constants.SquareMetersToAcres)
                : 0;
        }


        public static double EquivalentAreaAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
        {
            return trashGeneratingUnits.Where(x =>
                x.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentBaselineScoreID ==
                (int) OnlandVisualTrashAssessmentScoreEnum.A &&
                !x.IsFullTrashCapture() &&
                // This is how to check "PLU == true"
                x.LandUseBlock.PriorityLandUseTypeID != (int)PriorityLandUseTypeEnum.ALU
            ).GetArea();
        }

        public static double FullTrashCaptureAcreage(this List<TrashGeneratingUnit> trashGeneratingUnits)
        {
            return trashGeneratingUnits.Where(x =>
                x.IsFullTrashCapture() &&
                // This is how to check "PLU == true"
                x.LandUseBlock.PriorityLandUseTypeID != (int)PriorityLandUseTypeEnum.ALU
            ).GetArea();
        }

        public static double GetArea(this IEnumerable<TrashGeneratingUnit> trashGeneratingUnits)
        {
            return Math.Round(trashGeneratingUnits
                .Select(x => x.TrashGeneratingUnitGeometry.Area * Constants.SquareMetersToAcres).Sum(), 0); // will never be null
        }

        public static bool IsFullTrashCapture(this TrashGeneratingUnit trashGeneratingUnit)
        {
            return (trashGeneratingUnit.Delineation?.TreatmentBMP.TrashCaptureStatusTypeID ==
                (int) TrashCaptureStatusTypeEnum.Full ||
                trashGeneratingUnit.ActiveWaterQualityManagementPlan()?.TrashCaptureStatusTypeID ==
                (int)TrashCaptureStatusTypeEnum.Full);
        }

        // NPT-1051: Only Active WQMPs contribute to trash result calculations. Treats Draft/Inactive WQMPs as if the TGU has no WQMP association.
        private static WaterQualityManagementPlan ActiveWaterQualityManagementPlan(this TrashGeneratingUnit trashGeneratingUnit)
        {
            return trashGeneratingUnit.WaterQualityManagementPlan?.WaterQualityManagementPlanStatusID == (int)WaterQualityManagementPlanStatusEnum.Active
                ? trashGeneratingUnit.WaterQualityManagementPlan
                : null;
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
            return trashGeneratingUnits.Where(x =>
                x.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentBaselineScoreID ==
                onlandVisualTrashAssessmentScore.OnlandVisualTrashAssessmentScoreID &&
                // This is how to check "PLU == true"
                x.LandUseBlock.PriorityLandUseTypeID == (int)PriorityLandUseTypeEnum.ALU).GetArea();
        }

        private static double GetPriorityOVTAScoreAcreageImpl(List<TrashGeneratingUnit> trashGeneratingUnits,
            OnlandVisualTrashAssessmentScore onlandVisualTrashAssessmentScore)
        {
            return trashGeneratingUnits.Where(x =>
                x.OnlandVisualTrashAssessmentArea?.OnlandVisualTrashAssessmentBaselineScoreID ==
                onlandVisualTrashAssessmentScore.OnlandVisualTrashAssessmentScoreID &&
                // This is how to check "PLU == true"
                x.LandUseBlock.PriorityLandUseTypeID != (int)PriorityLandUseTypeEnum.ALU).GetArea();
        }
    }
}
