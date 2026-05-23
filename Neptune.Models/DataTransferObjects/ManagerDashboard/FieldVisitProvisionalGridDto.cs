namespace Neptune.Models.DataTransferObjects.ManagerDashboard;

public class FieldVisitProvisionalGridDto
{
    public int FieldVisitID { get; set; }
    public int TreatmentBMPID { get; set; }
    public string? TreatmentBMPName { get; set; }
    public DateTime VisitDate { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string? StormwaterJurisdictionName { get; set; }
    public int PerformedByPersonID { get; set; }
    public string? PerformedByPersonName { get; set; }
    public int FieldVisitStatusID { get; set; }
    public string? FieldVisitStatusDisplayName { get; set; }
    public int FieldVisitTypeID { get; set; }
    public string? FieldVisitTypeDisplayName { get; set; }
    public bool IsFieldVisitVerified { get; set; }
    public int? TreatmentBMPAssessmentIDInitial { get; set; }
    public bool IsAssessmentCompleteInitial { get; set; }
    public double? AssessmentScoreInitial { get; set; }
    public int? MaintenanceRecordID { get; set; }
    public int? TreatmentBMPAssessmentIDPM { get; set; }
    public bool IsAssessmentCompletePM { get; set; }
    public double? AssessmentScorePM { get; set; }
}
