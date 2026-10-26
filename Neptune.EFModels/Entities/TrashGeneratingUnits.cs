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
}