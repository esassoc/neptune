namespace Neptune.QGISAPI.Services
{
    public class QGISAPIConfiguration
    {
        public string DatabaseConnectionString { get; set; }

        // Upper bound on a single python3/QGIS run (LGU/TGU/PLGU overlay). The nightly full TGU refresh is the
        // longest legitimate run; tune via env var (e.g. in the qgisapi configmap) if it starts flirting with this.
        // Must stay below the QGISAPIService HttpClient timeout (Neptune.API) so the process timeout fires first
        // and the controller can return a real 500 with the captured stderr.
        public int QgisProcessTimeoutMinutes { get; set; } = 240;
    }
}
