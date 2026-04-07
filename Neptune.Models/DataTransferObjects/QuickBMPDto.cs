namespace Neptune.Models.DataTransferObjects;

public class QuickBMPDto
{
    public int QuickBMPID { get; set; }
    public string? QuickBMPName { get; set; }
    public int TreatmentBMPTypeID { get; set; }
    public string? TreatmentBMPTypeName { get; set; }
    public string? QuickBMPNote { get; set; }
    public int NumberOfIndividualBMPs { get; set; }
    public decimal? PercentOfSiteTreated { get; set; }
    public decimal? PercentCaptured { get; set; }
    public decimal? PercentRetained { get; set; }
    public int? DryWeatherFlowOverrideID { get; set; }
}
