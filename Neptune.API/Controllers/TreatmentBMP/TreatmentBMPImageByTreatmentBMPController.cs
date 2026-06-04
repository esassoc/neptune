using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.Common.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptune.API.Controllers;

[ApiController]
[Route("treatment-bmps/{treatmentBMPID}/treatment-bmp-images")]
public class TreatmentBMPImageByTreatmentBMPController(NeptuneDbContext dbContext, ILogger<TreatmentBMPImageByTreatmentBMPController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration, AzureBlobStorageService blobStorageService, PersonDto callingPerson)
    : SitkaController<TreatmentBMPImageByTreatmentBMPController>(dbContext, logger, neptuneConfiguration)
{
    [HttpPost]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<TreatmentBMPImageDto>> Create([FromRoute] int treatmentBMPID, [FromForm] TreatmentBMPImageCreateDto imageCreateDto)
    {
        var errors = FileResources.ValidateFileUpload(imageCreateDto.File, true);
        if (!ModelState.IsValid || errors.Any())
        {
            errors.ForEach(x => ModelState.AddModelError(x.Type, x.Message));
            return BadRequest(ModelState);
        }

        var fileResource = await HttpUtilities.MakeFileResourceFromFormFileAsync(DbContext, HttpContext, blobStorageService, imageCreateDto.File);

        var treatmentBMPImageDto = await TreatmentBMPImages.CreateAsync(DbContext, treatmentBMPID, fileResource.FileResourceID, imageCreateDto, callingPerson.PersonID);
        return CreatedAtAction(nameof(Get), new { treatmentBMPID, treatmentBMPImageID = treatmentBMPImageDto.TreatmentBMPImageID }, treatmentBMPImageDto);
    }

    [HttpGet]
    [AllowAnonymous]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<IEnumerable<TreatmentBMPImageDto>>> List([FromRoute] int treatmentBMPID)
    {
        var treatmentBMPImageDtos = await TreatmentBMPImages.ListAsync(DbContext, treatmentBMPID);
        return Ok(treatmentBMPImageDtos);
    }

    /// <summary>
    /// Carousel feed for the BMP detail page — inventory images plus assessment photos from
    /// verified field visits. Edit/delete affordances should keep using <see cref="List"/> since
    /// the assessment-photo rows here carry synthetic IDs and aren't routable back through the
    /// TreatmentBMPImage delete/update endpoints.
    /// </summary>
    [HttpGet("carousel")]
    [AllowAnonymous]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<IEnumerable<TreatmentBMPImageDto>>> ListForCarousel([FromRoute] int treatmentBMPID)
    {
        var dtos = await TreatmentBMPImages.ListForCarouselAsync(DbContext, treatmentBMPID);
        return Ok(dtos);
    }

    [HttpGet("{treatmentBMPImageID}")]
    [AllowAnonymous]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    [EntityNotFound(typeof(TreatmentBMPImage), "treatmentBMPImageID")]
    public async Task<ActionResult<TreatmentBMPImageDto>> Get([FromRoute] int treatmentBMPID, [FromRoute] int treatmentBMPImageID)
    {
        var treatmentBMPImageDto = await TreatmentBMPImages.GetAsync(DbContext, treatmentBMPID, treatmentBMPImageID);
        return Ok(treatmentBMPImageDto);
    }

    [HttpPut]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<IActionResult> Update([FromRoute] int treatmentBMPID, [FromBody] List<TreatmentBMPImageUpdateDto> updateDtos)
    {
        var errors = await TreatmentBMPImages.ValidateUpdateAsync(DbContext, treatmentBMPID, updateDtos);
        errors.ForEach(e => ModelState.AddModelError(e.Type, e.Message));

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var updatedTreatmentBMPImageDtos = await TreatmentBMPImages.UpdateAsync(DbContext, treatmentBMPID, updateDtos);
        return Ok(updatedTreatmentBMPImageDtos);
    }

    [HttpDelete("{treatmentBMPImageID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    [EntityNotFound(typeof(TreatmentBMPImage), "treatmentBMPImageID")]
    public async Task<IActionResult> Delete([FromRoute] int treatmentBMPID, [FromRoute] int treatmentBMPImageID)
    {
        var treatmentBMPImageDto = await TreatmentBMPImages.GetAsync(DbContext, treatmentBMPID, treatmentBMPImageID);
        if (treatmentBMPImageDto != null)
        {
            var fileResource = FileResources.GetByID(DbContext, treatmentBMPImageDto.FileResourceID);
            await blobStorageService.DeleteFileResourceBlob(fileResource.FileResourceGUID);
        }

        await TreatmentBMPImages.DeleteAsync(DbContext, treatmentBMPID, treatmentBMPImageID);
        return NoContent();
    }
}