using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

public static class HRUCharacteristics
{
    public static async Task<DateTime?> GetLatestUpdateAsync(NeptuneDbContext dbContext)
    {
        return await dbContext.HRUCharacteristics.AsNoTracking().MaxAsync(x => (DateTime?)x.LastUpdated);
    }
}
