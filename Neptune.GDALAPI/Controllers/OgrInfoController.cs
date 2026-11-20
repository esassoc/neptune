using Neptune.Common;
using Neptune.GDALAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Neptune.Common.GeoSpatial.Gdal;
using Neptune.Common.Services.GDAL;

namespace Neptune.GDALAPI.Controllers;

[ApiController]
public class OgrInfoController : ControllerBase
{
    private readonly ILogger<OgrInfoController> _logger;
    private readonly IAzureStorage _azureStorage;

    public OgrInfoController(ILogger<OgrInfoController> logger, IAzureStorage azureStorage)
    {
        _logger = logger;
        _azureStorage = azureStorage;
    }

    // Takes a pointer to a File Geodatabase already staged in blob storage rather than the file itself.
    // The caller has to upload it to blob storage anyway for the subsequent gdb-geojson conversion, so
    // pushing the bytes through here as well meant reading and buffering the same upload twice.
    [HttpPost("ogrinfo/gdb-feature-classes")]
    public async Task<ActionResult<List<FeatureClassInfo>>> GdbToFeatureClassInfo([FromBody] GdbFeatureClassInfoRequestDto requestDto)
    {
        using var disposableTempGdbZipFile = DisposableTempFile.MakeDisposableTempFileEndingIn(".gdb.zip");

        _logger.LogInformation("Retrieving GDB File from blob storage: {CanonicalName}", requestDto.CanonicalName);
        await _azureStorage.DownloadToAsync(requestDto.BlobContainer, requestDto.CanonicalName,
            disposableTempGdbZipFile.FileInfo.FullName);

        try
        {
            // Read from the layer definitions in-process; this used to parse ogrinfo's text output.
            return Ok(GdalVectorOperations.ListFeatureClasses(disposableTempGdbZipFile.FileInfo.FullName));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error occurred while listing feature classes for {CanonicalName}", requestDto.CanonicalName);
            throw;
        }
    }
}
