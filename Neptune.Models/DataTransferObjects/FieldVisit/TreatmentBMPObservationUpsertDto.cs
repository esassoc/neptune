namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPObservationUpsertDto
{
    public int TreatmentBMPAssessmentObservationTypeID { get; set; }
    /// <summary>
    /// Raw JSON ObservationData payload (DiscreteObservationSchema, PassFailObservationSchema,
    /// or PercentageObservationSchema depending on the observation type's collection method).
    /// </summary>
    public string? ObservationData { get; set; }
}
