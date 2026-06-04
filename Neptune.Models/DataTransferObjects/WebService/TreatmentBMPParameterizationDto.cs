namespace Neptune.Models.DataTransferObjects.WebService;

public class TreatmentBMPParameterizationDto
{
    public int TreatmentBMPID { get; set; }
    public string TreatmentBMPName { get; set; }
    public string TreatmentBMPTypeName { get; set; }
    public string FullyParameterized { get; set; }
    public string IsReadyForModeling { get; set; }
}
