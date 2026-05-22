using Microsoft.AspNetCore.Http;

namespace Neptune.Models.DataTransferObjects;

public class OvtaBulkUploadFormDto
{
    public IFormFile File { get; set; }
}
