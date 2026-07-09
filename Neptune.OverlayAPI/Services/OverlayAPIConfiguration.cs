namespace Neptune.OverlayAPI.Services
{
    public class OverlayAPIConfiguration
    {
        public string DatabaseConnectionString { get; set; }

        // When set (local parity work only), the NTS overlay endpoints also write their result set
        // as GeoJSON into this folder so tools/compare_geojson.py can score them against the
        // captured QGIS golden outputs. Leave unset in QA/prod.
        public string? OverlayDebugExportFolder { get; set; }
    }
}
