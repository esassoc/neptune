using Microsoft.AspNetCore.Http;

namespace Neptune.Models.DataTransferObjects;

public class DelineationGdbUploadFormDto
{
    public IFormFile File { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string TreatmentBMPNameField { get; set; }
    public string? DelineationStatusField { get; set; }
}
