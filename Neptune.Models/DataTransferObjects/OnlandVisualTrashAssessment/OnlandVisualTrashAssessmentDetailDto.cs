namespace Neptune.Models.DataTransferObjects;

public class OnlandVisualTrashAssessmentDetailDto
{
    public int OnlandVisualTrashAssessmentID { get; set; }
    public string CreatedByPersonFullName { get; set; }
    public string? SecondAssessorName { get; set; }
    public DateTime CreatedDate { get; set; }
    public int? OnlandVisualTrashAssessmentAreaID { get; set; }
    public string? OnlandVisualTrashAssessmentAreaName { get; set; }
    public string Notes { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string StormwaterJurisdictionName { get; set; }
    public int OnlandVisualTrashAssessmentStatusID { get; set; }
    public string OnlandVisualTrashAssessmentStatusName { get; set; }
    public string OnlandVisualTrashAssessmentScoreName { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string IsProgressAssessment { get; set; }
    public Dictionary<string, List<string>> PreliminarySourceIdentificationsByCategory { get; set; }
}