namespace Neptune.Models.DataTransferObjects;

public class WQMPBoundaryUploadResultDto
{
    public List<string> Errors { get; set; } = new();
    public int AddedCount { get; set; }
    public int UpdatedCount { get; set; }
    public List<string> MissingApns { get; set; } = new();
}
