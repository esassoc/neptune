using Microsoft.AspNetCore.Http;

namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPCsvUploadFormDto
{
    public IFormFile File { get; set; }
    public int TreatmentBMPTypeID { get; set; }
}
