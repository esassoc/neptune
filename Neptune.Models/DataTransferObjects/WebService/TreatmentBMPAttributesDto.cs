namespace Neptune.Models.DataTransferObjects.WebService;

public class TreatmentBMPAttributesDto
{
    public int PrimaryKey { get; set; }
    public double? LocationLon { get; set; }
    public double? LocationLat { get; set; }
    public string Watershed { get; set; }
    public int? WaterQualityManagementPlanID { get; set; }
    public int TreatmentBMPID { get; set; }
    public int? UpstreamTreatmentBMPID { get; set; }
    public double? AverageDivertedFlowrate { get; set; }
    public double? AverageTreatmentFlowrate { get; set; }
    public double? DesignDryWeatherTreatmentCapacity { get; set; }
    public double? DesignLowFlowDiversionCapacity { get; set; }
    public double? DesignMediaFiltrationRate { get; set; }
    public double? DiversionRate { get; set; }
    public double? DrawdownTimeforWQDetentionVolume { get; set; }
    public double? EffectiveFootprint { get; set; }
    public double? EffectiveRetentionDepth { get; set; }
    public double? InfiltrationDischargeRate { get; set; }
    public double? InfiltrationSurfaceArea { get; set; }
    public double? MediaBedFootprint { get; set; }
    public double? PermanentPoolorWetlandVolume { get; set; }
    public string RoutingConfiguration { get; set; }
    public double? StorageVolumeBelowLowestOutletElevation { get; set; }
    public double? SummerHarvestedWaterDemand { get; set; }
    public string TimeOfConcentration { get; set; }
    public double? DrawdownTimeForDetentionVolume { get; set; }
    public double? TotalEffectiveBMPVolume { get; set; }
    public double? TotalEffectiveDrywellBMPVolume { get; set; }
    public double? TreatmentRate { get; set; }
    public string UnderlyingHydrologicSoilGroup { get; set; }
    public double? UnderlyingInfiltrationRate { get; set; }
    public double? WaterQualityDetentionVolume { get; set; }
    public double? WettedFootprint { get; set; }
    public double? WinterHarvestedWaterDemand { get; set; }
    public string DelineationType { get; set; }
    public string TreatmentBMPTypeName { get; set; }
    public string TreatmentBMPName { get; set; }
    public string Jurisdiction { get; set; }
}
