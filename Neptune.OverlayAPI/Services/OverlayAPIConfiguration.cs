namespace Neptune.OverlayAPI.Services
{
    public class OverlayAPIConfiguration
    {
        public string DatabaseConnectionString { get; set; }

        // When set (local work only), the TGU and LGU endpoints also write their result set as
        // GeoJSON into this folder, so tools/compare_geojson.py can compare a run before a change
        // with a run after it. Leave unset in QA/prod.
        public string? OverlayDebugExportFolder { get; set; }
    }
}
