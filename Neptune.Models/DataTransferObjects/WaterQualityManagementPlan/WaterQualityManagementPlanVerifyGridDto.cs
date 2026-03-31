namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanVerifyGridDto
{
    public int WaterQualityManagementPlanVerifyID { get; set; }
    public DateTime VerificationDate { get; set; }
    public DateTime LastEditedDate { get; set; }
    public string? LastEditedByPersonFullName { get; set; }
    public string? WaterQualityManagementPlanVerifyTypeDisplayName { get; set; }
    public string? WaterQualityManagementPlanVisitStatusDisplayName { get; set; }
    public string? WaterQualityManagementPlanVerifyStatusDisplayName { get; set; }
    public bool IsDraft { get; set; }
}
