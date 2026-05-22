using Microsoft.AspNetCore.Http;

namespace Neptune.Models.DataTransferObjects;

public class LandUseBlockGdbUploadFormDto
{
    public IFormFile File { get; set; }
    public int StormwaterJurisdictionID { get; set; }
}
