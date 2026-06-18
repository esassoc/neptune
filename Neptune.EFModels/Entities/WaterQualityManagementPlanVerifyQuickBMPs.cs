using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanVerifyQuickBMPs
{
    public static IQueryable<WaterQualityManagementPlanVerifyQuickBMP> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.WaterQualityManagementPlanVerifyQuickBMPs
            .Include(x => x.WaterQualityManagementPlanVerify)
            .ThenInclude(x => x.WaterQualityManagementPlan)
            .Include(x => x.QuickBMP)
            .ThenInclude(x => x.TreatmentBMPType)
            ;
    }

    public static WaterQualityManagementPlanVerifyQuickBMP GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int waterQualityManagementPlanVerifyQuickBMPID)
    {
        var waterQualityManagementPlanVerifyQuickBMP = GetImpl(dbContext)
            .SingleOrDefault(x => x.WaterQualityManagementPlanVerifyQuickBMPID == waterQualityManagementPlanVerifyQuickBMPID);
        Check.RequireNotNull(waterQualityManagementPlanVerifyQuickBMP,
            $"WaterQualityManagementPlanVerifyQuickBMP with ID {waterQualityManagementPlanVerifyQuickBMPID} not found!");
        return waterQualityManagementPlanVerifyQuickBMP;
    }

    public static WaterQualityManagementPlanVerifyQuickBMP GetByID(NeptuneDbContext dbContext, int waterQualityManagementPlanVerifyQuickBMPID)
    {
        var waterQualityManagementPlanVerifyQuickBMP = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.WaterQualityManagementPlanVerifyQuickBMPID == waterQualityManagementPlanVerifyQuickBMPID);
        Check.RequireNotNull(waterQualityManagementPlanVerifyQuickBMP,
            $"WaterQualityManagementPlanVerifyQuickBMP with ID {waterQualityManagementPlanVerifyQuickBMPID} not found!");
        return waterQualityManagementPlanVerifyQuickBMP;
    }
}