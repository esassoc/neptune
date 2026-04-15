using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanExtractionResults
{
    public static async Task<WaterQualityManagementPlanExtractionResult?> GetByWqmpIDAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return await dbContext.WaterQualityManagementPlanExtractionResults
            .SingleOrDefaultAsync(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
    }

    public static async Task<WaterQualityManagementPlanExtractionResultDto?> GetByWqmpIDAsDtoAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return await dbContext.WaterQualityManagementPlanExtractionResults
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(WaterQualityManagementPlanExtractionResultProjections.AsDto)
            .SingleOrDefaultAsync();
    }

    public static async Task SaveDraftAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, string draftOverlayJson, int personID)
    {
        var entity = await GetByWqmpIDAsync(dbContext, waterQualityManagementPlanID)
            ?? throw new InvalidOperationException($"No extraction result exists for WQMP {waterQualityManagementPlanID}.");

        entity.ApplyDraftOverlay(draftOverlayJson, personID, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();
    }

    public static async Task ClearDraftAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var entity = await GetByWqmpIDAsync(dbContext, waterQualityManagementPlanID);
        if (entity == null) return;

        entity.ClearDraftOverlay();
        await dbContext.SaveChangesAsync();
    }

    public static async Task MarkApprovedAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, int personID)
    {
        var entity = await GetByWqmpIDAsync(dbContext, waterQualityManagementPlanID)
            ?? throw new InvalidOperationException($"No extraction result exists for WQMP {waterQualityManagementPlanID}.");

        entity.Approve(personID, DateTime.UtcNow);
        await dbContext.SaveChangesAsync();
    }
}
