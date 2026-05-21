namespace Neptune.Models.DataTransferObjects;

public class DelineationReconciliationDiscrepancyGridDto
{
    public int DelineationID { get; set; }
    public int TreatmentBMPID { get; set; }
    public string? TreatmentBMPName { get; set; }
    public string? TreatmentBMPTypeName { get; set; }
    public int DelineationTypeID { get; set; }
    public string? DelineationTypeName { get; set; }
    public double? AreaInAcres { get; set; }
    public DateTime DateLastModified { get; set; }
    public DateTime? DateLastVerified { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string? StormwaterJurisdictionName { get; set; }
}
