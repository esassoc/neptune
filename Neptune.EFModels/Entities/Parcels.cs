/*-----------------------------------------------------------------------
<copyright file="Jurisdiction.DatabaseContextExtensions.cs" company="Tahoe Regional Planning Agency">
Copyright (c) Tahoe Regional Planning Agency. All rights reserved.
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
    public static class Parcels
    {
        private static IQueryable<Parcel> GetImpl(NeptuneDbContext dbContext)
        {
            return dbContext.Parcels;
        }

        public static Parcel GetByIDWithChangeTracking(NeptuneDbContext dbContext, int parcelID)
        {
            var parcel = GetImpl(dbContext)
                .SingleOrDefault(x => x.ParcelID == parcelID);
            Check.RequireNotNull(parcel, $"Parcel with ID {parcelID} not found!");
            return parcel;
        }

        public static Parcel GetByIDWithChangeTracking(NeptuneDbContext dbContext, ParcelPrimaryKey parcelPrimaryKey)
        {
            return GetByIDWithChangeTracking(dbContext, parcelPrimaryKey.PrimaryKeyValue);
        }

        public static Parcel GetByID(NeptuneDbContext dbContext, int parcelID)
        {
            var parcel = GetImpl(dbContext).AsNoTracking()
                .SingleOrDefault(x => x.ParcelID == parcelID);
            Check.RequireNotNull(parcel, $"Parcel with ID {parcelID} not found!");
            return parcel;
        }

        public static Parcel GetByID(NeptuneDbContext dbContext, ParcelPrimaryKey parcelPrimaryKey)
        {
            return GetByID(dbContext, parcelPrimaryKey.PrimaryKeyValue);
        }

        public static List<Parcel> List(NeptuneDbContext dbContext)
        {
            return GetImpl(dbContext).AsNoTracking().OrderBy(x => x.ParcelNumber).ToList();
        }

        public static Parcel GetParcelByParcelNumber(NeptuneDbContext dbContext, string parcelNumber)
        {
            var parcel = GetImpl(dbContext).FirstOrDefault(x => x.ParcelNumber == parcelNumber);
            Check.RequireNotNull(parcel, $"Parcel with number {parcelNumber} not found!");
            return parcel;
        }

        public static List<ParcelDisplayDto> Search(NeptuneDbContext dbContext, string term)
        {
            var searchString = term.Trim();
            return dbContext.Parcels
                .AsNoTracking()
                .Where(x => x.ParcelNumber.Contains(searchString) ||
                             x.ParcelAddress.Contains(searchString))
                .OrderBy(x => x.ParcelAddress)
                .ThenBy(x => x.ParcelNumber)
                .Take(20)
                .Select(ParcelProjections.AsDisplayDto)
                .ToList();
        }
    }
}
