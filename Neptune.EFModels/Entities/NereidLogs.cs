using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class NereidLogs
{
    public static NereidLog? GetByTreatmentBMPID(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return dbContext.TreatmentBMPs.Include(x => x.LastNereidLog).AsNoTracking().SingleOrDefault(x => x.TreatmentBMPID == treatmentBMPID)?.LastNereidLog;
    }

    public static NereidLog? GetByWaterQualityManagementPlanID(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return dbContext.WaterQualityManagementPlans.Include(x => x.LastNereidLog).AsNoTracking().SingleOrDefault(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)?.LastNereidLog;
    }

    /// <summary>
    /// NPT-1068: Sitka-admin download link payload for the BMP detail Modeled BMP Performance
    /// panel. Returns null if the BMP has no associated NereidLog row.
    /// </summary>
    public static async Task<TreatmentBMPNereidLogContentDto?> GetLatestForTreatmentBMPAsDtoAsync(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var log = await dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .Select(x => x.LastNereidLog)
            .SingleOrDefaultAsync();
        if (log == null)
        {
            return null;
        }
        return new TreatmentBMPNereidLogContentDto
        {
            NereidLogID = log.NereidLogID,
            NereidRequest = log.NereidRequest,
            NereidResponse = log.NereidResponse,
        };
    }
}