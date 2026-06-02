namespace Neptune.Models.DataTransferObjects;

public class LandUseBlockGdbUploadResultDto
{
    public List<string> Errors { get; set; } = new();
    public int StagedRowCount { get; set; }
    // NPT-1077: dropped BackgroundJobEnqueued — the upload endpoint no longer enqueues the job.
    // The SPA now redirects to the approve page (which calls staging-report, then approve, which
    // enqueues the job).
}
