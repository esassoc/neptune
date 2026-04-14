using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPAssessmentObservationTypeUpsertDto
{
    [Required]
    [MaxLength(100)]
    public string TreatmentBMPAssessmentObservationTypeName { get; set; }
    [Required]
    public int ObservationTypeSpecificationID { get; set; }
    public string TreatmentBMPAssessmentObservationTypeSchema { get; set; }
}
