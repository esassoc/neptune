using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Neptune.Common.Services.GDAL
{
    public class GDALAPIService
    {
        /// <summary>
        /// A HttpClient is registered in the Startup.cs file for this service.
        /// That is where the BaseUrl is set from the projects Configuration.
        /// </summary>
        private readonly HttpClient _httpClient;
        private readonly ILogger<GDALAPIService> _logger;
        private readonly AzureBlobStorageService _azureBlobStorageService;

        public GDALAPIService(ILogger<GDALAPIService> logger, HttpClient httpClient, AzureBlobStorageService azureBlobStorageService)
        {
            _logger = logger;
            _httpClient = httpClient;
            _azureBlobStorageService = azureBlobStorageService;
        }

        public async Task Ogr2OgrInputToGdb(GdbInputToGdbRequestDto gdbInputToGdbRequestDto)
        {
            var stagedBlobNames = await StageFileContentsToBlobStorage(new[] { gdbInputToGdbRequestDto.GdbInput });
            try
            {
                _logger.LogInformation("Sending request to GDAL API");
                var response = await _httpClient.PostAsJsonAsync("/ogr2ogr/upsert-gdb", gdbInputToGdbRequestDto);
                await EnsureSuccessAsync(response, "ogr2ogr/upsert-gdb");
            }
            finally
            {
                await DeleteStagedBlobs(stagedBlobNames);
            }
        }

        public async Task<byte[]> Ogr2OgrInputToGdbAsZip(GdbInputsToGdbRequestDto gdbInputsToGdbRequestDto)
        {
            var stagedBlobNames = await StageFileContentsToBlobStorage(gdbInputsToGdbRequestDto.GdbInputs);
            try
            {
                _logger.LogInformation("Sending request to GDAL API");
                var response = await _httpClient.PostAsJsonAsync("/ogr2ogr/upsert-gdb-as-zip", gdbInputsToGdbRequestDto);
                await EnsureSuccessAsync(response, "ogr2ogr/upsert-gdb-as-zip");
                return await response.Content.ReadAsByteArrayAsync();
            }
            finally
            {
                await DeleteStagedBlobs(stagedBlobNames);
            }
        }

        public async Task<byte[]> Ogr2OgrGdbToGeoJson(GdbToGeoJsonRequestDto geoJsonRequestToGdbDto)
        {
            _logger.LogInformation("Sending request to GDAL API");
            var response = await _httpClient.PostAsJsonAsync("/ogr2ogr/gdb-geojson", geoJsonRequestToGdbDto);
            await EnsureSuccessAsync(response, "ogr2ogr/gdb-geojson");
            return await response.Content.ReadAsByteArrayAsync();
        }

        public async Task<List<FeatureClassInfo>> OgrInfoGdbToFeatureClassInfo(string blobContainer, string canonicalName)
        {
            _logger.LogInformation("Sending request to GDAL API");
            var response = await _httpClient.PostAsJsonAsync("/ogrinfo/gdb-feature-classes",
                new GdbFeatureClassInfoRequestDto { BlobContainer = blobContainer, CanonicalName = canonicalName });
            await EnsureSuccessAsync(response, "ogrinfo/gdb-feature-classes");
            return await response.Content.ReadFromJsonAsync<List<FeatureClassInfo>>();
        }

        /// <summary>
        /// Callers build their GeoJSON in memory and hand it over as <see cref="GdbInput.FileContents"/>.
        /// Rather than shipping those bytes to the GDAL API as multipart form data — which required the
        /// form field names on both sides to stay in lockstep by convention alone — stage them in blob
        /// storage and let the request carry only a pointer, matching how the GDB-to-GeoJSON direction
        /// already works. Returns the temporary blob names so they can be cleaned up afterwards.
        /// </summary>
        private async Task<List<string>> StageFileContentsToBlobStorage(IEnumerable<GdbInput> gdbInputs)
        {
            var stagedBlobNames = new List<string>();
            foreach (var gdbInput in gdbInputs.Where(x => x.FileContents != null))
            {
                var blobName = Guid.NewGuid().ToString();
                await _azureBlobStorageService.UploadToBlobStorage(gdbInput.FileContents!, blobName, ".json");
                gdbInput.BlobContainer = AzureBlobStorageService.BlobContainerName;
                gdbInput.CanonicalName = blobName;
                // Drop the in-memory copy now that it is in blob storage; it is [JsonIgnore]d anyway,
                // and these payloads can be large.
                gdbInput.FileContents = null;
                stagedBlobNames.Add(blobName);
            }

            return stagedBlobNames;
        }

        private async Task DeleteStagedBlobs(List<string> stagedBlobNames)
        {
            foreach (var blobName in stagedBlobNames)
            {
                try
                {
                    await _azureBlobStorageService.DeleteFromBlobStorage(blobName);
                }
                catch (Exception ex)
                {
                    // A leftover temp blob is not worth failing (or masking) the caller's operation over.
                    _logger.LogWarning(ex, "Failed to clean up staged GDAL input blob {BlobName}", blobName);
                }
            }
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string operation)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"GDAL API request to '{operation}' failed with status {(int)response.StatusCode} ({response.ReasonPhrase}): {body}");
        }
    }
}
