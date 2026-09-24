namespace Neptune.Models.DataTransferObjects.NearbyAsset;

public class NearbyAssetDto
{
    public const string TreatmentBMPAssetType = "BMP";
    public const string WaterQualityManagementPlanAssetType = "WQMP";
    public const string OnlandVisualTrashAssessmentAreaAssetType = "OVTA";

    // "BMP" | "WQMP" | "OVTA"
    public string AssetType { get; set; } = string.Empty;
    public int AssetID { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public int StormwaterJurisdictionID { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double DistanceMeters { get; set; }
}
