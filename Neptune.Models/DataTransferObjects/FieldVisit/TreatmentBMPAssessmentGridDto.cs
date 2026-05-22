using System;

namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPAssessmentGridDto
{
    public int TreatmentBMPAssessmentID { get; set; }
    public int FieldVisitID { get; set; }
    public int TreatmentBMPID { get; set; }
    public string? TreatmentBMPName { get; set; }
    public int TreatmentBMPTypeID { get; set; }
    public string? TreatmentBMPTypeName { get; set; }
    public DateTime VisitDate { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string StormwaterJurisdictionName { get; set; } = null!;
    public int? WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public int PerformedByPersonID { get; set; }
    public string? PerformedByPersonName { get; set; }
    public string? FieldVisitTypeDisplayName { get; set; }
    public string? TreatmentBMPAssessmentTypeDisplayName { get; set; }
    public bool IsAssessmentComplete { get; set; }
    public double? AssessmentScore { get; set; }
    public bool IsFieldVisitVerified { get; set; }
    public string Status => IsAssessmentComplete ? "Complete" : "In Progress";

    // NPT-984: comma-separated list of failure notes from the assessment's PassFail
    // observations whose recorded value was false. Mirrors the legacy MVC
    // `TreatmentBMPController.TreatmentBMPAssessmentSummaryGridJsonData` "Failure Notes" column.
    // Per-observation chunks look like "{ObservationTypeName} Failure Notes: {notes or [None provided]}".
    public string? FailureNotes { get; set; }
}
