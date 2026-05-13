namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPTypeDetailDto
{
    public int TreatmentBMPTypeID { get; set; }
    public string TreatmentBMPTypeName { get; set; }
    public string TreatmentBMPTypeDescription { get; set; }
    public bool IsAnalyzedInModelingModule { get; set; }
    public int? TreatmentBMPModelingTypeID { get; set; }
    public List<TreatmentBMPTypeObservationTypeDetailDto> ObservationTypes { get; set; } = new();
    public List<TreatmentBMPTypeCustomAttributeTypeDetailDto> CustomAttributeTypes { get; set; } = new();
}

public class TreatmentBMPTypeObservationTypeDetailDto
{
    public int TreatmentBMPTypeAssessmentObservationTypeID { get; set; }
    public int TreatmentBMPAssessmentObservationTypeID { get; set; }
    public string TreatmentBMPAssessmentObservationTypeName { get; set; }
    public decimal? AssessmentScoreWeight { get; set; }
    public double? DefaultThresholdValue { get; set; }
    public double? DefaultBenchmarkValue { get; set; }
    public bool OverrideAssessmentScoreIfFailing { get; set; }
    public int? SortOrder { get; set; }
    public string ObservationTypeCollectionMethodDisplayName { get; set; }
    public int ObservationTypeSpecificationID { get; set; }

    // Spec-driven fields used by the SPA editor + detail UIs to drive per-OT conditional rendering
    // (Pass/Fail rows hide Benchmark/Threshold; threshold unit label depends on TargetType + ThresholdType).
    public int ObservationTypeCollectionMethodID { get; set; }
    public int ObservationTargetTypeID { get; set; }
    public int ObservationThresholdTypeID { get; set; }
    public bool HasBenchmarkAndThreshold { get; set; }
    public string BenchmarkUnitDisplayName { get; set; }
    public string ThresholdUnitDisplayName { get; set; }

    // Formatted display values used by the public detail page (matches MVC's GetFormattedBenchmarkValue / GetFormattedThresholdValue).
    public string FormattedBenchmarkValue { get; set; }
    public string FormattedThresholdValue { get; set; }
}

public class TreatmentBMPTypeCustomAttributeTypeDetailDto
{
    public int TreatmentBMPTypeCustomAttributeTypeID { get; set; }
    public int CustomAttributeTypeID { get; set; }
    public string CustomAttributeTypeName { get; set; }
    public string CustomAttributeTypePurposeDisplayName { get; set; }
    public int CustomAttributeTypePurposeID { get; set; }
    public int? SortOrder { get; set; }
    // Surfaced for the flat editor table + detail page tables.
    public int CustomAttributeDataTypeID { get; set; }
    public string CustomAttributeDataTypeDisplayName { get; set; }
    public int? MeasurementUnitTypeID { get; set; }
    public string MeasurementUnitDisplayName { get; set; }
    public bool IsRequired { get; set; }
    public string CustomAttributeTypeDescription { get; set; }
}
