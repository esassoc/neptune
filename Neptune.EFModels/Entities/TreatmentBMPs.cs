/*-----------------------------------------------------------------------
<copyright file="TreatmentBMP.DatabaseContextExtensions.cs" company="Tahoe Regional Planning Agency">
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
using Neptune.Common.GeoSpatial;
using Neptune.Models.DataTransferObjects;
using Neptune.Models.DataTransferObjects.ManagerDashboard;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPs
{

    #region Create

    public static async Task<List<ErrorMessage>> ValidateCreateAsync(NeptuneDbContext dbContext, TreatmentBMPCreateDto createDto)
    {
        var errors = new List<ErrorMessage>();

        // Validate basic shared fields
        errors.AddRange(await ValidateBasicInfoAsync(dbContext, createDto));

        // Validate Treatment BMP Type
        var hasValidType = await dbContext.TreatmentBMPTypes.AnyAsync(x => x.TreatmentBMPTypeID == createDto.TreatmentBMPTypeID);
        if (!hasValidType)
        {
            errors.Add(new ErrorMessage("TreatmentBMPTypeID", "Valid Treatment BMP Type is required."));
        }

        // Validate Stormwater Jurisdiction
        var hasValidJurisdiction = await dbContext.StormwaterJurisdictions.AnyAsync(x => x.StormwaterJurisdictionID == createDto.StormwaterJurisdictionID);
        if (!hasValidJurisdiction)
        {
            errors.Add(new ErrorMessage("StormwaterJurisdictionID", "Valid Stormwater Jurisdiction is required."));
        }

        return errors;
    }

    public static async Task<TreatmentBMPDto> CreateAsync(NeptuneDbContext dbContext, TreatmentBMPCreateDto createDto, PersonDto creatingPerson)
    {
        if (!createDto.OwnerOrganizationID.HasValue)
        {
            var stormwaterJurisdiction = await dbContext.StormwaterJurisdictions
                .AsNoTracking()
                .SingleAsync(x => x.StormwaterJurisdictionID == createDto.StormwaterJurisdictionID);

            createDto.OwnerOrganizationID = stormwaterJurisdiction.OrganizationID;
        }

        var treatmentBMP = new TreatmentBMP
        {
            TreatmentBMPName = createDto.TreatmentBMPName,
            TreatmentBMPTypeID = createDto.TreatmentBMPTypeID,
            StormwaterJurisdictionID = createDto.StormwaterJurisdictionID,
            OwnerOrganizationID = createDto.OwnerOrganizationID.Value,
            YearBuilt = createDto.YearBuilt,
            SystemOfRecordID = createDto.SystemOfRecordID,
            WaterQualityManagementPlanID = createDto.WaterQualityManagementPlanID,
            TreatmentBMPLifespanTypeID = createDto.TreatmentBMPLifespanTypeID,
            TreatmentBMPLifespanEndDate = createDto.TreatmentBMPLifespanTypeID.HasValue
                && createDto.TreatmentBMPLifespanTypeID.Value == TreatmentBMPLifespanType.FixedEndDate.TreatmentBMPLifespanTypeID
                ? createDto.TreatmentBMPLifespanEndDate
                : null,
            SizingBasisTypeID = createDto.SizingBasisTypeID,
            TrashCaptureStatusTypeID = createDto.TrashCaptureStatusTypeID,
            TrashCaptureEffectiveness = createDto.TrashCaptureStatusTypeID == TrashCaptureStatusType.Partial.TrashCaptureStatusTypeID
                ? createDto.TrashCaptureEffectiveness
                : null,
            RequiredFieldVisitsPerYear = createDto.RequiredFieldVisitsPerYear,
            RequiredPostStormFieldVisitsPerYear = createDto.RequiredPostStormFieldVisitsPerYear,
            Notes = createDto.Notes,
            InventoryIsVerified = false
        };

        if (createDto.Latitude.HasValue && createDto.Longitude.HasValue)
        {
            treatmentBMP.LocationPoint4326 = CreateLocationPoint4326FromLatLong(createDto.Latitude.Value, createDto.Longitude.Value);
            treatmentBMP.LocationPoint = treatmentBMP.LocationPoint4326.ProjectTo2771();
            treatmentBMP.SetTreatmentBMPPointInPolygonDataByLocationPoint(treatmentBMP.LocationPoint, dbContext);
        }

        await dbContext.TreatmentBMPs.AddAsync(treatmentBMP);

        // NPT-1069: seed default Benchmark & Threshold rows from the BMP type's observation-type
        // configuration, mirroring the legacy MVC NewViewModel.UpdateModel behavior so BMPs created
        // through the API land with the same defaults as MVC-created ones.
        var seedTemplates = await TreatmentBMPBenchmarkAndThresholds.BuildSeedTemplatesAsync(dbContext, createDto.TreatmentBMPTypeID);
        TreatmentBMPBenchmarkAndThresholds.AttachSeedsToBMP(treatmentBMP, seedTemplates);

        await dbContext.SaveChangesAsync();
        await dbContext.Entry(treatmentBMP).ReloadAsync();

        var createdBMP = await GetByIDAsDtoAsync(dbContext, treatmentBMP.TreatmentBMPID);

        return createdBMP;
    }

    #endregion

    private static async Task<List<TreatmentBMP>> ListTreatmentBMPsDisplayOnlyAsync(
        NeptuneDbContext dbContext,
        Func<IQueryable<TreatmentBMP>, IQueryable<TreatmentBMP>>? applyFilters = null,
        bool checkIsAnalyzedInModelingModule = true)
    {
        var query = dbContext.TreatmentBMPs
            .Where(x => !checkIsAnalyzedInModelingModule || x.TreatmentBMPType.IsAnalyzedInModelingModule);

        query = applyFilters?.Invoke(query) ?? query;

        return await ListTreatmentBMPsDisplayOnlyMaterializedAsync(dbContext, query);
    }

    private static async Task<List<TreatmentBMP>> ListTreatmentBMPsDisplayOnlyMaterializedAsync(NeptuneDbContext dbContext, IQueryable<TreatmentBMP> treatmentBmpsQuery)
    {
        var treatmentBMPs = await treatmentBmpsQuery
            .Include(x => x.TreatmentBMPType)
            .Include(x => x.Delineation)
            .Include(x => x.Project)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();

        var treatmentBMPIDs = treatmentBMPs.Select(x => x.TreatmentBMPID).ToList();
        if (treatmentBMPIDs.Count == 0)
        {
            return treatmentBMPs;
        }

        var customAttributes = await dbContext.CustomAttributes
            .Where(x => treatmentBMPIDs.Contains(x.TreatmentBMPID))
            .Include(x => x.CustomAttributeValues)
            .AsNoTracking()
            .ToListAsync();

        var customAttributesByTreatmentBMPID = customAttributes
            .GroupBy(x => x.TreatmentBMPID)
            .ToDictionary(x => x.Key, x => (ICollection<CustomAttribute>)x.ToList());

        foreach (var treatmentBMP in treatmentBMPs)
        {
            treatmentBMP.CustomAttributes = customAttributesByTreatmentBMPID.GetValueOrDefault(treatmentBMP.TreatmentBMPID) ?? new List<CustomAttribute>();
        }

        return treatmentBMPs;
    }

    // Manager Dashboard: provisional BMPs projected straight to the grid DTO. Mirrors the legacy
    // MVC ProvisionalTreatmentBMPGridSpec column list. SQL-side projection via
    // TreatmentBMPDtoProjections.AsProvisionalGridDto so the HasPhotos / BenchmarkAndThresholdsSet
    // booleans resolve in the database (the original implementation read those off un-included
    // navigation properties and silently always returned false/true respectively).
    public static async Task<List<TreatmentBMPProvisionalGridDto>> GetProvisionalTreatmentBMPsAsGridDtoAsync(NeptuneDbContext dbContext, Person currentPerson)
    {
        var jurisdictionIDs = (await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, currentPerson.PersonID)).ToList();
        var thresholdRequiringSpecIDs = ObservationTypeSpecification.All
            .Where(s => s.ObservationThresholdType != ObservationThresholdType.None)
            .Select(s => s.ObservationTypeSpecificationID)
            .ToList();

        var rows = await dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.ProjectID == null && x.InventoryIsVerified == false && jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
            .OrderBy(x => x.TreatmentBMPName)
            .Select(TreatmentBMPDtoProjections.AsProvisionalGridDto(thresholdRequiringSpecIDs))
            .ToListAsync();

        // CanDelete depends on calling Person + the row's jurisdiction; computed in C# because
        // EF can't translate Person.IsAssignedToStormwaterJurisdiction. Since the SQL filter
        // already restricts to ProjectID == null, only the role + jurisdiction-match remain.
        var isManagerOrAdmin = currentPerson.IsManagerOrAdmin();
        foreach (var row in rows)
        {
            row.CanDelete = isManagerOrAdmin && currentPerson.IsAssignedToStormwaterJurisdiction(row.StormwaterJurisdictionID);
        }
        return rows;
    }

    // Manager Dashboard: bulk-verify a set of BMP inventory records. Jurisdiction-scoped via
    // .CanView; silently drops anything outside the caller's reach. Returns verified count.
    public static async Task<int> BulkMarkAsVerifiedAsync(NeptuneDbContext dbContext, IList<int> treatmentBMPIDs, Person currentPerson)
    {
        if (treatmentBMPIDs == null || treatmentBMPIDs.Count == 0) return 0;

        var viewableJurisdictionIDs = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForBMPs(dbContext, currentPerson).ToList();
        var treatmentBMPs = await dbContext.TreatmentBMPs
            .Where(x => treatmentBMPIDs.Contains(x.TreatmentBMPID)
                && viewableJurisdictionIDs.Contains(x.StormwaterJurisdictionID))
            .ToListAsync();
        foreach (var treatmentBMP in treatmentBMPs)
        {
            treatmentBMP.MarkAsVerified(currentPerson);
        }
        await dbContext.SaveChangesAsync();
        return treatmentBMPs.Count;
    }

    public static async Task<List<TreatmentBMPDelineationMapDto>> ListForDelineationMapAsync(NeptuneDbContext dbContext, Person person)
    {
        var isAdmin = person.RoleID == (int)RoleEnum.Admin || person.RoleID == (int)RoleEnum.SitkaAdmin;
        var jurisdictionIDs = isAdmin
            ? null
            : await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, person.PersonID);

        var rows = await dbContext.TreatmentBMPs
            .AsNoTracking()
            .Where(x => x.ProjectID == null && (jurisdictionIDs == null || jurisdictionIDs.Contains(x.StormwaterJurisdictionID)))
            .Select(x => new
            {
                x.TreatmentBMPID,
                x.TreatmentBMPName,
                TreatmentBMPTypeName = x.TreatmentBMPType.TreatmentBMPTypeName,
                x.StormwaterJurisdictionID,
                x.LocationPoint4326,
                DelineationID = (int?)(x.Delineation != null ? x.Delineation.DelineationID : (int?)null),
                DelineationTypeID = (int?)(x.Delineation != null ? x.Delineation.DelineationTypeID : (int?)null),
                IsVerified = (bool?)(x.Delineation != null ? x.Delineation.IsVerified : (bool?)null),
            })
            .ToListAsync();

        return rows.Select(x => new TreatmentBMPDelineationMapDto
        {
            TreatmentBMPID = x.TreatmentBMPID,
            TreatmentBMPName = x.TreatmentBMPName,
            TreatmentBMPTypeName = x.TreatmentBMPTypeName,
            StormwaterJurisdictionID = x.StormwaterJurisdictionID,
            Latitude = x.LocationPoint4326?.Coordinate.Y ?? 0,
            Longitude = x.LocationPoint4326?.Coordinate.X ?? 0,
            HasDelineation = x.DelineationID.HasValue,
            DelineationID = x.DelineationID,
            DelineationTypeID = x.DelineationTypeID,
            IsVerified = x.IsVerified,
        }).ToList();
    }

    public static IQueryable<TreatmentBMP> GetNonPlanningModuleBMPs(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.ProjectID == null);
    }

    private static IQueryable<TreatmentBMP> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPs
            .Include(x => x.TreatmentBMPType)
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .Include(x => x.OwnerOrganization)
            .Include(x => x.UpstreamBMP)
            .Include(x => x.InventoryVerifiedByPerson)
            .Include(x => x.WaterQualityManagementPlan)
            .Include(x => x.CustomAttributes)
            .ThenInclude(x => x.CustomAttributeValues);
    }

    public static TreatmentBMP GetByIDWithChangeTracking(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var treatmentBMP = GetImpl(dbContext)
            .SingleOrDefault(x => x.TreatmentBMPID == treatmentBMPID);

        Check.RequireNotNull(treatmentBMP, $"TreatmentBMP with ID {treatmentBMPID} not found!");

        return treatmentBMP;
    }

    public static async Task<List<TreatmentBMPDisplayDto>> ListByProjectIDsAsDisplayDtoAsync(NeptuneDbContext dbContext, List<int> projectIDs)
    {
        var treatmentBMPs = await ListTreatmentBMPsDisplayOnlyAsync(dbContext,
            q => q.Where(x => x.ProjectID.HasValue && projectIDs.Contains(x.ProjectID.Value)));

        return await ListAsDisplayDtosAsync(dbContext, treatmentBMPs);
    }

    private static async Task<List<TreatmentBMPDisplayDto>> ListAsDisplayDtosAsync(NeptuneDbContext dbContext, List<TreatmentBMP> treatmentBMPs)
    {
        var treatmentBMPModelingAttributes = await dbContext.vTreatmentBMPModelingAttributes.ToListAsync();

        var treatmentBMPDisplayDtos = treatmentBMPs
            .GroupJoin(treatmentBMPModelingAttributes,
                       x => x.TreatmentBMPID,
                       y => y.TreatmentBMPID,
                       (x, y) => new { TreatmentBMP = x, TreatmentBmpModelingAttribute = y.SingleOrDefault() })
            .Select(x => x.TreatmentBMP.AsDisplayDto(x.TreatmentBmpModelingAttribute))
            .ToList();

        return treatmentBMPDisplayDtos;
    }

    private static FeatureCollection AsFeatureCollection(List<TreatmentBMP> treatmentBMPs)
    {
        var featureCollection = new FeatureCollection();
        foreach (var treatmentBMP in treatmentBMPs)
        {
            var attributesTable = new AttributesTable
            {
                { "TreatmentBMPID", treatmentBMP.TreatmentBMPID },
                { "TreatmentBMPName", treatmentBMP.TreatmentBMPName },
                //{ "TreatmentBMPTypeID", treatmentBMP.TreatmentBMPTypeID },
                { "TreatmentBMPTypeName", treatmentBMP.TreatmentBMPType.TreatmentBMPTypeName },
                //{ "StormwaterJurisdictionID", treatmentBMP.StormwaterJurisdictionID },
                //{ "Latitude", treatmentBMP.LocationPoint4326?.Coordinate.Y},
                //{ "Longitude", treatmentBMP.LocationPoint4326?.Coordinate.Z},
                { "FeatureColor", $"#{treatmentBMP.TrashCaptureStatusType.TrashCaptureStatusTypeColorCode}" },
                { "TrashCaptureStatusTypeID", treatmentBMP.TrashCaptureStatusTypeID },
                { "StormwaterJurisdictionID", treatmentBMP.StormwaterJurisdictionID }
            };

            var feature = new Feature(treatmentBMP.LocationPoint4326, attributesTable);
            featureCollection.Add(feature);
        }

        return featureCollection;
    }

    public static async Task<FeatureCollection> ListInventoryIsVerifiedByPersonAsFeatureCollectionAsync(NeptuneDbContext dbContext, PersonDto person)
    {
        var treatmentBmps = await ListByPersonAsync(dbContext, person);

        return AsFeatureCollection(treatmentBmps.Where(x => x.ProjectID == null && x.InventoryIsVerified).ToList());
    }

    public static async Task<FeatureCollection> ListInventoryIsVerifiedByPersonAndJurisdictionIDAsFeatureCollectionAsync(
        NeptuneDbContext dbContext,
        PersonDto person,
        int jurisdictionID)
    {
        var treatmentBmps = await ListByPersonAsync(dbContext, person, false);

        return AsFeatureCollection(treatmentBmps.Where(x => x.ProjectID == null && x.StormwaterJurisdictionID == jurisdictionID && x.InventoryIsVerified).ToList());
    }

    public static async Task<List<TreatmentBMPDisplayDto>> ListWithProjectByPersonAsDisplayDtoAsync(NeptuneDbContext dbContext, PersonDto person)
    {
        var jurisdictionIDs = (person == null || !(person.RoleID == (int)RoleEnum.Admin || person.RoleID == (int)RoleEnum.SitkaAdmin))
            ? await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, person?.PersonID)
            : null;

        var treatmentBmps = await ListTreatmentBMPsDisplayOnlyAsync(dbContext,
            q =>
            {
                q = q.Where(x => x.ProjectID != null);
                if (jurisdictionIDs != null)
                {
                    q = q.Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID));
                }

                return q;
            });

        return await ListAsDisplayDtosAsync(dbContext, treatmentBmps);
    }

    public static async Task<List<TreatmentBMPDisplayDto>> ListWithOCTAM2Tier2GrantProgramByPersonAsDisplayDtoAsync(NeptuneDbContext dbContext, PersonDto person)
    {
        var treatmentBmps = (await ListByPersonAsync(dbContext, person)).Where(x => x.Project is { ShareOCTAM2Tier2Scores: true }).ToList();
        return await ListAsDisplayDtosAsync(dbContext, treatmentBmps);
    }

    private static async Task<List<TreatmentBMP>> ListByPersonAsync(NeptuneDbContext dbContext, PersonDto? person, bool checkIsAnalyzedInModelingModule = true)
    {
        var jurisdictionIDs = (person == null || !(person.RoleID == (int)RoleEnum.Admin || person.RoleID == (int)RoleEnum.SitkaAdmin))
            ? await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, person?.PersonID)
            : null;

        return await ListTreatmentBMPsDisplayOnlyAsync(dbContext,
            q => jurisdictionIDs != null
                ? q.Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
                : q,
            checkIsAnalyzedInModelingModule);
    }

    public static List<TreatmentBMPUpsertDto> ListByProjectIDAsUpsertDto(NeptuneDbContext dbContext, int projectID)
    {
        var treatmentBMPs = dbContext.TreatmentBMPs
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .Include(x => x.TreatmentBMPType)
            .Include(x => x.Watershed)
            .Include(x => x.OwnerOrganization)
            .Include(x => x.Delineation)
            .Include(x => x.CustomAttributes)
            .ThenInclude(x => x.CustomAttributeValues)
            .Include(x => x.CustomAttributes)
            .ThenInclude(x => x.CustomAttributeType)
            .AsNoTracking()
            .Where(x => x.ProjectID == projectID)
            .ToList();

        var treatmentBMPIDs = treatmentBMPs.Select(x => x.TreatmentBMPID).ToList();

        var treatmentBMPModelingAttributes = dbContext.vTreatmentBMPModelingAttributes.Where(x => treatmentBMPIDs.Contains(x.TreatmentBMPID)).ToList();

        var treatmentBMPUpsertDtos = treatmentBMPs
            .GroupJoin(treatmentBMPModelingAttributes,
                       x => x.TreatmentBMPID,
                       y => y.TreatmentBMPID,
                       (x, y) => new { TreatmentBMP = x, TreatmentBmpModelingAttribute = y.SingleOrDefault() })
            .Select(x => x.TreatmentBMP.AsUpsertDtoWithModelingAttributes(x.TreatmentBmpModelingAttribute))
            .ToList();

        return treatmentBMPUpsertDtos;
    }

    public static List<TreatmentBMPTypeWithModelingAttributesDto> ListWithModelingAttributesAsDto(
        NeptuneDbContext dbContext)
    {
        var treatmentBMPTypeWithModelingAttributesDtos = dbContext.TreatmentBMPTypes
            .Include(x => x.TreatmentBMPTypeCustomAttributeTypes)
            .ThenInclude(x => x.CustomAttributeType)
            .AsNoTracking()
            .OrderBy(x => x.TreatmentBMPTypeName)
            .Select(x =>
                        new TreatmentBMPTypeWithModelingAttributesDto()
                        {
                            TreatmentBMPTypeID = x.TreatmentBMPTypeID,
                            TreatmentBMPTypeName = x.TreatmentBMPTypeName,
                            TreatmentBMPModelingTypeID = x.TreatmentBMPModelingTypeID,
                            TreatmentBMPModelingAttributes = x.GetModelingAttributes()
                        }
                   )
            .ToList();

        return treatmentBMPTypeWithModelingAttributesDtos;
    }

    public static async Task<TreatmentBMPDto> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var dto = await dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .Select(TreatmentBMPDtoProjections.AsDto)
            .SingleAsync();

        ResolveClientSideLookups(dto);

        // Fetch supplemental data for Delineation GeoJSON and OtherTreatmentBMPsExistInSubbasin
        var supplemental = await dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.TreatmentBMPID == treatmentBMPID)
            .Select(x => new
            {
                x.LocationPoint,
                DelineationID = x.Delineation != null ? (int?)x.Delineation.DelineationID : null,
                DelineationTreatmentBMPID = x.Delineation != null ? (int?)x.Delineation.TreatmentBMPID : null,
                DelineationGeometry4326 = x.Delineation != null ? x.Delineation.DelineationGeometry4326 : null
            })
            .SingleAsync();

        // Resolve Delineation GeoJSON and DelineationTypeName
        if (dto.Delineation != null && supplemental.DelineationGeometry4326 != null)
        {
            var attributesTable = new AttributesTable
            {
                { "DelineationID", supplemental.DelineationID },
                { "TreatmentBMPID", supplemental.DelineationTreatmentBMPID }
            };
            var feature = new Feature(supplemental.DelineationGeometry4326, attributesTable);
            dto.Delineation.Geometry = GeoJsonSerializer.Serialize(feature);
        }

        // OtherTreatmentBMPsExistInSubbasin requires a spatial query with LocationPoint (EPSG 2771)
        if (supplemental.LocationPoint != null)
        {
            var subregion = dbContext.RegionalSubbasins.AsNoTracking()
                .SingleOrDefault(x => x.CatchmentGeometry.Contains(supplemental.LocationPoint));
            var otherBMPsInSubregion = subregion?.GetTreatmentBMPs(dbContext);
            dto.OtherTreatmentBMPsExistInSubbasin = otherBMPsInSubregion?.Where(x => x.TreatmentBMPID != treatmentBMPID).Any() ?? false;
        }

        // Compute HasSettableBenchmarkAndThresholdValues using static ObservationTypeSpecification lookup
        var specIdsWithBenchmarks = ObservationTypeSpecification.All
            .Where(s => s.ObservationThresholdTypeID != (int)ObservationThresholdTypeEnum.None)
            .Select(s => s.ObservationTypeSpecificationID)
            .ToList();

        dto.HasSettableBenchmarkAndThresholdValues = await dbContext.TreatmentBMPTypeAssessmentObservationTypes
            .AnyAsync(x => x.TreatmentBMPTypeID == dto.TreatmentBMPTypeID
                && specIdsWithBenchmarks.Contains(x.TreatmentBMPAssessmentObservationType.ObservationTypeSpecificationID));

        // IsFullyParameterized is a C# computation (joins BMP + type + modeling config + the
        // vTreatmentBMPModelingAttributes view + delineation verification status), so it can't
        // live in the EF expression projection — load the inputs and call the existing helper.
        // Previously left null on the DTO, which made the SPA detail page always render the
        // "missing fields required to calculate model results" fallback inside the Modeled BMP
        // Performance panel even for fully-parameterized BMPs.
        // TreatmentBMPModelingType is a static lookup class (resolved by ID via the generated
        // AllLookupDictionary at C# runtime), not an EF navigation — don't try to ThenInclude it.
        var bmpForParameterizationCheck = await dbContext.TreatmentBMPs.AsNoTracking()
            .Include(x => x.TreatmentBMPType)
            .SingleAsync(x => x.TreatmentBMPID == treatmentBMPID);
        // IsFullyParameterized's comment: "assumes the delineation passed in is the from the
        // 'upstreamest' BMP" — downstream BMPs inherit their upstream's verified delineation.
        // Mirror the pattern in vTreatmentBMPUpstreams.ListWithDelineationAsDictionary: read
        // the upstream BMP ID from the tree view, and load that BMP's delineation if present;
        // otherwise this BMP's own.
        var upstreamRow = await dbContext.vTreatmentBMPUpstreams.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TreatmentBMPID == treatmentBMPID);
        var delineationBMPID = upstreamRow?.UpstreamBMPID ?? treatmentBMPID;
        var delineationForParameterizationCheck = await dbContext.Delineations.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TreatmentBMPID == delineationBMPID);
        var modelingAttribute = await dbContext.vTreatmentBMPModelingAttributes.AsNoTracking()
            .SingleOrDefaultAsync(x => x.TreatmentBMPID == treatmentBMPID);
        dto.IsFullyParameterized = bmpForParameterizationCheck.IsFullyParameterized(delineationForParameterizationCheck, modelingAttribute);

        if (dto.UpstreamBMPID.HasValue)
        {
            dto.UpstreamBMP = await GetByIDAsDtoAsync(dbContext, dto.UpstreamBMPID.Value);
        }

        return dto;
    }

    private static void ResolveClientSideLookups(TreatmentBMPDto dto)
    {
        // Resolve SizingBasisType names
        if (dto.SizingBasisType != null && SizingBasisType.AllLookupDictionary.TryGetValue(dto.SizingBasisType.SizingBasisTypeID, out var sizingBasisType))
        {
            dto.SizingBasisType.SizingBasisTypeName = sizingBasisType.SizingBasisTypeName;
            dto.SizingBasisType.SizingBasisTypeDisplayName = sizingBasisType.SizingBasisTypeDisplayName;
        }

        // Resolve TrashCaptureStatusType names
        if (dto.TrashCaptureStatusType != null && TrashCaptureStatusType.AllLookupDictionary.TryGetValue(dto.TrashCaptureStatusType.TrashCaptureStatusTypeID, out var trashCaptureStatusType))
        {
            dto.TrashCaptureStatusType.TrashCaptureStatusTypeName = trashCaptureStatusType.TrashCaptureStatusTypeName;
            dto.TrashCaptureStatusType.TrashCaptureStatusTypeDisplayName = trashCaptureStatusType.TrashCaptureStatusTypeDisplayName;
        }

        // Resolve TreatmentBMPLifespanType names
        if (dto.TreatmentBMPLifespanType != null && TreatmentBMPLifespanType.AllLookupDictionary.TryGetValue(dto.TreatmentBMPLifespanType.TreatmentBMPLifeSpanTypeID, out var lifespanType))
        {
            dto.TreatmentBMPLifespanType.TreatmentBMPLifeSpanTypeName = lifespanType.TreatmentBMPLifespanTypeName;
            dto.TreatmentBMPLifespanType.TreatmentBMPLifeSpanTypeDisplayName = lifespanType.TreatmentBMPLifespanTypeDisplayName;
        }

        // Resolve DelineationType name
        if (dto.Delineation != null && DelineationType.AllLookupDictionary.TryGetValue(dto.Delineation.DelineationTypeID, out var delineationType))
        {
            dto.Delineation.DelineationTypeName = delineationType.DelineationTypeDisplayName;
        }
    }

    public static TreatmentBMP GetByID(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var treatmentBMP = GetImpl(dbContext)
            .AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPID == treatmentBMPID);

        Check.RequireNotNull(treatmentBMP, $"TreatmentBMP with ID {treatmentBMPID} not found!");

        return treatmentBMP;
    }


    public static TreatmentBMP GetByID(NeptuneDbContext dbContext, TreatmentBMPPrimaryKey treatmentBMPPrimaryKey)
    {
        return GetByID(dbContext, treatmentBMPPrimaryKey.PrimaryKeyValue);
    }

    public static TreatmentBMP? GetUpstreamestTreatmentBMP(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var treatmentBMPTree = dbContext.vTreatmentBMPUpstreams.AsNoTracking()
            .Single(x => x.TreatmentBMPID == treatmentBMPID);

        var upstreamestBMP = treatmentBMPTree.UpstreamBMPID.HasValue
            ? GetByID(dbContext, treatmentBMPTree.UpstreamBMPID)
            : null;

        return upstreamestBMP;
    }

    public static Dictionary<int, int> ListCountByStormwaterJurisdiction(NeptuneDbContext dbContext)
    {
        return dbContext.TreatmentBMPs.AsNoTracking()
            .GroupBy(x => x.StormwaterJurisdictionID)
            .Select(x => new { x.Key, Count = x.Count() })
            .ToDictionary(x => x.Key, x => x.Count);
    }

    /// <summary>
    /// NPT-1038 round 4: rows for the "Treatment BMPs of this Type" grid on the SPA
    /// `/program-info/treatment-bmp-types/{id}` page. Filters out planning-module BMPs
    /// (those with a ProjectID — the legacy MVC GridSpec query uses
    /// <see cref="GetNonPlanningModuleBMPs"/>), restricts to the caller's viewable
    /// jurisdictions, then loads SystemOfRecordID + WQMP linkage off the TreatmentBMP
    /// entity (not surfaced on the view) and CustomAttributeValues for each in-scope BMP
    /// (one extra query each, no N+1). Multi-value attributes are pre-joined with ", "
    /// server-side to mirror the legacy GridSpec.GetCustomAttributeValue helper.
    /// </summary>
    public static async Task<List<TreatmentBMPByTypeGridDto>> ListByTypeAsGridDtoForJurisdictionsAsync(
        NeptuneDbContext dbContext,
        int treatmentBMPTypeID,
        IEnumerable<int> stormwaterJurisdictionIDsPersonCanView)
    {
        var jurisdictionIDs = stormwaterJurisdictionIDsPersonCanView.ToList();

        // SystemOfRecordID + WQMP linkage live on the TreatmentBMP entity (not on the view),
        // and we need the same query to filter ProjectID == null (planning-module exclusion),
        // so run the entity query first and capture both the eligible BMP IDs and the entity
        // fields in one pass. Legacy MVC TreatmentBMPTypeController.TreatmentBMPsInTreatmentBMPTypeGridJsonData
        // uses GetNonPlanningModuleBMPs + Where(TreatmentBMPTypeID && jurisdictionIDs) — same shape.
        var entityRows = await dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => x.ProjectID == null
                        && x.TreatmentBMPTypeID == treatmentBMPTypeID
                        && jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
            .Select(x => new
            {
                x.TreatmentBMPID,
                x.SystemOfRecordID,
                x.WaterQualityManagementPlanID,
                WaterQualityManagementPlanName = x.WaterQualityManagementPlan != null ? x.WaterQualityManagementPlan.WaterQualityManagementPlanName : null,
            })
            .ToListAsync();

        if (entityRows.Count == 0) return new List<TreatmentBMPByTypeGridDto>();

        var bmpIDs = entityRows.Select(x => x.TreatmentBMPID).ToList();
        var entityLookup = entityRows.ToDictionary(x => x.TreatmentBMPID);

        var rows = await dbContext.vTreatmentBMPDetaileds.AsNoTracking()
            .Where(x => bmpIDs.Contains(x.TreatmentBMPID))
            .OrderBy(x => x.TreatmentBMPName)
            .ToListAsync();

        // One query for every custom-attribute-value of every in-scope BMP. Group by BMPID +
        // CustomAttributeTypeID so multi-value attributes collapse into a single ", "-joined
        // string per attribute (matches the legacy MVC's `GetCustomAttributeValue` join).
        var attributeRows = await dbContext.CustomAttributes.AsNoTracking()
            .Where(ca => bmpIDs.Contains(ca.TreatmentBMPID))
            .Select(ca => new
            {
                ca.TreatmentBMPID,
                ca.CustomAttributeTypeID,
                Values = ca.CustomAttributeValues
                    .OrderBy(v => v.AttributeValue)
                    .Select(v => v.AttributeValue)
                    .ToList(),
            })
            .ToListAsync();

        var attributeLookup = attributeRows
            .GroupBy(x => x.TreatmentBMPID)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    x => x.CustomAttributeTypeID,
                    x => string.Join(", ", x.Values)));

        return rows.Select(x => new TreatmentBMPByTypeGridDto
        {
            TreatmentBMPID = x.TreatmentBMPID,
            TreatmentBMPName = x.TreatmentBMPName,
            StormwaterJurisdictionID = x.StormwaterJurisdictionID,
            StormwaterJurisdictionName = x.OrganizationName,
            OwnerOrganizationName = x.OwnerOrganizationName,
            YearBuilt = x.YearBuilt,
            SystemOfRecordID = entityLookup.TryGetValue(x.TreatmentBMPID, out var e) ? e.SystemOfRecordID : null,
            WaterQualityManagementPlanID = entityLookup.TryGetValue(x.TreatmentBMPID, out var e2) ? e2.WaterQualityManagementPlanID : null,
            WaterQualityManagementPlanName = entityLookup.TryGetValue(x.TreatmentBMPID, out var e3) ? e3.WaterQualityManagementPlanName : null,
            Notes = x.Notes,
            LatestAssessmentDate = x.LatestAssessmentDate,
            LatestAssessmentScore = x.LatestAssessmentScore,
            NumberOfAssessments = x.NumberOfAssessments,
            LatestMaintenanceDate = x.LatestMaintenanceDate,
            NumberOfMaintenanceRecords = x.NumberOfMaintenanceRecords,
            BenchmarkAndThresholdSet = x.NumberOfBenchmarkAndThresholds == x.NumberOfBenchmarkAndThresholdsEntered,
            TreatmentBMPLifespanTypeDisplayName = x.TreatmentBMPLifespanTypeDisplayName,
            TreatmentBMPLifespanEndDate = x.TreatmentBMPLifespanEndDate,
            RequiredFieldVisitsPerYear = x.RequiredFieldVisitsPerYear,
            RequiredPostStormFieldVisitsPerYear = x.RequiredPostStormFieldVisitsPerYear,
            SizingBasisTypeDisplayName = x.SizingBasisTypeDisplayName,
            TrashCaptureStatusTypeDisplayName = x.TrashCaptureStatusTypeDisplayName,
            DelineationTypeDisplayName = x.DelineationTypeDisplayName,
            CustomAttributeValues = attributeLookup.TryGetValue(x.TreatmentBMPID, out var attrs) ? attrs : new Dictionary<int, string>(),
        }).ToList();
    }

    public static TreatmentBMP GetByIDForFeatureContextCheck(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        var treatmentBMP = dbContext.TreatmentBMPs
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .AsNoTracking()
            .SingleOrDefault(x => x.TreatmentBMPID == treatmentBMPID);

        Check.RequireNotNull(treatmentBMP, $"TreatmentBMP with ID {treatmentBMPID} not found!");

        return treatmentBMP;
    }

    public static List<TreatmentBMP> ListByWaterQualityManagementPlanIDWithChangeTracking(
        NeptuneDbContext dbContext,
        int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext)
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .ToList();
    }

    public static int? ChangeTreatmentBMPType(NeptuneDbContext dbContext, int treatmentBMPID, int treatmentBMPTypeID)
    {
        dbContext.Database.ExecuteSqlRaw(
                                         "EXECUTE dbo.pTreatmentBMPUpdateTreatmentBMPType @treatmentBMPID={0}, @treatmentBMPTypeID={1}",
                                         treatmentBMPID, treatmentBMPTypeID);

        var treatmentBMPModelingType = dbContext.TreatmentBMPTypes
            .SingleOrDefault(x => x.TreatmentBMPTypeID == treatmentBMPTypeID)
            ?.TreatmentBMPModelingTypeID;

        return treatmentBMPModelingType;
    }

    public static TreatmentBMP? GetByTreatmentBMPID(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return dbContext.TreatmentBMPs.SingleOrDefault(x => x.TreatmentBMPID == treatmentBMPID);
    }

    public static Geometry CreateLocationPoint4326FromLatLong(double latitude, double longitude)
    {
        return new Point(longitude, latitude) { SRID = 4326 };
    }

    public static TreatmentBMP TreatmentBMPFromUpsertDtoAndProject(NeptuneDbContext dbContext,
                                                                   TreatmentBMPUpsertDto treatmentBMPUpsertDto,
                                                                   Project project)
    {
        var locationPointGeometry4326 = CreateLocationPoint4326FromLatLong(treatmentBMPUpsertDto.Latitude.Value,
                                                                           treatmentBMPUpsertDto.Longitude.Value);

        var locationPoint = locationPointGeometry4326.ProjectTo2771();
        var treatmentBMP = new TreatmentBMP()
        {
            TreatmentBMPName = treatmentBMPUpsertDto.TreatmentBMPName,
            TreatmentBMPTypeID = treatmentBMPUpsertDto.TreatmentBMPTypeID.Value,
            ProjectID = project.ProjectID,
            StormwaterJurisdictionID = project.StormwaterJurisdictionID,
            OwnerOrganizationID = project.OrganizationID,
            LocationPoint4326 = locationPointGeometry4326,
            LocationPoint = locationPoint,
            Notes = treatmentBMPUpsertDto.Notes,
            InventoryIsVerified = false,
            TrashCaptureStatusTypeID = (int)TrashCaptureStatusTypeEnum.NotProvided,
            SizingBasisTypeID = (int)SizingBasisTypeEnum.NotProvided
        };

        treatmentBMP.SetTreatmentBMPPointInPolygonDataByLocationPoint(locationPoint, dbContext);

        if (treatmentBMPUpsertDto.TreatmentBMPID > 0)
        {
            treatmentBMP.TreatmentBMPID = treatmentBMPUpsertDto.TreatmentBMPID;
        }

        return treatmentBMP;
    }

    public static List<TreatmentBMP> ListModelingTreatmentBMPs(NeptuneDbContext dbContext,
                                                               int? projectID = null,
                                                               List<int>? projectRSBIDs = null)
    {
        var toReturn = dbContext.TreatmentBMPs
            .Include(x => x.TreatmentBMPType)
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .Include(x => x.OwnerOrganization)
            .Include(x => x.WaterQualityManagementPlan)
            .AsNoTracking()
            .Where(x => x.RegionalSubbasinID != null && x.TreatmentBMPType.TreatmentBMPModelingTypeID != null &&
                        x.ModelBasinID != null)
            .ToList();

        if (projectID != null && projectRSBIDs != null)
        {
            toReturn = toReturn.Where(x =>
                                          projectRSBIDs.Contains(x.RegionalSubbasinID.Value) &&
                                          (x.ProjectID == null || x.ProjectID == projectID))
                .ToList();
        }
        else
        {
            toReturn = toReturn.Where(x => x.ProjectID == null).ToList();
        }

        return toReturn;
    }

    public static async Task<List<TreatmentBMPModelingAttributesDto>> ListWithModelingAttributesAsync(NeptuneDbContext dbContext, List<int> stormwaterJurisdictionIDsPersonCanView)
    {
        var treatmentBmps = await dbContext.TreatmentBMPs
            .Include(x => x.TreatmentBMPType)
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .Include(x => x.UpstreamBMP)
            .ThenInclude(x => x.TreatmentBMPType)
            .Include(x => x.Watershed)
            .Include(x => x.Delineation)
            .AsNoTracking()
            .Where(x => x.TreatmentBMPType.IsAnalyzedInModelingModule &&
                        stormwaterJurisdictionIDsPersonCanView.Contains(x.StormwaterJurisdictionID))
            .ToListAsync();

        var delineations = vTreatmentBMPUpstreams.ListWithDelineationAsDictionaryIncludeTreatmentBMPType(dbContext);
        var watersheds = await dbContext.Watersheds.AsNoTracking().Select(x => new { x.WatershedID, x.WatershedName }).ToDictionaryAsync(x => x.WatershedID, x => x.WatershedName);
        var precipitationZones = await dbContext.PrecipitationZones.AsNoTracking()
            .Select(x => new { x.PrecipitationZoneID, x.DesignStormwaterDepthInInches })
            .ToDictionaryAsync(x => x.PrecipitationZoneID, x => x.DesignStormwaterDepthInInches);

        var modeledLandUseAreas = await dbContext.vTreatmentBMPModeledLandUseAreas.AsNoTracking().ToDictionaryAsync(x => x.TreatmentBMPID.Value, x => x.Area);
        var modelingAttributes = await dbContext.vTreatmentBMPModelingAttributes.AsNoTracking().ToDictionaryAsync(x => x.TreatmentBMPID, x => x);

        return treatmentBmps.Select(bmp =>
            {
                var delineation = bmp.Delineation ?? (delineations.GetValueOrDefault(bmp.TreatmentBMPID));
                var modeling = modelingAttributes.GetValueOrDefault(bmp.TreatmentBMPID);
                var watershedName = bmp.WatershedID.HasValue && watersheds.TryGetValue(bmp.WatershedID.Value, out var watershed) ? watershed : null;
                var precipitationDepth = bmp.PrecipitationZoneID.HasValue && precipitationZones.TryGetValue(bmp.PrecipitationZoneID.Value, out var zone) ? zone : (double?)null;
                var modeledArea = modeledLandUseAreas.GetValueOrDefault(bmp.TreatmentBMPID, null);

                return new TreatmentBMPModelingAttributesDto
                {
                    TreatmentBMPID = bmp.TreatmentBMPID,
                    TreatmentBMPName = bmp.TreatmentBMPName,
                    TreatmentBMPTypeID = bmp.TreatmentBMPTypeID,
                    TreatmentBMPTypeName = bmp.TreatmentBMPType?.TreatmentBMPTypeName,
                    StormwaterJurisdictionID = bmp.StormwaterJurisdictionID,
                    StormwaterJurisdictionName = bmp.StormwaterJurisdiction?.Organization?.OrganizationName,
                    WatershedID = bmp.WatershedID,
                    WatershedName = watershedName,
                    PrecipitationZoneID = bmp.PrecipitationZoneID,
                    DesignStormwaterDepthInInches = precipitationDepth,
                    DelineationID = delineation?.DelineationID,
                    DelineationTypeName = delineation?.DelineationType?.DelineationTypeDisplayName,
                    DelineationStatus = delineation?.GetDelineationStatus(),
                    DelineationAreaAcres = delineation?.GetDelineationArea(),
                    ModeledLandUseAreaAcres = modeledArea,
                    IsFullyParameterized = bmp.IsFullyParameterized(delineation, modeling),
                    AverageDivertedFlowrate = modeling?.AverageDivertedFlowrate,
                    AverageTreatmentFlowrate = modeling?.AverageTreatmentFlowrate,
                    DesignDryWeatherTreatmentCapacity = modeling?.DesignDryWeatherTreatmentCapacity,
                    DesignLowFlowDiversionCapacity = modeling?.DesignLowFlowDiversionCapacity,
                    DesignMediaFiltrationRate = modeling?.DesignMediaFiltrationRate,
                    DrawdownTimeForWQDetentionVolume = modeling?.DrawdownTimeForWQDetentionVolume,
                    EffectiveFootprint = modeling?.EffectiveFootprint,
                    EffectiveRetentionDepth = modeling?.EffectiveRetentionDepth,
                    InfiltrationDischargeRate = modeling?.InfiltrationDischargeRate,
                    InfiltrationSurfaceArea = modeling?.InfiltrationSurfaceArea,
                    MediaBedFootprint = modeling?.MediaBedFootprint,
                    MonthsOperational = modeling?.MonthsOperational,
                    PermanentPoolOrWetlandVolume = modeling?.PermanentPoolOrWetlandVolume,
                    StorageVolumeBelowLowestOutletElevation = modeling?.StorageVolumeBelowLowestOutletElevation,
                    SummerHarvestedWaterDemand = modeling?.SummerHarvestedWaterDemand,
                    TimeOfConcentration = modeling?.TimeOfConcentration,
                    TotalEffectiveBMPVolume = modeling?.TotalEffectiveBMPVolume,
                    TotalEffectiveDrywellBMPVolume = modeling?.TotalEffectiveDrywellBMPVolume,
                    TreatmentRate = modeling?.TreatmentRate,
                    UnderlyingHydrologicSoilGroup = modeling?.UnderlyingHydrologicSoilGroup,
                    UnderlyingInfiltrationRate = modeling?.UnderlyingInfiltrationRate,
                    ExtendedDetentionSurchargeVolume = modeling?.ExtendedDetentionSurchargeVolume,
                    WettedFootprint = modeling?.WettedFootprint,
                    WinterHarvestedWaterDemand = modeling?.WinterHarvestedWaterDemand,
                    UpstreamBMPID = bmp.UpstreamBMPID,
                    UpstreamBMPName = bmp.UpstreamBMP?.TreatmentBMPName,
                    DownstreamOfNonModeledBMP = bmp is { UpstreamBMPID: not null, UpstreamBMP: not null } && !(bmp.UpstreamBMP.TreatmentBMPType?.IsAnalyzedInModelingModule ?? true),
                    DryWeatherFlowOverride = modeling?.DryWeatherFlowOverride
                };
            })
            .ToList();
    }

    public static async Task<List<ErrorMessage>> ValidateUpdateBasicInfoAsync(NeptuneDbContext dbContext, int treatmentBMPID, TreatmentBMPBasicInfoUpdateDto updateDto)
    {
        var errors = await ValidateBasicInfoAsync(dbContext, updateDto, treatmentBMPID);
        return errors;
    }

    public static async Task<TreatmentBMPDto> UpdateBasicInfoAsync(NeptuneDbContext dbContext, int treatmentBMPID, TreatmentBMPBasicInfoUpdateDto updateDto, PersonDto callingUser)
    {
        var treatmentBMPToUpdate = dbContext.TreatmentBMPs
            .Include(x => x.StormwaterJurisdiction)
            .Single(x => x.TreatmentBMPID == treatmentBMPID);

        treatmentBMPToUpdate.TreatmentBMPName = updateDto.TreatmentBMPName;
        treatmentBMPToUpdate.OwnerOrganizationID = updateDto.OwnerOrganizationID ?? treatmentBMPToUpdate.StormwaterJurisdiction.OrganizationID;
        treatmentBMPToUpdate.YearBuilt = updateDto.YearBuilt;
        treatmentBMPToUpdate.SystemOfRecordID = updateDto.SystemOfRecordID;
        treatmentBMPToUpdate.WaterQualityManagementPlanID = updateDto.WaterQualityManagementPlanID;
        treatmentBMPToUpdate.TreatmentBMPLifespanTypeID = updateDto.TreatmentBMPLifespanTypeID;
        treatmentBMPToUpdate.TreatmentBMPLifespanEndDate = updateDto.TreatmentBMPLifespanTypeID.HasValue
            && updateDto.TreatmentBMPLifespanTypeID.Value == TreatmentBMPLifespanType.FixedEndDate.TreatmentBMPLifespanTypeID
            ? updateDto.TreatmentBMPLifespanEndDate
            : null;

        treatmentBMPToUpdate.SizingBasisTypeID = updateDto.SizingBasisTypeID;
        treatmentBMPToUpdate.TrashCaptureStatusTypeID = updateDto.TrashCaptureStatusTypeID;
        treatmentBMPToUpdate.TrashCaptureEffectiveness = updateDto.TrashCaptureStatusTypeID == TrashCaptureStatusType.Partial.TrashCaptureStatusTypeID
            ? updateDto.TrashCaptureEffectiveness
            : null;

        treatmentBMPToUpdate.RequiredFieldVisitsPerYear = updateDto.RequiredFieldVisitsPerYear;
        treatmentBMPToUpdate.RequiredPostStormFieldVisitsPerYear = updateDto.RequiredPostStormFieldVisitsPerYear;
        treatmentBMPToUpdate.Notes = updateDto.Notes;

        await dbContext.SaveChangesAsync();
        await dbContext.Entry(treatmentBMPToUpdate).ReloadAsync();

        var updatedTreatmentBMPDto = await GetByIDAsDtoAsync(dbContext, treatmentBMPID);

        return updatedTreatmentBMPDto;
    }

    // Shared validation for fields present on both Create and BasicInfo Update DTOs
    private static async Task<List<ErrorMessage>> ValidateBasicInfoAsync(NeptuneDbContext dbContext, IHaveTreatmentBMPBasicInfo dto, int? existingTreatmentBMPID = null)
    {
        var errors = new List<ErrorMessage>();

        // Validate Name Uniqueness (exclude existingTreatmentBMPID when updating)
        var hasUniqueName = await dbContext.TreatmentBMPs.AsNoTracking()
            .AllAsync(x => x.TreatmentBMPID == existingTreatmentBMPID || x.TreatmentBMPName != dto.TreatmentBMPName);

        if (!hasUniqueName)
        {
            errors.Add(new ErrorMessage("TreatmentBMPName", "Treatment BMP Name must be unique."));
        }

        // Owner organization (optional)
        if (dto.OwnerOrganizationID.HasValue)
        {
            var hasValidOwner = await dbContext.Organizations.AnyAsync(x => x.OrganizationID == dto.OwnerOrganizationID.Value);
            if (!hasValidOwner)
            {
                errors.Add(new ErrorMessage("OwnerOrganizationID", "Valid Owner Organization is required."));
            }
        }

        // Sizing basis
        var hasValidSizingBasis = SizingBasisType.All.Any(x => x.SizingBasisTypeID == dto.SizingBasisTypeID);
        if (!hasValidSizingBasis)
        {
            errors.Add(new ErrorMessage("SizingBasisTypeID", "Valid Sizing Basis Type is required."));
        }

        // Trash capture status
        var hasValidTrashCapture = TrashCaptureStatusType.All.Any(x => x.TrashCaptureStatusTypeID == dto.TrashCaptureStatusTypeID);
        if (!hasValidTrashCapture)
        {
            errors.Add(new ErrorMessage("TrashCaptureStatusTypeID", "Valid Trash Capture Status Type is required."));
        }

        // Lifespan type (optional — only validate when supplied)
        if (dto.TreatmentBMPLifespanTypeID.HasValue)
        {
            var hasValidLifespan = TreatmentBMPLifespanType.All.Any(x => x.TreatmentBMPLifespanTypeID == dto.TreatmentBMPLifespanTypeID.Value);
            if (!hasValidLifespan)
            {
                errors.Add(new ErrorMessage("TreatmentBMPLifespanTypeID", "Valid Lifespan Type is required."));
            }

            // Lifespan end date required if type is Fixed End Date
            if (dto.TreatmentBMPLifespanTypeID.Value == TreatmentBMPLifespanType.FixedEndDate.TreatmentBMPLifespanTypeID && !dto.TreatmentBMPLifespanEndDate.HasValue)
            {
                errors.Add(new ErrorMessage("LifespanEndDate", "The Lifespan End Date must be set if the Lifespan Type is Fixed End Date."));
            }
        }

        // Water quality management plan (optional — only validate when supplied)
        if (dto.WaterQualityManagementPlanID.HasValue)
        {
            var hasValidWQMP = await dbContext.WaterQualityManagementPlans.AnyAsync(x => x.WaterQualityManagementPlanID == dto.WaterQualityManagementPlanID.Value);
            if (!hasValidWQMP)
            {
                errors.Add(new ErrorMessage("WaterQualityManagementPlanID", "Valid Water Quality Management Plan is required."));
            }
        }

        return errors;
    }

    public static async Task<List<ErrorMessage>> ValidateUpdateLocationAsync(NeptuneDbContext dbContext, int treatmentBMPID, TreatmentBMPLocationUpdateDto locationUpdateDto)
    {
        var errors = new List<ErrorMessage>();

        return errors;
    }

    public static async Task<TreatmentBMPDto> UpdateLocationAsync(NeptuneDbContext dbContext, int treatmentBMPID, TreatmentBMPLocationUpdateDto locationUpdateDto, PersonDto callingUser)
    {
        var treatmentBMPToUpdate = dbContext.TreatmentBMPs
            .Include(x => x.StormwaterJurisdiction)
            .Include(x => x.InverseUpstreamBMP)
            .Single(x => x.TreatmentBMPID == treatmentBMPID);

        var locationPointGeometry4326 = CreateLocationPoint4326FromLatLong(locationUpdateDto.Latitude!.Value, locationUpdateDto.Longitude!.Value);
        var locationPoint = locationPointGeometry4326.ProjectTo2771();

        treatmentBMPToUpdate.LocationPoint4326 = locationPointGeometry4326;
        treatmentBMPToUpdate.LocationPoint = locationPoint;

        treatmentBMPToUpdate.SetTreatmentBMPPointInPolygonDataByLocationPoint(locationPoint, dbContext);

        UpdateUpstreamBMPReferencesIfNecessary(dbContext, treatmentBMPToUpdate);

        var existingDelineation = Delineations.GetByTreatmentBMPIDWithChangeTracking(dbContext, treatmentBMPID);
        await UpdateCentralizedBMPDelineationIfPresentAsync(dbContext, treatmentBMPToUpdate, existingDelineation);

        await dbContext.SaveChangesAsync();
        await dbContext.Entry(treatmentBMPToUpdate).ReloadAsync();

        var updatedTreatmentBMPDto = await GetByIDAsDtoAsync(dbContext, treatmentBMPID);

        return updatedTreatmentBMPDto;
    }

    private static void UpdateUpstreamBMPReferencesIfNecessary(NeptuneDbContext dbContext, TreatmentBMP treatmentBMP)
    {
        if (treatmentBMP.UpstreamBMPID != null)
        {
            var regionalSubbasin = treatmentBMP.GetRegionalSubbasin(dbContext);
            if (regionalSubbasin == null
                || !regionalSubbasin.GetTreatmentBMPs(dbContext).Select(x => x.TreatmentBMPID).Contains(treatmentBMP.UpstreamBMPID.Value))
            {
                treatmentBMP.UpstreamBMPID = null;
            }
        }

        if (treatmentBMP.InverseUpstreamBMP.Any())
        {
            foreach (var dependent in treatmentBMP.InverseUpstreamBMP.ToList())
            {
                var regionalSubbasin = dependent.GetRegionalSubbasin(dbContext);
                if (regionalSubbasin == null || !regionalSubbasin.CatchmentGeometry.Contains(treatmentBMP.LocationPoint))
                {
                    dependent.UpstreamBMPID = null;
                }
            }
        }
    }

    private static async Task UpdateCentralizedBMPDelineationIfPresentAsync(NeptuneDbContext dbContext, TreatmentBMP treatmentBMP, Delineation? delineation)
    {
        if (delineation is not { DelineationTypeID: (int)DelineationTypeEnum.Centralized })
        {
            return;
        }

        var updated4326Geometry = treatmentBMP.GetCentralizedDelineationGeometry4326(dbContext);

        if (updated4326Geometry != null && updated4326Geometry.EqualsExact(delineation.DelineationGeometry4326))
        {
            return;
        }

        if (updated4326Geometry != null)
        {
            delineation.DelineationGeometry = treatmentBMP.GetCentralizedDelineationGeometry2771(dbContext);
            delineation.DelineationGeometry4326 = updated4326Geometry;
            delineation.IsVerified = false;
            delineation.DateLastModified = DateTime.UtcNow;
        }
        else
        {
            await delineation.DeleteFull(dbContext);
        }
    }

    public static async Task<List<ErrorMessage>> ValidateUpdateTypeAsync(NeptuneDbContext dbContext, int treatmentBMPID, TreatmentBMPTypeUpdateDto typeUpdateDto)
    {
        var errors = new List<ErrorMessage>();

        var hasValidType = await dbContext.TreatmentBMPTypes.AnyAsync(x => x.TreatmentBMPTypeID == typeUpdateDto.TreatmentBMPTypeID);
        if (!hasValidType)
        {
            errors.Add(new ErrorMessage("TreatmentBMPTypeID", "Valid Treatment BMP Type is required."));
        }

        return errors;
    }

    public static async Task<TreatmentBMPDto> UpdateTypeAsync(NeptuneDbContext dbContext, int treatmentBMPID, TreatmentBMPTypeUpdateDto typeUpdateDto, PersonDto callingUser)
    {
        await dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.pTreatmentBMPUpdateTreatmentBMPType @treatmentBMPID={0}, @treatmentBMPTypeID={1}",
                                                    treatmentBMPID, typeUpdateDto.TreatmentBMPTypeID);

        var updatedTreatmentBMPDto = await GetByIDAsDtoAsync(dbContext, treatmentBMPID);
        return updatedTreatmentBMPDto;
    }

    public static async Task<List<ErrorMessage>> ValidateUpdateUpstreamBMPAsync(NeptuneDbContext dbContext, int treatmentBMPID, TreatmentBMPUpstreamBMPUpdateDto upstreamUpdateDto)
    {
        var errors = new List<ErrorMessage>();

        if (upstreamUpdateDto.UpstreamBMPID.HasValue)
        {
            var treatmentBMP = await dbContext.TreatmentBMPs.AsNoTracking()
                .SingleAsync(x => x.TreatmentBMPID == treatmentBMPID);

            var regionSubbasin = treatmentBMP.GetRegionalSubbasin(dbContext);
            var otherTreatmentBMPsInSubbasin = regionSubbasin?.GetTreatmentBMPs(dbContext).Where(x => x.TreatmentBMPID != treatmentBMPID) ?? new List<TreatmentBMP>();

            var hasValidBMP = otherTreatmentBMPsInSubbasin.Any(x => x.TreatmentBMPID == upstreamUpdateDto.UpstreamBMPID);
            if (!hasValidBMP)
            {
                errors.Add(new ErrorMessage("UpstreamBMPID", "Must be a valid Treatment BMP in regional subbasin."));
            }

            var alreadyUpstreamBMP = dbContext.TreatmentBMPs.AsNoTracking().Any(x => x.TreatmentBMPID != treatmentBMPID && x.UpstreamBMPID == upstreamUpdateDto.UpstreamBMPID);
            if (alreadyUpstreamBMP)
            {
                errors.Add(new ErrorMessage("UpstreamBMPID", "The BMP is already set as the Upstream BMP for another BMP."));
            }

            var isClosedLoop = IsClosedLoop(dbContext, upstreamUpdateDto.UpstreamBMPID.Value);
            if (isClosedLoop)
            {
                errors.Add(new ErrorMessage("UpstreamBMPID", "The choice of Upstream BMP would create a closed loop."));
            }

            var isInfiniteLoop = IsInfiniteLoop(dbContext, upstreamUpdateDto.UpstreamBMPID.Value);
            if (isInfiniteLoop)
            {
                errors.Add(new ErrorMessage("UpstreamBMPID", "The choice of Upstream BMP would create an infinite loop."));
            }
        }

        return errors;
    }

    private static bool IsClosedLoop(NeptuneDbContext dbContext, int upstreamBMPID)
    {
        var upstreamBMPChoice = dbContext.TreatmentBMPs.Find(upstreamBMPID);

        var nextUpstreamBMPID = upstreamBMPChoice?.UpstreamBMPID;

        while (nextUpstreamBMPID != null)
        {
            if (nextUpstreamBMPID == upstreamBMPID)
            {
                return true;
            }

            nextUpstreamBMPID = dbContext.TreatmentBMPs.Find(nextUpstreamBMPID.Value)?.UpstreamBMPID;
        }

        return false;
    }

    private static bool IsInfiniteLoop(NeptuneDbContext dbContext, int upstreamBMPID)
    {
        var upstreamBMPChoice = dbContext.TreatmentBMPs.Find(upstreamBMPID);

        var nextUpstreamBMPID = upstreamBMPChoice?.UpstreamBMPID;

        while (nextUpstreamBMPID != null)
        {
            if (nextUpstreamBMPID == upstreamBMPID)
            {
                return true;
            }

            nextUpstreamBMPID = dbContext.TreatmentBMPs.Find(nextUpstreamBMPID.Value)?.UpstreamBMPID;
        }

        return false;
    }

    public static async Task<TreatmentBMPDto> UpdateUpstreamBMPAsync(NeptuneDbContext dbContext, int treatmentBMPID, TreatmentBMPUpstreamBMPUpdateDto upstreamUpdateDto)
    {
        var treatmentBMP = await dbContext.TreatmentBMPs
            .SingleAsync(x => x.TreatmentBMPID == treatmentBMPID);

        treatmentBMP.UpstreamBMPID = upstreamUpdateDto.UpstreamBMPID;

        await dbContext.SaveChangesAsync();

        var updatedTreatmentBMPDto = await GetByIDAsDtoAsync(dbContext, treatmentBMPID);
        return updatedTreatmentBMPDto;
    }

    public static async Task UpdateWaterQualityManagementPlanAssociationsAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanID, List<int> treatmentBMPIDs)
    {
        var existingTreatmentBMPs = ListByWaterQualityManagementPlanIDWithChangeTracking(dbContext, waterQualityManagementPlanID);
        existingTreatmentBMPs.ForEach(x => { x.WaterQualityManagementPlanID = null; });

        dbContext.TreatmentBMPs.Where(x => treatmentBMPIDs.Contains(x.TreatmentBMPID))
            .ToList()
            .ForEach(x => { x.WaterQualityManagementPlanID = waterQualityManagementPlanID; });

        await dbContext.SaveChangesAsync();
    }

    public static async Task<List<TreatmentBMPMinimalDto>> ListAvailableForWaterQualityManagementPlanAsync(
        NeptuneDbContext dbContext, int stormwaterJurisdictionID, int waterQualityManagementPlanID)
    {
        return await dbContext.TreatmentBMPs
            .AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID &&
                        (x.WaterQualityManagementPlanID == null || x.WaterQualityManagementPlanID == waterQualityManagementPlanID))
            .Select(x => new TreatmentBMPMinimalDto
            {
                TreatmentBMPID = x.TreatmentBMPID,
                TreatmentBMPName = x.TreatmentBMPName,
                TreatmentBMPTypeName = x.TreatmentBMPType.TreatmentBMPTypeName
            })
            .OrderBy(x => x.TreatmentBMPName)
            .ToListAsync();
    }
}