using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.Common.Services;
using Neptune.API.Services.Authorization;
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("delineations/gdb")]
    public class DelineationGeometryController(
        NeptuneDbContext dbContext,
        ILogger<DelineationGeometryController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration,
        AzureBlobStorageService azureBlobStorageService,
        GDALAPIService gdalApiService)
        : SitkaController<DelineationGeometryController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpPost("upload")]
        [JurisdictionEditFeature]
        [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult<DelineationGdbUploadValidationDto>> Upload([FromForm] DelineationGdbUploadFormDto form)
        {
            var stormwaterJurisdictionID = form.StormwaterJurisdictionID;
            var treatmentBMPNameField = form.TreatmentBMPNameField;
            var delineationStatusField = form.DelineationStatusField;
            var file = form.File;

            var dto = new DelineationGdbUploadValidationDto { StormwaterJurisdictionID = stormwaterJurisdictionID };

            if (file == null || file.Length == 0)
            {
                dto.Errors.Add("Please select a zipped File Geodatabase to upload.");
                return Ok(dto);
            }
            if (string.IsNullOrWhiteSpace(treatmentBMPNameField))
            {
                dto.Errors.Add("Treatment BMP Name field is required.");
                return Ok(dto);
            }

            // Validate the GDB feature-class shape BEFORE uploading to blob storage so a malformed file
            // doesn't leave an orphan blob behind.
            var featureClasses = await gdalApiService.OgrInfoGdbToFeatureClassInfo(file);
            if (featureClasses.Count == 0)
            {
                dto.Errors.Add("The file geodatabase contained no feature class. Please upload a file geodatabase containing exactly one feature class.");
                return Ok(dto);
            }
            if (featureClasses.Count > 1)
            {
                dto.Errors.Add("The file geodatabase contained more than one feature class. Please upload a file geodatabase containing exactly one feature class.");
                return Ok(dto);
            }

            var featureClassName = featureClasses.Single().LayerName;
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);

            var blobName = Guid.NewGuid().ToString();
            await azureBlobStorageService.UploadToBlobStorage(await FileStreamHelpers.StreamToBytes(file), blobName, ".gdb");

            try
            {
                var columns = new List<string>
                {
                    $"{currentPerson.PersonID} as UploadedByPersonID",
                    $"{stormwaterJurisdictionID} as StormwaterJurisdictionID",
                    $"{treatmentBMPNameField} as TreatmentBMPName",
                };
                if (!string.IsNullOrWhiteSpace(delineationStatusField))
                {
                    columns.Add($"{delineationStatusField} as DelineationStatus");
                }

                var apiRequest = new GdbToGeoJsonRequestDto
                {
                    BlobContainer = AzureBlobStorageService.BlobContainerName,
                    CanonicalName = blobName,
                    GdbLayerOutputs = new List<GdbLayerOutput>
                    {
                        new()
                        {
                            Columns = columns,
                            FeatureLayerName = featureClassName,
                            NumberOfSignificantDigits = 4,
                            Filter = "",
                            CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
                        },
                    },
                };

                var geoJson = await gdalApiService.Ogr2OgrGdbToGeoJson(apiRequest);
                var processErrors = await DelineationStagings.ProcessDeserializedStagingAsync(DbContext, geoJson, currentPerson);
                dto.Errors.AddRange(processErrors);

                if (dto.Errors.Count > 0)
                {
                    await DelineationStagings.DiscardForUserAsync(DbContext, currentPerson);
                    return Ok(dto);
                }
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("AK_DelineationStaging_TreatmentBMPName_StormwaterJurisdictionID") == true)
            {
                var msg = dbEx.InnerException!.Message;
                var start = msg.IndexOf('(') + 1;
                var end = msg.IndexOf(',');
                var duplicate = end > start ? msg.Substring(start, end - start) : "(unknown)";
                dto.Errors.Add($"The Treatment BMP Name field must contain unique values. There was at least one duplicated Treatment BMP Name: {duplicate}");
                await DelineationStagings.DiscardForUserAsync(DbContext, currentPerson);
                return Ok(dto);
            }
            catch (Exception ex) when (ex.Message.Contains("Unrecognized field name", StringComparison.InvariantCultureIgnoreCase))
            {
                dto.Errors.Add("The columns in the uploaded file did not match the Delineation schema. Ensure that your field names match the GDB exactly, and if DelineationStatus is not present in the GDB ensure that field is left blank.");
                await DelineationStagings.DiscardForUserAsync(DbContext, currentPerson);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to process delineation GDB upload");
                dto.Errors.Add($"There was a problem processing the Feature Class \"{featureClassName}\". The file may be corrupted or invalid. Ensure that your field names entered above match the GDB exactly, and if DelineationStatus is not present in the GDB ensure that field is left blank.");
                await DelineationStagings.DiscardForUserAsync(DbContext, currentPerson);
                return Ok(dto);
            }

            // After staging persisted, build the report so the SPA can advance straight to Approve.
            var report = DelineationStagings.BuildReportForCurrentUser(DbContext, currentPerson);
            return Ok(report);
        }

        [HttpGet("staging-report")]
        [JurisdictionEditFeature]
        public ActionResult<DelineationGdbUploadValidationDto> StagingReport()
        {
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
            var report = DelineationStagings.BuildReportForCurrentUser(DbContext, currentPerson);
            return Ok(report);
        }

        [HttpPost("approve")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<int>> Approve()
        {
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);

            // Re-validate server-side so a client that bypasses the SPA's gate can't commit a staging batch with errors.
            var report = DelineationStagings.BuildReportForCurrentUser(DbContext, currentPerson);
            if (report.Errors.Count > 0)
            {
                return BadRequest(report);
            }

            var count = await DelineationStagings.ApproveAsync(DbContext, currentPerson);
            return Ok(count);
        }

        [HttpDelete("staging")]
        [JurisdictionEditFeature]
        public async Task<ActionResult> DiscardStaging()
        {
            var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
            await DelineationStagings.DiscardForUserAsync(DbContext, currentPerson);
            return NoContent();
        }

        [HttpPost("download")]
        [JurisdictionEditFeature]
        [Produces("application/zip")]
        public async Task<FileResult> Download([FromBody] DelineationGdbDownloadRequestDto dto)
        {
            var (bytes, fileName) = await DelineationGdbExport.BuildJurisdictionGdbExportAsync(DbContext, gdalApiService, dto.StormwaterJurisdictionID, dto.DelineationTypeID);
            return File(bytes, "application/zip", fileName);
        }
    }
}
