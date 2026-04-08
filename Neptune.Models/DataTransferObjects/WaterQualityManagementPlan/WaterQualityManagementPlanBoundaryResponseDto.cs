namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanBoundaryResponseDto
{
    public object? BoundaryAsFeatureCollection { get; set; }
    public List<ParcelDisplayDto> Parcels { get; set; } = new();
    public double? CalculatedWQMPAcreage { get; set; }
    public BoundingBoxDto? BoundingBox { get; set; }
}
