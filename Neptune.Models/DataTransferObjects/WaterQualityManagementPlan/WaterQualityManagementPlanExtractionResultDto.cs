namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanExtractionResultDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public int WaterQualityManagementPlanDocumentID { get; set; }
    public string ExtractionResultJson { get; set; }
    public DateTime ExtractedAt { get; set; }
    public string FileResourceGuid { get; set; }
}
