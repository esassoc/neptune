using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

public static class PrecipitationZones
{
    public static async Task<DateTime?> GetLatestUpdateAsync(NeptuneDbContext dbContext)
    {
        return await dbContext.PrecipitationZones.AsNoTracking().MaxAsync(x => (DateTime?)x.LastUpdate);
    }
}
