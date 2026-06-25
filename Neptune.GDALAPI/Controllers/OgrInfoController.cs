using Neptune.Common;
using Neptune.GDALAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Neptune.Common.Services.GDAL;

namespace Neptune.GDALAPI.Controllers;

[ApiController]
public class OgrInfoController : ControllerBase
{
    private readonly ILogger<Ogr2OgrController> _logger;
    private readonly OgrInfoService _ogrInfoService;
    private readonly IAzureStorage _azureStorage;

    public OgrInfoController(ILogger<Ogr2OgrController> logger, OgrInfoService ogrInfoService,
        IAzureStorage azureStorage)
    {
        _logger = logger;
        _ogrInfoService = ogrInfoService;
        _azureStorage = azureStorage;
    }

    [HttpPost("ogrinfo/gdb-feature-classes")]
    [RequestSizeLimit(10_000_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 10_000_000_000)]
    public async Task<ActionResult<List<FeatureClassInfo>>> GdbToFeatureClassInfo([FromForm] IFormFile file)
    {
        using var disposableTempGdbZipFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".gdb.zip");

        await using (var fileStream = new FileStream(disposableTempGdbZipFile.FileInfo.FullName, FileMode.Create))
        {
            await file.CopyToAsync(fileStream);
        }

        var args = BuildOgrInfoCommandLineArgumentsToListFeatureClassInfos(disposableTempGdbZipFile.FileInfo.FullName,
            null);

        try
        {
            var processUtilityResult = _ogrInfoService.Run(args);
            var stdOutString = processUtilityResult.StdOut;
            var featureClassesFromFileGdb = stdOutString.Split(new[] { "\r\nLayer name: " }, StringSplitOptions.RemoveEmptyEntries).Skip(1).ToList();
            var featureClassInfos = new List<FeatureClassInfo>();
            foreach (var featureClassBlob in featureClassesFromFileGdb)
            {
                var featureClassInfo = new FeatureClassInfo();
                var features = featureClassBlob.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                // Preserve the original casing of the feature-class name. It is used verbatim in the ogr2ogr
                // "SELECT ... FROM <layer>" statement, and OGR's SQL is case-sensitive on Linux/Docker, so a
                // lowercased name (e.g. "dma" for a "DMA" layer) is not found and the conversion fails.
                featureClassInfo.LayerName = features.First();
                featureClassInfo.FeatureType = features.First(x => x.StartsWith("Geometry: ")).Substring("Geometry: ".Length);
                featureClassInfo.FeatureCount = int.Parse(features.First(x => x.StartsWith("Feature Count: ")).Substring("Feature Count: ".Length));

                var columnNamesBlob = featureClassBlob.Split(new[] { "FID Column = " }, StringSplitOptions.RemoveEmptyEntries);
                if (columnNamesBlob.Length == 2)
                {
                    featureClassInfo.Columns = columnNamesBlob.Skip(1).Single()
                        .Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Where(x => !x.StartsWith("Geometry Column")).Select(x =>
                            x.Split(new[] { ": " }, StringSplitOptions.RemoveEmptyEntries).First().ToLower()).ToList();
                }
                else
                {
                    featureClassInfo.Columns = new List<string>();
                }

                featureClassInfos.Add(featureClassInfo);
            }

            return Ok(featureClassInfos);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public static List<string> BuildOgrInfoCommandLineArgumentsToListFeatureClassInfos(string inputGdbFile, DirectoryInfo gdalDataDirectoryInfo)
    {
        var commandLineArguments = new List<string>
        {
            "-al",
            "-ro",
            "-so",
            "-noextent",
            inputGdbFile
        };

        if (gdalDataDirectoryInfo != null)
        {
            commandLineArguments.Add("--config");
            commandLineArguments.Add("GDAL_DATA");
            commandLineArguments.Add(gdalDataDirectoryInfo.FullName);
        }

        return commandLineArguments;
    }
}