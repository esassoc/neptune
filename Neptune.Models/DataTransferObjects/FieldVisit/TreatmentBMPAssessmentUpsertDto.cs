using System.Collections.Generic;

namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPAssessmentUpsertDto
{
    public List<TreatmentBMPObservationUpsertDto> Observations { get; set; } = new();
}
