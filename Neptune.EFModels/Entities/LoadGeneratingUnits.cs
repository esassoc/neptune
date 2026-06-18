using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class LoadGeneratingUnits
{

    public static async Task<LoadGeneratingUnitDto?> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int loadGeneratingUnitID)
    {
        var entity = await dbContext.LoadGeneratingUnits
            .Include(x => x.RegionalSubbasin)
            .Include(x => x.WaterQualityManagementPlan)
            .Include(x => x.Delineation)
            .ThenInclude(x => x.TreatmentBMP)
            .Include(x => x.HRULog)
            .Include(x => x.ModelBasin)
            .AsNoTracking().FirstOrDefaultAsync(x => x.LoadGeneratingUnitID == loadGeneratingUnitID);
        return entity?.AsDto();
    }
}