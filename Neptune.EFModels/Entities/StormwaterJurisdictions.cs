using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class StormwaterJurisdictions
{
    public static List<StormwaterJurisdiction> List(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().OrderBy(ht => ht.Organization.OrganizationName).ToList();
    }

    public static async Task<List<StormwaterJurisdictionDisplayDto>> ListByIDsAsDisplayDtoAsync(NeptuneDbContext dbContext, List<int> stormwaterJurisdictionIDs)
    {
        var entities = await dbContext.StormwaterJurisdictions
            .Include(x => x.Organization)
            .Where(x => stormwaterJurisdictionIDs.Contains(x.StormwaterJurisdictionID))
            .OrderBy(x => x.Organization.OrganizationName).ToListAsync();
        return entities
            .Select(x => x.AsDisplayDto())
            .ToList();
    }

    public static BoundingBoxDto GetBoundingBoxDtoByJurisdictionID(NeptuneDbContext dbContext, int stormwaterJurisdictionID)
    {
        return GetBoundingBoxDtoByJurisdictionIDList(dbContext, new List<int>{stormwaterJurisdictionID});
    }

    public static BoundingBoxDto GetBoundingBoxDtoByJurisdictionIDList(NeptuneDbContext dbContext, IEnumerable<int> stormwaterJurisdictionIDList)
    {
        var stormwaterJurisdictionGeometry = dbContext.StormwaterJurisdictionGeometries
            .Where(x => stormwaterJurisdictionIDList.Contains(x.StormwaterJurisdictionID))
            .Select(x => x.Geometry4326);
        return new BoundingBoxDto(stormwaterJurisdictionGeometry);
    }

    public static async Task<BoundingBoxDto> GetBoundingBoxDtoByPersonAsync(NeptuneDbContext dbContext, PersonDto personDto)
    {
        var jurisdictions = dbContext.StormwaterJurisdictionGeometries;
        if (personDto.RoleID != (int)RoleEnum.Admin || personDto.RoleID != (int)RoleEnum.SitkaAdmin)
        {
            var jurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, personDto.PersonID);
            return new BoundingBoxDto(jurisdictions.Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID))
                .Select(x => x.Geometry4326).ToList());
        }
        return new BoundingBoxDto(jurisdictions.Select(x => x.Geometry4326).ToList());
    }

    public static StormwaterJurisdiction GetByIDWithChangeTracking(NeptuneDbContext dbContext, int stormwaterJurisdictionID)
    {
        var stormwaterJurisdiction = GetImpl(dbContext)
            .SingleOrDefault(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        Check.RequireNotNull(stormwaterJurisdiction, $"StormwaterJurisdiction with ID {stormwaterJurisdictionID} not found!");
        return stormwaterJurisdiction;
    }

    public static StormwaterJurisdiction GetByIDWithChangeTracking(NeptuneDbContext dbContext, StormwaterJurisdictionPrimaryKey stormwaterJurisdictionPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, stormwaterJurisdictionPrimaryKey.PrimaryKeyValue);
    }

    public static StormwaterJurisdiction GetByID(NeptuneDbContext dbContext, int stormwaterJurisdictionID)
    {
        var stormwaterJurisdiction = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        Check.RequireNotNull(stormwaterJurisdiction, $"StormwaterJurisdiction with ID {stormwaterJurisdictionID} not found!");
        return stormwaterJurisdiction;
    }

    public static StormwaterJurisdiction GetByID(NeptuneDbContext dbContext, StormwaterJurisdictionPrimaryKey stormwaterJurisdictionPrimaryKey)
    {
        return GetByID(dbContext, stormwaterJurisdictionPrimaryKey.PrimaryKeyValue);
    }

    private static IQueryable<StormwaterJurisdiction> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.StormwaterJurisdictions
            .Include(x => x.Organization)
            .ThenInclude(x => x.OrganizationType)
            .Include(x => x.TreatmentBMPs);
    }

    public static List<StormwaterJurisdiction> ListViewableByPersonForBMPs(NeptuneDbContext dbContext, Person person)
    {
        var stormwaterJurisdictionIDsViewable  = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForBMPs(dbContext, person);
        return GetImpl(dbContext).AsNoTracking().Where(x => stormwaterJurisdictionIDsViewable.Contains(x.StormwaterJurisdictionID)).ToList();
    }

    public static List<StormwaterJurisdiction> ListViewableByPersonForWQMPs(NeptuneDbContext dbContext, Person person)
    {
        var stormwaterJurisdictionIDsViewable  = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(dbContext, person);
        return GetImpl(dbContext).AsNoTracking().Where(x => stormwaterJurisdictionIDsViewable.Contains(x.StormwaterJurisdictionID)).ToList();
    }

    public static async Task<List<StormwaterJurisdictionGridDto>> ListAsDtoAsync(NeptuneDbContext dbContext)
    {
        var peopleCountByStormwaterJurisdiction = StormwaterJurisdictionPeople.ListCountByStormwaterJurisdiction(dbContext);
        var bmpCountByStormwaterJurisdiction = TreatmentBMPs.ListCountByStormwaterJurisdiction(dbContext);

        var entities = await dbContext.StormwaterJurisdictions
            .Include(x => x.Organization)
            .AsNoTracking().OrderBy(ht => ht.Organization.OrganizationName).ToListAsync();
        return entities.Select(x => {
            var dto = x.AsGridDto();
            dto.NumberOfUsers = peopleCountByStormwaterJurisdiction.GetValueOrDefault(x.StormwaterJurisdictionID, 0);
            dto.NumberOfBMPs = bmpCountByStormwaterJurisdiction.GetValueOrDefault(x.StormwaterJurisdictionID, 0);
            return dto;
        }).ToList();
    }

    public static async Task<StormwaterJurisdictionGridDto> UpdateAsync(NeptuneDbContext dbContext, int stormwaterJurisdictionID, StormwaterJurisdictionUpsertDto dto)
    {
        var entity = await dbContext.StormwaterJurisdictions
            .SingleAsync(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        entity.StormwaterJurisdictionPublicBMPVisibilityTypeID = dto.StormwaterJurisdictionPublicBMPVisibilityTypeID;
        entity.StormwaterJurisdictionPublicWQMPVisibilityTypeID = dto.StormwaterJurisdictionPublicWQMPVisibilityTypeID;
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, stormwaterJurisdictionID);
    }

    public static async Task<StormwaterJurisdictionGridDto> GetByIDAsDtoAsync(NeptuneDbContext dbContext,
        int stormwaterJurisdictionID)
    {
        var peopleCountByStormwaterJurisdiction =
            StormwaterJurisdictionPeople.ListCountByStormwaterJurisdiction(dbContext);
        var bmpCountByStormwaterJurisdiction = TreatmentBMPs.ListCountByStormwaterJurisdiction(dbContext);

        var entity = await dbContext.StormwaterJurisdictions
            .Include(x => x.Organization)
            .AsNoTracking().OrderBy(ht => ht.Organization.OrganizationName)
            .SingleAsync(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        var dto = entity.AsGridDto();
        dto.NumberOfUsers = peopleCountByStormwaterJurisdiction.GetValueOrDefault(entity.StormwaterJurisdictionID, 0);
        dto.NumberOfBMPs = bmpCountByStormwaterJurisdiction.GetValueOrDefault(entity.StormwaterJurisdictionID, 0);
        return dto;
    }

}