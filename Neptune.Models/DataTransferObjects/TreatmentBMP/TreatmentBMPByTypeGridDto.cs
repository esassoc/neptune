namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// NPT-1038 round 4: row shape for the "Treatment BMPs of this Type" grid that hangs off the
/// Treatment BMP Type detail page (`/program-info/treatment-bmp-types/{id}`). Standalone (not
/// inheriting from <see cref="TreatmentBMPGridDto"/>) so changes on either grid stay local.
///
/// <see cref="CustomAttributeValues"/> is keyed by CustomAttributeTypeID; the SPA component
/// builds one dynamic column per custom attribute defined on the Treatment BMP Type and reads
/// the value off this map. Multi-value attributes are pre-joined with ", " server-side to
/// keep the cell renderer dumb.
/// </summary>
public class TreatmentBMPByTypeGridDto
{
    public int TreatmentBMPID { get; set; }
    public string TreatmentBMPName { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string StormwaterJurisdictionName { get; set; }
    public string OwnerOrganizationName { get; set; }
    public int? YearBuilt { get; set; }
    public string SystemOfRecordID { get; set; }
    public int? WaterQualityManagementPlanID { get; set; }
    public string WaterQualityManagementPlanName { get; set; }
    public string Notes { get; set; }
    public DateTime? LatestAssessmentDate { get; set; }
    public double? LatestAssessmentScore { get; set; }
    public long NumberOfAssessments { get; set; }
    public DateTime? LatestMaintenanceDate { get; set; }
    public long NumberOfMaintenanceRecords { get; set; }
    public bool BenchmarkAndThresholdSet { get; set; }
    public string TreatmentBMPLifespanTypeDisplayName { get; set; }
    public DateTime? TreatmentBMPLifespanEndDate { get; set; }
    public int? RequiredFieldVisitsPerYear { get; set; }
    public int? RequiredPostStormFieldVisitsPerYear { get; set; }
    public string SizingBasisTypeDisplayName { get; set; }
    public string TrashCaptureStatusTypeDisplayName { get; set; }
    public string DelineationTypeDisplayName { get; set; }
    public Dictionary<int, string> CustomAttributeValues { get; set; } = new();
}
