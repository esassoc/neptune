namespace Neptune.Models.DataTransferObjects;

public class LandUseBlockGdbUploadResultDto
{
    public List<string> Errors { get; set; } = new();
    public int StagedRowCount { get; set; }
    public bool BackgroundJobEnqueued { get; set; }
}
