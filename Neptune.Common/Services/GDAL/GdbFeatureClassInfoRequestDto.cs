namespace Neptune.Common.Services.GDAL;

/// <summary>
/// Points the GDAL API at a File Geodatabase already staged in blob storage so it can be inspected
/// in place, rather than pushing the whole .gdb.zip across the wire as multipart form data.
/// </summary>
public class GdbFeatureClassInfoRequestDto
{
    public string BlobContainer { get; set; }
    public string CanonicalName { get; set; }
}
