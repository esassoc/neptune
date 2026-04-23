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
    public string? DraftOverlayJson { get; set; }
    public DateTime? DraftUpdatedDate { get; set; }
    public string? DraftUpdatedByFullName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedByFullName { get; set; }
}
