namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanConflictDto
{
    public int ExistingWaterQualityManagementPlanID { get; set; }
    public string ExistingStatus { get; set; }
    public bool CanOverwrite { get; set; }
    public string Message { get; set; }
}
