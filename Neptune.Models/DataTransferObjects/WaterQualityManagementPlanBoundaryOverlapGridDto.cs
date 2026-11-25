namespace Neptune.Models.DataTransferObjects;

/// <summary>
/// One pair of overlapping WQMP boundaries on the WQMP Boundary Overlap Report (NPT-1155).
/// </summary>
public class WaterQualityManagementPlanBoundaryOverlapGridDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string? StormwaterJurisdictionName { get; set; }
    /// <summary>Whether this plan has a simple BMP the model uses.</summary>
    public bool TreatsRunoff { get; set; }

    public int OverlappingWaterQualityManagementPlanID { get; set; }
    public string? OverlappingWaterQualityManagementPlanName { get; set; }
    public int OverlappingStormwaterJurisdictionID { get; set; }
    public string? OverlappingStormwaterJurisdictionName { get; set; }
    public bool OverlappingTreatsRunoff { get; set; }

    public double AreaOfOverlapInAcres { get; set; }

    /// <summary>
    /// True when the model counts the shared ground under both plans: both treat, or neither does.
    /// When only one of them treats, that plan gets the ground and it is counted once.
    /// </summary>
    public bool IsCountedTwice { get; set; }
}
