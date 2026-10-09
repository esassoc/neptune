using Neptune.Common;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class OnlandVisualTrashAssessmentAreaExtensionMethods
{
    public static OnlandVisualTrashAssessmentAreaSimpleDto AsSimpleDto(this OnlandVisualTrashAssessmentArea onlandVisualTrashAssessmentArea)
    {
        var dto = new OnlandVisualTrashAssessmentAreaSimpleDto()
        {
            OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaID,
            OnlandVisualTrashAssessmentAreaName = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaName,
            StormwaterJurisdictionID = onlandVisualTrashAssessmentArea.StormwaterJurisdictionID,
            OnlandVisualTrashAssessmentBaselineScoreID = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentBaselineScoreID,
            AssessmentAreaDescription = onlandVisualTrashAssessmentArea.AssessmentAreaDescription,
            OnlandVisualTrashAssessmentProgressScoreID = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentProgressScoreID
        };
        return dto;
    }

    /// <param name="landUseBlocks">Land Use Blocks overlapping this area, from
    /// <see cref="LandUseBlocks.ListByOnlandVisualTrashAssessmentAreaID"/>. Pass an empty list when unknown.</param>
    public static OnlandVisualTrashAssessmentAreaGridDto AsGridDto(this OnlandVisualTrashAssessmentArea onlandVisualTrashAssessmentArea,
        IReadOnlyCollection<OnlandVisualTrashAssessmentAreaLandUseBlock> landUseBlocks)
    {
        var completedAssessments = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessments
            .Where(x => x.OnlandVisualTrashAssessmentStatusID == (int)OnlandVisualTrashAssessmentStatusEnum.Complete).ToList();
        var landUseTypeNames = landUseBlocks
            .Where(x => x.PriorityLandUseTypeID.HasValue)
            .Select(x => PriorityLandUseType.AllLookupDictionary[x.PriorityLandUseTypeID!.Value].PriorityLandUseTypeDisplayName)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        var landUseBlockIDs = landUseBlocks.Select(x => x.LandUseBlockID).Distinct().OrderBy(x => x).ToList();

        var dto = new OnlandVisualTrashAssessmentAreaGridDto()
        {
            OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaID,
            OnlandVisualTrashAssessmentAreaName = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaName,
            AssessmentAreaDescription = onlandVisualTrashAssessmentArea.AssessmentAreaDescription,
            StormwaterJurisdictionID = onlandVisualTrashAssessmentArea.StormwaterJurisdictionID,
            StormwaterJurisdictionName = onlandVisualTrashAssessmentArea.StormwaterJurisdiction.GetOrganizationDisplayName(),
            OnlandVisualTrashAssessmentBaselineScoreName = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentBaselineScore?.OnlandVisualTrashAssessmentScoreDisplayName,
            OnlandVisualTrashAssessmentProgressScoreName = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentProgressScore?.OnlandVisualTrashAssessmentScoreDisplayName,
            NumberOfAssessmentsInProgress = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessments.Count(x => x.OnlandVisualTrashAssessmentStatusID == (int) OnlandVisualTrashAssessmentStatusEnum.InProgress),
            NumberOfAssessmentsCompleted = completedAssessments.Count,
            NumberOfBaselineAssessmentsCompleted = completedAssessments.Count(x => !x.IsProgressAssessment),
            NumberOfProgressAssessmentsCompleted = completedAssessments.Count(x => x.IsProgressAssessment),
            LastAssessmentDate = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessments.Select(x => x.CompletedDate).Max(),
            AreaAcres = Math.Round(onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaGeometry.Area * Constants.SquareMetersToAcres, 2),
            LandUseTypes = landUseTypeNames.Count > 0 ? string.Join(", ", landUseTypeNames) : null,
            LandUseBlockIDs = landUseBlockIDs.Count > 0 ? string.Join(", ", landUseBlockIDs) : null,
        };
        return dto;
    }

    public static OnlandVisualTrashAssessmentAreaDetailDto AsDetailDto(this OnlandVisualTrashAssessmentArea onlandVisualTrashAssessmentArea)
    {
        var dto = new OnlandVisualTrashAssessmentAreaDetailDto()
        {
            OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaID,
            OnlandVisualTrashAssessmentAreaName = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaName,
            AssessmentAreaDescription = onlandVisualTrashAssessmentArea.AssessmentAreaDescription,
            StormwaterJurisdictionID = onlandVisualTrashAssessmentArea.StormwaterJurisdictionID,
            StormwaterJurisdictionName = onlandVisualTrashAssessmentArea.StormwaterJurisdiction.GetOrganizationDisplayName(),
            OnlandVisualTrashAssessmentBaselineScoreName = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentBaselineScore?.OnlandVisualTrashAssessmentScoreDisplayName,
            OnlandVisualTrashAssessmentBaselineScoreTrashGenerationRate= onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentBaselineScore?.TrashGenerationRate,
            OnlandVisualTrashAssessmentProgressScoreName = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentProgressScore?.OnlandVisualTrashAssessmentScoreDisplayName,
            OnlandVisualTrashAssessmentProgressScoreTrashGenerationRate= onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentProgressScore?.TrashGenerationRate,
            LastAssessmentDate = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessments.Where(x => x.OnlandVisualTrashAssessmentStatusID == (int)OnlandVisualTrashAssessmentStatusEnum.Complete).Select(x => x.CompletedDate).Max(),
            CompletedBaselineAssessmentCount = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessments.Count(x => x.OnlandVisualTrashAssessmentStatusID == (int)OnlandVisualTrashAssessmentStatusEnum.Complete && !x.IsProgressAssessment),
            CompletedProgressAssessmentCount = onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessments.Count(x => x.OnlandVisualTrashAssessmentStatusID == (int)OnlandVisualTrashAssessmentStatusEnum.Complete && x.IsProgressAssessment),
            BoundingBox = new BoundingBoxDto(onlandVisualTrashAssessmentArea.OnlandVisualTrashAssessmentAreaGeometry4326),
            Geometry = onlandVisualTrashAssessmentArea.GetGeometry4326GeoJson()
        };
        return dto;
    }

}