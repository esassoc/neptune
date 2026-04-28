namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPTypeGridDto
{
    public int TreatmentBMPTypeID { get; set; }
    public string TreatmentBMPTypeName { get; set; }
    public string TreatmentBMPTypeDescription { get; set; }
    public bool IsAnalyzedInModelingModule { get; set; }
    public int ObservationTypeCount { get; set; }
    public int CustomAttributeTypeCount { get; set; }
    public int TreatmentBMPCount { get; set; }
}
