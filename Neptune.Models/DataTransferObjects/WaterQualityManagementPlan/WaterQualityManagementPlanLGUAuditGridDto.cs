namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanLGUAuditGridDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public string WaterQualityManagementPlanName { get; set; }
    public bool LoadGeneratingUnitsPopulated { get; set; }
    public bool BoundaryIsDefined { get; set; }
    public bool IntersectsModelBasins { get; set; }
}
