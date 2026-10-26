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
    /// <summary>
    /// Optional ogr2ogr -where filter on the source features. Lets an export include a typed schema row
    /// (so OGR infers every field's type even when a column is all null) and drop it from the output.
    /// </summary>
    public string? Where { get; set; }
}