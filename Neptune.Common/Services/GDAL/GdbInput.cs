using System.Text.Json.Serialization;

namespace Neptune.Common.Services.GDAL;

public class GdbInput
{
    public string? BlobContainer { get; set; }
    public string? CanonicalName { get; set; }
    [JsonIgnore]
    public byte[]? FileContents { get; set; }
    public string LayerName { get; set; }
    public string GeometryTypeName { get; set; }
    public int CoordinateSystemID { get; set; }
}