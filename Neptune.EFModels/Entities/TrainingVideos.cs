using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TrainingVideos
{
    public static async Task<List<TrainingVideoDto>> ListAsDtoAsync(NeptuneDbContext dbContext, int? neptuneAreaID = null)
    {
        var query = dbContext.TrainingVideos.AsNoTracking().AsQueryable();
        if (neptuneAreaID.HasValue)
        {
            query = query.Where(x => x.NeptuneAreaID == neptuneAreaID.Value);
        }
        var entities = await query.ToListAsync();
        return entities.Select(x => x.AsDto()).ToList();
    }
}
