namespace Neptune.Models.DataTransferObjects;

public class TreatmentBMPCsvUploadResultDto
{
    public List<string> Errors { get; set; } = new();
    public int AddedCount { get; set; }
    public int UpdatedCount { get; set; }
}
