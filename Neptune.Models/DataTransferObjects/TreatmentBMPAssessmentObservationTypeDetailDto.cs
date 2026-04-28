namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPAssessmentObservationTypeDetailDto
{
    public int TreatmentBMPAssessmentObservationTypeID { get; set; }
    public string TreatmentBMPAssessmentObservationTypeName { get; set; }
    public int ObservationTypeSpecificationID { get; set; }
    public string ObservationTypeCollectionMethodDisplayName { get; set; }
    public string ObservationTargetTypeDisplayName { get; set; }
    public string ObservationThresholdTypeDisplayName { get; set; }
    public string TreatmentBMPAssessmentObservationTypeSchema { get; set; }
    public List<string> TreatmentBMPTypeNames { get; set; } = new();
}
