using System.Linq.Expressions;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class ProjectDtoProjections
{
    public static readonly Expression<Func<Project, ProjectDto>> AsDto = x => new ProjectDto
    {
        ProjectID = x.ProjectID,
        ProjectName = x.ProjectName,
        OrganizationID = x.OrganizationID,
        StormwaterJurisdictionID = x.StormwaterJurisdictionID,
        ProjectStatusID = x.ProjectStatusID,
        PrimaryContactPersonID = x.PrimaryContactPersonID,
        CreatePersonID = x.CreatePersonID,
        DateCreated = x.DateCreated,
        ProjectDescription = x.ProjectDescription,
        AdditionalContactInformation = x.AdditionalContactInformation,
        DoesNotIncludeTreatmentBMPs = x.DoesNotIncludeTreatmentBMPs,
        CalculateOCTAM2Tier2Scores = x.CalculateOCTAM2Tier2Scores,
        ShareOCTAM2Tier2Scores = x.ShareOCTAM2Tier2Scores,
        OCTAM2Tier2ScoresLastSharedDate = x.OCTAM2Tier2ScoresLastSharedDate,
        OCTAWatersheds = x.OCTAWatersheds,
        PollutantVolume = x.PollutantVolume,
        PollutantMetals = x.PollutantMetals,
        PollutantBacteria = x.PollutantBacteria,
        PollutantNutrients = x.PollutantNutrients,
        PollutantTSS = x.PollutantTSS,
        TPI = x.TPI,
        SEA = x.SEA,
        DryWeatherWQLRI = x.DryWeatherWQLRI,
        WetWeatherWQLRI = x.WetWeatherWQLRI,
        AreaTreatedAcres = x.AreaTreatedAcres,
        ImperviousAreaTreatedAcres = x.ImperviousAreaTreatedAcres,
        UpdatePersonID = x.UpdatePersonID,
        DateUpdated = x.DateUpdated,
        Organization = new OrganizationSimpleDto
        {
            OrganizationID = x.Organization.OrganizationID,
            OrganizationGuid = x.Organization.OrganizationGuid,
            OrganizationName = x.Organization.OrganizationName,
            OrganizationShortName = x.Organization.OrganizationShortName,
            PrimaryContactPersonID = x.Organization.PrimaryContactPersonID,
            IsActive = x.Organization.IsActive,
            OrganizationUrl = x.Organization.OrganizationUrl,
            LogoFileResourceID = x.Organization.LogoFileResourceID,
            OrganizationTypeID = x.Organization.OrganizationTypeID
        },
        StormwaterJurisdiction = new StormwaterJurisdictionDisplayDto
        {
            StormwaterJurisdictionID = x.StormwaterJurisdiction.StormwaterJurisdictionID,
            StormwaterJurisdictionName = x.StormwaterJurisdiction.Organization.OrganizationName
        },
        // ProjectStatus is a lookup type - resolved client-side via ResolveClientSideLookups
        PrimaryContactPerson = new PersonSimpleDto
        {
            PersonID = x.PrimaryContactPerson.PersonID,
            FirstName = x.PrimaryContactPerson.FirstName,
            LastName = x.PrimaryContactPerson.LastName,
            Email = x.PrimaryContactPerson.Email,
            Phone = x.PrimaryContactPerson.Phone,
            RoleID = x.PrimaryContactPerson.RoleID,
            // RoleName is a lookup type - resolved client-side via ResolveClientSideLookups
            CreateDate = x.PrimaryContactPerson.CreateDate,
            UpdateDate = x.PrimaryContactPerson.UpdateDate,
            LastActivityDate = x.PrimaryContactPerson.LastActivityDate,
            IsActive = x.PrimaryContactPerson.IsActive,
            OrganizationID = x.PrimaryContactPerson.OrganizationID,
            OrganizationName = x.PrimaryContactPerson.Organization.OrganizationName,
            ReceiveSupportEmails = x.PrimaryContactPerson.ReceiveSupportEmails,
            ReceiveRSBRevisionRequestEmails = x.PrimaryContactPerson.ReceiveRSBRevisionRequestEmails,
            IsOCTAGrantReviewer = x.PrimaryContactPerson.IsOCTAGrantReviewer,
            HasAssignedStormwaterJurisdiction = x.PrimaryContactPerson.StormwaterJurisdictionPeople.Any()
        },
        CreatePerson = new PersonSimpleDto
        {
            PersonID = x.CreatePerson.PersonID,
            FirstName = x.CreatePerson.FirstName,
            LastName = x.CreatePerson.LastName,
            Email = x.CreatePerson.Email,
            Phone = x.CreatePerson.Phone,
            RoleID = x.CreatePerson.RoleID,
            // RoleName is a lookup type - resolved client-side via ResolveClientSideLookups
            CreateDate = x.CreatePerson.CreateDate,
            UpdateDate = x.CreatePerson.UpdateDate,
            LastActivityDate = x.CreatePerson.LastActivityDate,
            IsActive = x.CreatePerson.IsActive,
            OrganizationID = x.CreatePerson.OrganizationID,
            OrganizationName = x.CreatePerson.Organization.OrganizationName,
            ReceiveSupportEmails = x.CreatePerson.ReceiveSupportEmails,
            ReceiveRSBRevisionRequestEmails = x.CreatePerson.ReceiveRSBRevisionRequestEmails,
            IsOCTAGrantReviewer = x.CreatePerson.IsOCTAGrantReviewer,
            HasAssignedStormwaterJurisdiction = x.CreatePerson.StormwaterJurisdictionPeople.Any()
        },
        HasModeledResults = x.AreaTreatedAcres != null
    };
}
