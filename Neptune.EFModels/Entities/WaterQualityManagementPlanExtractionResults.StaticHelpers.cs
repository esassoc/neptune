using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanExtractionResults
{

    public static async Task<WaterQualityManagementPlanExtractionResultDto?> GetByWqmpIDAsDtoAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return await dbContext.WaterQualityManagementPlanExtractionResults
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(WaterQualityManagementPlanExtractionResultProjections.AsDto)
            .SingleOrDefaultAsync();
    }

    // Used when a reviewer re-runs the AI extraction — drops the existing extraction result row
    // so a fresh result can take its place. Caller confirms in the UI.
    public static async Task DeleteByWqmpIDAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        await dbContext.WaterQualityManagementPlanExtractionResults
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .ExecuteDeleteAsync();
    }
}
