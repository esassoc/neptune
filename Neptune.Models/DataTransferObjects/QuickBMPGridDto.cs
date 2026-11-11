namespace Neptune.Models.DataTransferObjects;

public class QuickBMPGridDto
{
    public int QuickBMPID { get; set; }
    public string? QuickBMPName { get; set; }
    public int TreatmentBMPTypeID { get; set; }
    public string? TreatmentBMPTypeName { get; set; }
    public int NumberOfIndividualBMPs { get; set; }
    public int WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public string? StormwaterJurisdictionName { get; set; }
    public string? WaterQualityManagementPlanStatusDisplayName { get; set; }
    public string? HydrologicSubareaName { get; set; }
    public decimal? PercentOfSiteTreated { get; set; }
    public decimal? RecordedWQMPAreaInAcres { get; set; }
    public decimal? CalculatedBoundaryAcreage { get; set; }
    public decimal? DrainageAreaTreatedAcres { get; set; }
    public decimal? PercentCaptured { get; set; }
    public decimal? PercentRetained { get; set; }
    public int? DryWeatherFlowOverrideID { get; set; }
    public string? DryWeatherFlowOverrideDisplayName { get; set; }
    public string? QuickBMPNote { get; set; }
}
