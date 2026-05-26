/*-----------------------------------------------------------------------
<copyright file="FieldVisit.DatabaseContextExtensions.cs" company="Tahoe Regional Planning Agency and Sitka Technology Group">
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
using Neptune.Models.DataTransferObjects;
using Neptune.Models.DataTransferObjects.ManagerDashboard;
using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Common.DesignByContract;
using Neptune.Common.GeoSpatial;
using NetTopologySuite.Features;

namespace Neptune.EFModels.Entities
{
    public static class Delineations
    {
        public static IQueryable<Delineation> GetImpl(NeptuneDbContext dbContext)
        {
            return dbContext.Delineations
                    .Include(x => x.TreatmentBMP)
                    .Include(x => x.VerifiedByPerson)
                ;
        }

        public static Delineation GetByIDWithChangeTracking(NeptuneDbContext dbContext,
            int delineationID)
        {
            var delineation = GetImpl(dbContext)
                .SingleOrDefault(x => x.DelineationID == delineationID);
            Check.RequireNotNull(delineation,
                $"Delineation with ID {delineationID} not found!");
            return delineation;
        }

        public static Delineation GetByIDWithChangeTracking(NeptuneDbContext dbContext,
            DelineationPrimaryKey delineationPrimaryKey)
        {
            return GetByIDWithChangeTracking(dbContext, delineationPrimaryKey.PrimaryKeyValue);
        }

        public static List<DelineationUpsertDto> ListByProjectIDAsUpsertDto(NeptuneDbContext dbContext, int projectID)
        {
            return GetImpl(dbContext)
                .Where(x => x.TreatmentBMP.ProjectID == projectID)
                .Select(x => x.AsUpsertDto())
                .ToList();
        }

        public static List<DelineationDto> ListProjectDelineationsAsDto(NeptuneDbContext dbContext)
        {
            var dtos = GetImpl(dbContext).Where(x => x.TreatmentBMP.ProjectID != null)
                .Select(x => x.AsDto())
                .ToList();

            return dtos;
        }

        public static Delineation GetByID(NeptuneDbContext dbContext, int delineationID)
        {
            var delineation = GetImpl(dbContext).AsNoTracking()
                .SingleOrDefault(x => x.DelineationID == delineationID);
            Check.RequireNotNull(delineation,
                $"Delineation with ID {delineationID} not found!");
            return delineation;
        }

        public static Delineation GetByID(NeptuneDbContext dbContext,
            DelineationPrimaryKey delineationPrimaryKey)
        {
            return GetByID(dbContext, delineationPrimaryKey.PrimaryKeyValue);
        }

        public static Delineation? GetByTreatmentBMPID(NeptuneDbContext dbContext, int treatmentBMPID)
        {
            var delineation = GetImpl(dbContext).AsNoTracking()
                .SingleOrDefault(x => x.TreatmentBMPID == treatmentBMPID);
            return delineation;
        }

        public static Delineation? GetByTreatmentBMPIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPID)
        {
            var delineation = GetImpl(dbContext)
                .SingleOrDefault(x => x.TreatmentBMPID == treatmentBMPID);
            return delineation;
        }

        public static List<Delineation> ListByTreatmentBMPIDList(NeptuneDbContext dbContext, IEnumerable<int> treatmentBMPIDList)
        {
            return GetImpl(dbContext).AsNoTracking()
                .Where(x => treatmentBMPIDList.Contains(x.TreatmentBMPID)).OrderBy(x => x.DelineationID).ToList();
        }

        public static List<Delineation> ListByDelineationIDList(NeptuneDbContext dbContext, IEnumerable<int> delineationIDList)
        {
            return GetImpl(dbContext).AsNoTracking()
                .Where(x => delineationIDList.Contains(x.DelineationID)).OrderBy(x => x.DelineationID).ToList();
        }

        public static List<Delineation> ListByDelineationIDListWithChangeTracking(NeptuneDbContext dbContext, IEnumerable<int> delineationIDList)
        {
            return GetImpl(dbContext)
                .Where(x => delineationIDList.Contains(x.DelineationID)).OrderBy(x => x.DelineationID).ToList();
        }

        public static List<Delineation> GetProvisionalBMPDelineations(NeptuneDbContext dbContext, Person currentPerson)
        {
            return GetImpl(dbContext).AsNoTracking()
                .Include(x => x.TreatmentBMP)
                .ThenInclude(x => x.TreatmentBMPType)
                .Include(x => x.TreatmentBMP)
                .ThenInclude(x => x.StormwaterJurisdiction)
                .ThenInclude(x => x.Organization)
                .Where(x => x.IsVerified == false).ToList()
                .Where(x => x.TreatmentBMP.CanView(currentPerson))
                .OrderBy(x => x.TreatmentBMP.TreatmentBMPName).ToList();
        }

        // Manager Dashboard: provisional delineations projected straight to the grid DTO via the
        // DelineationProjections.AsProvisionalGridDto SQL projection. Mirrors the sibling
        // ListDiscrepancyGridDtosAsync helper — DelineationTypeName is resolved from the static
        // lookup in C# (EF can't translate it). Area rounding happens here too because Math.Round
        // on a nullable doesn't compose cleanly inside the Expression.
        public static async Task<List<DelineationProvisionalGridDto>> GetProvisionalBMPDelineationsAsGridDtoAsync(NeptuneDbContext dbContext, Person currentPerson)
        {
            var jurisdictionIDs = (await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, currentPerson.PersonID)).ToList();

            var dtos = await dbContext.Delineations.AsNoTracking()
                .Where(x => x.IsVerified == false && x.TreatmentBMP.ProjectID == null && jurisdictionIDs.Contains(x.TreatmentBMP.StormwaterJurisdictionID))
                .OrderBy(x => x.TreatmentBMP.TreatmentBMPName)
                .Select(DelineationProjections.AsProvisionalGridDto)
                .ToListAsync();

            foreach (var dto in dtos)
            {
                dto.DelineationTypeName = DelineationType.AllLookupDictionary.TryGetValue(dto.DelineationTypeID, out var t) ? t.DelineationTypeDisplayName : null;
                if (dto.DelineationAreaInAcres.HasValue)
                {
                    dto.DelineationAreaInAcres = Math.Round(dto.DelineationAreaInAcres.Value, 2);
                }
            }
            return dtos;
        }

        // Manager Dashboard: bulk-verify a set of delineations. Jurisdiction-scoped via
        // TreatmentBMP.CanView. Calls NereidUtilities.MarkDelineationDirty so the model
        // queue knows these need re-running. Returns verified count.
        public static async Task<int> BulkMarkAsVerifiedAsync(NeptuneDbContext dbContext, IList<int> delineationIDs, Person currentPerson)
        {
            if (delineationIDs == null || delineationIDs.Count == 0) return 0;

            var viewableJurisdictionIDs = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForBMPs(dbContext, currentPerson).ToList();
            var delineations = await dbContext.Delineations
                .Include(x => x.TreatmentBMP)
                .Where(x => delineationIDs.Contains(x.DelineationID)
                    && viewableJurisdictionIDs.Contains(x.TreatmentBMP.StormwaterJurisdictionID))
                .ToListAsync();
            foreach (var delineation in delineations)
            {
                delineation.MarkAsVerified(currentPerson);
            }
            await Nereid.NereidUtilities.MarkDelineationDirty(delineations, dbContext);
            // MarkDelineationDirty does its own SaveChangesAsync internally; defensive call
            // ensures the IsVerified/DateLastVerified flags persist if the helper changes shape.
            await dbContext.SaveChangesAsync();
            return delineations.Count;
        }

        public static async Task<List<DelineationDto>> ListByPersonIDAsDto(NeptuneDbContext dbContext, int personID)
        {
            var person = People.GetByID(dbContext, personID);
            if (person.RoleID is (int)RoleEnum.Admin or (int)RoleEnum.SitkaAdmin)
            {
                return ListProjectDelineationsAsDto(dbContext);
            }

            var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, personID);

            var dtos = GetImpl(dbContext)
                .Where(x => jurisdictionIDs.Contains(x.TreatmentBMP.StormwaterJurisdictionID))
                .Select(x => x.AsDto())
                .ToList();

            return dtos;
        }

        public static async Task MergeDelineations(NeptuneDbContext dbContext, List<DelineationUpsertDto> delineationUpsertDtos, int projectID)
        {
            var existingProjectDelineations = dbContext.Delineations.Include(x => x.TreatmentBMP).Where(x => x.TreatmentBMP.ProjectID == projectID).ToList();

            var allDelineationsInDatabase = dbContext.Delineations;

            // merge new Delineations
            var newDelineations = delineationUpsertDtos
                .Select(DelineationFromUpsertDto).ToList();

            existingProjectDelineations.MergeNew(newDelineations, allDelineationsInDatabase,
                (x, y) => x.TreatmentBMPID == y.TreatmentBMPID);

            await dbContext.SaveChangesAsync();

            // update upsert dtos with new DelineationIDs
            foreach (var delineationUpsertDto in delineationUpsertDtos)
            {
                delineationUpsertDto.DelineationID = existingProjectDelineations
                    .Single(x => x.TreatmentBMPID == delineationUpsertDto.TreatmentBMPID).DelineationID;
            }

            existingProjectDelineations.MergeUpdate(newDelineations,
                (x, y) => x.DelineationID == y.DelineationID,
                (x, y) =>
                {
                    x.DelineationTypeID = y.DelineationTypeID;
                    x.DelineationGeometry = y.DelineationGeometry;
                    x.DelineationGeometry4326 = y.DelineationGeometry4326;
                    x.DateLastModified = y.DateLastModified;
                });
            await dbContext.SaveChangesAsync();

            var delineationsToBeDeletedIDs = existingProjectDelineations.Where(x => newDelineations.Any(y => y.DelineationID == x.DelineationID)).Select(x => x.DelineationID).ToList();
            // delete all the Delineation related entities
            await dbContext.ProjectHRUCharacteristics.Include(x => x.ProjectLoadGeneratingUnit).Where(x =>
                    x.ProjectLoadGeneratingUnit.DelineationID.HasValue &&
                    delineationsToBeDeletedIDs.Contains(x.ProjectLoadGeneratingUnit.DelineationID.Value))
                .ExecuteDeleteAsync();
            await dbContext.ProjectLoadGeneratingUnits
                .Where(x => x.DelineationID.HasValue && delineationsToBeDeletedIDs.Contains(x.DelineationID.Value))
                .ExecuteDeleteAsync();
            await dbContext.DelineationOverlaps.Where(x =>
                delineationsToBeDeletedIDs.Contains(x.DelineationID) ||
                delineationsToBeDeletedIDs.Contains(x.DelineationOverlapID)).ExecuteDeleteAsync();

            existingProjectDelineations.MergeDelete(newDelineations,
                (x, y) => x.DelineationID == y.DelineationID,
                allDelineationsInDatabase);

            await dbContext.SaveChangesAsync();
        }

        public static Delineation DelineationFromUpsertDto(DelineationUpsertDto delineationUpsertDto)
        {
            IFeature? delineationGeometry;
            if (!string.IsNullOrWhiteSpace(delineationUpsertDto.Geometry))
            {
                delineationGeometry = GeoJsonSerializer.Deserialize<IFeature>(delineationUpsertDto.Geometry);
                delineationGeometry.Geometry.SRID = Proj4NetHelper.WEB_MERCATOR;
            }
            else
            {
                delineationGeometry = null;
            }

            var delineation = new Delineation()
            {
                DelineationTypeID = delineationUpsertDto.DelineationTypeID,
                DelineationGeometry4326 = delineationGeometry?.Geometry,
                DelineationGeometry = delineationGeometry != null ? delineationGeometry.Geometry.ProjectTo2771() : null,
                DateLastModified = DateTime.UtcNow,
                TreatmentBMPID = delineationUpsertDto.TreatmentBMPID
            };

            if (delineationUpsertDto.DelineationID > 0)
            {
                delineation.DelineationID = delineationUpsertDto.DelineationID;
            }

            return delineation;
        }


        public static void MarkAsVerified(Delineation delineation, Person currentPerson)
        {
            delineation.IsVerified = true;
            delineation.DateLastVerified = DateTime.UtcNow;
            delineation.VerifiedByPersonID = currentPerson.PersonID;
        }

        public static List<Delineation> ListHavingOverlaps(NeptuneDbContext dbContext, Person currentPerson)
        {
            return GetImpl(dbContext).AsNoTracking()
                .Include(x => x.TreatmentBMP)
                .ThenInclude(x => x.TreatmentBMPType)
                .Include(x => x.TreatmentBMP)
                .ThenInclude(x => x.StormwaterJurisdiction)
                .ThenInclude(x => x.Organization)
                .Include(x => x.DelineationOverlapDelineations)
                .ThenInclude(x => x.OverlappingDelineation)
                .ThenInclude(x => x.TreatmentBMP)
                .Where(x => x.DelineationOverlapDelineations.Any()).ToList()
                .Where(x => x.TreatmentBMP.CanView(currentPerson))
                .OrderBy(x => x.TreatmentBMP.TreatmentBMPName).ToList();
        }

        public static List<Delineation> ListHavingDiscrepancies(NeptuneDbContext dbContext, Person currentPerson)
        {
            return GetImpl(dbContext).AsNoTracking()
                .Include(x => x.TreatmentBMP)
                .ThenInclude(x => x.TreatmentBMPType)
                .Include(x => x.TreatmentBMP)
                .ThenInclude(x => x.StormwaterJurisdiction)
                .ThenInclude(x => x.Organization)
                .Where(x => x.HasDiscrepancies).ToList()
                .Where(x => x.TreatmentBMP.CanView(currentPerson))
                .OrderBy(x => x.TreatmentBMP.TreatmentBMPName).ToList();
        }

        public static DelineationDto? GetByTreatmentBMPIDAsDto(NeptuneDbContext dbContext, int treatmentBMPID)
        {
            return GetImpl(dbContext).AsNoTracking()
                .Where(x => x.TreatmentBMPID == treatmentBMPID)
                .Select(x => x.AsDto())
                .SingleOrDefault();
        }

        // NPT-1064 — SPA reconciliation report endpoints. These push the viewable-jurisdiction
        // filter into SQL (replacing the legacy in-memory `.Where(CanView)` in
        // ListHavingDiscrepancies / ListHavingOverlaps) and use the DtoProjections so EF
        // emits a single SELECT with JOINs instead of an Include chain.

        public static async Task<List<DelineationReconciliationDiscrepancyGridDto>> ListDiscrepancyGridDtosAsync(NeptuneDbContext dbContext, Person currentPerson)
        {
            var viewableJurisdictionIDs = StormwaterJurisdictionPeople
                .ListViewableStormwaterJurisdictionIDsByPersonForBMPs(dbContext, currentPerson).ToList();

            var dtos = await dbContext.Delineations.AsNoTracking()
                .Where(x => x.HasDiscrepancies
                            && x.TreatmentBMP.ProjectID == null
                            && viewableJurisdictionIDs.Contains(x.TreatmentBMP.StormwaterJurisdictionID))
                .OrderBy(x => x.TreatmentBMP.TreatmentBMPName)
                .Select(DelineationProjections.AsDiscrepancyGridDto)
                .ToListAsync();

            ResolveDelineationTypeNames(dtos);
            return dtos;
        }

        public static async Task<List<DelineationReconciliationOverlapGridDto>> ListOverlapGridDtosAsync(NeptuneDbContext dbContext, Person currentPerson)
        {
            var viewableJurisdictionIDs = StormwaterJurisdictionPeople
                .ListViewableStormwaterJurisdictionIDsByPersonForBMPs(dbContext, currentPerson).ToList();

            return await dbContext.Delineations.AsNoTracking()
                .Where(x => x.DelineationOverlapDelineations.Any()
                            && x.TreatmentBMP.ProjectID == null
                            && viewableJurisdictionIDs.Contains(x.TreatmentBMP.StormwaterJurisdictionID))
                .OrderBy(x => x.TreatmentBMP.TreatmentBMPName)
                .Select(DelineationProjections.AsOverlapGridDto)
                .ToListAsync();
        }

        public static void ResolveDelineationTypeNames(IEnumerable<DelineationReconciliationDiscrepancyGridDto> dtos)
        {
            foreach (var dto in dtos)
            {
                dto.DelineationTypeName = DelineationType.AllLookupDictionary[dto.DelineationTypeID].DelineationTypeDisplayName;
            }
        }
    }
}