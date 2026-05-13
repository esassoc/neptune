using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPAssessmentDetailDto
{
    public int TreatmentBMPAssessmentID { get; set; }
    public int FieldVisitID { get; set; }
    public int TreatmentBMPID { get; set; }
    public int TreatmentBMPTypeID { get; set; }
    public int TreatmentBMPAssessmentTypeID { get; set; }
    public string? TreatmentBMPAssessmentTypeDisplayName { get; set; }
    public bool IsAssessmentComplete { get; set; }
    public double? AssessmentScore { get; set; }
    public string? Notes { get; set; }
    public List<TreatmentBMPObservationDto> Observations { get; set; } = new();

    /// <summary>
    /// The full list of observation types applicable to the BMP type, including JSON schema
    /// and collection-method metadata, so the SPA can render the dynamic observations form
    /// without an N+1 round-trip to the observation-type endpoint.
    /// </summary>
    public List<TreatmentBMPAssessmentObservationTypeForFormDto> ObservationTypes { get; set; } = new();
}

public class TreatmentBMPAssessmentObservationTypeForFormDto
{
    public int TreatmentBMPAssessmentObservationTypeID { get; set; }
    public string TreatmentBMPAssessmentObservationTypeName { get; set; } = null!;
    public int ObservationTypeSpecificationID { get; set; }
    public string ObservationTypeCollectionMethodName { get; set; } = null!;
    public string? TreatmentBMPAssessmentObservationTypeSchema { get; set; }
    public int? SortOrder { get; set; }
}
