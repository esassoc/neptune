namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPDelineationMapDto
{
    public int TreatmentBMPID { get; set; }
    public string TreatmentBMPName { get; set; }
    public string TreatmentBMPTypeName { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool HasDelineation { get; set; }
    public int? DelineationID { get; set; }
    public int? DelineationTypeID { get; set; }
    public bool? IsVerified { get; set; }
}
