using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

public static class ModelBasins
{
    public static async Task<DateTime?> GetLatestUpdateAsync(NeptuneDbContext dbContext)
    {
        return await dbContext.ModelBasins.AsNoTracking().MaxAsync(x => (DateTime?)x.LastUpdate);
    }
}
