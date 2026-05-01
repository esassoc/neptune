using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPTypeProjections
{
    public static readonly Expression<Func<TreatmentBMPType, TreatmentBMPTypeGridDto>> AsGridDto =
        x => new TreatmentBMPTypeGridDto
        {
            TreatmentBMPTypeID = x.TreatmentBMPTypeID,
            TreatmentBMPTypeName = x.TreatmentBMPTypeName,
            TreatmentBMPTypeDescription = x.TreatmentBMPTypeDescription,
            IsAnalyzedInModelingModule = x.IsAnalyzedInModelingModule,
            ObservationTypeCount = x.TreatmentBMPTypeAssessmentObservationTypes.Count,
            CustomAttributeTypeCount = x.TreatmentBMPTypeCustomAttributeTypes.Count,
            TreatmentBMPCount = x.TreatmentBMPs.Count,
        };

    public static readonly Expression<Func<TreatmentBMPType, TreatmentBMPTypeDetailDto>> AsDetailDto =
        x => new TreatmentBMPTypeDetailDto
        {
            TreatmentBMPTypeID = x.TreatmentBMPTypeID,
            TreatmentBMPTypeName = x.TreatmentBMPTypeName,
            TreatmentBMPTypeDescription = x.TreatmentBMPTypeDescription,
            IsAnalyzedInModelingModule = x.IsAnalyzedInModelingModule,
            TreatmentBMPModelingTypeID = x.TreatmentBMPModelingTypeID,
            ObservationTypes = x.TreatmentBMPTypeAssessmentObservationTypes
                .OrderBy(y => y.SortOrder)
                .Select(y => new TreatmentBMPTypeObservationTypeDetailDto
                {
                    TreatmentBMPTypeAssessmentObservationTypeID = y.TreatmentBMPTypeAssessmentObservationTypeID,
                    TreatmentBMPAssessmentObservationTypeID = y.TreatmentBMPAssessmentObservationTypeID,
                    TreatmentBMPAssessmentObservationTypeName = y.TreatmentBMPAssessmentObservationType.TreatmentBMPAssessmentObservationTypeName,
                    AssessmentScoreWeight = y.AssessmentScoreWeight,
                    DefaultThresholdValue = y.DefaultThresholdValue,
                    DefaultBenchmarkValue = y.DefaultBenchmarkValue,
                    OverrideAssessmentScoreIfFailing = y.OverrideAssessmentScoreIfFailing,
                    SortOrder = y.SortOrder,
                    ObservationTypeSpecificationID = y.TreatmentBMPAssessmentObservationType.ObservationTypeSpecificationID,
                }).ToList(),
            CustomAttributeTypes = x.TreatmentBMPTypeCustomAttributeTypes
                .OrderBy(y => y.SortOrder)
                .Select(y => new TreatmentBMPTypeCustomAttributeTypeDetailDto
                {
                    TreatmentBMPTypeCustomAttributeTypeID = y.TreatmentBMPTypeCustomAttributeTypeID,
                    CustomAttributeTypeID = y.CustomAttributeTypeID,
                    CustomAttributeTypeName = y.CustomAttributeType.CustomAttributeTypeName,
                    CustomAttributeTypePurposeID = y.CustomAttributeType.CustomAttributeTypePurposeID,
                    SortOrder = y.SortOrder,
                    CustomAttributeDataTypeID = y.CustomAttributeType.CustomAttributeDataTypeID,
                    MeasurementUnitTypeID = y.CustomAttributeType.MeasurementUnitTypeID,
                    IsRequired = y.CustomAttributeType.IsRequired,
                    CustomAttributeTypeDescription = y.CustomAttributeType.CustomAttributeTypeDescription,
                }).ToList(),
        };
}
