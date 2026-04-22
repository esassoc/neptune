using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TrainingVideos
{
    public static async Task<List<TrainingVideoDto>> ListAsDtoAsync(NeptuneDbContext dbContext)
    {
        var entities = await dbContext.TrainingVideos.AsNoTracking().ToListAsync();
        return entities.Select(x => x.AsDto()).ToList();
    }
}
