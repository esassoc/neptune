namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanVerifyGridDto
{
    public int WaterQualityManagementPlanVerifyID { get; set; }
    public DateOnly VerificationDate { get; set; }
    public DateTime LastEditedDate { get; set; }
    public string? LastEditedByPersonFullName { get; set; }
    public int WaterQualityManagementPlanVerifyTypeID { get; set; }
    public string? WaterQualityManagementPlanVerifyTypeDisplayName { get; set; }
    public int WaterQualityManagementPlanVisitStatusID { get; set; }
    public string? WaterQualityManagementPlanVisitStatusDisplayName { get; set; }
    public int? WaterQualityManagementPlanVerifyStatusID { get; set; }
    public string? WaterQualityManagementPlanVerifyStatusDisplayName { get; set; }
    public bool IsDraft { get; set; }
}
