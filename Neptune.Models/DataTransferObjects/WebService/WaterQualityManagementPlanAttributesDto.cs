namespace Neptune.Models.DataTransferObjects.WebService;

public class WaterQualityManagementPlanAttributesDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public string Name { get; set; }
    public string Jurisdiction { get; set; }
    public string Status { get; set; }
    public string DevelopmentType { get; set; }
    public string LandUse { get; set; }
    public string PermitTerm { get; set; }
    public int? ApprovalDate { get; set; }
    public int? DateOfConstruction { get; set; }
    public string HydromodificationApplies { get; set; }
    public string HydrologicSubarea { get; set; }
    public decimal? RecordedWQMPAreaInAcres { get; set; }
    public string TrashCaptureStatus { get; set; }
    public int? TrashCaptureEffectiveness { get; set; }
    public string ModelingApproach { get; set; }
}
