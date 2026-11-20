using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using Neptune.Common.GeoSpatial.Gdal;
using Neptune.Common.Services.GDAL;

namespace Neptune.Tests;

/// <summary>
/// Exercises the in-process GDAL operations that replace the ogr2ogr / ogrinfo executables: the export
/// direction (GeoJSON -> File Geodatabase), the feature-class listing, and the import direction
/// (zipped File Geodatabase -> GeoJSON with OGR SQL column aliases). No database or container needed.
/// </summary>
[TestClass]
public class GdalVectorOperationsTests
{
    private const int Srid = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID;

    // Two polygons in EPSG:2771 (metres), near Orange County. DelineationArea is null on every feature
    // except the schema row, mirroring how the exports force a field's type.
    private const string DelineationsGeoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            { "type": "Feature",
              "geometry": { "type": "Polygon", "coordinates": [[[1850000,660000],[1850100,660000],[1850100,660100],[1850000,660100],[1850000,660000]]] },
              "properties": { "DelineationID": 1, "TreatmentBMPName": "North Basin", "DelineationArea": 2.47, "PermitType": "Phase I MS4" } },
            { "type": "Feature",
              "geometry": { "type": "Polygon", "coordinates": [[[1851000,661000],[1851200,661000],[1851200,661200],[1851000,661200],[1851000,661000]]] },
              "properties": { "DelineationID": 2, "TreatmentBMPName": "South Swale", "DelineationArea": 9.88, "PermitType": null } }
          ]
        }
        """;

    private const string PointsGeoJson = """
        {
          "type": "FeatureCollection",
          "features": [
            { "type": "Feature",
              "geometry": { "type": "Point", "coordinates": [1850050,660050] },
              "properties": { "TreatmentBMPID": 10, "TreatmentBMPName": "North Basin" } }
          ]
        }
        """;

    [TestMethod]
    public void GdalLoadsAndHasTheDriversWeNeed()
    {
        Console.WriteLine(GdalSetup.VersionInfo());
        using var fileGdb = OSGeo.OGR.Ogr.GetDriverByName("OpenFileGDB");
        using var geoJson = OSGeo.OGR.Ogr.GetDriverByName("GeoJSON");
        Assert.IsNotNull(fileGdb, "No OpenFileGDB driver in this GDAL build.");
        Assert.IsNotNull(geoJson, "No GeoJSON driver in this GDAL build.");
    }

    [TestMethod]
    public void ExportThenListThenImport_RoundTripsThroughAZippedFileGdb()
    {
        using var workingDirectory = DisposableTempDirectory.MakeDisposableTempDirectoryEndingIn("-gdal");
        var root = Directory.CreateDirectory(workingDirectory.DirectoryInfo.FullName).FullName;
        var gdbPath = Path.Combine(root, "export.gdb");

        // Export direction: two layers into one geodatabase, the second with -update, as the API does.
        var delineationsPath = WriteFile(root, "delineations.json", DelineationsGeoJson);
        var pointsPath = WriteFile(root, "points.json", PointsGeoJson);
        GdalVectorOperations.GeoJsonToFileGdb(delineationsPath, Srid, gdbPath, "distributed-delineations", false, "POLYGON");
        GdalVectorOperations.GeoJsonToFileGdb(pointsPath, Srid, gdbPath, "Treatment BMPs", true, "POINT");

        Assert.IsTrue(Directory.Exists(gdbPath), "The geodatabase directory was not created.");

        // Zip it the way exports are returned and uploads arrive, and read it back zipped.
        var zipPath = Path.Combine(root, "export.gdb.zip");
        ZipFile.CreateFromDirectory(gdbPath, zipPath, CompressionLevel.Optimal, true);

        var featureClasses = GdalVectorOperations.ListFeatureClasses(zipPath);
        foreach (var featureClass in featureClasses)
        {
            Console.WriteLine($"{featureClass.LayerName} | {featureClass.FeatureType} | {featureClass.FeatureCount} | {string.Join(",", featureClass.Columns)}");
        }

        Assert.AreEqual(2, featureClasses.Count);
        var delineations = featureClasses.Single(x => x.LayerName == "distributeddelineations");
        Assert.AreEqual(2, delineations.FeatureCount);
        CollectionAssert.AreEquivalent(
            new[] { "delineationid", "treatmentbmpname", "delineationarea", "permittype" },
            delineations.Columns);
        var points = featureClasses.Single(x => x.LayerName == "Treatment_BMPs");
        Assert.AreEqual(1, points.FeatureCount);

        // Import direction: OGR SQL aliases and a literal column, exactly like the upload controllers.
        var geoJson = GdalVectorOperations.FileGdbToGeoJson(zipPath, new GdbLayerOutput
        {
            FeatureLayerName = delineations.LayerName,
            Columns = new List<string>
            {
                "42 as UploadedByPersonID",
                "treatmentbmpname as TreatmentBMPName",
                "delineationarea as DelineationArea",
                "permittype as PermitType",
            },
            Filter = "",
            NumberOfSignificantDigits = 4,
            CoordinateSystemID = Srid,
        });

        using var document = JsonDocument.Parse(geoJson);
        var features = document.RootElement.GetProperty("features").EnumerateArray().ToList();
        Assert.AreEqual(2, features.Count);

        var first = features.Single(x => x.GetProperty("properties").GetProperty("TreatmentBMPName").GetString() == "North Basin");
        var properties = first.GetProperty("properties");
        Assert.AreEqual(42, properties.GetProperty("UploadedByPersonID").GetInt32());
        Assert.AreEqual(2.47, properties.GetProperty("DelineationArea").GetDouble(), 1e-9);
        Assert.AreEqual("Phase I MS4", properties.GetProperty("PermitType").GetString());

        // Coordinates survive the trip in the projected CRS (not silently reprojected to lon/lat).
        var firstCoordinate = first.GetProperty("geometry").GetProperty("coordinates")[0][0][0];
        var x = firstCoordinate[0].GetDouble();
        Assert.IsTrue(x is > 1_849_000 and < 1_852_000, $"Unexpected X coordinate {x}; expected EPSG:{Srid} metres.");
    }

    [TestMethod]
    public void Export_WhereFilterDropsTheSchemaRowButKeepsItsFields()
    {
        using var workingDirectory = DisposableTempDirectory.MakeDisposableTempDirectoryEndingIn("-gdal");
        var root = Directory.CreateDirectory(workingDirectory.DirectoryInfo.FullName).FullName;
        var gdbPath = Path.Combine(root, "filtered.gdb");
        var delineationsPath = WriteFile(root, "delineations.json", DelineationsGeoJson);

        // Filter out every feature: the layer and its typed fields must still be created.
        GdalVectorOperations.GeoJsonToFileGdb(delineationsPath, Srid, gdbPath, "delineations", false, "POLYGON", "DelineationID <> 1 AND DelineationID <> 2");

        var featureClass = GdalVectorOperations.ListFeatureClasses(gdbPath).Single();
        Assert.AreEqual(0, featureClass.FeatureCount);
        CollectionAssert.Contains(featureClass.Columns, "delineationarea");
    }

    [TestMethod]
    public void Import_MissingColumnSurfacesTheMessageTheUploadControllersMatchOn()
    {
        using var workingDirectory = DisposableTempDirectory.MakeDisposableTempDirectoryEndingIn("-gdal");
        var root = Directory.CreateDirectory(workingDirectory.DirectoryInfo.FullName).FullName;
        var gdbPath = Path.Combine(root, "missing.gdb");
        GdalVectorOperations.GeoJsonToFileGdb(WriteFile(root, "delineations.json", DelineationsGeoJson), Srid, gdbPath, "delineations", false, "POLYGON");

        var exception = Assert.ThrowsExactly<GdalOperationException>(() => GdalVectorOperations.FileGdbToGeoJson(gdbPath, new GdbLayerOutput
        {
            FeatureLayerName = "delineations",
            Columns = new List<string> { "trashgenerationrate as TrashGenerationRate" },
            Filter = "",
            CoordinateSystemID = Srid,
        }));

        Console.WriteLine(exception.Message);
        // LandUseBlockController / OVTA / Delineation uploads turn this phrase into a friendly error.
        StringAssert.Contains(exception.Message, "Unrecognized field name", StringComparison.InvariantCultureIgnoreCase);
    }

    [TestMethod]
    public void ListFeatureClasses_OnSomethingThatIsNotAGeodatabase_Throws()
    {
        using var notAGdb = DisposableTempFile.MakeDisposableTempFileEndingIn(".gdb.zip");
        File.WriteAllText(notAGdb.FileInfo.FullName, "this is not a zip file");

        Assert.ThrowsExactly<GdalOperationException>(() => GdalVectorOperations.ListFeatureClasses(notAGdb.FileInfo.FullName));
    }

    /// <summary>
    /// Native GDAL parses user-uploaded files; it lives in the GDAL API container so a native crash
    /// cannot take Neptune.API down. Only that host (and this test project) may reference a native
    /// runtime package — anything else, including a shared library, would pull it into the API.
    /// </summary>
    [TestMethod]
    public void OnlyTheGdalApiAndTestsReferenceTheNativeGdalRuntime()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Neptune.sln")))
        {
            directory = directory.Parent;
        }

        Assert.IsNotNull(directory, "Could not find Neptune.sln above the test output directory.");

        var allowed = new[] { "Neptune.GDALAPI.csproj", "Neptune.Tests.csproj" };
        var offenders = directory.GetDirectories("Neptune.*")
            .SelectMany(x => x.GetFiles("*.csproj", SearchOption.TopDirectoryOnly))
            .Where(x => !allowed.Contains(x.Name))
            .Where(x => System.Text.RegularExpressions.Regex.IsMatch(File.ReadAllText(x.FullName), @"MaxRev\.Gdal\.\w*Runtime", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            .Select(x => x.Name)
            .ToList();

        Assert.AreEqual(0, offenders.Count, "Native GDAL runtime referenced outside the GDAL API: " + string.Join(", ", offenders));
    }

    private static string WriteFile(string directory, string fileName, string contents)
    {
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, contents);
        return path;
    }
}
