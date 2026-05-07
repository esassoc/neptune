using Microsoft.AspNetCore.Http;

namespace Neptune.Models.DataTransferObjects;

public class TrashScreenFieldVisitUploadFormDto
{
    public IFormFile File { get; set; }
}
