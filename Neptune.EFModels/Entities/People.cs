using System.Security.Claims;
using Neptune.Models.DataTransferObjects;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects.Person;
using Neptune.Models.Helpers;

namespace Neptune.EFModels.Entities;

public static class People
{
    public static IEnumerable<string> GetEmailAddressesForAdminsThatReceiveSupportEmails(NeptuneDbContext dbContext)
    {
        var persons = GetImpl(dbContext).AsNoTracking()
            .Where(x => x.IsActive && (x.RoleID == (int)RoleEnum.Admin || x.RoleID == (int)RoleEnum.SitkaAdmin) && x.ReceiveSupportEmails)
            .Select(x => x.Email)
            .AsEnumerable();

        return persons;
    }

    public static async Task<List<PersonSimpleDto>> ListAsSimpleDtoAsync(NeptuneDbContext dbContext)
    {
        var people = await GetImpl(dbContext)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
            .ToListAsync();
        return people.Select(x => x.AsSimpleDto()).ToList();
    }

    public static IQueryable<Person> ListActive(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x => x.IsActive)
            .OrderBy(x => x.LastName).ThenBy(x => x.FirstName);
    }

    public static Person GetByIDWithChangeTracking(NeptuneDbContext dbContext, int personID)
    {
        var person = GetImpl(dbContext)
            .SingleOrDefault(x => x.PersonID == personID);
        Check.RequireNotNull(person, $"Person with ID {personID} not found!");
        return person;
    }

    public static Person GetByID(NeptuneDbContext dbContext, int personID)
    {
        var person = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.PersonID == personID);
        Check.RequireNotNull(person, $"Person with ID {personID} not found!");
        return person;
    }

    public static PersonDto GetByIDAsDto(NeptuneDbContext dbContext, int personID)
    {
        var person = GetByID(dbContext, personID);
        return person.AsDto();
    }

    public static async Task<PersonDto?> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int personID)
    {
        var person = await GetImpl(dbContext).AsNoTracking().SingleOrDefaultAsync(x => x.PersonID == personID);
        return person?.AsDto();
    }

    public static PersonDto? GetByEmailAsDto(NeptuneDbContext dbContext, string email)
    {
        var person = GetImpl(dbContext).AsNoTracking().SingleOrDefault(x => x.Email == email);
        return person?.AsDto();
    }

    public static Person? GetByGlobalID(NeptuneDbContext dbContext, string globalID)
    {
        return dbContext.People.AsNoTracking()
            .Include(x => x.Organization)
            .Include(x => x.StormwaterJurisdictionPeople)
            .ThenInclude(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .SingleOrDefault(x => x.GlobalID == globalID);
    }

    public static async Task<PersonDto?> GetByGlobalIDAsDtoAsync(NeptuneDbContext dbContext, string globalID)
    {
        var person = await dbContext.People
            .Include(x => x.Organization)
            .ThenInclude(x => x.OrganizationType)
            .Include(x => x.StormwaterJurisdictionPeople)
            .ThenInclude(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .ThenInclude(x => x.OrganizationType).AsNoTracking().Where(x => x.GlobalID == globalID).SingleOrDefaultAsync();
        return person?.AsDto();
    }

    private static IQueryable<Person> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.People
                .Include(x => x.Organization)
                .ThenInclude(x => x.OrganizationType)
                .Include(x => x.StormwaterJurisdictionPeople)
                .ThenInclude(x => x.StormwaterJurisdiction)
                .ThenInclude(x => x.Organization)
                .ThenInclude(x => x.OrganizationType)
            ;
    }

    public static Task<Person?> GetByWebServiceAccessTokenAsync(NeptuneDbContext dbContext, Guid webServiceAccessToken)
    {
        return GetImpl(dbContext).AsNoTracking().SingleOrDefaultAsync(x => x.WebServiceAccessToken == webServiceAccessToken);
    }

    public static async Task<Guid> GenerateAndPersistWebServiceAccessTokenAsync(NeptuneDbContext dbContext, int personID)
    {
        var person = await dbContext.People.SingleAsync(x => x.PersonID == personID);
        person.WebServiceAccessToken = Guid.NewGuid();
        await dbContext.SaveChangesAsync();
        return person.WebServiceAccessToken.Value;
    }

    public static PersonDto CreateUnassignedPerson(NeptuneDbContext dbContext, PersonCreateDto userCreateDto)
    {
        var userUpsertDto = new PersonUpsertDto()
        {
            FirstName = userCreateDto.FirstName,
            LastName = userCreateDto.LastName,
            OrganizationName = userCreateDto.OrganizationName,
            Email = userCreateDto.Email,
            RoleID = (int)RoleEnum.Unassigned,  // don't allow non-admin user to set their role to something other than Unassigned
        };
        return CreateNewPerson(dbContext, userUpsertDto);
    }

    public static List<ErrorMessage> ValidateCreateUnassignedPerson(NeptuneDbContext dbContext, PersonCreateDto userCreateDto)
    {
        var result = new List<ErrorMessage>();

        var userByEmailDto = GetByEmailAsDto(dbContext, userCreateDto.Email);  // A duplicate email leads to 500s, so need to prevent duplicates
        if (userByEmailDto != null)
        {
            result.Add(new ErrorMessage() { Type = "Person Creation", Message = "There is already a user account with this email address." });
        }

        return result;
    }

    public static PersonDto CreateNewPerson(NeptuneDbContext dbContext, PersonUpsertDto personToCreate)
    {
        if (!personToCreate.RoleID.HasValue)
        {
            return null;
        }

        var organizationID = Organizations.OrganizationIDUnassigned;
        var organization = Organizations.GetByName(dbContext, personToCreate.OrganizationName);
        if (organization != null)
        {
            organizationID = organization.OrganizationID;
        }

        var person = new Person
        {
            Email = personToCreate.Email,
            FirstName = personToCreate.FirstName,
            LastName = personToCreate.LastName,
            IsActive = true,
            RoleID = personToCreate.RoleID.Value,
            CreateDate = DateTime.UtcNow,
            OrganizationID = organizationID,
        };

        dbContext.People.Add(person);
        dbContext.SaveChanges();
        dbContext.Entry(person).Reload();

        return GetByIDAsDto(dbContext, person.PersonID);
    }

    public static async Task<PersonDto> CreateAsync(NeptuneDbContext dbContext, PersonUpsertDto dto)
    {
        if (!dto.RoleID.HasValue)
        {
            return null;
        }
        var organizationID = Organizations.OrganizationIDUnassigned;
        var organization = Organizations.GetByName(dbContext, dto.OrganizationName);
        if (organization != null)
        {
            organizationID = organization.OrganizationID;
        }
        var person = new Person
        {
            Email = dto.Email,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            IsActive = true,
            RoleID = dto.RoleID.Value,
            CreateDate = DateTime.UtcNow,
            OrganizationID = organizationID,
        };
        dbContext.People.Add(person);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, person.PersonID);
    }

    public static async Task<PersonDto?> UpdateAsync(NeptuneDbContext dbContext, int personID, PersonUpsertDto dto)
    {
        var person = await dbContext.People.FirstOrDefaultAsync(x => x.PersonID == personID);
        if (person == null) return null;
        person.FirstName = dto.FirstName;
        person.LastName = dto.LastName;
        person.Email = dto.Email;
        person.RoleID = dto.RoleID ?? person.RoleID;
        var organization = Organizations.GetByName(dbContext, dto.OrganizationName);
        if (organization != null)
        {
            person.OrganizationID = organization.OrganizationID;
        }
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, person.PersonID);
    }

    public static async Task<PersonDetailDto?> GetByIDAsDetailDtoAsync(NeptuneDbContext dbContext, int personID)
    {
        // Focused projection — emits a single SELECT covering only the columns PersonDetailDto
        // needs. Avoids the heavy Organizations.GetImpl() Include chain (FundingSources, People,
        // PrimaryContactPerson, OrganizationType, etc.) that the previous Include-based
        // implementation pulled in for each primary-contact org.
        var dto = await dbContext.People.AsNoTracking()
            .Where(x => x.PersonID == personID)
            .Select(PersonProjections.AsDetailDto)
            .SingleOrDefaultAsync();
        if (dto == null) return null;

        // Role static lookup is resolved post-materialize.
        if (Role.AllLookupDictionary.TryGetValue(dto.RoleID, out var role))
        {
            dto.RoleName = role.RoleName;
            dto.RoleDisplayName = role.RoleDisplayName;
        }

        // Separate focused query for primary-contact organizations: only the OrganizationSimpleDto
        // columns, no entity hydration of OrganizationType / FundingSources / People.
        dto.PrimaryContactOrganizations = await dbContext.Organizations.AsNoTracking()
            .Where(o => o.PrimaryContactPersonID == personID)
            .OrderBy(o => o.OrganizationName)
            .Select(o => new OrganizationSimpleDto
            {
                OrganizationID = o.OrganizationID,
                OrganizationGuid = o.OrganizationGuid,
                OrganizationName = o.OrganizationName,
                OrganizationShortName = o.OrganizationShortName,
                PrimaryContactPersonID = o.PrimaryContactPersonID,
                IsActive = o.IsActive,
                OrganizationUrl = o.OrganizationUrl,
                LogoFileResourceID = o.LogoFileResourceID,
                OrganizationTypeID = o.OrganizationTypeID,
            })
            .ToListAsync();

        return dto;
    }

    public static async Task<PersonDto?> UpdateRoleAsync(NeptuneDbContext dbContext, int personID, PersonRoleUpdateDto dto)
    {
        var person = await dbContext.People.FirstOrDefaultAsync(x => x.PersonID == personID);
        if (person == null) return null;

        person.RoleID = dto.RoleID;
        person.OrganizationID = dto.OrganizationID;
        person.IsOCTAGrantReviewer = dto.IsOCTAGrantReviewer;
        person.ReceiveSupportEmails = dto.ReceiveSupportEmails;
        person.ReceiveRSBRevisionRequestEmails = dto.ReceiveRSBRevisionRequestEmails;
        person.UpdateDate = DateTime.UtcNow;

        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, personID);
    }

    public static async Task<PersonDetailDto?> UpdateJurisdictionsAsync(NeptuneDbContext dbContext, int personID, List<int> stormwaterJurisdictionIDs)
    {
        var person = await dbContext.People.FirstOrDefaultAsync(x => x.PersonID == personID);
        if (person == null) return null;

        // Replace the user's StormwaterJurisdictionPerson rows wholesale: delete what's there, insert the new set.
        await dbContext.StormwaterJurisdictionPeople
            .Where(x => x.PersonID == personID)
            .ExecuteDeleteAsync();

        foreach (var stormwaterJurisdictionID in stormwaterJurisdictionIDs.Distinct())
        {
            dbContext.StormwaterJurisdictionPeople.Add(new StormwaterJurisdictionPerson
            {
                PersonID = personID,
                StormwaterJurisdictionID = stormwaterJurisdictionID,
            });
        }

        person.UpdateDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDetailDtoAsync(dbContext, personID);
    }

    public static async Task<PersonDetailDto?> UpdateActiveStatusAsync(NeptuneDbContext dbContext, int personID, bool isActive)
    {
        var person = await dbContext.People.FirstOrDefaultAsync(x => x.PersonID == personID);
        if (person == null) return null;

        person.IsActive = isActive;
        person.UpdateDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDetailDtoAsync(dbContext, personID);
    }

    public static async Task<List<PersonNotificationDto>> ListNotificationsByPersonIDAsync(NeptuneDbContext dbContext, int personID)
    {
        var notifications = await dbContext.Notifications.AsNoTracking()
            .Where(x => x.PersonID == personID)
            .OrderByDescending(x => x.NotificationDate)
            .Select(x => new { x.NotificationID, x.NotificationDate, x.NotificationTypeID })
            .ToListAsync();

        return notifications
            .Select(x => new PersonNotificationDto
            {
                NotificationID = x.NotificationID,
                NotificationDate = x.NotificationDate,
                NotificationTypeID = x.NotificationTypeID,
                NotificationTypeDisplayName = NotificationType.AllLookupDictionary.TryGetValue(x.NotificationTypeID, out var nt)
                    ? nt.NotificationTypeDisplayName
                    : null,
            })
            .ToList();
    }

    public static async Task<bool> DeleteAsync(NeptuneDbContext dbContext, int personID)
    {
        var person = await dbContext.People.FirstOrDefaultAsync(x => x.PersonID == personID);
        if (person == null) return false;
        await person.DeleteFull(dbContext);
        return true;
    }

    public static async Task<PersonDto?> UpdateClaims(NeptuneDbContext dbContext, ClaimsPrincipal claimsPrincipal)
    {
        int? personID = null;
        var globalID = claimsPrincipal?.Claims.SingleOrDefault(c => c.Type == ClaimsConstants.Sub)?.Value;
        if (!string.IsNullOrEmpty(globalID))
        {
            personID = await dbContext.People.AsNoTracking().Where(x => x.GlobalID == globalID).Select(x => x.PersonID).SingleOrDefaultAsync();
        }

        Person person;
        var email = claimsPrincipal?.Claims.SingleOrDefault(c => c.Type == ClaimsConstants.Emails)?.Value;
        if (personID is > 0)
        {
            person = await dbContext.People.FirstOrDefaultAsync(x => x.PersonID == personID);
        }
        else
        {
            person = await dbContext.People.FirstOrDefaultAsync(x => x.Email == email);
        }

        if (person == null)
        {
            person = new Person
            {
                GlobalID = globalID,
                RoleID = Role.Unassigned.RoleID,
                CreateDate = DateTime.UtcNow,
                IsActive = true,
                OrganizationID = Organizations.OrganizationIDUnassigned,
                ReceiveSupportEmails = false
            };

            dbContext.People.Add(person);
        }

        var firstName = claimsPrincipal?.Claims.SingleOrDefault(c => c.Type == ClaimsConstants.GivenName)?.Value;
        var lastName = claimsPrincipal?.Claims.SingleOrDefault(c => c.Type == ClaimsConstants.FamilyName)?.Value;

        if (!string.IsNullOrEmpty(globalID))
        {
            person.GlobalID = globalID;
        }

        if (!string.IsNullOrEmpty(firstName))
        {
            person.FirstName = firstName;
        }

        if (!string.IsNullOrEmpty(lastName))
        {
            person.LastName = lastName;
        }

        if (!string.IsNullOrEmpty(email))
        {
            person.Email = email;
        }

        //if (person.RoleID == (int)RoleEnum.PendingLogin)
        //{
        //    person.RoleID = (int) RoleEnum.JurisdictionEditor;
        //}

        await dbContext.SaveChangesAsync();
        await dbContext.Entry(person).ReloadAsync();

        return await GetByIDAsDtoAsync(dbContext, person.PersonID);
    }

}