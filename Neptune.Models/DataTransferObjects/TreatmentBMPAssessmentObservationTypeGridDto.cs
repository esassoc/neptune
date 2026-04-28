namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPAssessmentObservationTypeGridDto
{
    public int TreatmentBMPAssessmentObservationTypeID { get; set; }
    public string TreatmentBMPAssessmentObservationTypeName { get; set; }
    public int ObservationTypeSpecificationID { get; set; }
    public string ObservationTypeCollectionMethodDisplayName { get; set; }
    public string ObservationTargetTypeDisplayName { get; set; }
    public string ObservationThresholdTypeDisplayName { get; set; }
    public int TreatmentBMPTypeCount { get; set; }
}
