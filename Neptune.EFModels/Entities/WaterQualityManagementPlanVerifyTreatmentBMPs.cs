using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanVerifyTreatmentBMPs
{
    public static IQueryable<WaterQualityManagementPlanVerifyTreatmentBMP> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.WaterQualityManagementPlanVerifyTreatmentBMPs
            .Include(x => x.WaterQualityManagementPlanVerify)
            .ThenInclude(x => x.WaterQualityManagementPlan)
            .Include(x => x.TreatmentBMP)
            .ThenInclude(x => x.TreatmentBMPType)
            ;
    }

    public static WaterQualityManagementPlanVerifyTreatmentBMP GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int waterQualityManagementPlanVerifyTreatmentBMPID)
    {
        var waterQualityManagementPlanVerifyTreatmentBMP = GetImpl(dbContext)
            .SingleOrDefault(x => x.WaterQualityManagementPlanVerifyTreatmentBMPID == waterQualityManagementPlanVerifyTreatmentBMPID);
        Check.RequireNotNull(waterQualityManagementPlanVerifyTreatmentBMP,
            $"WaterQualityManagementPlanVerifyTreatmentBMP with ID {waterQualityManagementPlanVerifyTreatmentBMPID} not found!");
        return waterQualityManagementPlanVerifyTreatmentBMP;
    }

    public static WaterQualityManagementPlanVerifyTreatmentBMP GetByID(NeptuneDbContext dbContext, int waterQualityManagementPlanVerifyTreatmentBMPID)
    {
        var waterQualityManagementPlanVerifyTreatmentBMP = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.WaterQualityManagementPlanVerifyTreatmentBMPID == waterQualityManagementPlanVerifyTreatmentBMPID);
        Check.RequireNotNull(waterQualityManagementPlanVerifyTreatmentBMP,
            $"WaterQualityManagementPlanVerifyTreatmentBMP with ID {waterQualityManagementPlanVerifyTreatmentBMPID} not found!");
        return waterQualityManagementPlanVerifyTreatmentBMP;
    }
}