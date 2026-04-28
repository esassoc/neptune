/*-----------------------------------------------------------------------
<copyright file="FieldDefinitionData.DatabaseContextExtensions.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
Copyright (c) Tahoe Regional Planning Agency and Sitka Technology Group. All rights reserved.
<author>Sitka Technology Group</author>
</copyright>

<license>
This program is free software: you can redistribute it and/or modify
it under the terms of the GNU Affero General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Affero General Public License <http://www.gnu.org/licenses/> for more details.

Source code is available upon request via <support@sitkatech.com>.
</license>
-----------------------------------------------------------------------*/

using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public static class CustomAttributeTypes
    {
        public const int CustomAttributeTypeIDNumberOfInletScreens = 90;
        public const int CustomAttributeTypeIDNumberOfConnectorPipeScreens = 91;
        public const int CustomAttributeTypeIDNumberOfTrashBaskets = 97;

        public static IQueryable<CustomAttributeType> GetImpl(NeptuneDbContext dbContext)
        {
            return dbContext.CustomAttributeTypes.Include(x => x.TreatmentBMPTypeCustomAttributeTypes)
                .ThenInclude(x => x.TreatmentBMPType)
                .ThenInclude(x => x.TreatmentBMPTypeAssessmentObservationTypes)
                ;
        }

        public static CustomAttributeType GetByIDWithChangeTracking(NeptuneDbContext dbContext,
            int customAttributeTypeID)
        {
            var customAttributeType = GetImpl(dbContext)
                .SingleOrDefault(x => x.CustomAttributeTypeID == customAttributeTypeID);
            Check.RequireNotNull(customAttributeType,
                $"CustomAttributeType with ID {customAttributeTypeID} not found!");
            return customAttributeType;
        }

        public static CustomAttributeType GetByIDWithChangeTracking(NeptuneDbContext dbContext,
            CustomAttributeTypePrimaryKey customAttributeTypePrimaryKey)
        {
            return GetByIDWithChangeTracking(dbContext, customAttributeTypePrimaryKey.PrimaryKeyValue);
        }

        public static CustomAttributeType GetByID(NeptuneDbContext dbContext, int customAttributeTypeID)
        {
            var customAttributeType = GetImpl(dbContext).AsNoTracking()
                .SingleOrDefault(x => x.CustomAttributeTypeID == customAttributeTypeID);
            Check.RequireNotNull(customAttributeType,
                $"CustomAttributeType with ID {customAttributeTypeID} not found!");
            return customAttributeType;
        }

        public static CustomAttributeType GetByID(NeptuneDbContext dbContext,
            CustomAttributeTypePrimaryKey customAttributeTypePrimaryKey)
        {
            return GetByID(dbContext, customAttributeTypePrimaryKey.PrimaryKeyValue);
        }

        public static CustomAttributeTypeDto GetByIDAsDto(NeptuneDbContext dbContext, int customAttributeTypeID)
        {
            var dto = dbContext.CustomAttributeTypes.AsNoTracking()
                .Where(x => x.CustomAttributeTypeID == customAttributeTypeID)
                .Select(CustomAttributeTypeProjections.AsDto)
                .SingleOrDefault();
            if (dto != null) ResolveLookupDisplayNames(dto);
            return dto;
        }

        public static List<CustomAttributeType> List(NeptuneDbContext dbContext)
        {
            return GetImpl(dbContext).AsNoTracking().OrderBy(x => x.CustomAttributeTypeName).ToList();
        }

        public static List<CustomAttributeTypeDto> ListAsDto(NeptuneDbContext dbContext)
        {
            var dtos = dbContext.CustomAttributeTypes.AsNoTracking()
                .OrderBy(x => x.CustomAttributeTypeName)
                .Select(CustomAttributeTypeProjections.AsDto)
                .ToList();
            foreach (var dto in dtos) ResolveLookupDisplayNames(dto);
            return dtos;
        }

        private static void ResolveLookupDisplayNames(CustomAttributeTypeDto dto)
        {
            if (CustomAttributeDataType.AllLookupDictionary.TryGetValue(dto.CustomAttributeDataTypeID, out var dataType))
            {
                dto.DataTypeName = dataType.CustomAttributeDataTypeName;
                dto.DataTypeDisplayName = dataType.CustomAttributeDataTypeDisplayName;
            }
            if (CustomAttributeTypePurpose.AllLookupDictionary.TryGetValue(dto.CustomAttributeTypePurposeID, out var purpose))
            {
                dto.Purpose = purpose.CustomAttributeTypePurposeDisplayName;
            }
            if (dto.MeasurementUnitTypeID.HasValue && MeasurementUnitType.AllLookupDictionary.TryGetValue(dto.MeasurementUnitTypeID.Value, out var unit))
            {
                dto.MeasurementUnitDisplayName = unit.MeasurementUnitTypeDisplayName;
            }
        }

        public static List<CustomAttributeType> GetCustomAttributeTypes(NeptuneDbContext dbContext, List<CustomAttributeUpsertDto> customAttributes)
        {
            var customAttributeTypeIDs = customAttributes.Select(x => x.CustomAttributeTypeID).ToList();
            return GetImpl(dbContext).AsNoTracking().Where(x => customAttributeTypeIDs.Contains(x.CustomAttributeTypeID)).OrderBy(x => x.CustomAttributeTypeName).ToList();
        }

        public static List<CustomAttributeTypeDto> GetByCustomAttributeTypePurposeAndTreatmentBMPTypeAsDto(NeptuneDbContext dbContext, int customAttributeTypePurposeID, int treatmentBmpTypeID)
        {
            return GetImpl(dbContext).AsNoTracking().Where(x =>
                    x.CustomAttributeTypePurposeID == customAttributeTypePurposeID &&
                    x.TreatmentBMPTypeCustomAttributeTypes.Any(y => y.TreatmentBMPTypeID == treatmentBmpTypeID))
                .Select(x => x.AsDto())
                .ToList();
        }

        public static List<CustomAttributeTypeDto> GetByCustomAttributeTypePurposeAsDto(NeptuneDbContext dbContext, int customAttributeTypePurposeID)
        {
            return GetImpl(dbContext).AsNoTracking().Where(x =>
                    x.CustomAttributeTypePurposeID == customAttributeTypePurposeID)
                .Select(x => x.AsDto())
                .ToList();
        }

        public static List<CustomAttributeTypeWithTreatmentBMPTypeIDsDto> GetByCustomAttributeTypePurposeAsWithTreatmentBMPTypeIDsDto(NeptuneDbContext dbContext, int customAttributeTypePurposeID)
        {
            return GetImpl(dbContext).AsNoTracking().Where(x =>
                    x.CustomAttributeTypePurposeID == customAttributeTypePurposeID)
                .Select(x => x.AsDtoWithTreatmentBmpTypeIDs())
                .ToList();
        }

        public static async Task<CustomAttributeTypeDto> CreateAsync(NeptuneDbContext dbContext, CustomAttributeTypeUpsertDto dto)
        {
            var entity = new CustomAttributeType
            {
                CustomAttributeTypeName = dto.CustomAttributeTypeName,
                CustomAttributeDataTypeID = dto.CustomAttributeDataTypeID,
                MeasurementUnitTypeID = dto.MeasurementUnitTypeID,
                IsRequired = dto.IsRequired,
                CustomAttributeTypePurposeID = dto.CustomAttributeTypePurposeID,
                CustomAttributeTypeDescription = dto.CustomAttributeTypeDescription,
                CustomAttributeTypeOptionsSchema = dto.CustomAttributeTypeOptionsSchema,
                CustomAttributeTypeDefaultValue = dto.CustomAttributeTypeDefaultValue,
            };
            dbContext.CustomAttributeTypes.Add(entity);
            await dbContext.SaveChangesAsync();
            return GetByIDAsDto(dbContext, entity.CustomAttributeTypeID);
        }

        public static async Task<CustomAttributeTypeDto> UpdateAsync(NeptuneDbContext dbContext, int customAttributeTypeID, CustomAttributeTypeUpsertDto dto)
        {
            var entity = GetByIDWithChangeTracking(dbContext, customAttributeTypeID);
            entity.CustomAttributeTypeName = dto.CustomAttributeTypeName;
            entity.MeasurementUnitTypeID = dto.MeasurementUnitTypeID;
            entity.IsRequired = dto.IsRequired;
            entity.CustomAttributeTypePurposeID = dto.CustomAttributeTypePurposeID;
            entity.CustomAttributeTypeDescription = dto.CustomAttributeTypeDescription;
            entity.CustomAttributeTypeOptionsSchema = dto.CustomAttributeTypeOptionsSchema;
            entity.CustomAttributeTypeDefaultValue = dto.CustomAttributeTypeDefaultValue;
            // Data type can only change from String to PickFromList/MultiSelect (or vice versa)
            if (entity.CustomAttributeDataTypeID != dto.CustomAttributeDataTypeID)
            {
                var stringTypeID = (int)CustomAttributeDataTypeEnum.String;
                var pickFromListTypeID = (int)CustomAttributeDataTypeEnum.PickFromList;
                var multiSelectTypeID = (int)CustomAttributeDataTypeEnum.MultiSelect;
                var allowedFromString = new[] { stringTypeID, pickFromListTypeID, multiSelectTypeID };
                if (allowedFromString.Contains(entity.CustomAttributeDataTypeID) && allowedFromString.Contains(dto.CustomAttributeDataTypeID))
                {
                    entity.CustomAttributeDataTypeID = dto.CustomAttributeDataTypeID;
                }
                // Otherwise silently ignore the type change
            }
            await dbContext.SaveChangesAsync();
            return GetByIDAsDto(dbContext, customAttributeTypeID);
        }

        public static async Task DeleteAsync(NeptuneDbContext dbContext, int customAttributeTypeID)
        {
            var entity = GetByIDWithChangeTracking(dbContext, customAttributeTypeID);
            entity.DeleteFull(dbContext);
            await dbContext.SaveChangesAsync();
        }
    }
}
