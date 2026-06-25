using Microsoft.AspNetCore.Http;

namespace Neptune.Models.DataTransferObjects;

public class OvtaAreaGdbUploadFormDto
{
    public IFormFile File { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string AreaNameField { get; set; } = "OVTAAreaName";
}
