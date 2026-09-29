namespace Neptune.Models.DataTransferObjects.Search;

// One row of the header record search index (NPT-1125). Deliberately slim: the index ships every record
// the caller can see, and the SPA filters it client-side.
public class SearchRecordDto
{
    public const string TreatmentBMPScope = "bmp";
    public const string WaterQualityManagementPlanScope = "wqmp";
    public const string OnlandVisualTrashAssessmentScope = "ovta";
    public const string ProjectScope = "project";

    // "bmp" | "wqmp" | "ovta" | "project"
    public string Scope { get; set; } = string.Empty;
    public int ID { get; set; }
    public string Title { get; set; } = string.Empty;
    // The one distinguishing attribute (type or status); matched by the search, so keep it short
    public string? Subtitle { get; set; }
    // Trailing datum that pins which record this is (jurisdiction, or date for OVTAs); not matched
    public string? Meta { get; set; }
}
