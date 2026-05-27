using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptune.API.Controllers;

[ApiController]
[Route("onland-visual-trash-assessment-areas")]
public class OnlandVisualTrashAssessmentAreaController(
    NeptuneDbContext dbContext,
    ILogger<OnlandVisualTrashAssessmentAreaController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration,
    AzureBlobStorageService azureBlobStorageService,
    GDALAPIService gdalApiService)
    : SitkaController<OnlandVisualTrashAssessmentAreaController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [JurisdictionEditFeature]
    public async Task<ActionResult<List<OnlandVisualTrashAssessmentAreaGridDto>>> List()
    {
        var stormwaterJurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var onlandVisualTrashAssessmentAreaGridDtos = OnlandVisualTrashAssessmentAreas
            .ListByStormwaterJurisdictionIDList(DbContext, stormwaterJurisdictionIDs).Select(x => x.AsGridDto()).ToList();
        return Ok(onlandVisualTrashAssessmentAreaGridDtos);
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<OnlandVisualTrashAssessmentAreaDetailDto> Get([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var onlandVisualTrashAssessmentAreaDetailDto = OnlandVisualTrashAssessmentAreas.GetByID(DbContext, onlandVisualTrashAssessmentAreaID).AsDetailDto();
        // NPT-1066: set here (not in AsDetailDto) so the extension stays dbContext-free; the edit
        // page uses this to disable the Land Use Block toggle option when none exist.
        onlandVisualTrashAssessmentAreaDetailDto.JurisdictionHasLandUseBlocks =
            LandUseBlocks.JurisdictionHasLandUseBlocks(DbContext, onlandVisualTrashAssessmentAreaDetailDto.StormwaterJurisdictionID!.Value);
        return Ok(onlandVisualTrashAssessmentAreaDetailDto);
    }

    [HttpPut("{onlandVisualTrashAssessmentAreaID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public async Task<ActionResult> Update([FromRoute] int onlandVisualTrashAssessmentAreaID, [FromBody] OnlandVisualTrashAssessmentAreaSimpleDto onlandVisualTrashAssessmentAreaDto)
    {
        await OnlandVisualTrashAssessmentAreas.Update(DbContext, onlandVisualTrashAssessmentAreaID, onlandVisualTrashAssessmentAreaDto);
        return Ok();
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}/onland-visual-trash-assessments")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<List<OnlandVisualTrashAssessmentGridDto>> ListAssessmentsByOVTAID([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var visualTrashAssessmentGridDtos = OnlandVisualTrashAssessments.ListByOnlandVisualTrashAssessmentAreaID(DbContext, onlandVisualTrashAssessmentAreaID).Select(x => x.AsGridDto());
        return Ok(visualTrashAssessmentGridDtos);
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}/parcel-geometries")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<List<ParcelGeometrySimpleDto>> GetParcelGeometries([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var onlandVisualTrashAssessmentArea = OnlandVisualTrashAssessmentAreas.GetByID(DbContext, onlandVisualTrashAssessmentAreaID);
        var geometries = ParcelGeometries.GetIntersected(DbContext,
            onlandVisualTrashAssessmentArea.TransectLine).Select(x => x.AsSimpleDto()).ToList();
        return Ok(geometries);
    }

    [HttpPost("{onlandVisualTrashAssessmentAreaID}/parcel-geometries")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public async Task<ActionResult> UpdateOnlandVisualTrashAssessmentWithParcels([FromRoute] int onlandVisualTrashAssessmentAreaID, [FromBody] OnlandVisualTrashAssessmentAreaGeometryDto onlandVisualTrashAssessmentAreaGeometryDto)
    {
        OnlandVisualTrashAssessmentAreas.UpdateGeometry(DbContext, onlandVisualTrashAssessmentAreaGeometryDto);
        await DbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}/area-as-feature-collection")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<FeatureCollection> GetAreaAsFeatureCollection([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var featureCollection = OnlandVisualTrashAssessmentAreas.GetAssessmentAreaByIDAsFeatureCollection(DbContext, onlandVisualTrashAssessmentAreaID);
        return Ok(featureCollection);
    }

    [HttpGet("{onlandVisualTrashAssessmentAreaID}/transect-line-as-feature-collection")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public ActionResult<FeatureCollection> GetTransectLineAsFeatureCollection([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var featureCollection = OnlandVisualTrashAssessmentAreas.GetTransectLineByIDAsFeatureCollection(DbContext, onlandVisualTrashAssessmentAreaID);
        return Ok(featureCollection);
    }

    [HttpGet("jurisdictions/{jurisdictionID}")]
    [AllowAnonymous]
    public ActionResult<List<OnlandVisualTrashAssessmentAreaSimpleDto>> ListByJurisdictionID([FromRoute] int jurisdictionID)
    {
        var onlandVisualTrashAssessmentAreaSimpleDtos =
            DbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking().Where(
                    x => x.StormwaterJurisdictionID == jurisdictionID)
                .OrderBy(x => x.OnlandVisualTrashAssessmentAreaName)
                .Select(x => x.AsSimpleDto()).ToList();

        return Ok(onlandVisualTrashAssessmentAreaSimpleDtos);
    }

    [HttpPost("{onlandVisualTrashAssessmentAreaID}/move-assessments")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public async Task<ActionResult> MoveAssessments([FromRoute] int onlandVisualTrashAssessmentAreaID, [FromBody] OnlandVisualTrashAssessmentAreaMoveAssessmentsDto dto)
    {
        if (dto == null || dto.TargetOnlandVisualTrashAssessmentAreaID <= 0)
        {
            return BadRequest("A target OVTA Area must be specified.");
        }

        if (onlandVisualTrashAssessmentAreaID == dto.TargetOnlandVisualTrashAssessmentAreaID)
        {
            return BadRequest("Cannot move an OVTA Area's assessments to itself.");
        }

        var sourceArea = OnlandVisualTrashAssessmentAreas.GetByID(DbContext, onlandVisualTrashAssessmentAreaID);
        var targetArea = DbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
            .SingleOrDefault(x => x.OnlandVisualTrashAssessmentAreaID == dto.TargetOnlandVisualTrashAssessmentAreaID);
        if (targetArea == null)
        {
            return BadRequest("Target OVTA Area not found.");
        }

        if (sourceArea.StormwaterJurisdictionID != targetArea.StormwaterJurisdictionID)
        {
            return BadRequest("Source and target OVTA Areas must belong to the same Jurisdiction.");
        }

        if (!await CallingUser.CanEditJurisdiction(sourceArea.StormwaterJurisdictionID, DbContext))
        {
            return Forbid();
        }

        var hasInProgressAssessment = DbContext.OnlandVisualTrashAssessments.Any(x =>
            x.OnlandVisualTrashAssessmentAreaID == onlandVisualTrashAssessmentAreaID &&
            x.OnlandVisualTrashAssessmentStatusID != (int)OnlandVisualTrashAssessmentStatusEnum.Complete);
        if (hasInProgressAssessment)
        {
            return BadRequest("Cannot move assessments: the source OVTA Area has assessments still in progress. Finish or delete those assessments first.");
        }

        await OnlandVisualTrashAssessmentAreas.MoveAssessmentsAsync(DbContext, onlandVisualTrashAssessmentAreaID, dto.TargetOnlandVisualTrashAssessmentAreaID);

        return Ok();
    }

    [HttpDelete("{onlandVisualTrashAssessmentAreaID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessmentArea), "onlandVisualTrashAssessmentAreaID")]
    public async Task<ActionResult> Delete([FromRoute] int onlandVisualTrashAssessmentAreaID)
    {
        var area = OnlandVisualTrashAssessmentAreas.GetByID(DbContext, onlandVisualTrashAssessmentAreaID);
        if (!await CallingUser.CanEditJurisdiction(area.StormwaterJurisdictionID, DbContext))
        {
            return Forbid();
        }

        await OnlandVisualTrashAssessmentAreas.DeleteAreaAsync(DbContext, onlandVisualTrashAssessmentAreaID);
        return Ok();
    }

    [HttpPost("gdb-upload")]
    [JurisdictionEditFeature]
    [RequestFormLimits(MultipartBodyLengthLimit = 524288000)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<OvtaAreaGdbStagingReportDto>> GdbUpload([FromForm] OvtaAreaGdbUploadFormDto form)
    {
        var report = new OvtaAreaGdbStagingReportDto { StormwaterJurisdictionID = form.StormwaterJurisdictionID };
        if (form.File == null || form.File.Length == 0)
        {
            report.Errors.Add("Please select a zipped File Geodatabase to upload.");
            return Ok(report);
        }
        if (string.IsNullOrWhiteSpace(form.AreaNameField))
        {
            report.Errors.Add("OVTA Area Name field is required.");
            return Ok(report);
        }

        var featureClasses = await gdalApiService.OgrInfoGdbToFeatureClassInfo(form.File);
        if (featureClasses.Count == 0)
        {
            report.Errors.Add("The file geodatabase contained no feature class. Please upload a file geodatabase containing exactly one feature class.");
            return Ok(report);
        }
        if (featureClasses.Count > 1)
        {
            report.Errors.Add("The file geodatabase contained more than one feature class. Please upload a file geodatabase containing exactly one feature class.");
            return Ok(report);
        }

        var featureClassName = featureClasses.Single().LayerName;
        var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);

        var blobName = Guid.NewGuid().ToString();
        await azureBlobStorageService.UploadToBlobStorage(await FileStreamHelpers.StreamToBytes(form.File), blobName, ".gdb");

        try
        {
            var columns = new List<string>
            {
                $"{form.StormwaterJurisdictionID} as StormwaterJurisdictionID",
                $"{form.AreaNameField} as AreaName",
                "Description",
                $"{currentPerson.PersonID} as UploadedByPersonID",
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
                        FeatureLayerName = featureClassName,
                        NumberOfSignificantDigits = 4,
                        Filter = "",
                        CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
                    },
                },
            };

            var geoJson = await gdalApiService.Ogr2OgrGdbToGeoJson(apiRequest);
            var stagings = await GeoJsonSerializer.DeserializeFromFeatureCollectionWithCCWCheck<OnlandVisualTrashAssessmentAreaStaging>(
                geoJson, GeoJsonSerializer.DefaultSerializerOptions, Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID);

            var validStagings = stagings.Where(x => x.Geometry is { IsValid: true, Area: > 0 }).ToList();
            if (validStagings.Count == 0)
            {
                report.Errors.Add("No valid OVTA Area features were found in the upload.");
                await OnlandVisualTrashAssessmentAreaGdbExport.DiscardStagingForUserAsync(DbContext, currentPerson);
                return Ok(report);
            }

            var duplicateNames = validStagings.GroupBy(x => x.AreaName).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (duplicateNames.Count > 0)
            {
                report.Errors.Add($"Duplicate OVTA Area Names: {string.Join(", ", duplicateNames)}");
                await OnlandVisualTrashAssessmentAreaGdbExport.DiscardStagingForUserAsync(DbContext, currentPerson);
                return Ok(report);
            }

            await DbContext.OnlandVisualTrashAssessmentAreaStagings
                .Where(x => x.UploadedByPersonID == currentPerson.PersonID).ExecuteDeleteAsync();
            DbContext.OnlandVisualTrashAssessmentAreaStagings.AddRange(validStagings);
            await DbContext.SaveChangesAsync();
        }
        catch (Exception ex) when (ex.Message.Contains("Unrecognized field name", StringComparison.InvariantCultureIgnoreCase))
        {
            report.Errors.Add("The columns in the uploaded file did not match the OVTA area schema. Ensure your AreaName / Description field names match the GDB exactly.");
            await OnlandVisualTrashAssessmentAreaGdbExport.DiscardStagingForUserAsync(DbContext, currentPerson);
            return Ok(report);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to process OVTA Area GDB upload");
            report.Errors.Add($"There was a problem processing the Feature Class \"{featureClassName}\". The file may be corrupted or invalid.");
            await OnlandVisualTrashAssessmentAreaGdbExport.DiscardStagingForUserAsync(DbContext, currentPerson);
            return Ok(report);
        }

        return Ok(OnlandVisualTrashAssessmentAreaGdbExport.BuildStagingReportForCurrentUser(DbContext, currentPerson));
    }

    [HttpGet("gdb-staging-report")]
    [JurisdictionEditFeature]
    public ActionResult<OvtaAreaGdbStagingReportDto> GdbStagingReport()
    {
        var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
        return Ok(OnlandVisualTrashAssessmentAreaGdbExport.BuildStagingReportForCurrentUser(DbContext, currentPerson));
    }

    [HttpPost("gdb-approve")]
    [JurisdictionEditFeature]
    public async Task<ActionResult<int>> GdbApprove()
    {
        var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);

        // Re-validate server-side so a client that bypasses the SPA gate can't commit a staging batch with errors.
        var report = OnlandVisualTrashAssessmentAreaGdbExport.BuildStagingReportForCurrentUser(DbContext, currentPerson);
        if (report.Errors.Count > 0)
        {
            return BadRequest(report);
        }

        var count = await OnlandVisualTrashAssessmentAreaGdbExport.ApproveStagingForUserAsync(DbContext, currentPerson);
        return Ok(count);
    }

    [HttpDelete("gdb-staging")]
    [JurisdictionEditFeature]
    public async Task<ActionResult> GdbDiscardStaging()
    {
        var currentPerson = People.GetByID(DbContext, CallingUser.PersonID);
        await OnlandVisualTrashAssessmentAreaGdbExport.DiscardStagingForUserAsync(DbContext, currentPerson);
        return NoContent();
    }

    // NPT-998: UserViewFeature (Admin/SA/JM/JE/Unassigned) mirrors legacy MVC
    // OnlandVisualTrashAssessmentExportController.ExportAssessmentGeospatialData which used
    // [NeptuneViewAndRequiresJurisdictionsFeature]. The Data Hub link itself is JM/JE-gated,
    // but a user hitting the URL directly (e.g., from a saved link) shouldn't be locked out
    // at the API. The export payload is already scoped to a single StormwaterJurisdictionID.
    [HttpPost("download-gdb")]
    [UserViewFeature]
    [Produces("application/zip")]
    public async Task<FileResult> DownloadGdb([FromBody] OvtaAreaGdbDownloadRequestDto dto)
    {
        var (bytes, fileName) = await OnlandVisualTrashAssessmentAreaGdbExport.BuildJurisdictionGdbExportAsync(DbContext, gdalApiService, dto.StormwaterJurisdictionID);
        return File(bytes, "application/zip", fileName);
    }

}