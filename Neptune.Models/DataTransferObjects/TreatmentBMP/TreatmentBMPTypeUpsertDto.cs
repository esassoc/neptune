using System.ComponentModel.DataAnnotations;

namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPTypeUpsertDto
{
    [Required]
    [MaxLength(100)]
    public string TreatmentBMPTypeName { get; set; }
    [MaxLength(1000)]
    public string TreatmentBMPTypeDescription { get; set; }
    public List<TreatmentBMPTypeObservationTypeUpsertDto> ObservationTypes { get; set; } = new();
    public List<TreatmentBMPTypeCustomAttributeTypeUpsertDto> CustomAttributeTypes { get; set; } = new();
}

public class TreatmentBMPTypeObservationTypeUpsertDto
{
    public int TreatmentBMPAssessmentObservationTypeID { get; set; }
    public decimal? AssessmentScoreWeight { get; set; }
    public double? DefaultThresholdValue { get; set; }
    public double? DefaultBenchmarkValue { get; set; }
    public bool OverrideAssessmentScoreIfFailing { get; set; }
    public int? SortOrder { get; set; }
}

public class TreatmentBMPTypeCustomAttributeTypeUpsertDto
{
    public int CustomAttributeTypeID { get; set; }
    public int? SortOrder { get; set; }
}
