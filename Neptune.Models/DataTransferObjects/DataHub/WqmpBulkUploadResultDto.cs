namespace Neptune.Models.DataTransferObjects;

public class WqmpBulkUploadResultDto
{
    public List<string> Errors { get; set; } = new();
    public int AddedCount { get; set; }
    public int UpdatedCount { get; set; }
}
