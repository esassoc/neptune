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
}

public class TreatmentBMPTypeCustomAttributeTypeDetailDto
{
    public int TreatmentBMPTypeCustomAttributeTypeID { get; set; }
    public int CustomAttributeTypeID { get; set; }
    public string CustomAttributeTypeName { get; set; }
    public string CustomAttributeTypePurposeDisplayName { get; set; }
    public int CustomAttributeTypePurposeID { get; set; }
    public int? SortOrder { get; set; }
}
