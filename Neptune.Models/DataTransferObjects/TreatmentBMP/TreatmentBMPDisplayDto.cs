namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPDisplayDto
{
    public int TreatmentBMPID { get; set; }
    public string TreatmentBMPName { get; set; }
    public string TreatmentBMPTypeName { get; set; }
    public string DisplayName { get; set; }
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public int? ProjectID { get; set; }
    public bool InventoryIsVerified { get; set; }
    public bool IsFullyParameterized { get; set; }
    public vTreatmentBMPModelingAttributeDto TreatmentBMPModelingAttribute { get; set; }
    public List<CustomAttributeUpsertDto> CustomAttributes { get; set; }
    public string WatershedName { get; set; }
    public string Notes { get; set; }
    public int TreatmentBMPTypeID { get; set; }
}

public class TreatmentBMPMinimalDto
{
    public int TreatmentBMPID { get; set; }
    public string TreatmentBMPName { get; set; }
    public string TreatmentBMPTypeName { get; set; }
    public string Notes { get; set; }
    public string DelineationStatus { get; set; }
    public double? Area { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}