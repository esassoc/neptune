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

        // Hand off to the existing Hangfire job that processes the staging table and emails the user the results.
        BackgroundJob.Enqueue<LandUseBlockUploadBackgroundJob>(x => x.RunJob(currentPerson.PersonID));
        result.BackgroundJobEnqueued = true;
        return Ok(result);
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
