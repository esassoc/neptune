namespace Neptune.Models.DataTransferObjects;

public class SupportRequestSubmissionDto
{
    public string Name { get; set; }
    public string Email { get; set; }
    public string Organization { get; set; }
    public string Phone { get; set; }
    public int SupportRequestTypeID { get; set; }
    public string Description { get; set; }
    public string CurrentPageUrl { get; set; }
    public string RecaptchaToken { get; set; }
}
