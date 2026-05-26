namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// NPT-1056: per-observation scoring breakdown for the SPA assessment detail page. Ports the
/// data the legacy MVC ScoreDetail.cshtml renders — one row per observation type with the
/// Threshold / Observed / Benchmark / Weight / Score columns. Computed server-side because
/// threshold/benchmark values come from the BMP's TreatmentBMPBenchmarkAndThresholds tree and
/// the weight comes from TreatmentBMPTypeAssessmentObservationType.AssessmentScoreWeight,
/// neither of which the SPA can resolve from the existing observation-level DTO graph.
/// </summary>
public class TreatmentBMPAssessmentScoreDetailDto
{
    public int TreatmentBMPAssessmentID { get; set; }
    public bool IsAssessmentComplete { get; set; }
    public bool IsBenchmarkAndThresholdsComplete { get; set; }
    public string? AssessmentScore { get; set; }
    public bool OverrideScore { get; set; }
    public List<TreatmentBMPAssessmentObservationTypeForScoringDto> ObservationTypes { get; set; } = new();
}
