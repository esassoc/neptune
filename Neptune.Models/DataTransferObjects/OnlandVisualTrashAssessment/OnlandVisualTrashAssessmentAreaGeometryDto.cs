namespace Neptune.Models.DataTransferObjects;

public class OnlandVisualTrashAssessmentAreaGeometryDto
{
    public int OnlandVisualTrashAssessmentAreaID { get; set; }
    // NPT-1066: which source built the geometry. Null = manually drawn (Geoman) — use
    // GeometryAsGeoJson. Parcel / LandUseBlock unions the corresponding selected IDs server-side.
    public int? OvtaAreaSourceTypeID { get; set; }
    public List<int> ParcelIDs { get; set; } = new();
    public List<int> SelectedLandUseBlockIDs { get; set; } = new();
    public string GeometryAsGeoJson { get; set; }
}
