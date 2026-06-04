using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Common;
using Neptune.Common.Services;
using Neptune.API.Services;
using Neptune.Common.Services;
using Neptune.API.Services.Attributes;
using Neptune.Common.Services;
using Neptune.API.Services.Authorization;
using Neptune.Common.Services;
using HttpUtilities = Neptune.API.Services.HttpUtilities;
using Neptune.EFModels.Entities;
using Neptune.Common.Services;
using Neptune.Models.DataTransferObjects;
using Neptune.Common.Services;

namespace Neptune.API.Controllers;

[ApiController]
[Route("treatment-bmp-assessments/{treatmentBMPAssessmentID}/photos")]
public class TreatmentBMPAssessmentPhotoController(
    NeptuneDbContext dbContext,
    ILogger<TreatmentBMPAssessmentPhotoController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration,
    AzureBlobStorageService blobStorageService)
    : SitkaController<TreatmentBMPAssessmentPhotoController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMPAssessment), "treatmentBMPAssessmentID")]
    public async Task<ActionResult<List<TreatmentBMPAssessmentPhotoDto>>> List([FromRoute] int treatmentBMPAssessmentID)
    {
        var dtos = await TreatmentBMPAssessmentPhotos.ListByTreatmentBMPAssessmentIDAsDtoAsync(DbContext, treatmentBMPAssessmentID);
        return Ok(dtos);
    }

    [HttpPost]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMPAssessment), "treatmentBMPAssessmentID")]
    public async Task<ActionResult<TreatmentBMPAssessmentPhotoDto>> Create([FromRoute] int treatmentBMPAssessmentID, [FromForm] TreatmentBMPAssessmentPhotoCreateDto createDto)
    {
        var errors = FileResources.ValidateFileUpload(createDto.File, true);
        if (!ModelState.IsValid || errors.Any())
        {
            errors.ForEach(x => ModelState.AddModelError(x.Type, x.Message));
            return BadRequest(ModelState);
        }

        var fileResource = await HttpUtilities.MakeFileResourceFromFormFileAsync(DbContext, HttpContext, blobStorageService, createDto.File);
        var dto = await TreatmentBMPAssessmentPhotos.CreateAsync(DbContext, treatmentBMPAssessmentID, fileResource.FileResourceID, createDto);
        return Ok(dto);
    }

    [HttpPut("{treatmentBMPAssessmentPhotoID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMPAssessment), "treatmentBMPAssessmentID")]
    [EntityNotFound(typeof(TreatmentBMPAssessmentPhoto), "treatmentBMPAssessmentPhotoID")]
    public async Task<ActionResult<TreatmentBMPAssessmentPhotoDto>> UpdateCaption(
        [FromRoute] int treatmentBMPAssessmentID,
        [FromRoute] int treatmentBMPAssessmentPhotoID,
        [FromBody] TreatmentBMPAssessmentPhotoUpdateDto updateDto)
    {
        var dto = await TreatmentBMPAssessmentPhotos.UpdateCaptionAsync(DbContext, treatmentBMPAssessmentID, treatmentBMPAssessmentPhotoID, updateDto);
        return Ok(dto);
    }

    [HttpDelete("{treatmentBMPAssessmentPhotoID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMPAssessment), "treatmentBMPAssessmentID")]
    [EntityNotFound(typeof(TreatmentBMPAssessmentPhoto), "treatmentBMPAssessmentPhotoID")]
    public async Task<IActionResult> Delete([FromRoute] int treatmentBMPAssessmentID, [FromRoute] int treatmentBMPAssessmentPhotoID)
    {
        var existing = await TreatmentBMPAssessmentPhotos.GetAsDtoAsync(DbContext, treatmentBMPAssessmentID, treatmentBMPAssessmentPhotoID);
        if (existing != null)
        {
            var fileResource = FileResources.GetByID(DbContext, existing.FileResourceID);
            await blobStorageService.DeleteFileResourceBlob(fileResource.FileResourceGUID);
        }
        await TreatmentBMPAssessmentPhotos.DeleteAsync(DbContext, treatmentBMPAssessmentID, treatmentBMPAssessmentPhotoID);
        return NoContent();
    }
}
