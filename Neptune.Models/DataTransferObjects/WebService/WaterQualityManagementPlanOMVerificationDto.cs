namespace Neptune.Models.DataTransferObjects.WebService;

public class WaterQualityManagementPlanOMVerificationDto
{
    public int WQMPID { get; set; }
    public string WQMPName { get; set; }
    public string Jurisdiction { get; set; }
    public string VerificationDate { get; set; }
    public string LastEditedDate { get; set; }
    public string LastEditedBy { get; set; }
    public string TypeOfVerification { get; set; }
    public string VisitStatus { get; set; }
    public string VerificationStatus { get; set; }
    public string SourceControlCondition { get; set; }
    public string EnforcementOrFollowupActions { get; set; }
    public string DraftOrFinalized { get; set; }
}
