using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPAssessmentObservationTypeProjections
{
    public static readonly Expression<Func<TreatmentBMPAssessmentObservationType, TreatmentBMPAssessmentObservationTypeGridDto>> AsGridDto =
        x => new TreatmentBMPAssessmentObservationTypeGridDto
        {
            TreatmentBMPAssessmentObservationTypeID = x.TreatmentBMPAssessmentObservationTypeID,
            TreatmentBMPAssessmentObservationTypeName = x.TreatmentBMPAssessmentObservationTypeName,
            ObservationTypeSpecificationID = x.ObservationTypeSpecificationID,
            TreatmentBMPTypeCount = x.TreatmentBMPTypeAssessmentObservationTypes.Count,
        };

    public static readonly Expression<Func<TreatmentBMPAssessmentObservationType, TreatmentBMPAssessmentObservationTypeDetailDto>> AsDetailDto =
        x => new TreatmentBMPAssessmentObservationTypeDetailDto
        {
            TreatmentBMPAssessmentObservationTypeID = x.TreatmentBMPAssessmentObservationTypeID,
            TreatmentBMPAssessmentObservationTypeName = x.TreatmentBMPAssessmentObservationTypeName,
            ObservationTypeSpecificationID = x.ObservationTypeSpecificationID,
            TreatmentBMPAssessmentObservationTypeSchema = x.TreatmentBMPAssessmentObservationTypeSchema,
            TreatmentBMPTypeNames = x.TreatmentBMPTypeAssessmentObservationTypes
                .Select(y => y.TreatmentBMPType.TreatmentBMPTypeName).ToList(),
        };
}
