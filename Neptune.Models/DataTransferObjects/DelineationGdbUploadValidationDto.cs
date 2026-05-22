namespace Neptune.Models.DataTransferObjects;

public class DelineationGdbUploadValidationDto
{
    public int? StormwaterJurisdictionID { get; set; }
    public int? NumberOfDelineations { get; set; }
    public int? NumberOfDelineationsToBeUpdated { get; set; }
    public int? NumberOfDelineationsToBeCreated { get; set; }
    public List<string> Errors { get; set; } = new();
}
