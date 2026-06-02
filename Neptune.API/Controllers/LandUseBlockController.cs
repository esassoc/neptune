using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.Jobs.Hangfire;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("land-use-blocks")]
public class LandUseBlockController(
    NeptuneDbContext dbContext,
    ILogger<LandUseBlockController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration,
    AzureBlobStorageService azureBlobStorageService,
    GDALAPIService gdalApiService)
    : SitkaController<LandUseBlockController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<List<LandUseBlockGridDto>> List()
    {
        var landUseBlockGridDtos = LandUseBlocks.List(dbContext);
        return landUseBlockGridDtos;
    }

    [HttpPut]
    [AdminFeature]
    public async Task<IActionResult> Update(int landUseBlockID, LandUseBlockUpsertDto landUseBlockUpsertDto)
    {
        var landUseBlock = LandUseBlocks.GetByIDWithChangeTracking(dbContext, landUseBlockID);
        await LandUseBlocks.Update(DbContext, landUseBlock, landUseBlockUpsertDto, CallingUser.PersonID);
        return Ok();
    }

    [HttpPost("upload-gdb")]
    [JurisdictionEditFeature]
    [RequestSizeLimit(524_288_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 524_288_000)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<LandUseBlockGdbUploadResultDto>> UploadGdb([FromForm] LandUseBlockGdbUploadFormDto form)
    {
        var result = new LandUseBlockGdbUploadResultDto();
        if (form.File == null || form.File.Length == 0)
        {
            result.Errors.Add("Please select a zipped File Geodatabase to upload.");
            return Ok(result);
        }

        var featureClasses = await gdalApiService.OgrInfoGdbToFeatureClassInfo(form.File);
        if (featureClasses.Count == 0)
        {
            result.Errors.Add("The file geodatabase contained no feature class. Please upload a file geodatabase containing exactly one feature class.");
            return Ok(result);
        }
        if (featureClasses.Count > 1)
        {
            result.Errors.Add("The file geodatabase contained more than one feature class. Please upload a file geodatabase containing exactly one feature class.");
            return Ok(result);
        }

        var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
        var blobName = Guid.NewGuid().ToString();
        await azureBlobStorageService.UploadToBlobStorage(await FileStreamHelpers.StreamToBytes(form.File), blobName, ".gdb");

        try
        {
            var columns = new List<string>
            {
                $"{currentPerson.PersonID} as UploadedByPersonID",
                "prioritylandusetype as PriorityLandUseType",
                "landusedescription as LandUseDescription",
                "trashgenerationrate as TrashGenerationRate",
                "landusefortgr as LandUseForTGR",
                "medianhouseholdincomeresidential as MedianHouseholdIncomeResidential",
                "medianhouseholdincomeretail as MedianHouseholdIncomeRetail",
                $"{form.StormwaterJurisdictionID} as StormwaterJurisdictionID",
                "permittype as PermitType",
            };
            var apiRequest = new GdbToGeoJsonRequestDto
            {
                BlobContainer = AzureBlobStorageService.BlobContainerName,
                CanonicalName = blobName,
                GdbLayerOutputs = new List<GdbLayerOutput>
                {
                    new()
                    {
                        Columns = columns,
                        FeatureLayerName = featureClasses.Single().LayerName,
                        NumberOfSignificantDigits = 4,
                        Filter = "",
                        CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
                    },
                },
            };

            var geoJson = await gdalApiService.Ogr2OgrGdbToGeoJson(apiRequest);
            var stagings = await GeoJsonSerializer.DeserializeFromFeatureCollectionWithCCWCheck<LandUseBlockStaging>(
                geoJson, GeoJsonSerializer.DefaultSerializerOptions, Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);
            var validStagings = stagings.Where(x => x.Geometry is { IsValid: true, Area: > 0 }).ToList();
            if (validStagings.Count == 0)
            {
                result.Errors.Add("No valid Land Use Block features were found in the upload.");
                return Ok(result);
            }

            await DbContext.Database.ExecuteSqlAsync($"dbo.pLandUseBlockStagingDeleteByPersonID @PersonID = {currentPerson.PersonID}");
            DbContext.LandUseBlockStagings.AddRange(validStagings);
            await DbContext.SaveChangesAsync();
            result.StagedRowCount = validStagings.Count;
        }
        catch (Exception ex) when (ex.Message.Contains("Unrecognized field name", StringComparison.InvariantCultureIgnoreCase))
        {
            result.Errors.Add("The columns in the uploaded file did not match the LandUseBlock schema. Ensure every required field is present (PriorityLandUseType, LandUseDescription, TrashGenerationRate, LandUseForTGR, MedianHouseholdIncomeResidential, MedianHouseholdIncomeRetail, PermitType) and does not rely on an alias.");
            return Ok(result);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process Land Use Block GDB upload");
            result.Errors.Add($"There was a problem processing the Feature Class \"{featureClasses[0].LayerName}\". The file may be corrupted or invalid.");
            return Ok(result);
        }

        // NPT-1077: the job is no longer enqueued here. The SPA redirects to the approve page,
        // which calls GET /land-use-blocks/staging-report to fetch the validation report, then
        // POST /land-use-blocks/staging/approve to enqueue the job if the report is error-free.
        return Ok(result);
    }

    [HttpGet("staging-report")]
    [JurisdictionEditFeature]
    public async Task<ActionResult<LandUseBlockGdbUploadValidationDto>> StagingReport()
    {
        var report = await LandUseBlockStagings.BuildReportForCurrentUserAsync(DbContext, CallingUser.PersonID);
        return Ok(report);
    }

    [HttpPost("staging/approve")]
    [JurisdictionEditFeature]
    public async Task<ActionResult<int>> ApproveStaging()
    {
        // Re-validate server-side so a client that bypasses the SPA's gate can't commit a staging
        // batch with errors. The background job itself also runs ValidateStagings as a safety net.
        var report = await LandUseBlockStagings.BuildReportForCurrentUserAsync(DbContext, CallingUser.PersonID);
        if (report.Errors.Count > 0)
        {
            return BadRequest(report);
        }

        BackgroundJob.Enqueue<LandUseBlockUploadBackgroundJob>(x => x.RunJob(CallingUser.PersonID));
        return Ok(report.TotalStagedRowCount);
    }

    [HttpDelete("staging")]
    [JurisdictionEditFeature]
    public async Task<ActionResult> DiscardStaging()
    {
        await LandUseBlockStagings.DiscardForUserAsync(DbContext, CallingUser.PersonID);
        return NoContent();
    }

    [HttpPost("download-gdb")]
    [JurisdictionEditFeature]
    [Produces("application/zip")]
    public async Task<FileResult> DownloadGdb([FromBody] LandUseBlockGdbDownloadRequestDto dto)
    {
        var (bytes, fileName) = await LandUseBlockGdbExport.BuildJurisdictionGdbExportAsync(DbContext, gdalApiService, dto.StormwaterJurisdictionID);
        return File(bytes, "application/zip", fileName);
    }
}
