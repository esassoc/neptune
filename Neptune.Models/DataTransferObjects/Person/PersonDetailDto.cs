namespace Neptune.Models.DataTransferObjects.Person;

public class PersonDetailDto
{
    public int PersonID { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public DateTime CreateDate { get; set; }
    public DateTime? UpdateDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public bool IsActive { get; set; }

    public int RoleID { get; set; }
    public string RoleName { get; set; }
    public string RoleDisplayName { get; set; }

    public OrganizationSimpleDto Organization { get; set; }

    public bool IsOCTAGrantReviewer { get; set; }
    public bool ReceiveSupportEmails { get; set; }
    public bool ReceiveRSBRevisionRequestEmails { get; set; }

    public List<StormwaterJurisdictionDisplayDto> AssignedStormwaterJurisdictions { get; set; } = new();
    public List<OrganizationSimpleDto> PrimaryContactOrganizations { get; set; } = new();
}
