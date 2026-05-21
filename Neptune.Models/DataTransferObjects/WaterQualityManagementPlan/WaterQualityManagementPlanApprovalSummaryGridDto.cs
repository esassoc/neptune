namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanApprovalSummaryGridDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public string? Priority { get; set; }
    public string? LandUse { get; set; }
    public string? HydrologicSubareaName { get; set; }
    public decimal? RecordedWQMPAreaInAcres { get; set; }
    public DateTime? ApprovalDate { get; set; }
}
