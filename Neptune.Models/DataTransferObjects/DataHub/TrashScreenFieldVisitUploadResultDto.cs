namespace Neptune.Models.DataTransferObjects;

public class TrashScreenFieldVisitUploadResultDto
{
    public List<string> Errors { get; set; } = new();
    public int RowsProcessed { get; set; }
}
