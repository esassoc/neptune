namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPModelingAttributesDto
{
    public int TreatmentBMPID { get; set; }
    public string? TreatmentBMPName { get; set; }
    public int? TreatmentBMPTypeID { get; set; }
    public string? TreatmentBMPTypeName { get; set; }
    public int? StormwaterJurisdictionID { get; set; }
    public string? StormwaterJurisdictionName { get; set; }
    public int? WaterQualityManagementPlanID { get; set; }
    public string? WaterQualityManagementPlanName { get; set; }
    public int? WatershedID { get; set; }
    public string? WatershedName { get; set; }
    public int? PrecipitationZoneID { get; set; }
    public double? DesignStormwaterDepthInInches { get; set; }
    public int? DelineationID { get; set; }
    public string? DelineationTypeName { get; set; }
    public string? DelineationStatus { get; set; }
    public double? DelineationAreaAcres { get; set; }
    public double? ModeledLandUseAreaAcres { get; set; }
    public bool IsFullyParameterized { get; set; }
    // Modeling attributes
    public double? AverageDivertedFlowrate { get; set; }
    public double? AverageTreatmentFlowrate { get; set; }
    public double? DesignDryWeatherTreatmentCapacity { get; set; }
    public double? DesignLowFlowDiversionCapacity { get; set; }
    public double? DesignMediaFiltrationRate { get; set; }
    public double? DrawdownTimeForWQDetentionVolume { get; set; }
    public double? EffectiveFootprint { get; set; }
    public double? EffectiveRetentionDepth { get; set; }
    public double? InfiltrationDischargeRate { get; set; }
    public double? InfiltrationSurfaceArea { get; set; }
    public double? MediaBedFootprint { get; set; }
    public string? MonthsOperational { get; set; }
    public double? PermanentPoolOrWetlandVolume { get; set; }
    public double? StorageVolumeBelowLowestOutletElevation { get; set; }
    public double? SummerHarvestedWaterDemand { get; set; }
    public string? TimeOfConcentration { get; set; }
    public double? TotalEffectiveBMPVolume { get; set; }
    public double? TotalEffectiveDrywellBMPVolume { get; set; }
    public double? TreatmentRate { get; set; }
    public string? UnderlyingHydrologicSoilGroup { get; set; }
    public double? UnderlyingInfiltrationRate { get; set; }
    public double? ExtendedDetentionSurchargeVolume { get; set; }
    public double? WettedFootprint { get; set; }
    public double? WinterHarvestedWaterDemand { get; set; }
    public int? UpstreamBMPID { get; set; }
    public string? UpstreamBMPName { get; set; }
    public bool DownstreamOfNonModeledBMP { get; set; }
    public string? DryWeatherFlowOverride { get; set; }
}