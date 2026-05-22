namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanPostConstructionVerificationGridDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public string? WaterQualityManagementPlanVerifyStatusName { get; set; }
    public int? NumberOfBMPs { get; set; }
    public int? NumberOfBMPsAdequate { get; set; }
    public int? NumberOfBMPsDeficient { get; set; }
    public string? WQMPVerificationComments { get; set; }
}
