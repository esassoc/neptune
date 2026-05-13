using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPAssessmentPhotoDto
{
    public int TreatmentBMPAssessmentPhotoID { get; set; }
    public int TreatmentBMPAssessmentID { get; set; }
    public int FileResourceID { get; set; }
    public string FileResourceGUID { get; set; } = null!;
    public string? Caption { get; set; }
}

public class TreatmentBMPAssessmentPhotoCreateDto
{
    [Required]
    public IFormFile File { get; set; } = null!;
    public string? Caption { get; set; }
}

public class TreatmentBMPAssessmentPhotoUpdateDto
{
    public int TreatmentBMPAssessmentPhotoID { get; set; }
    public string? Caption { get; set; }
}
