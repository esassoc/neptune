namespace Neptune.Models.DataTransferObjects;

public class WaterQualityManagementPlanExtractionResultDto
{
    public int WaterQualityManagementPlanID { get; set; }
    public int WaterQualityManagementPlanDocumentID { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    // User-entered at upload. Surfaced to the frontend review so it can render the field
    // with a "user-entered" origin pill instead of treating Claude's suggestion as canonical.
    public string WaterQualityManagementPlanName { get; set; }
    public string ExtractionResultJson { get; set; }
    public DateTime ExtractedAt { get; set; }
    public string FileResourceGuid { get; set; }
    // NPT-1051: populated on failed extraction so the wizard can show why instead of
    // returning a misleading "no extraction" state on subsequent page loads.
    public string ErrorMessage { get; set; }
    public string ErrorCode { get; set; }
}
