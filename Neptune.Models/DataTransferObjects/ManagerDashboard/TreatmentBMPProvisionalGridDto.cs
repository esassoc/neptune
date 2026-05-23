namespace Neptune.Models.DataTransferObjects.ManagerDashboard;

public class TreatmentBMPProvisionalGridDto
{
    public int TreatmentBMPID { get; set; }
    public string? TreatmentBMPName { get; set; }
    public string? TreatmentBMPTypeName { get; set; }
    public DateTime? DateOfLastInventoryVerification { get; set; }
    public DateTime? InventoryLastChangedDate { get; set; }
    public bool HasPhotos { get; set; }
    public bool BenchmarkAndThresholdsSet { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string? StormwaterJurisdictionName { get; set; }
    public bool CanDelete { get; set; }
}
