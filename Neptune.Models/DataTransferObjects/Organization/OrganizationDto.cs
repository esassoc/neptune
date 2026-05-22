namespace Neptune.Models.DataTransferObjects;

public class OrganizationDto
{
    public int OrganizationID { get; set; }
    public Guid? OrganizationGuid { get; set; }
    public string OrganizationName { get; set; }
    public string OrganizationShortName { get; set; }
    public PersonSimpleDto? PrimaryContactPerson { get; set; }
    public bool IsActive { get; set; }
    public string OrganizationUrl { get; set; }
    public FileResourceSimpleDto? LogoFileResource { get; set; }
    public OrganizationTypeSimpleDto OrganizationType { get; set; }
    /// <summary>NPT-999: populated when this Organization also has a StormwaterJurisdiction row.
    /// Drives the legacy-parity "is a Jurisdiction" alert at the top of the SPA detail page.</summary>
    public int? StormwaterJurisdictionID { get; set; }
}