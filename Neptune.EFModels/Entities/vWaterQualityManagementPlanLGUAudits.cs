using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class vWaterQualityManagementPlanLGUAudits
{
    public static async Task<List<WaterQualityManagementPlanLGUAuditGridDto>> ListAsGridDtoAsync(NeptuneDbContext dbContext)
    {
        return await dbContext.vWaterQualityManagementPlanLGUAudits
            .AsNoTracking()
            .OrderBy(x => x.WaterQualityManagementPlanName)
            .Select(x => new WaterQualityManagementPlanLGUAuditGridDto
            {
                WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
                WaterQualityManagementPlanName = x.WaterQualityManagementPlanName,
                LoadGeneratingUnitsPopulated = x.LoadGeneratingUnitsPopulated == true,
                BoundaryIsDefined = x.BoundaryIsDefined == true,
                IntersectsModelBasins = x.CountOfIntersectingModelBasins != 0,
            })
            .ToListAsync();
    }
}
