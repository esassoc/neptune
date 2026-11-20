using Neptune.Common;
using Neptune.GDALAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using Neptune.Common.GeoSpatial.Gdal;
using Neptune.Common.Services.GDAL;

namespace Neptune.GDALAPI.Controllers
{
    [ApiController]
    public class Ogr2OgrController : ControllerBase
    {
        private readonly ILogger<Ogr2OgrController> _logger;
        private readonly IAzureStorage _azureStorage;

        public Ogr2OgrController(ILogger<Ogr2OgrController> logger, IAzureStorage azureStorage)
        {
            _logger = logger;
            _azureStorage = azureStorage;
        }

        [HttpGet("/")]
        public ActionResult Get()
        {
            return Ok("Hello from the GDAL API!");
        }
        
        [HttpPost("ogr2ogr/upsert-gdb-as-zip")]
        public async Task<FileStreamResult> UpsertGdbAndReturnAsZip([FromBody] GdbInputsToGdbRequestDto requestDto)
        {
            using var disposableTempGdbFile = DisposableTempDirectory.MakeDisposableTempDirectoryEndingIn(".gdb");
            var gdbFileFolder = disposableTempGdbFile.DirectoryInfo;

            await GdbInputsToGdb(requestDto, gdbFileFolder.FullName);

            using var disposableTempGdbZipFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".gdb.zip");
            GdbFolderToZipFile(gdbFileFolder, requestDto.GdbName, disposableTempGdbZipFile);

            var stream = new StreamContent(disposableTempGdbZipFile.FileInfo.OpenRead());
            return new FileStreamResult(await stream.ReadAsStreamAsync(), "application/zip");
        }

        private async Task GdbInputsToGdb(GdbInputsToGdbRequestDto requestDto, string gdbOutputPath)
        {
            for (var i = 0; i < requestDto.GdbInputs.Count; i++)
            {
                var update = i != 0;
                var gdbInput = requestDto.GdbInputs[i];
                await GdbInputToGdb(gdbOutputPath, gdbInput, update);
            }
        }

        private async Task GdbInputToGdb(string gdbOutputPath, GdbInput gdbInput, bool update)
        {
            if (string.IsNullOrWhiteSpace(gdbInput.CanonicalName))
            {
                _logger.LogWarning($"Received an input that doesn't have a canonical name.");
                return;
            }

            using var disposableJsonTempFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".json");
            _logger.LogInformation($"Beginning processing of {gdbInput.CanonicalName}");
            await _azureStorage.DownloadToAsync(gdbInput.BlobContainer, gdbInput.CanonicalName,
                disposableJsonTempFile.FileInfo.FullName);

            GdalVectorOperations.GeoJsonToFileGdb(disposableJsonTempFile.FileInfo.FullName,
                gdbInput.CoordinateSystemID, gdbOutputPath, gdbInput.LayerName, update,
                gdbInput.GeometryTypeName, gdbInput.Where);
        }

        [HttpPost("ogr2ogr/gdb-geojson")]
        [RequestSizeLimit(100_000_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 100_000_000_000)]
        public async Task<ActionResult<string>> GdbToGeoJson([FromBody] GdbToGeoJsonRequestDto requestDto)
        {
            try
            {
                if (requestDto.GdbLayerOutputs.Count != 1)
                {
                    return BadRequest(
                        "Expecting to process only one layer for this gdb.  If you need to process more than one please use the ogr2ogr/gdb-geojsons end point!");
                }

                using var disposableTempGdbZip = DisposableTempFile.MakeDisposableTempFileEndingIn(".gdb.zip");
                await RetrieveGdbFromFileOrBlobStorage(requestDto, disposableTempGdbZip);
                var layerOutput = requestDto.GdbLayerOutputs.Single();
                var geoJson = GdalVectorOperations.FileGdbToGeoJson(disposableTempGdbZip.FileInfo.FullName, layerOutput);
                return Ok(geoJson);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing GDB to GeoJSON request");
                return StatusCode(500, ex.Message);
            }
        }

        private async Task RetrieveGdbFromFileOrBlobStorage(GdbToGeoJsonRequestDto requestDto,
            DisposableTempFile disposableTempGdbZip)
        {
            _logger.LogInformation($"Retrieving GDB File from blob storage: {requestDto.CanonicalName}");
            await _azureStorage.DownloadToAsync(requestDto.BlobContainer, requestDto.CanonicalName,
                disposableTempGdbZip.FileInfo.FullName);
        }

        private static void GdbFolderToZipFile(DirectoryInfo folderToZip, string desiredName, DisposableTempFile zipDestination)
        {
            var directoryInfo = new DirectoryInfo(folderToZip.FullName);
            var gdbDirectoryFullPath = $"{directoryInfo.Parent}/{desiredName}.gdb";
            if (directoryInfo.FullName != gdbDirectoryFullPath)
            {
                directoryInfo.MoveTo(gdbDirectoryFullPath);
            }

            ZipFile.CreateFromDirectory(gdbDirectoryFullPath, zipDestination.FileInfo.FullName, CompressionLevel.Optimal, true);
            Directory.Delete(gdbDirectoryFullPath, true);
        }
    }
}
