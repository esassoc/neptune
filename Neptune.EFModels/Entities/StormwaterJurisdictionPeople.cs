using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class StormwaterJurisdictionPeople
{
    private static IQueryable<StormwaterJurisdictionPerson> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.StormwaterJurisdictionPeople;
    }

    public static StormwaterJurisdictionPerson GetByIDWithChangeTracking(NeptuneDbContext dbContext, int stormwaterJurisdictionPersonID)
    {
        var stormwaterJurisdictionPerson = GetImpl(dbContext)
            .SingleOrDefault(x => x.StormwaterJurisdictionPersonID == stormwaterJurisdictionPersonID);
        Check.RequireNotNull(stormwaterJurisdictionPerson, $"StormwaterJurisdictionPerson with ID {stormwaterJurisdictionPersonID} not found!");
        return stormwaterJurisdictionPerson;
    }

    public static StormwaterJurisdictionPerson GetByIDWithChangeTracking(NeptuneDbContext dbContext, StormwaterJurisdictionPersonPrimaryKey stormwaterJurisdictionPersonPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, stormwaterJurisdictionPersonPrimaryKey.PrimaryKeyValue);
    }

    public static StormwaterJurisdictionPerson GetByID(NeptuneDbContext dbContext, int stormwaterJurisdictionPersonID)
    {
        var stormwaterJurisdictionPerson = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.StormwaterJurisdictionPersonID == stormwaterJurisdictionPersonID);
        Check.RequireNotNull(stormwaterJurisdictionPerson, $"StormwaterJurisdictionPerson with ID {stormwaterJurisdictionPersonID} not found!");
        return stormwaterJurisdictionPerson;
    }

    public static StormwaterJurisdictionPerson GetByID(NeptuneDbContext dbContext, StormwaterJurisdictionPersonPrimaryKey stormwaterJurisdictionPersonPrimaryKey)
    {
        return GetByID(dbContext, stormwaterJurisdictionPersonPrimaryKey.PrimaryKeyValue);
    }

    public static async Task<List<int>> ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(NeptuneDbContext dbContext, int? personID)
    {
        var personDto = personID.HasValue ? await People.GetByIDAsDtoAsync(dbContext, personID.Value) : null;
        return await ListViewableStormwaterJurisdictionIDsByPersonDtoForBMPsAsync(dbContext, personDto);
    }

    public static Task<List<int>> ListViewableStormwaterJurisdictionIDsByPersonDtoForBMPsAsync(NeptuneDbContext dbContext, PersonDto? personDto)
    {
        if (personDto == null || personDto.RoleID == (int)RoleEnum.Unassigned)
        {
            return dbContext.StormwaterJurisdictions.AsNoTracking().Where(x => x.StormwaterJurisdictionPublicBMPVisibilityTypeID ==
                                                                               (int)StormwaterJurisdictionPublicBMPVisibilityTypeEnum.VerifiedOnly).Select(x => x.StormwaterJurisdictionID).ToListAsync();
        }

        if (personDto.RoleID is (int)RoleEnum.Admin or (int)RoleEnum.SitkaAdmin)
        {
            return dbContext.StormwaterJurisdictions.AsNoTracking().Select(x => x.StormwaterJurisdictionID).ToListAsync();
        }

        return dbContext.StormwaterJurisdictionPeople
            .Where(x => x.PersonID == personDto.PersonID)
            .Select(x => x.StormwaterJurisdictionID)
            .ToListAsync();
    }

    public static IEnumerable<int> ListViewableStormwaterJurisdictionIDsByPersonForBMPs(NeptuneDbContext dbContext, Person person)
    {
        if (person.IsAdministrator())
        {
            return GetImpl(dbContext).AsNoTracking().Select(x => x.StormwaterJurisdictionID).ToList();
        }

        if (person.IsAnonymousOrUnassigned())
        {
            return StormwaterJurisdictions.List(dbContext)
                .Where(x => x.StormwaterJurisdictionPublicBMPVisibilityTypeID ==
                            (int)StormwaterJurisdictionPublicBMPVisibilityTypeEnum.VerifiedOnly)
                .Select(x => x.StormwaterJurisdictionID);
        }

        return GetImpl(dbContext).AsNoTracking().Where(x => x.PersonID == person.PersonID).Select(x => x.StormwaterJurisdictionID).ToList();
    }

    public static IEnumerable<int> ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(NeptuneDbContext dbContext, Person person)
    {
        if (person.IsAdministrator())
        {
            return GetImpl(dbContext).AsNoTracking().Select(x => x.StormwaterJurisdictionID).ToList();
        }

        if (person.IsAnonymousOrUnassigned())
        {
            return StormwaterJurisdictions.List(dbContext)
                .Where(x => x.StormwaterJurisdictionPublicWQMPVisibilityTypeID !=
                            (int)StormwaterJurisdictionPublicWQMPVisibilityTypeEnum.None)
                .Select(x => x.StormwaterJurisdictionID);
        }

        return GetImpl(dbContext).AsNoTracking().Where(x => x.PersonID == person.PersonID).Select(x => x.StormwaterJurisdictionID).ToList();
    }

    public static Dictionary<int, int> ListCountByStormwaterJurisdiction(NeptuneDbContext dbContext)
    {
        return dbContext.StormwaterJurisdictionPeople.AsNoTracking().GroupBy(x => x.StormwaterJurisdictionID).Select(x => new { x.Key, Count = x.Count() })
            .ToDictionary(x => x.Key, x => x.Count);
    }

    public static List<StormwaterJurisdictionPerson> ListByStormwaterJurisdictionID(NeptuneDbContext dbContext, int stormwaterJurisdictionID)
    {
        return dbContext.StormwaterJurisdictionPeople
            .Include(x => x.Person)
            .ThenInclude(x => x.Organization)
            .AsNoTracking().Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID).ToList();
    }

    public static async Task<List<PersonDisplayDto>> ListByStormwaterJurisdictionIDAsPersonDto(NeptuneDbContext dbContext, int stormwaterJurisdictionID)
    {
        var entities = await dbContext.StormwaterJurisdictionPeople
            .Include(x => x.Person)
            .ThenInclude(x => x.Organization)
            .AsNoTracking().Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID).ToListAsync();
        return entities.Select(x => x.Person.AsDisplayDto()).OrderBy(x => x.FullName).ToList();
    }

    /// <summary>
    /// Replaces the set of people assigned to a jurisdiction with the supplied PersonIDs — adds the
    /// newly-selected, removes the deselected, leaves unchanged rows in place (NPT-1061 item 4).
    /// </summary>
    public static async Task SetJurisdictionPeopleAsync(NeptuneDbContext dbContext, int stormwaterJurisdictionID, List<int> personIDs)
    {
        var desiredPersonIDs = (personIDs ?? new List<int>()).Distinct().ToList();
        var existing = await dbContext.StormwaterJurisdictionPeople
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .ToListAsync();
        var existingPersonIDs = existing.Select(x => x.PersonID).ToHashSet();

        var toRemove = existing.Where(x => !desiredPersonIDs.Contains(x.PersonID)).ToList();
        dbContext.StormwaterJurisdictionPeople.RemoveRange(toRemove);

        var toAdd = desiredPersonIDs
            .Where(id => !existingPersonIDs.Contains(id))
            .Select(id => new StormwaterJurisdictionPerson { StormwaterJurisdictionID = stormwaterJurisdictionID, PersonID = id });
        await dbContext.StormwaterJurisdictionPeople.AddRangeAsync(toAdd);

        await dbContext.SaveChangesAsync();
    }
}