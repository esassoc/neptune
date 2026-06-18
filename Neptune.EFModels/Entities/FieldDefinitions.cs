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
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities
{
    public static class FieldDefinitions
    {

        public static FieldDefinition? GetByFieldDefinitionType(NeptuneDbContext dbContext, int fieldDefinitionTypeID)
        {
            return GetImpl(dbContext).AsNoTracking().SingleOrDefault(x => x.FieldDefinitionTypeID == fieldDefinitionTypeID);
        }

        private static IQueryable<FieldDefinition> GetImpl(NeptuneDbContext dbContext)
        {
            return dbContext.FieldDefinitions;
        }

        public static List<FieldDefinitionDto> List(NeptuneDbContext dbContext)
        {
            var dtos = GetImpl(dbContext).AsNoTracking()
                .Select(FieldDefinitionProjections.AsDto)
                .ToList();
            PopulateFieldDefinitionTypeNames(dtos);
            return dtos;
        }

        public static FieldDefinitionDto? GetByFieldDefinitionTypeID(NeptuneDbContext dbContext, int fieldDefinitionTypeID)
        {
            var dto = GetImpl(dbContext).AsNoTracking()
                .Where(x => x.FieldDefinitionTypeID == fieldDefinitionTypeID)
                .Select(FieldDefinitionProjections.AsDto)
                .SingleOrDefault();
            if (dto != null)
            {
                PopulateFieldDefinitionTypeNames(new[] { dto });
            }
            return dto;
        }

        private static void PopulateFieldDefinitionTypeNames(IEnumerable<FieldDefinitionDto> dtos)
        {
            foreach (var dto in dtos)
            {
                if (dto.FieldDefinitionType != null
                    && FieldDefinitionType.AllLookupDictionary.TryGetValue(dto.FieldDefinitionType.FieldDefinitionTypeID, out var fdt))
                {
                    dto.FieldDefinitionType.FieldDefinitionTypeName = fdt.FieldDefinitionTypeName;
                    dto.FieldDefinitionType.FieldDefinitionTypeDisplayName = fdt.FieldDefinitionTypeDisplayName;
                }
            }
        }

        public static async Task<FieldDefinitionDto> Update(NeptuneDbContext dbContext, int fieldDefinitionTypeID,
            FieldDefinitionDto fieldDefinitionUpdateDto)
        {
            var fieldDefinition = dbContext.FieldDefinitions
                .Single(x => x.FieldDefinitionTypeID == fieldDefinitionTypeID);

            // null check occurs in calling endpoint method.
            fieldDefinition.FieldDefinitionValue = fieldDefinitionUpdateDto.FieldDefinitionValue;

            await dbContext.SaveChangesAsync();

            return fieldDefinition.AsDto();
        }
    }
}
