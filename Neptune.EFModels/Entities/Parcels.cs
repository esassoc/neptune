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

        public static async Task<List<ParcelGridDto>> ListAsGridDtoAsync(NeptuneDbContext dbContext)
        {
            return await GetImpl(dbContext)
                .AsNoTracking()
                .OrderBy(x => x.ParcelNumber)
                .Select(ParcelProjections.AsGridDto)
                .ToListAsync();
        }

        // Cheap fingerprint of the Parcel table for HTTP ETag-based caching of the grid list
        // endpoint. Changes whenever any parcel is added, removed, or has its LastUpdate column
        // bumped — which the OC Survey refresh and bulk-upload flows already do. Computed in a
        // single query so COUNT and MAX are atomic and we only pay one DB round-trip.
        public static async Task<string> GetGridVersionETagAsync(NeptuneDbContext dbContext)
        {
            var stats = await dbContext.Parcels
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Count = g.Count(),
                    MaxLastUpdate = g.Max(p => p.LastUpdate),
                })
                .FirstOrDefaultAsync();

            var count = stats?.Count ?? 0;
            var maxTicks = stats?.MaxLastUpdate.Ticks ?? 0L;
            return $"\"{count}-{maxTicks}\"";
        }

        // Bulk lookup used by the WQMP AI-extraction review flow to resolve the accepted
        // list of APN strings into Parcel IDs before writing them to the WQMP. Returns every
        // requested parcel number, with ParcelID = null when not found so the caller can
        // surface missing ones to the user.
        public static List<ParcelLookupResultDto> LookupByParcelNumbers(NeptuneDbContext dbContext, List<string> parcelNumbers)
        {
            var normalized = parcelNumbers
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct()
                .ToList();

            var matches = dbContext.Parcels
                .AsNoTracking()
                .Where(x => normalized.Contains(x.ParcelNumber))
                .Select(x => new { x.ParcelNumber, x.ParcelID })
                .ToList()
                .ToDictionary(x => x.ParcelNumber, x => (int?)x.ParcelID);

            return normalized
                .Select(pn => new ParcelLookupResultDto
                {
                    ParcelNumber = pn,
                    ParcelID = matches.TryGetValue(pn, out var id) ? id : null,
                })
                .ToList();
        }

        public static async Task<DateTime?> GetLatestUpdateAsync(NeptuneDbContext dbContext)
        {
            return await dbContext.Parcels.AsNoTracking().MaxAsync(x => (DateTime?)x.LastUpdate);
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
