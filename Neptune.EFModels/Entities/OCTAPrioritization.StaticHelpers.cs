using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

public static class OCTAPrioritizations
{
    public static async Task<DateTime?> GetLatestUpdateAsync(NeptuneDbContext dbContext)
    {
        return await dbContext.OCTAPrioritizations.AsNoTracking().MaxAsync(x => (DateTime?)x.LastUpdate);
    }
}
