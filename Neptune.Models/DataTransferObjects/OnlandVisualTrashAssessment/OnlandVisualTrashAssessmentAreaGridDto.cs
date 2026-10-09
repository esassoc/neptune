namespace Neptune.Models.DataTransferObjects;

public class OnlandVisualTrashAssessmentAreaGridDto
{
    public int OnlandVisualTrashAssessmentAreaID { get; set; }
    public string? OnlandVisualTrashAssessmentAreaName { get; set; }
    public int? StormwaterJurisdictionID { get; set; }
    public string? StormwaterJurisdictionName { get; set; }
    public string? OnlandVisualTrashAssessmentBaselineScoreName { get; set; }
    public string? AssessmentAreaDescription { get; set; }
    public string? OnlandVisualTrashAssessmentProgressScoreName { get; set; }
    public int NumberOfAssessmentsInProgress { get; set; }
    public int NumberOfAssessmentsCompleted { get; set; }
    // NPT-1128 rework: completed count split by assessment kind.
    public int NumberOfBaselineAssessmentsCompleted { get; set; }
    public int NumberOfProgressAssessmentsCompleted { get; set; }
    public DateOnly? LastAssessmentDate { get; set; }
    // NPT-1128 rework: acreage from the native (SRID 2771) geometry, not the 4326 copy.
    public double AreaAcres { get; set; }
    // NPT-1128 rework: comma-separated, from a live intersect against Land Use Blocks (not TGU output).
    public string? LandUseTypes { get; set; }
    public string? LandUseBlockIDs { get; set; }
}