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
using Neptune.API.Services.Attributes;
using Neptune.Common.Services;
using Neptune.API.Services.Authorization;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using Neptune.Common.Services;
using Neptune.Models.DataTransferObjects;
using Neptune.Common.Services;

namespace Neptune.API.Controllers;

[ApiController]
[Route("onland-visual-trash-assessments/{onlandVisualTrashAssessmentID}/observations")]
public class OnlandVisualTrashAssessmentObservationController(
    NeptuneDbContext dbContext,
    ILogger<OnlandVisualTrashAssessmentObservationController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration,
    AzureBlobStorageService azureBlobStorageService)
    : SitkaController<OnlandVisualTrashAssessmentObservationController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public ActionResult<List<OnlandVisualTrashAssessmentObservationWithPhotoDto>> ListByOnlandVisualTrashAssessmentID([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessmentObservations = OnlandVisualTrashAssessmentObservations.ListByOnlandVisualTrashAssessmentID(DbContext, onlandVisualTrashAssessmentID).Select(x => x.AsPhotoDto()).ToList();
        return Ok(onlandVisualTrashAssessmentObservations);
    }

    [HttpGet("feature-collection")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public ActionResult<List<OnlandVisualTrashAssessmentObservationLocationDto>> GetAsFeatureCollection([FromRoute] int onlandVisualTrashAssessmentID)
    {
        var onlandVisualTrashAssessmentObservationLocationDtos = OnlandVisualTrashAssessmentObservations
            .ListByOnlandVisualTrashAssessmentID(DbContext, onlandVisualTrashAssessmentID)
            .Select(x => new OnlandVisualTrashAssessmentObservationLocationDto() {
                OnlandVisualTrashAssessmentObservationID = x.OnlandVisualTrashAssessmentObservationID,
                OnlandVisualTrashAssessmentID = x.OnlandVisualTrashAssessmentID,
                Longitude = x.LocationPoint4326.Coordinate[0],
                Latitude = x.LocationPoint4326.Coordinate[1],
            });

        return Ok(onlandVisualTrashAssessmentObservationLocationDtos);
    }

    [HttpPost]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public async Task<ActionResult> UpdateObservations([FromRoute] int onlandVisualTrashAssessmentID, [FromBody] OnlandVisualTrashAssessmentObservationsUpsertDto onlandVisualTrashAssessmentObservationsUpsertDto)
    {
        await OnlandVisualTrashAssessmentObservations.Update(DbContext, onlandVisualTrashAssessmentID, onlandVisualTrashAssessmentObservationsUpsertDto.Observations);
        return Ok();
    }

    [HttpPost("observation-photo")]
    [JurisdictionEditFeature]
    [RequestSizeLimit(30 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 30 * 1024 * 1024), ProducesResponseType(StatusCodes.Status413RequestEntityTooLarge)]
    [EntityNotFound(typeof(OnlandVisualTrashAssessment), "onlandVisualTrashAssessmentID")]
    public async Task<ActionResult<OnlandVisualTrashAssessmentObservationPhotoStagingDto>> StageObservationPhoto([FromRoute]
        int onlandVisualTrashAssessmentID, [FromForm] OnlandVisualTrashAssessmentObservationPhotoDto file)
    {

        var fileResource =
            await HttpUtilities.MakeFileResourceFromFormFileAsync(DbContext,
                                                             HttpContext, azureBlobStorageService, file.file);
        var onlandVisualTrashAssessmentObservationPhotoStaging = new OnlandVisualTrashAssessmentObservationPhotoStaging
        {
            FileResource = fileResource,
            OnlandVisualTrashAssessmentID = onlandVisualTrashAssessmentID
        };
        await DbContext.OnlandVisualTrashAssessmentObservationPhotoStagings.AddAsync(onlandVisualTrashAssessmentObservationPhotoStaging);
        await DbContext.SaveChangesAsync();
        await DbContext.OnlandVisualTrashAssessmentObservationPhotoStagings
            .Entry(onlandVisualTrashAssessmentObservationPhotoStaging).ReloadAsync();

        var onlandVisualTrashAssessmentObservationPhotoStagingDto = new OnlandVisualTrashAssessmentObservationPhotoStagingDto
        {
            OnlandVisualTrashAssessmentID = onlandVisualTrashAssessmentID,
            OnlandVisualTrashAssessmentObservationPhotoStagingID = onlandVisualTrashAssessmentObservationPhotoStaging.OnlandVisualTrashAssessmentObservationPhotoStagingID,
            FileResourceID = onlandVisualTrashAssessmentObservationPhotoStaging.FileResourceID,
            FileResourceGUID = onlandVisualTrashAssessmentObservationPhotoStaging.FileResource.GetFileResourceGUIDAsString(),
        };
        return Ok(onlandVisualTrashAssessmentObservationPhotoStagingDto);
    }

    [HttpDelete("observation-photos/{fileResourceID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(FileResource), "fileResourceID")]
    public async Task<ActionResult> DeleteObservationPhoto([FromRoute]
        int onlandVisualTrashAssessmentID, [FromRoute] int fileResourceID)
    {
        await DbContext.OnlandVisualTrashAssessmentObservationPhotoStagings.Where(x =>
                x.FileResourceID == fileResourceID && x.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID)
            .ExecuteDeleteAsync();
        await DbContext.OnlandVisualTrashAssessmentObservationPhotos.Include(x => x.OnlandVisualTrashAssessmentObservation).Where(x =>
            x.FileResourceID == fileResourceID && x.OnlandVisualTrashAssessmentObservation.OnlandVisualTrashAssessmentID == onlandVisualTrashAssessmentID).ExecuteDeleteAsync();

        return Ok();
    }
}