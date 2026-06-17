using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class Organizations
{
    public const int OrganizationIDUnassigned = 2;
    private static IQueryable<Organization> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.Organizations
            .Include(x => x.OrganizationType)
            .Include(x => x.LogoFileResource)
            .Include(x => x.FundingSources)
            .Include(x => x.People)
            // NPT-999: hydrate StormwaterJurisdiction so AsDto() can populate the optional
            // StormwaterJurisdictionID for the SPA detail page's "is a Jurisdiction" alert.
            // It's a left-join over a unique-keyed nav so the cost is negligible.
            .Include(x => x.StormwaterJurisdiction)
            .Include(x => x.PrimaryContactPerson)
            .ThenInclude(x => x.Organization).ThenInclude(x => x.OrganizationType);
    }

    public static Organization GetByIDWithChangeTracking(NeptuneDbContext dbContext, int organizationID)
    {
        var organization = GetImpl(dbContext)
            .SingleOrDefault(x => x.OrganizationID == organizationID);
        Check.RequireNotNull(organization, $"Organization with ID {organizationID} not found!");
        return organization;
    }

    public static Organization GetByIDWithChangeTracking(NeptuneDbContext dbContext, OrganizationPrimaryKey organizationPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, organizationPrimaryKey.PrimaryKeyValue);
    }

    public static Organization GetByID(NeptuneDbContext dbContext, int organizationID)
    {
        var organization = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.OrganizationID == organizationID);
        Check.RequireNotNull(organization, $"Organization with ID {organizationID} not found!");
        return organization;
    }

    public static Organization GetByID(NeptuneDbContext dbContext, OrganizationPrimaryKey organizationPrimaryKey)
    {
        return GetByID(dbContext, organizationPrimaryKey.PrimaryKeyValue);
    }

    public static List<Organization> List(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().ToList().OrderBy(x => x.GetDisplayName()).ToList();
    }

    public static List<Organization> ListActive(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.IsActive).ToList().OrderBy(x => x.GetDisplayName()).ToList();
    }

    public static Organization GetByGuid(NeptuneDbContext dbContext, Guid organizationGuid)
    {
        return GetImpl(dbContext).AsNoTracking().SingleOrDefault(x => x.OrganizationGuid == organizationGuid);
    }

    public static Organization GetByName(NeptuneDbContext dbContext, string organizationName)
    {
        return GetImpl(dbContext).AsNoTracking().SingleOrDefault(x => x.OrganizationName == organizationName);
    }

    public static Organization GetUnknownOrganization(NeptuneDbContext dbContext)
    {
        return GetByName(dbContext, Organization.OrganizationUnknown);
    }

    public static List<Organization> ListByPrimaryContactPersonID(NeptuneDbContext dbContext, int primaryContactPersonID)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.PrimaryContactPersonID == primaryContactPersonID).ToList().OrderBy(x => x.GetDisplayName()).ToList();
    }

    public static List<Organization> ListByPrimaryContactPersonIDWithChangeTracking(NeptuneDbContext dbContext, int primaryContactPersonID)
    {
        return GetImpl(dbContext).Where(x => x.PrimaryContactPersonID == primaryContactPersonID).ToList().OrderBy(x => x.GetDisplayName()).ToList();
    }

    public static int GetUnknownOrganizationID(NeptuneDbContext dbContext)
    {
        return dbContext.Organizations.Single(x => x.OrganizationName == Organization.OrganizationUnknown).OrganizationID;
    }

    public static async Task<List<OrganizationDto>> ListAsDtoAsync(NeptuneDbContext dbContext)
    {
        var entities = await GetImpl(dbContext).AsNoTracking().OrderBy(x => x.OrganizationName).ToListAsync();
        return entities.Select(x => x.AsDto()).ToList();
    }

    public static async Task<OrganizationDto?> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int organizationID)
    {
        var entity = await GetImpl(dbContext).AsNoTracking().SingleOrDefaultAsync(x => x.OrganizationID == organizationID);
        return entity?.AsDto();
    }

    /// <summary>
    /// NPT-999: focused query for the SPA org-detail page's Funding Sources panel.
    /// Returns active + inactive sources so the panel matches the legacy view's full list.
    /// </summary>
    public static async Task<List<FundingSourceSimpleDto>> ListFundingSourcesByOrganizationIDAsync(NeptuneDbContext dbContext, int organizationID)
    {
        return await dbContext.FundingSources.AsNoTracking()
            .Where(fs => fs.OrganizationID == organizationID)
            .OrderBy(fs => fs.FundingSourceName)
            .Select(fs => new FundingSourceSimpleDto
            {
                FundingSourceID = fs.FundingSourceID,
                OrganizationID = fs.OrganizationID,
                FundingSourceName = fs.FundingSourceName,
                IsActive = fs.IsActive,
                FundingSourceDescription = fs.FundingSourceDescription,
            })
            .ToListAsync();
    }

    /// <summary>
    /// NPT-999: focused query for the SPA org-detail page's Users panel — active users only,
    /// ordered Last, First. Matches the legacy MVC Organization/Detail view's filter.
    /// </summary>
    public static async Task<List<PersonSimpleDto>> ListActivePeopleByOrganizationIDAsync(NeptuneDbContext dbContext, int organizationID)
    {
        return await dbContext.People.AsNoTracking()
            .Where(p => p.OrganizationID == organizationID && p.IsActive)
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Select(p => new PersonSimpleDto
            {
                PersonID = p.PersonID,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = p.Email,
                Phone = p.Phone,
                IsActive = p.IsActive,
                OrganizationID = p.OrganizationID,
            })
            .ToListAsync();
    }

    public static async Task<OrganizationDto> CreateAsync(NeptuneDbContext dbContext, OrganizationUpsertDto dto)
    {
        var entity = dto.AsEntity();
        dbContext.Organizations.Add(entity);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.OrganizationID);
    }

    public static async Task<OrganizationDto?> UpdateAsync(NeptuneDbContext dbContext, int organizationID, OrganizationUpsertDto dto)
    {
        var entity = await dbContext.Organizations.FirstOrDefaultAsync(x => x.OrganizationID == organizationID);
        if (entity == null) return null;
        entity.UpdateFromUpsertDto(dto);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.OrganizationID);
    }

    public static async Task<bool> DeleteAsync(NeptuneDbContext dbContext, int organizationID)
    {
        var entity = await dbContext.Organizations.FirstOrDefaultAsync(x => x.OrganizationID == organizationID);
        if (entity == null) return false;
        await entity.DeleteFull(dbContext);
        return true;
    }
}