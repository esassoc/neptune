namespace Neptune.Common.GeoSpatial.Gdal;

/// <summary>
/// One-time, process-wide GDAL initialisation for the in-process (MaxRev) bindings.
///
/// Neptune.Common references only the managed half (MaxRev.Gdal.Core). The native runtime comes from the
/// host project — Neptune.GDALAPI and Neptune.Tests — and deliberately NOT from Neptune.API: native GDAL
/// parses user-uploaded geodatabases, and a native crash must only ever take down the GDAL container.
/// </summary>
public static class GdalSetup
{
    private static readonly Lazy<bool> Initialiser = new(() =>
    {
        // Registers drivers and points GDAL_DATA / PROJ_LIB at the data bundled in the runtime package.
        MaxRev.Gdal.Core.GdalBase.ConfigureAll();
        OSGeo.OGR.Ogr.RegisterAll();
        return true;
    }, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Idempotent and cheap after the first call. Every entry point into GDAL calls this first.</summary>
    public static void EnsureInitialised() => _ = Initialiser.Value;

    /// <summary>The GDAL release actually loaded, for startup logging and diagnostics.</summary>
    public static string VersionInfo()
    {
        EnsureInitialised();
        return OSGeo.GDAL.Gdal.VersionInfo("RELEASE_NAME");
    }
}
