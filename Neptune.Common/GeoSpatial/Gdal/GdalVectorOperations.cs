using Neptune.Common.DesignByContract;
using Neptune.Common.Services.GDAL;
using OSGeo.GDAL;
using OSGeo.OGR;

namespace Neptune.Common.GeoSpatial.Gdal;

/// <summary>
/// The File Geodatabase operations Neptune needs, run in-process through the GDAL library instead of by
/// shelling out to the ogr2ogr / ogrinfo executables.
///
/// The conversions go through GDALVectorTranslate — the library function the ogr2ogr executable itself
/// is a thin wrapper around — with the same options the command line used to get, so the output is the
/// same; what goes away is the process launch, the stdout/stderr scraping and the full GDAL base image.
/// Feature classes are listed from the layer definitions directly rather than parsed out of ogrinfo text.
/// </summary>
public static class GdalVectorOperations
{
    private const string FileGdbDriverName = "OpenFileGDB";

    /// <summary>
    /// Lists the layers of a File Geodatabase. <paramref name="gdbPath"/> may be a .gdb directory or a
    /// zipped one (.gdb.zip), which the OpenFileGDB driver opens directly.
    /// </summary>
    public static List<FeatureClassInfo> ListFeatureClasses(string gdbPath)
    {
        GdalSetup.EnsureInitialised();

        using var dataset = OpenVectorReadOnly(gdbPath);
        var featureClassInfos = new List<FeatureClassInfo>();
        for (var i = 0; i < dataset.GetLayerCount(); i++)
        {
            using var layer = dataset.GetLayer(i);
            using var layerDefinition = layer.GetLayerDefn();

            var columns = new List<string>();
            for (var fieldIndex = 0; fieldIndex < layerDefinition.GetFieldCount(); fieldIndex++)
            {
                using var fieldDefinition = layerDefinition.GetFieldDefn(fieldIndex);
                columns.Add(fieldDefinition.GetName().ToLower());
            }

            featureClassInfos.Add(new FeatureClassInfo
            {
                // Original casing is preserved: the name is used verbatim in the OGR SQL "FROM <layer>"
                // of the GeoJSON conversion, and OGR SQL is case-sensitive on Linux.
                LayerName = layer.GetName(),
                FeatureType = Ogr.GeometryTypeToName(layer.GetGeomType()),
                FeatureCount = (int)layer.GetFeatureCount(1),
                Columns = columns,
            });
        }

        return featureClassInfos;
    }

    /// <summary>
    /// Writes a GeoJSON file into a File Geodatabase as one layer. With <paramref name="update"/> the
    /// layer is added to an existing geodatabase; without it the geodatabase is created.
    /// </summary>
    public static void GeoJsonToFileGdb(string geoJsonPath, int coordinateSystemID, string gdbOutputPath,
        string layerName, bool update, string geometryTypeName, string? where = null)
    {
        GdalSetup.EnsureInitialised();

        var options = new List<string>();
        if (update)
        {
            options.Add("-update");
        }

        options.AddRange([
            "-s_srs", GetMapProjection(coordinateSystemID),
            "-a_srs", GetMapProjection(coordinateSystemID),
            "-f", FileGdbDriverName,
            "-nln", SanitizeStringForGdb(layerName),
            "-nlt", geometryTypeName,
            "-append",
        ]);

        // The layer is still created with the source schema when the filter leaves no features.
        if (!string.IsNullOrWhiteSpace(where))
        {
            options.Add("-where");
            options.Add(where);
        }

        VectorTranslate(geoJsonPath, gdbOutputPath, options);
    }

    /// <summary>
    /// Extracts one layer of a File Geodatabase as GeoJSON, selecting/renaming columns with OGR SQL and
    /// reprojecting to the requested coordinate system.
    /// </summary>
    public static string FileGdbToGeoJson(string gdbPath, GdbLayerOutput layerOutput)
    {
        GdalSetup.EnsureInitialised();

        var reservedFields = new[] { "Ogr_Fid", "Ogr_Geometry" };
        var columnNames = layerOutput.Columns
            .Where(x => reservedFields.All(y => !string.Equals(x, y, StringComparison.InvariantCultureIgnoreCase)))
            .ToList();
        const string columnListSeparator = ",";
        Check.Require(columnNames.All(x => !x.Contains(columnListSeparator)),
            $"Found column names with separator character \"{columnListSeparator}\", can't continue. Columns:{string.Join("\r\n", columnNames)}");

        var selectList = columnNames.Any() ? string.Join(columnListSeparator + " ", columnNames) : "*";
        var options = new List<string>
        {
            "-sql", $"select {selectList} from {layerOutput.FeatureLayerName} {layerOutput.Filter}",
            "-t_srs", GetMapProjection(layerOutput.CoordinateSystemID),
            "-f", "GeoJSON",
            "-nln", layerOutput.FeatureLayerName,
        };

        if (layerOutput.Extent != null)
        {
            options.AddRange([
                "-clipsrc",
                layerOutput.Extent.MinX.ToString(System.Globalization.CultureInfo.InvariantCulture),
                layerOutput.Extent.MinY.ToString(System.Globalization.CultureInfo.InvariantCulture),
                layerOutput.Extent.MaxX.ToString(System.Globalization.CultureInfo.InvariantCulture),
                layerOutput.Extent.MaxY.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ]);
        }

        using var geoJsonTempFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".geojson");
        VectorTranslate(gdbPath, geoJsonTempFile.FileInfo.FullName, options);
        return File.ReadAllText(geoJsonTempFile.FileInfo.FullName);
    }

    public static string GetMapProjection(int coordinateSystemID) => $"EPSG:{coordinateSystemID}";

    public static string SanitizeStringForGdb(string str)
    {
        var arr = str.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray();
        return new string(arr).Replace(" ", "_");
    }

    private static Dataset OpenVectorReadOnly(string path)
    {
        return OSGeo.GDAL.Gdal.OpenEx(path, (uint)(GdalConst.OF_VECTOR | GdalConst.OF_READONLY), null, null, null)
               ?? throw new GdalOperationException($"GDAL could not open \"{path}\" as a vector dataset. {LastError()}");
    }

    private static void VectorTranslate(string sourcePath, string destinationPath, List<string> options)
    {
        try
        {
            using var source = OpenVectorReadOnly(sourcePath);
            using var translateOptions = new GDALVectorTranslateOptions(options.ToArray());
            // Disposing the returned dataset is what flushes and closes the output.
            using var result = OSGeo.GDAL.Gdal.wrapper_GDALVectorTranslateDestName(destinationPath, source, translateOptions, null, null);
            if (result == null)
            {
                throw new GdalOperationException($"GDAL vector translate produced no output. {LastError()}");
            }
        }
        catch (Exception ex) when (ex is not GdalOperationException)
        {
            // The bindings surface GDAL errors as bare ApplicationExceptions; add what was being attempted.
            throw new GdalOperationException(
                $"GDAL vector translate failed: {ex.Message}\r\nOptions: {string.Join(" ", options)}", ex);
        }
    }

    private static string LastError()
    {
        var message = OSGeo.GDAL.Gdal.GetLastErrorMsg();
        return string.IsNullOrWhiteSpace(message) ? string.Empty : $"GDAL said: {message}";
    }
}

public class GdalOperationException : Exception
{
    public GdalOperationException(string message) : base(message)
    {
    }

    public GdalOperationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
