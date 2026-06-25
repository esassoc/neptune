using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class vLoadGeneratingUnits
{

    public static async Task<List<LoadGeneratingUnitGridDto>> ListAsGridDtoAsync(NeptuneDbContext dbContext)
    {
        var entities = await dbContext.vLoadGeneratingUnits
            .AsNoTracking().ToListAsync();
        return entities.Select(x => x.AsGridDto()).ToList();
    }

    public static async Task<List<LoadGeneratingUnitGridDto>> ListByRegionalSubbasinAsGridDtoAsync(NeptuneDbContext dbContext, int regionalSubbasinID)
    {
        var entities = await dbContext.vLoadGeneratingUnits
            .AsNoTracking().Where(x => x.RegionalSubbasinID == regionalSubbasinID).ToListAsync();
        return entities.Select(x => x.AsGridDto()).ToList();
    }


}