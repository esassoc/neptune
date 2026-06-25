namespace Neptune.Models.DataTransferObjects;

public class OvtaAreaGdbStagingReportDto
{
    public List<string> Errors { get; set; } = new();
    public int? StormwaterJurisdictionID { get; set; }
    public int? NumberOfOvtaAreas { get; set; }
    public int? NumberOfOvtaAreasToBeUpdated { get; set; }
    public int? NumberOfOvtaAreasToBeCreated { get; set; }
}
