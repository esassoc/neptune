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
            TreatmentBMPTypeNames = x.TreatmentBMPTypeCustomAttributeTypes
                .Select(y => y.TreatmentBMPType.TreatmentBMPTypeName)
                .ToList(),
        };
}
