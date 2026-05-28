namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// One row per benchmark/threshold-bearing observation type for a Treatment BMP's type — whether
/// or not a benchmark/threshold has actually been set. Lets the SPA detail panel + edit modal show
/// and edit every applicable observation type (matching the legacy MVC behavior), instead of only
/// the rows that already exist. TreatmentBMPBenchmarkAndThresholdID + the values are null when unset.
/// </summary>
public class TreatmentBMPBenchmarkAndThresholdWithObservationTypeDto
{
    public int? TreatmentBMPBenchmarkAndThresholdID { get; set; }
    public int TreatmentBMPID { get; set; }
    public int TreatmentBMPTypeID { get; set; }
    public int TreatmentBMPTypeAssessmentObservationTypeID { get; set; }
    public int TreatmentBMPAssessmentObservationTypeID { get; set; }
    public string ObservationTypeName { get; set; }
    public string BenchmarkUnitLabel { get; set; }
    public string ThresholdUnitLabel { get; set; }
    public double? BenchmarkValue { get; set; }
    public double? ThresholdValue { get; set; }
}
