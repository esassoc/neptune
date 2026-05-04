using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Common;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("treatment-bmp-assessments")]
public class TreatmentBMPAssessmentController(NeptuneDbContext dbContext, ILogger<TreatmentBMPAssessmentController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<TreatmentBMPAssessmentController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [UserViewFeature]
    public async Task<ActionResult<List<TreatmentBMPAssessmentGridDto>>> List()
    {
        var stormwaterJurisdictionIDsPersonCanView = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var dtos = TreatmentBMPAssessments.ListAsGridDtoForJurisdictions(DbContext, stormwaterJurisdictionIDsPersonCanView);
        return Ok(dtos);
    }

    [HttpGet("{treatmentBMPAssessmentID}")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMPAssessment), "treatmentBMPAssessmentID")]
    public async Task<ActionResult<TreatmentBMPAssessmentDetailDto?>> GetByID([FromRoute] int treatmentBMPAssessmentID)
    {
        var dto = await TreatmentBMPAssessments.GetByIDAsDetailDtoAsync(DbContext, treatmentBMPAssessmentID);
        return Ok(dto);
    }

    [HttpPut("{treatmentBMPAssessmentID}/observations")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMPAssessment), "treatmentBMPAssessmentID")]
    public async Task<ActionResult<TreatmentBMPAssessmentDetailDto>> UpsertObservations([FromRoute] int treatmentBMPAssessmentID, [FromBody] TreatmentBMPAssessmentUpsertDto upsertDto)
    {
        var dto = await TreatmentBMPAssessments.UpsertObservationsAsync(DbContext, treatmentBMPAssessmentID, upsertDto.Observations, CallingUser.PersonID);
        return Ok(dto);
    }

    [HttpPost("{treatmentBMPAssessmentID}/copy-from-initial")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMPAssessment), "treatmentBMPAssessmentID")]
    public async Task<ActionResult<TreatmentBMPAssessmentDetailDto>> CopyFromInitial([FromRoute] int treatmentBMPAssessmentID)
    {
        var dto = await TreatmentBMPAssessments.CopyObservationsFromInitialAsync(DbContext, treatmentBMPAssessmentID, CallingUser.PersonID);
        return Ok(dto);
    }

    [HttpDelete("{treatmentBMPAssessmentID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMPAssessment), "treatmentBMPAssessmentID")]
    public async Task<IActionResult> Delete([FromRoute] int treatmentBMPAssessmentID)
    {
        await TreatmentBMPAssessments.DeleteAsync(DbContext, treatmentBMPAssessmentID);
        return NoContent();
    }
}
