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
    public List<TreatmentBMPTypeReferenceDto> TreatmentBMPTypes { get; set; } = new();
}

public class TreatmentBMPTypeReferenceDto
{
    public int TreatmentBMPTypeID { get; set; }
    public string TreatmentBMPTypeName { get; set; }
}
