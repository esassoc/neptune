using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects
{
    public class WaterQualityManagementPlanDocumentDto
    {
        public int WaterQualityManagementPlanDocumentID { get; set; }
        public int WaterQualityManagementPlanID { get; set; }
        public string WaterQualityManagementPlanName { get; set; }
        public FileResourceDto FileResource { get; set; }
        public string DisplayName { get; set; }
        public string? Description { get; set; }
        public DateTime UploadDate { get; set; }
        public int WaterQualityManagementPlanDocumentTypeID { get; set; }
    }

    public class WaterQualityManagementPlanDocumentCreateDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;
        [Required]
        public string DisplayName { get; set; } = string.Empty;
        [Required]
        public int WaterQualityManagementPlanDocumentTypeID { get; set; }
        public string? Description { get; set; }
    }

    // NPT-1068: file is optional on update so users can edit metadata without re-uploading.
    // When File is present the controller deletes the old blob and swaps in a fresh
    // FileResource, mirroring the MVC "delete then re-add" workflow but in a single round trip.
    public class WaterQualityManagementPlanDocumentUpdateDto
    {
        public IFormFile? File { get; set; }
        [Required]
        public string DisplayName { get; set; } = string.Empty;
        [Required]
        public int WaterQualityManagementPlanDocumentTypeID { get; set; }
        public string? Description { get; set; }
    }
}
