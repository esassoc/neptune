using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanModeledPerformance
{
    public static async Task<ProjectLoadReducingResultDto?> GetModeledPerformanceAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var wqmp = WaterQualityManagementPlans.GetByID(dbContext, waterQualityManagementPlanID);

        List<vLoadReducingResult> nereidResults;
        DateTime? lastDeltaQueue;

        if (wqmp.WaterQualityManagementPlanModelingApproachID ==
            (int)WaterQualityManagementPlanModelingApproachEnum.Detailed)
        {
            var treatmentBMPIDs = await dbContext.Delineations.AsNoTracking()
                .Where(x => x.TreatmentBMP.WaterQualityManagementPlanID == waterQualityManagementPlanID && x.IsVerified)
                .Select(x => x.TreatmentBMPID)
                .ToListAsync();

            nereidResults = await dbContext.vLoadReducingResults.AsNoTracking()
                .Where(x => x.TreatmentBMPID != null && !x.IsBaselineCondition &&
                            treatmentBMPIDs.Contains(x.TreatmentBMPID.Value))
                .ToListAsync();

            lastDeltaQueue = await dbContext.DirtyModelNodes.AsNoTracking()
                .Where(x => x.TreatmentBMPID != null && treatmentBMPIDs.Contains(x.TreatmentBMPID.Value))
                .OrderByDescending(x => x.CreateDate)
                .Select(x => (DateTime?)x.CreateDate)
                .FirstOrDefaultAsync();
        }
        else
        {
            nereidResults = await dbContext.vLoadReducingResults.AsNoTracking()
                .Where(x => x.WaterQualityManagementPlanID != null &&
                            x.WaterQualityManagementPlanID == waterQualityManagementPlanID &&
                            !x.IsBaselineCondition)
                .ToListAsync();

            lastDeltaQueue = await dbContext.DirtyModelNodes.AsNoTracking()
                .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
                .Select(x => (DateTime?)x.CreateDate)
                .FirstOrDefaultAsync();
        }

        if (!nereidResults.Any())
        {
            return null;
        }

        var dto = new ProjectLoadReducingResultDto
        {
            WetWeatherInflow = nereidResults.Sum(x => x.WetWeatherInflow ?? 0),
            WetWeatherTreated = nereidResults.Sum(x => x.WetWeatherTreated ?? 0),
            WetWeatherRetained = nereidResults.Sum(x => x.WetWeatherRetained ?? 0),
            WetWeatherUntreated = nereidResults.Sum(x => x.WetWeatherUntreated ?? 0),
            WetWeatherTSSReduced = nereidResults.Sum(x => x.WetWeatherTSSReduced ?? 0),
            WetWeatherTNReduced = nereidResults.Sum(x => x.WetWeatherTNReduced ?? 0),
            WetWeatherTPReduced = nereidResults.Sum(x => x.WetWeatherTPReduced ?? 0),
            WetWeatherFCReduced = nereidResults.Sum(x => x.WetWeatherFCReduced ?? 0),
            WetWeatherTCuReduced = nereidResults.Sum(x => x.WetWeatherTCuReduced ?? 0),
            WetWeatherTPbReduced = nereidResults.Sum(x => x.WetWeatherTPbReduced ?? 0),
            WetWeatherTZnReduced = nereidResults.Sum(x => x.WetWeatherTZnReduced ?? 0),
            WetWeatherTSSInflow = nereidResults.Sum(x => x.WetWeatherTSSInflow ?? 0),
            WetWeatherTNInflow = nereidResults.Sum(x => x.WetWeatherTNInflow ?? 0),
            WetWeatherTPInflow = nereidResults.Sum(x => x.WetWeatherTPInflow ?? 0),
            WetWeatherFCInflow = nereidResults.Sum(x => x.WetWeatherFCInflow ?? 0),
            WetWeatherTCuInflow = nereidResults.Sum(x => x.WetWeatherTCuInflow ?? 0),
            WetWeatherTPbInflow = nereidResults.Sum(x => x.WetWeatherTPbInflow ?? 0),
            WetWeatherTZnInflow = nereidResults.Sum(x => x.WetWeatherTZnInflow ?? 0),
            SummerDryWeatherInflow = nereidResults.Sum(x => x.SummerDryWeatherInflow ?? 0),
            SummerDryWeatherTreated = nereidResults.Sum(x => x.SummerDryWeatherTreated ?? 0),
            SummerDryWeatherRetained = nereidResults.Sum(x => x.SummerDryWeatherRetained ?? 0),
            SummerDryWeatherUntreated = nereidResults.Sum(x => x.SummerDryWeatherUntreated ?? 0),
            SummerDryWeatherTSSReduced = nereidResults.Sum(x => x.SummerDryWeatherTSSReduced ?? 0),
            SummerDryWeatherTNReduced = nereidResults.Sum(x => x.SummerDryWeatherTNReduced ?? 0),
            SummerDryWeatherTPReduced = nereidResults.Sum(x => x.SummerDryWeatherTPReduced ?? 0),
            SummerDryWeatherFCReduced = nereidResults.Sum(x => x.SummerDryWeatherFCReduced ?? 0),
            SummerDryWeatherTCuReduced = nereidResults.Sum(x => x.SummerDryWeatherTCuReduced ?? 0),
            SummerDryWeatherTPbReduced = nereidResults.Sum(x => x.SummerDryWeatherTPbReduced ?? 0),
            SummerDryWeatherTZnReduced = nereidResults.Sum(x => x.SummerDryWeatherTZnReduced ?? 0),
            SummerDryWeatherTSSInflow = nereidResults.Sum(x => x.SummerDryWeatherTSSInflow ?? 0),
            SummerDryWeatherTNInflow = nereidResults.Sum(x => x.SummerDryWeatherTNInflow ?? 0),
            SummerDryWeatherTPInflow = nereidResults.Sum(x => x.SummerDryWeatherTPInflow ?? 0),
            SummerDryWeatherFCInflow = nereidResults.Sum(x => x.SummerDryWeatherFCInflow ?? 0),
            SummerDryWeatherTCuInflow = nereidResults.Sum(x => x.SummerDryWeatherTCuInflow ?? 0),
            SummerDryWeatherTPbInflow = nereidResults.Sum(x => x.SummerDryWeatherTPbInflow ?? 0),
            SummerDryWeatherTZnInflow = nereidResults.Sum(x => x.SummerDryWeatherTZnInflow ?? 0),
            WinterDryWeatherInflow = nereidResults.Sum(x => x.WinterDryWeatherInflow ?? 0),
            WinterDryWeatherTreated = nereidResults.Sum(x => x.WinterDryWeatherTreated ?? 0),
            WinterDryWeatherRetained = nereidResults.Sum(x => x.WinterDryWeatherRetained ?? 0),
            WinterDryWeatherUntreated = nereidResults.Sum(x => x.WinterDryWeatherUntreated ?? 0),
            WinterDryWeatherTSSReduced = nereidResults.Sum(x => x.WinterDryWeatherTSSReduced ?? 0),
            WinterDryWeatherTNReduced = nereidResults.Sum(x => x.WinterDryWeatherTNReduced ?? 0),
            WinterDryWeatherTPReduced = nereidResults.Sum(x => x.WinterDryWeatherTPReduced ?? 0),
            WinterDryWeatherFCReduced = nereidResults.Sum(x => x.WinterDryWeatherFCReduced ?? 0),
            WinterDryWeatherTCuReduced = nereidResults.Sum(x => x.WinterDryWeatherTCuReduced ?? 0),
            WinterDryWeatherTPbReduced = nereidResults.Sum(x => x.WinterDryWeatherTPbReduced ?? 0),
            WinterDryWeatherTZnReduced = nereidResults.Sum(x => x.WinterDryWeatherTZnReduced ?? 0),
            WinterDryWeatherTSSInflow = nereidResults.Sum(x => x.WinterDryWeatherTSSInflow ?? 0),
            WinterDryWeatherTNInflow = nereidResults.Sum(x => x.WinterDryWeatherTNInflow ?? 0),
            WinterDryWeatherTPInflow = nereidResults.Sum(x => x.WinterDryWeatherTPInflow ?? 0),
            WinterDryWeatherFCInflow = nereidResults.Sum(x => x.WinterDryWeatherFCInflow ?? 0),
            WinterDryWeatherTCuInflow = nereidResults.Sum(x => x.WinterDryWeatherTCuInflow ?? 0),
            WinterDryWeatherTPbInflow = nereidResults.Sum(x => x.WinterDryWeatherTPbInflow ?? 0),
            WinterDryWeatherTZnInflow = nereidResults.Sum(x => x.WinterDryWeatherTZnInflow ?? 0),
            EffectiveAreaAcres = nereidResults.Sum(x => x.EffectiveAreaAcres ?? 0),
            DesignStormDepth85thPercentile = nereidResults.Sum(x => x.DesignStormDepth85thPercentile ?? 0),
            DesignVolume85thPercentile = nereidResults.Sum(x => x.DesignVolume85thPercentile ?? 0),
            LastCalculatedDate = nereidResults.Max(x => x.LastUpdate),
        };

        // Compute combined dry weather (summer + winter)
        dto.DryWeatherInflow = dto.SummerDryWeatherInflow + dto.WinterDryWeatherInflow;
        dto.DryWeatherTreated = dto.SummerDryWeatherTreated + dto.WinterDryWeatherTreated;
        dto.DryWeatherRetained = dto.SummerDryWeatherRetained + dto.WinterDryWeatherRetained;
        dto.DryWeatherUntreated = dto.SummerDryWeatherUntreated + dto.WinterDryWeatherUntreated;
        dto.DryWeatherTSSReduced = dto.SummerDryWeatherTSSReduced + dto.WinterDryWeatherTSSReduced;
        dto.DryWeatherTNReduced = dto.SummerDryWeatherTNReduced + dto.WinterDryWeatherTNReduced;
        dto.DryWeatherTPReduced = dto.SummerDryWeatherTPReduced + dto.WinterDryWeatherTPReduced;
        dto.DryWeatherFCReduced = dto.SummerDryWeatherFCReduced + dto.WinterDryWeatherFCReduced;
        dto.DryWeatherTCuReduced = dto.SummerDryWeatherTCuReduced + dto.WinterDryWeatherTCuReduced;
        dto.DryWeatherTPbReduced = dto.SummerDryWeatherTPbReduced + dto.WinterDryWeatherTPbReduced;
        dto.DryWeatherTZnReduced = dto.SummerDryWeatherTZnReduced + dto.WinterDryWeatherTZnReduced;
        dto.DryWeatherTSSInflow = dto.SummerDryWeatherTSSInflow + dto.WinterDryWeatherTSSInflow;
        dto.DryWeatherTNInflow = dto.SummerDryWeatherTNInflow + dto.WinterDryWeatherTNInflow;
        dto.DryWeatherTPInflow = dto.SummerDryWeatherTPInflow + dto.WinterDryWeatherTPInflow;
        dto.DryWeatherFCInflow = dto.SummerDryWeatherFCInflow + dto.WinterDryWeatherFCInflow;
        dto.DryWeatherTCuInflow = dto.SummerDryWeatherTCuInflow + dto.WinterDryWeatherTCuInflow;
        dto.DryWeatherTPbInflow = dto.SummerDryWeatherTPbInflow + dto.WinterDryWeatherTPbInflow;
        dto.DryWeatherTZnInflow = dto.SummerDryWeatherTZnInflow + dto.WinterDryWeatherTZnInflow;

        // Compute totals (dry + wet)
        dto.TotalInflow = dto.DryWeatherInflow + dto.WetWeatherInflow;
        dto.TotalTreated = dto.DryWeatherTreated + dto.WetWeatherTreated;
        dto.TotalRetained = dto.DryWeatherRetained + dto.WetWeatherRetained;
        dto.TotalUntreated = dto.DryWeatherUntreated + dto.WetWeatherUntreated;
        dto.TotalTSSReduced = dto.DryWeatherTSSReduced + dto.WetWeatherTSSReduced;
        dto.TotalTNReduced = dto.DryWeatherTNReduced + dto.WetWeatherTNReduced;
        dto.TotalTPReduced = dto.DryWeatherTPReduced + dto.WetWeatherTPReduced;
        dto.TotalFCReduced = dto.DryWeatherFCReduced + dto.WetWeatherFCReduced;
        dto.TotalTCuReduced = dto.DryWeatherTCuReduced + dto.WetWeatherTCuReduced;
        dto.TotalTPbReduced = dto.DryWeatherTPbReduced + dto.WetWeatherTPbReduced;
        dto.TotalTZnReduced = dto.DryWeatherTZnReduced + dto.WetWeatherTZnReduced;
        dto.TotalTSSInflow = dto.DryWeatherTSSInflow + dto.WetWeatherTSSInflow;
        dto.TotalTNInflow = dto.DryWeatherTNInflow + dto.WetWeatherTNInflow;
        dto.TotalTPInflow = dto.DryWeatherTPInflow + dto.WetWeatherTPInflow;
        dto.TotalFCInflow = dto.DryWeatherFCInflow + dto.WetWeatherFCInflow;
        dto.TotalTCuInflow = dto.DryWeatherTCuInflow + dto.WetWeatherTCuInflow;
        dto.TotalTPbInflow = dto.DryWeatherTPbInflow + dto.WetWeatherTPbInflow;
        dto.TotalTZnInflow = dto.DryWeatherTZnInflow + dto.WetWeatherTZnInflow;

        return dto;
    }
}
