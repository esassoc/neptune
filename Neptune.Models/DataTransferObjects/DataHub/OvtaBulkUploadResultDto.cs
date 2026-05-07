namespace Neptune.Models.DataTransferObjects;

public class OvtaBulkUploadResultDto
{
    public List<string> Errors { get; set; } = new();
    public int RowsProcessed { get; set; }
}
