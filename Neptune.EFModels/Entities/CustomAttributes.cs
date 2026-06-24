using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class CustomAttributes
{
    public static IQueryable<CustomAttribute> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.CustomAttributes
                .Include(x => x.CustomAttributeType)
                .Include(x => x.CustomAttributeValues);
    }

    public static List<CustomAttribute> List(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().OrderBy(x => x.CustomAttributeType.CustomAttributeTypeName).ToList();
    }

    public static List<CustomAttribute> ListByTreatmentBMPID(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.TreatmentBMPID == treatmentBMPID).OrderBy(x => x.CustomAttributeType.CustomAttributeTypeName).ToList();
    }

    public static List<CustomAttributeDto> ListByTreatmentBMPIDAsDto(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return ListByTreatmentBMPID(dbContext, treatmentBMPID).Select(x => x.AsDto()).ToList();
    }

    public static List<CustomAttribute> ListByTreatmentBMPIDAndPurposes(NeptuneDbContext dbContext, int treatmentBMPID, CustomAttributeTypePurposeEnum customAttributeTypePurposeEnum)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.TreatmentBMPID == treatmentBMPID && x.CustomAttributeType.CustomAttributeTypePurposeID == (int)customAttributeTypePurposeEnum).OrderBy(x => x.CustomAttributeType.CustomAttributeTypeName).ToList();
    }

    public static async Task<List<ErrorMessage>> ValidateUpdateCustomAttributesAsync(NeptuneDbContext dbContext, int treatmentBMPID, int customAttributeTypePurposeID, List<CustomAttributeUpsertDto> customAttributeUpsertDtos)
    {
        var errors = new List<ErrorMessage>();

        var customAttributeTypes = CustomAttributeTypes.GetCustomAttributeTypes(dbContext, customAttributeUpsertDtos);
        foreach (var customAttributeUpsertDto in customAttributeUpsertDtos.Where(x => x.CustomAttributeValues != null && x.CustomAttributeValues.Count > 0))
        {
            var customAttributeType = customAttributeTypes.SingleOrDefault(x => x.CustomAttributeTypeID == customAttributeUpsertDto.CustomAttributeTypeID);
            if (customAttributeType == null)
            {
                errors.Add(new ErrorMessage("CustomAttributeType", $"CustomAttributeType with ID {customAttributeUpsertDto.CustomAttributeTypeID} not found."));
                continue;
            }

            var customAttributeDataType = customAttributeType.CustomAttributeDataType;
            foreach (var value in customAttributeUpsertDto.CustomAttributeValues ?? new List<string>())
            {
                if (!string.IsNullOrWhiteSpace(value) && !customAttributeDataType.ValueIsCorrectDataType(value))
                {
                    errors.Add(new ErrorMessage($"{customAttributeType.CustomAttributeTypeName}", $"Entered value for {customAttributeType.CustomAttributeTypeName} does not match expected type ({customAttributeDataType.CustomAttributeDataTypeDisplayName})."));
                }
            }
        }

        await Task.CompletedTask;

        return errors;
    }

    public static async Task<List<CustomAttributeDto>> UpdateCustomAttributesAsync(NeptuneDbContext dbContext, int treatmentBMPID, int customAttributeTypePurposeID, List<CustomAttributeUpsertDto> customAttributeUpsertDtos, PersonDto callingUser)
    {
        var treatmentBMP = TreatmentBMPs.GetByID(dbContext, treatmentBMPID);

        await dbContext.CustomAttributeValues
            .Include(x => x.CustomAttribute)
            .ThenInclude(x => x.CustomAttributeType)
            .Where(x => x.CustomAttribute.TreatmentBMPID == treatmentBMPID && x.CustomAttribute.CustomAttributeType.CustomAttributeTypePurposeID == customAttributeTypePurposeID)
            .ExecuteDeleteAsync();

        var existingCustomAttributes = await dbContext.CustomAttributes
            .Include(x => x.CustomAttributeType)
            .Where(x => x.TreatmentBMPID == treatmentBMPID && x.CustomAttributeType.CustomAttributeTypePurposeID == customAttributeTypePurposeID)
            .ToListAsync();

        var validIncomingUpsertDtos = customAttributeUpsertDtos
            .Where(x => x.CustomAttributeValues != null && x.CustomAttributeValues.Count(v => !string.IsNullOrWhiteSpace(v)) > 0)
            .ToList();

        foreach (var customAttributeUpsertDto in validIncomingUpsertDtos)
        {
            var customAttribute = existingCustomAttributes.SingleOrDefault(x => x.TreatmentBMPID == treatmentBMPID && x.CustomAttributeTypeID == customAttributeUpsertDto.CustomAttributeTypeID);
            if (customAttribute == null)
            {
                customAttribute = new CustomAttribute
                {
                    TreatmentBMPID = treatmentBMPID,
                    TreatmentBMPTypeID = treatmentBMP.TreatmentBMPTypeID,
                    CustomAttributeTypeID = customAttributeUpsertDto.CustomAttributeTypeID,
                    TreatmentBMPTypeCustomAttributeTypeID = customAttributeUpsertDto.TreatmentBMPTypeCustomAttributeTypeID
                };

                dbContext.CustomAttributes.Add(customAttribute);
            }

            foreach (var customAttributeValueString in customAttributeUpsertDto.CustomAttributeValues!)
            {
                if (!string.IsNullOrWhiteSpace(customAttributeValueString))
                {
                    var customAttributeValue = new CustomAttributeValue
                    {
                        CustomAttribute = customAttribute,
                        AttributeValue = customAttributeValueString.Trim()
                    };

                    dbContext.CustomAttributeValues.Add(customAttributeValue);
                }
            }
        }

        foreach (var existingCustomAttribute in existingCustomAttributes)
        {
            var customAttributeUpsertDto = validIncomingUpsertDtos.SingleOrDefault(x => x.CustomAttributeTypeID == existingCustomAttribute.CustomAttributeTypeID);
            if (customAttributeUpsertDto == null)
            {
                dbContext.CustomAttributes.Remove(existingCustomAttribute);
            }
        }

        await dbContext.SaveChangesAsync();

        var purposeEnum = (CustomAttributeTypePurposeEnum) customAttributeTypePurposeID;

        // NPT-1062: Modeling-purpose custom attributes feed the Nereid model, so editing them must mark the BMP
        // dirty (mirrors the delineation-verify path in DelineationController). Without this no DirtyModelNode is
        // created, so the detail page never shows "awaiting calculation" and the hourly delta solve has nothing to
        // recalculate — results sit at zero until the nightly total solve. Scoped to Modeling so non-modeling
        // custom-attribute edits don't trigger needless recalcs.
        if (purposeEnum == CustomAttributeTypePurposeEnum.Modeling)
        {
            await Nereid.NereidUtilities.MarkTreatmentBMPDirty(treatmentBMP, dbContext);
        }

        var updatedCustomAttributes = ListByTreatmentBMPIDAndPurposes(dbContext, treatmentBMPID, purposeEnum);
        var updatedCustomAttributeDtos = updatedCustomAttributes.Select(x => x.AsDto()).ToList();
        return updatedCustomAttributeDtos;
    }
}