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
[Route("treatment-bmps/{treatmentBMPID}/treatment-bmp-documents")]
public class TreatmentBMPDocumentByTreatmentBMPController(NeptuneDbContext dbContext, ILogger<TreatmentBMPDocumentByTreatmentBMPController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration, AzureBlobStorageService blobStorageService, PersonDto callingPerson)
    : SitkaController<TreatmentBMPDocumentByTreatmentBMPController>(dbContext, logger, neptuneConfiguration)
{
    [HttpPost]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<TreatmentBMPDocumentDto>> Create([FromRoute] int treatmentBMPID, [FromForm] TreatmentBMPDocumentCreateDto documentCreateDto)
    {
        var errors = FileResources.ValidateFileUpload(documentCreateDto.File);
        if (!ModelState.IsValid || errors.Any())
        {
            errors.ForEach(x => ModelState.AddModelError(x.Type, x.Message));
            return BadRequest(ModelState);
        }

        var fileResource = await HttpUtilities.MakeFileResourceFromFormFileAsync(DbContext, HttpContext, blobStorageService, documentCreateDto.File);

        var treatmentBMPDocumentDto = await TreatmentBMPDocuments.CreateAsync(DbContext, treatmentBMPID, fileResource.FileResourceID, documentCreateDto, callingPerson.PersonID);
        return CreatedAtAction(nameof(Get), new { treatmentBMPID, treatmentBMPDocumentID = treatmentBMPDocumentDto.TreatmentBMPDocumentID }, treatmentBMPDocumentDto);
    }

    [HttpGet]
    [AllowAnonymous]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<IEnumerable<TreatmentBMPDocumentDto>>> List([FromRoute] int treatmentBMPID)
    {
        var treatmentBMPDocumentDtos = await TreatmentBMPDocuments.ListAsync(DbContext, treatmentBMPID);
        return Ok(treatmentBMPDocumentDtos);
    }

    [HttpGet("{treatmentBMPDocumentID}")]
    [AllowAnonymous]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    [EntityNotFound(typeof(TreatmentBMPDocument), "treatmentBMPDocumentID")]
    public async Task<ActionResult<TreatmentBMPDocumentDto>> Get([FromRoute] int treatmentBMPID, [FromRoute] int treatmentBMPDocumentID)
    {
        var treatmentBMPDocumentDto = await TreatmentBMPDocuments.GetAsync(DbContext, treatmentBMPID, treatmentBMPDocumentID);
        return Ok(treatmentBMPDocumentDto);
    }

    [HttpPut("{treatmentBMPDocumentID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    [EntityNotFound(typeof(TreatmentBMPDocument), "treatmentBMPDocumentID")]
    public async Task<IActionResult> Update([FromRoute] int treatmentBMPID, [FromRoute] int treatmentBMPDocumentID, [FromBody] TreatmentBMPDocumentUpdateDto updateDto)
    {
        var updatedTreatmentBMPDocumentDtos = await TreatmentBMPDocuments.UpdateAsync(DbContext, treatmentBMPID, treatmentBMPDocumentID, updateDto);
        return Ok(updatedTreatmentBMPDocumentDtos);
    }

    [HttpDelete("{treatmentBMPDocumentID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    [EntityNotFound(typeof(TreatmentBMPDocument), "treatmentBMPDocumentID")]
    public async Task<IActionResult> Delete([FromRoute] int treatmentBMPID, [FromRoute] int treatmentBMPDocumentID)
    {
        var treatmentBMPDocumentDto = await TreatmentBMPDocuments.GetAsync(DbContext, treatmentBMPID, treatmentBMPDocumentID);
        if (treatmentBMPDocumentDto != null)
        {
            var fileResource = FileResources.GetByID(DbContext, treatmentBMPDocumentDto.FileResourceID);
            await blobStorageService.DeleteFileResourceBlob(fileResource.FileResourceGUID);
        }

        await TreatmentBMPDocuments.DeleteAsync(DbContext, treatmentBMPID, treatmentBMPDocumentID);
        return NoContent();
    }
}