using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPAssessmentProjections
{
    public static readonly Expression<Func<TreatmentBMPAssessment, TreatmentBMPAssessmentDetailDto>> AsDetailDto =
        x => new TreatmentBMPAssessmentDetailDto
        {
            TreatmentBMPAssessmentID = x.TreatmentBMPAssessmentID,
            FieldVisitID = x.FieldVisitID,
            TreatmentBMPID = x.TreatmentBMPID,
            TreatmentBMPTypeID = x.TreatmentBMPTypeID,
            TreatmentBMPAssessmentTypeID = x.TreatmentBMPAssessmentTypeID,
            IsAssessmentComplete = x.IsAssessmentComplete,
            AssessmentScore = x.AssessmentScore,
            Notes = x.Notes,
            Observations = x.TreatmentBMPObservations.Select(o => new TreatmentBMPObservationDto
            {
                TreatmentBMPObservationID = o.TreatmentBMPObservationID,
                TreatmentBMPAssessmentObservationTypeID = o.TreatmentBMPAssessmentObservationTypeID,
                ObservationData = o.ObservationData,
            }).ToList(),
            ObservationTypes = x.TreatmentBMPType.TreatmentBMPTypeAssessmentObservationTypes
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.TreatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName)
                .Select(t => new TreatmentBMPAssessmentObservationTypeForFormDto
                {
                    TreatmentBMPAssessmentObservationTypeID = t.TreatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeID,
                    TreatmentBMPAssessmentObservationTypeName = t.TreatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName,
                    ObservationTypeSpecificationID = t.TreatmentBMPAssessmentObservationType.ObservationTypeSpecificationID,
                    TreatmentBMPAssessmentObservationTypeSchema = t.TreatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeSchema,
                    SortOrder = t.SortOrder,
                    // ObservationTypeCollectionMethodName resolved post-materialize from the static lookup.
                }).ToList(),
        };
}
