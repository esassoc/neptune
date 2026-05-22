namespace Neptune.Models.DataTransferObjects;

public class RegionalSubbasinRevisionRequestDto
{
    public int RegionalSubbasinRevisionRequestID { get; set; }
    public int RegionalSubbasinRevisionRequestStatusID { get; set; }
    public string RegionalSubbasinRevisionRequestStatusDisplayName { get; set; }
    public int TreatmentBMPID { get; set; }
    public string TreatmentBMPName { get; set; }
    public int RequestPersonID { get; set; }
    public string RequestPersonName { get; set; }
    public DateTime RequestDate { get; set; }
    public int? ClosedByPersonID { get; set; }
    public string? ClosedByPersonName { get; set; }
    public DateTime? ClosedDate { get; set; }
    public string? Notes { get; set; }
    public string? CloseNotes { get; set; }
    public string? GeometryGeoJson { get; set; }
}
