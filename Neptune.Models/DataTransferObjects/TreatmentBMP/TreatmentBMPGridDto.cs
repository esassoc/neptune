namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPGridDto
{
    public int TreatmentBMPID { get; set; }
    public string? TreatmentBMPName { get; set; }
    public int TreatmentBMPTypeID { get; set; }
    public string? TreatmentBMPTypeName { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string StormwaterJurisdictionName { get; set; }
    public int? WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public int OwnerOrganizationID { get; set; }
    public string OwnerOrganizationName { get; set; }
    public int? YearBuilt { get; set; }
    public string? Notes { get; set; }
    public DateTime? LatestAssessmentDate { get; set; }
    public double? LatestAssessmentScore { get; set; }
    public long NumberOfAssessments { get; set; }
    public DateTime? LatestMaintenanceDate { get; set; }
    public long NumberOfMaintenanceRecords { get; set; }
    public bool BenchmarkAndThresholdSet { get; set; }
    public string? TreatmentBMPLifespanTypeDisplayName { get; set; }
    public DateTime? TreatmentBMPLifespanEndDate { get; set; }
    public int? RequiredFieldVisitsPerYear { get; set; }
    public int? RequiredPostStormFieldVisitsPerYear { get; set; }
    public string? SizingBasisTypeDisplayName { get; set; }
    public string? TrashCaptureStatusTypeDisplayName { get; set; }
    public int? TrashCaptureEffectiveness { get; set; }
    public string? DelineationTypeDisplayName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}