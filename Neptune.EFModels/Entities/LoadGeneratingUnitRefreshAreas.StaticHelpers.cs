using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

public static class LoadGeneratingUnitRefreshAreas
{
    /// <summary>How long a refresh area is kept after it was queued.</summary>
    public const int RetentionDays = 90;

    /// <summary>
    /// Deletes refresh areas older than the retention period (NPT-1158). A refresh area is only read
    /// once, by the delta refresh it was queued for, so old rows are history and nothing more.
    ///
    /// The table has no created date, so age is taken from the order rows were added: everything
    /// queued before the oldest row that completed inside the retention period is older than that
    /// period. That also removes old rows that never completed, which have no date at all. If no
    /// row has completed inside the period there is nothing to measure against and nothing is deleted.
    /// </summary>
    /// <returns>The number of rows deleted.</returns>
    public static async Task<int> DeleteOlderThanRetentionPeriodAsync(NeptuneDbContext dbContext, DateTime utcNow)
    {
        var cutoff = utcNow.AddDays(-RetentionDays);
        var oldestIDToKeep = await dbContext.LoadGeneratingUnitRefreshAreas
            .Where(x => x.ProcessDate >= cutoff)
            .Select(x => (int?)x.LoadGeneratingUnitRefreshAreaID)
            .MinAsync();
        if (oldestIDToKeep == null)
        {
            return 0;
        }

        return await dbContext.LoadGeneratingUnitRefreshAreas
            .Where(x => x.LoadGeneratingUnitRefreshAreaID < oldestIDToKeep.Value)
            .ExecuteDeleteAsync();
    }
}
