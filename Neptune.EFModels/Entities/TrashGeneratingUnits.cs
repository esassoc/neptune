using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TrashGeneratingUnits
{
     public static async Task<List<TrashGeneratingUnitGridDto>> ListAsGridDtoAsync(NeptuneDbContext dbContext, PersonDto currentPerson)
    {
        var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, currentPerson.PersonID);
        var trashGeneratingUnitLoadStatistics = await dbContext.vTrashGeneratingUnitLoadStatistics
            .Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
            .OrderByDescending(x => x.LastUpdateDate)
            .Select(AsGridDtoProjection)
            .ToListAsync();
        return trashGeneratingUnitLoadStatistics;
    }

    // NPT-1134: shared by the grid and the GDB export so the download matches the grid.
    public static readonly Expression<Func<vTrashGeneratingUnitLoadStatistic, TrashGeneratingUnitGridDto>> AsGridDtoProjection = x => new TrashGeneratingUnitGridDto()
    {
        TrashGeneratingUnitID = x.TrashGeneratingUnitID,
        LandUseBlockID = x.LandUseBlockID,
        TrashCaptureEffectivenessBMP = x.TrashCaptureEffectivenessBMP,
        TrashCaptureStatusBMP = x.TrashCaptureStatusBMP,
        TreatmentBMPName = x.TreatmentBMPName,
        TreatmentBMPID = x.TreatmentBMPID,
        StormwaterJurisdictionID = x.StormwaterJurisdictionID,
        StormwaterJurisdictionName = x.OrganizationName,
        BaselineLoadingRate = x.BaselineLoadingRate,
        ProgressLoadingRate = x.ProgressLoadingRate,
        LandUseType = x.LandUseType,
        CurrentLoadingRate = x.CurrentLoadingRate,
        PriorityLandUseTypeDisplayName = x.PriorityLandUseTypeDisplayName,
        OnlandVisualTrashAssessmentAreaID = x.OnlandVisualTrashAssessmentAreaID,
        OnlandVisualTrashAssessmentAreaName = x.OnlandVisualTrashAssessmentAreaName,
        OnlandVisualTrashAssessmentAreaBaselineScore = x.OnlandVisualTrashAssessmentAreaBaselineScore,
        WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
        WaterQualityManagementPlanName = x.WaterQualityManagementPlanName,
        TrashCaptureStatusWQMP = x.TrashCaptureStatusWQMP,
        TrashCaptureEffectivenessWQMP = x.TrashCaptureEffectivenessWQMP,
        LastUpdateDate = x.LastUpdateDate,
        MedianHouseholdIncomeResidential = x.MedianHouseholdIncomeResidential,
        MedianHouseholdIncomeRetail = x.MedianHouseholdIncomeRetail,
        PermitClass = x.PermitClass,
        LandUseForTGR = x.LandUseForTGR,
        TrashGenerationRate = x.TrashGenerationRate,
        Area = x.Area * Constants.SquareMetersToAcres,
        LoadingRateDelta = x.LoadingRateDelta,
    };

    /// <summary>
    /// NPT-1156: empties TrashGeneratingUnitStaging and fills it with the units of one overlay run.
    /// Nothing reads the staging table, so this needs no transaction.
    /// </summary>
    public static async Task ReplaceStagingAsync(NeptuneDbContext dbContext, IReadOnlyList<TrashGeneratingUnitStaging> trashGeneratingUnitStagings)
    {
        await dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTrashGeneratingUnitStagingDelete");
        await StagingSwap.InsertChunkedAsync(dbContext, trashGeneratingUnitStagings);
    }

    /// <summary>
    /// NPT-1156: replaces TrashGeneratingUnit and TrashGeneratingUnit4326 with the contents of
    /// TrashGeneratingUnitStaging in one transaction. The swap is checked before commit (every
    /// staged unit is in both tables); any failure rolls back and the units that were there before
    /// stay in place.
    /// </summary>
    public static async Task SwapFromStagingAsync(NeptuneDbContext dbContext, int expectedTrashGeneratingUnitCount)
    {
        await StagingSwap.RunInTransactionAsync(dbContext, "TguSwapFromStaging", async () =>
        {
            await dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTrashGeneratingUnitUpdateFromStaging");

            var trashGeneratingUnitCount = await dbContext.TrashGeneratingUnits.CountAsync();
            if (trashGeneratingUnitCount != expectedTrashGeneratingUnitCount)
            {
                throw new InvalidOperationException($"Trash Generating Unit swap loaded {trashGeneratingUnitCount:N0} units but {expectedTrashGeneratingUnitCount:N0} were staged.");
            }

            var trashGeneratingUnit4326Count = await dbContext.TrashGeneratingUnit4326s.CountAsync(x => x.TrashGeneratingUnitID != null);
            if (trashGeneratingUnit4326Count != expectedTrashGeneratingUnitCount)
            {
                throw new InvalidOperationException($"Trash Generating Unit swap loaded {trashGeneratingUnit4326Count:N0} map (4326) units but {expectedTrashGeneratingUnitCount:N0} were staged.");
            }
        });
    }
}