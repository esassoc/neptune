using System.Text.Json.Serialization;
using Neptune.Common.GeoSpatial;
using NetTopologySuite.Geometries;

namespace Neptune.OverlayAPI;

public class TrashGeneratingUnitResult : IHasGeometry
{
    [JsonPropertyName("LUBID")]
    public int LandUseBlockID { get; set; }
    [JsonPropertyName("SJID")]
    public int StormwaterJurisdictionID { get; set; }
    [JsonPropertyName("DelinID")]
    public int? DelineationID { get; set; }
    [JsonPropertyName("WQMPID")]
    public int? WaterQualityManagementPlanID { get; set; }
    [JsonPropertyName("OVTAID")]
    public int? OnlandVisualTrashAssessmentAreaID { get; set; }
    public Geometry Geometry { get; set; }
}