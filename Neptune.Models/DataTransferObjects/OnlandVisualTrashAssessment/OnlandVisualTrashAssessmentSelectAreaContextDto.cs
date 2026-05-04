namespace Neptune.Models.DataTransferObjects;

public class OnlandVisualTrashAssessmentSelectAreaContextDto
{
    public int OnlandVisualTrashAssessmentID { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public bool JurisdictionHasLandUseBlocks { get; set; }
    public int OvtaAreaSourceTypeID { get; set; }
    public bool IsDraftGeometryManuallyRefined { get; set; }
    public List<int> SelectedParcelIDs { get; set; } = new();
    public List<int> SelectedLandUseBlockIDs { get; set; } = new();
}
