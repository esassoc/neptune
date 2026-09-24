namespace Neptune.Models.DataTransferObjects.NearbyAsset;

public class NearbyAssetsResultDto
{
    public double RadiusMeters { get; set; }
    public List<NearbyAssetDto> Assets { get; set; } = new();
}
