namespace Neptune.Models.DataTransferObjects.Person;

public class PersonRoleUpdateDto
{
    public int RoleID { get; set; }
    public int OrganizationID { get; set; }
    public bool IsOCTAGrantReviewer { get; set; }
    public bool ReceiveSupportEmails { get; set; }
    public bool ReceiveRSBRevisionRequestEmails { get; set; }
}
