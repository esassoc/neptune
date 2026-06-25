using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static partial class TreatmentBMPTypeCustomAttributeTypeExtensionMethods
{

    public static TreatmentBMPTypeCustomAttributeTypeDto AsDto(this TreatmentBMPTypeCustomAttributeType treatmentBMPTypeCustomAttributeType)
    {
        var dto = new TreatmentBMPTypeCustomAttributeTypeDto()
        {
            TreatmentBMPTypeCustomAttributeTypeID = treatmentBMPTypeCustomAttributeType.TreatmentBMPTypeCustomAttributeTypeID,
            TreatmentBMPTypeID = treatmentBMPTypeCustomAttributeType.TreatmentBMPTypeID,
            CustomAttributeTypeID = treatmentBMPTypeCustomAttributeType.CustomAttributeTypeID,
            CustomAttributeType = treatmentBMPTypeCustomAttributeType.CustomAttributeType.AsDto(),
            SortOrder = treatmentBMPTypeCustomAttributeType.SortOrder
        };
        return dto;
    }
}