using Microsoft.AspNetCore.Http;

namespace Neptune.Models.DataTransferObjects;

public class WQMPBulkUploadFormDto
{
    public IFormFile File { get; set; }
    public int StormwaterJurisdictionID { get; set; }
}
