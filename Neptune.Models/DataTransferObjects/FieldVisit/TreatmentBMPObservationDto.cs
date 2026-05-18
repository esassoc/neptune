namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPObservationDto
{
    public int TreatmentBMPObservationID { get; set; }
    public int TreatmentBMPAssessmentObservationTypeID { get; set; }
    public string? ObservationData { get; set; }

    // NPT-984: per-observation calculated score (e.g. "3.0", or "-" if benchmarks/thresholds
    // missing). Mirrors the legacy MVC AssessmentDetail view which shows a numeric score
    // per non-PassFail observation. PassFail observations return "-" since the value itself
    // IS the score signal. Populated post-materialize in TreatmentBMPAssessments by walking
    // the tracked entities and calling FormattedObservationScore().
    public string? ObservationScore { get; set; }
}
