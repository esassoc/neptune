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
    /// The table has no created date, so age is taken from the order rows were added. A row that
    /// completed before the cutoff was queued before the cutoff, and so was every row added before
    /// it. So the newest row that completed before the cutoff marks the line: it and everything
    /// queued before it is older than the retention period. That also removes old rows that never
    /// completed, which have no date at all.
    ///
    /// A row that never completed is only removed when a later row is known to be that old. A
    /// refresh that failed or is still queued after the last old completion is kept, however long
    /// the table has been quiet, so its geometry is still there to retry.
    /// </summary>
    /// <returns>The number of rows deleted.</returns>
    public static async Task<int> DeleteOlderThanRetentionPeriodAsync(NeptuneDbContext dbContext, DateTime utcNow)
    {
        var cutoff = utcNow.AddDays(-RetentionDays);
        var newestIDKnownToBeOld = await dbContext.LoadGeneratingUnitRefreshAreas
            .Where(x => x.ProcessDate < cutoff)
            .Select(x => (int?)x.LoadGeneratingUnitRefreshAreaID)
            .MaxAsync();
        if (newestIDKnownToBeOld == null)
        {
            return 0;
        }

        return await dbContext.LoadGeneratingUnitRefreshAreas
            .Where(x => x.LoadGeneratingUnitRefreshAreaID <= newestIDKnownToBeOld.Value)
            .ExecuteDeleteAsync();
    }
}
