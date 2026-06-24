using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class PersonExtensionMethods
{
    public static PersonDisplayDto AsDisplayDto(this Person person)
    {
        var dto = new PersonDisplayDto()
        {
            PersonID = person.PersonID,
            FirstName = person.FirstName,
            LastName = person.LastName,
            OrganizationID = person.OrganizationID,
            OrganizationName = person.Organization.OrganizationName,
        };
        return dto;
    }
    public static PersonSimpleDto AsSimpleDto(this Person person)
    {
        var dto = new PersonSimpleDto()
        {
            PersonID = person.PersonID,
            FirstName = person.FirstName,
            LastName = person.LastName,
            Email = person.Email,
            Phone = person.Phone,
            RoleID = person.RoleID,
            RoleName = person.Role.RoleDisplayName,
            CreateDate = person.CreateDate,
            UpdateDate = person.UpdateDate,
            LastActivityDate = person.LastActivityDate,
            IsActive = person.IsActive,
            OrganizationID = person.OrganizationID,
            OrganizationName = person.Organization.OrganizationName,
            ReceiveSupportEmails = person.ReceiveSupportEmails,
            ReceiveRSBRevisionRequestEmails = person.ReceiveRSBRevisionRequestEmails,
            WebServiceAccessToken = person.WebServiceAccessToken,
            IsOCTAGrantReviewer = person.IsOCTAGrantReviewer,
            HasAssignedStormwaterJurisdiction = person.StormwaterJurisdictionPeople.Any()
        };
        return dto;
    }

    public static PersonDto AsDto(this Person person)
    {
        var personDto = new PersonDto()
        {
            PersonID = person.PersonID,
            // GlobalID and ImpersonatedPersonID needed by the SPA to detect impersonation
            // (claimsUser.sub vs currentUser.GlobalID comparison in AuthenticationService).
            GlobalID = person.GlobalID,
            FirstName = person.FirstName,
            LastName = person.LastName,
            Email = person.Email,
            Phone = person.Phone,
            RoleID = person.RoleID,
            RoleName = person.Role.RoleDisplayName,
            CreateDate = person.CreateDate,
            UpdateDate = person.UpdateDate,
            LastActivityDate = person.LastActivityDate,
            IsActive = person.IsActive,
            OrganizationID = person.OrganizationID,
            OrganizationName = person.Organization.OrganizationName,
            ReceiveSupportEmails = person.ReceiveSupportEmails,
            ReceiveRSBRevisionRequestEmails = person.ReceiveRSBRevisionRequestEmails,
            WebServiceAccessToken = person.WebServiceAccessToken,
            LastWebServiceAccessDate = person.LastWebServiceAccessDate,
            IsOCTAGrantReviewer = person.IsOCTAGrantReviewer,
            HasAssignedStormwaterJurisdiction = person.StormwaterJurisdictionPeople.Any(),
            ImpersonatedPersonID = person.ImpersonatedPersonID,
        };
        return personDto;
    }

    public static async Task<bool> CanEditJurisdiction(this PersonDto person, int stormwaterJurisdictionID, NeptuneDbContext dbContext)
    {
        if (person.RoleID == (int) RoleEnum.Admin || person.RoleID == (int) RoleEnum.SitkaAdmin )
        {
            return true;
        }

        if (person.RoleID == (int)RoleEnum.JurisdictionEditor || person.RoleID == (int)RoleEnum.JurisdictionManager)
        {
            var stormwaterJurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, person.PersonID);
            return stormwaterJurisdictionIDs.Contains(stormwaterJurisdictionID);
        }

        return false;
    }
}