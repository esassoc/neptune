using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class CustomAttributeTypeProjections
{
    public static readonly Expression<Func<CustomAttributeType, CustomAttributeTypeDto>> AsDto =
        x => new CustomAttributeTypeDto
        {
            CustomAttributeTypeID = x.CustomAttributeTypeID,
            CustomAttributeTypeName = x.CustomAttributeTypeName,
            CustomAttributeDataTypeID = x.CustomAttributeDataTypeID,
            MeasurementUnitTypeID = x.MeasurementUnitTypeID,
            IsRequired = x.IsRequired,
            CustomAttributeTypeDescription = x.CustomAttributeTypeDescription,
            CustomAttributeTypePurposeID = x.CustomAttributeTypePurposeID,
            CustomAttributeTypeOptionsSchema = x.CustomAttributeTypeOptionsSchema,
            CustomAttributeTypeDefaultValue = x.CustomAttributeTypeDefaultValue,
            // NPT-1038: mirror the legacy detail page grid which shows the BMP Type name
            // alongside the total BMPs of that type (not specifically BMPs using this
            // attribute — that matches the legacy semantics).
            TreatmentBMPTypeUsages = x.TreatmentBMPTypeCustomAttributeTypes
                .Select(y => new TreatmentBMPTypeUsageDto
                {
                    TreatmentBMPTypeID = y.TreatmentBMPTypeID,
                    TreatmentBMPTypeName = y.TreatmentBMPType.TreatmentBMPTypeName,
                    ObservationTypeCount = y.TreatmentBMPType.TreatmentBMPTypeAssessmentObservationTypes.Count,
                    TreatmentBMPCount = y.TreatmentBMPType.TreatmentBMPs.Count,
                })
                .OrderBy(z => z.TreatmentBMPTypeName)
                .ToList(),
        };
}
