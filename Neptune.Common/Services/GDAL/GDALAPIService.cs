using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http;

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

        public GDALAPIService(ILogger<GDALAPIService> logger, HttpClient httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task Ogr2OgrInputToGdb(GdbInputToGdbRequestDto gdbInputToGdbRequestDto)
        {
            var requestContent = gdbInputToGdbRequestDto.ToMultipartFormDataContent();
            _logger.LogInformation("Sending request to GDAL API");
            var response = await _httpClient.PostAsync("/ogr2ogr/upsert-gdb", requestContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed");
            }
        }

        public async Task<byte[]> Ogr2OgrInputToGdbAsZip(GdbInputsToGdbRequestDto gdbInputsToGdbRequestDto)
        {
            var requestContent = gdbInputsToGdbRequestDto.ToMultipartFormDataContent();
            _logger.LogInformation("Sending request to GDAL API");
            var response = await _httpClient.PostAsync("/ogr2ogr/upsert-gdb-as-zip", requestContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed");
            }
            var gdbZip = await response.Content.ReadAsByteArrayAsync();

            return gdbZip;
        }

        public async Task<byte[]> Ogr2OgrGdbToGeoJson(GdbToGeoJsonRequestDto geoJsonRequestToGdbDto)
        {
            _logger.LogInformation("Sending request to GDAL API");
            var response = await _httpClient.PostAsJsonAsync("/ogr2ogr/gdb-geojson", geoJsonRequestToGdbDto);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsByteArrayAsync();
                return result;
            }

            var content = await response.Content.ReadAsStringAsync();
            throw new Exception($"Ogr2OgrGdbToGeoJson request failed: {content}");
        }

        public async Task<List<FeatureClassInfo>> OgrInfoGdbToFeatureClassInfo(IFormFile formFile)
        {
            using var ms = new MemoryStream();
            await formFile.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var byteContent = new StreamContent(ms);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType);

            var form = new MultipartFormDataContent();
            form.Add(byteContent, "file", formFile.FileName);

            _logger.LogInformation("Sending request to GDAL API");

            var response = await _httpClient.PostAsync("/ogrinfo/gdb-feature-classes", form);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<List<FeatureClassInfo>>();
                return result;
            }
            else
            {
                throw new Exception("Failed to POST");
            }
        }

        public async Task<Envelope> OgrInfoGdbExtent(IFormFile formFile, string featureClassName, int? boundingBoxBufferInFeet)
        {
            using var ms = new MemoryStream();
            await formFile.CopyToAsync(ms);
            ms.Seek(0, SeekOrigin.Begin);
            var byteContent = new StreamContent(ms);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue(formFile.ContentType);

            var form = new MultipartFormDataContent();
            form.Add(byteContent, "file", formFile.FileName);


            _logger.LogInformation("Sending request to GDAL API");

            var response = await _httpClient.PostAsync("/ogrinfo/gdb-feature-classes", form);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<Envelope>();
                return result;
            }
            else
            {
                throw new Exception("Failed to POST MyDto");
            }
        }
    }
}
