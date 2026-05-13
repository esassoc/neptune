using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;
using Neptune.Models.DataTransferObjects.Person;

namespace Neptune.EFModels.Entities;

public static class PersonProjections
{
    /// <summary>
    /// Expression projection from Person to PersonDetailDto. Used by
    /// <see cref="People.GetByIDAsDetailDtoAsync"/> so EF emits a focused SELECT instead of
    /// materializing the full Person + Organization + StormwaterJurisdiction graph through
    /// .Include() chains. Static lookups (Role.RoleDisplayName) and the primary-contact
    /// organization list are resolved post-materialize by the caller — never .Include() static
    /// lookup bindings, per the project pattern.
    /// </summary>
    public static readonly Expression<Func<Person, PersonDetailDto>> AsDetailDto = p => new PersonDetailDto
    {
        PersonID = p.PersonID,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Email = p.Email,
        Phone = p.Phone,
        CreateDate = p.CreateDate,
        UpdateDate = p.UpdateDate,
        LastActivityDate = p.LastActivityDate,
        IsActive = p.IsActive,
        RoleID = p.RoleID,
        // RoleName / RoleDisplayName resolved post-materialize via Role.AllLookupDictionary.
        IsOCTAGrantReviewer = p.IsOCTAGrantReviewer,
        ReceiveSupportEmails = p.ReceiveSupportEmails,
        ReceiveRSBRevisionRequestEmails = p.ReceiveRSBRevisionRequestEmails,
        Organization = p.Organization == null
            ? null
            : new OrganizationSimpleDto
            {
                OrganizationID = p.Organization.OrganizationID,
                OrganizationGuid = p.Organization.OrganizationGuid,
                OrganizationName = p.Organization.OrganizationName,
                OrganizationShortName = p.Organization.OrganizationShortName,
                PrimaryContactPersonID = p.Organization.PrimaryContactPersonID,
                IsActive = p.Organization.IsActive,
                OrganizationUrl = p.Organization.OrganizationUrl,
                LogoFileResourceID = p.Organization.LogoFileResourceID,
                OrganizationTypeID = p.Organization.OrganizationTypeID,
            },
        AssignedStormwaterJurisdictions = p.StormwaterJurisdictionPeople
            .Select(sjp => new StormwaterJurisdictionDisplayDto
            {
                StormwaterJurisdictionID = sjp.StormwaterJurisdiction.StormwaterJurisdictionID,
                StormwaterJurisdictionName = sjp.StormwaterJurisdiction.Organization.OrganizationName,
            })
            .OrderBy(j => j.StormwaterJurisdictionName)
            .ToList(),
        // PrimaryContactOrganizations populated by the caller via a separate focused query.
        PrimaryContactOrganizations = new List<OrganizationSimpleDto>(),
    };
}
