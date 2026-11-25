using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class LoadGeneratingUnits
{

    public static async Task<LoadGeneratingUnitDto?> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int loadGeneratingUnitID)
    {
        var entity = await dbContext.LoadGeneratingUnits
            .Include(x => x.RegionalSubbasin)
            .Include(x => x.WaterQualityManagementPlan)
            .Include(x => x.Delineation)
            .ThenInclude(x => x.TreatmentBMP)
            .Include(x => x.HRULog)
            .Include(x => x.ModelBasin)
            .AsNoTracking().FirstOrDefaultAsync(x => x.LoadGeneratingUnitID == loadGeneratingUnitID);
        return entity?.AsDto();
    }

    /// <summary>
    /// NPT-1156: empties LoadGeneratingUnitStaging and fills it with the units of one total overlay
    /// run. Nothing reads the staging table, so this needs no transaction.
    /// </summary>
    public static async Task ReplaceStagingAsync(NeptuneDbContext dbContext, IReadOnlyList<LoadGeneratingUnitStaging> loadGeneratingUnitStagings)
    {
        await dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pLoadGeneratingUnitStagingDelete");
        await StagingSwap.InsertChunkedAsync(dbContext, loadGeneratingUnitStagings);
    }

    /// <summary>
    /// NPT-1156: the total refresh. Replaces LoadGeneratingUnit with the contents of
    /// LoadGeneratingUnitStaging and empties HRUCharacteristic, in one transaction. The swap is
    /// checked before commit; any failure rolls back and the units and HRU characteristics that
    /// were there before stay in place. The new units get new IDs (see the procedure).
    /// </summary>
    public static async Task SwapFromStagingAsync(NeptuneDbContext dbContext, int expectedLoadGeneratingUnitCount)
    {
        await StagingSwap.RunInTransactionAsync(dbContext, "LguSwapFromStaging", async () =>
        {
            await dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pLoadGeneratingUnitUpdateFromStaging");

            var loadGeneratingUnitCount = await dbContext.LoadGeneratingUnits.CountAsync();
            if (loadGeneratingUnitCount != expectedLoadGeneratingUnitCount)
            {
                throw new InvalidOperationException($"Load Generating Unit swap loaded {loadGeneratingUnitCount:N0} units but {expectedLoadGeneratingUnitCount:N0} were staged.");
            }
        });
    }
}