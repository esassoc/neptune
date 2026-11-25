using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

/// <summary>
/// NPT-1156: runs a replace-from-staging in one transaction, so a failure anywhere leaves the rows
/// that were there before. Reuses a caller's transaction through a savepoint (the integration tests
/// wrap each test in one and roll it back).
/// </summary>
public static class StagingSwap
{
    public const int InsertChunkSize = 2000;

    // savepointName: SQL Server allows at most 32 characters
    public static async Task RunInTransactionAsync(NeptuneDbContext dbContext, string savepointName, Func<Task> swapAndVerify)
    {
        var outerTransaction = dbContext.Database.CurrentTransaction;
        await using var ownTransaction = outerTransaction == null ? await dbContext.Database.BeginTransactionAsync() : null;
        if (outerTransaction != null)
        {
            await outerTransaction.CreateSavepointAsync(savepointName);
        }

        try
        {
            await swapAndVerify();

            if (ownTransaction != null)
            {
                await ownTransaction.CommitAsync();
            }
            else
            {
                await outerTransaction!.ReleaseSavepointAsync(savepointName);
            }
        }
        catch
        {
            // A SQL error inside a swap procedure (xact_abort on) has already rolled the whole
            // transaction back server-side, so this rollback can itself fail; don't let that hide
            // the original exception.
            try
            {
                if (ownTransaction != null)
                {
                    await ownTransaction.RollbackAsync();
                }
                else
                {
                    await outerTransaction!.RollbackToSavepointAsync(savepointName);
                }
            }
            catch (Exception)
            {
                // already rolled back
            }
            throw;
        }
    }

    /// <summary>
    /// Inserts in chunks and clears the change tracker after each: one SaveChanges for a whole
    /// county of units was the overlay's memory peak.
    /// </summary>
    public static async Task InsertChunkedAsync<T>(NeptuneDbContext dbContext, IReadOnlyList<T> rows) where T : class
    {
        for (var offset = 0; offset < rows.Count; offset += InsertChunkSize)
        {
            dbContext.Set<T>().AddRange(rows.Skip(offset).Take(InsertChunkSize));
            await dbContext.SaveChangesAsync();
            dbContext.ChangeTracker.Clear();
        }
    }
}
