using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("observation-types")]
public class TreatmentBMPAssessmentObservationTypeController(
    NeptuneDbContext dbContext,
    ILogger<TreatmentBMPAssessmentObservationTypeController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<TreatmentBMPAssessmentObservationTypeController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<List<TreatmentBMPAssessmentObservationTypeGridDto>>> List()
    {
        var dtos = await TreatmentBMPAssessmentObservationTypes.ListAsGridDtoAsync(DbContext);
        return Ok(dtos);
    }

    [HttpGet("{observationTypeID}")]
    [AllowAnonymous]
    public async Task<ActionResult<TreatmentBMPAssessmentObservationTypeDetailDto>> Get([FromRoute] int observationTypeID)
    {
        var dto = await TreatmentBMPAssessmentObservationTypes.GetByIDAsDtoAsync(DbContext, observationTypeID);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    [HttpPost]
    [AdminFeature]
    public async Task<ActionResult<TreatmentBMPAssessmentObservationTypeDetailDto>> Create([FromBody] TreatmentBMPAssessmentObservationTypeUpsertDto dto)
    {
        var created = await TreatmentBMPAssessmentObservationTypes.CreateAsync(DbContext, dto);
        return Ok(created);
    }

    [HttpPut("{observationTypeID}")]
    [AdminFeature]
    public async Task<ActionResult<TreatmentBMPAssessmentObservationTypeDetailDto>> Update([FromRoute] int observationTypeID, [FromBody] TreatmentBMPAssessmentObservationTypeUpsertDto dto)
    {
        var updated = await TreatmentBMPAssessmentObservationTypes.UpdateAsync(DbContext, observationTypeID, dto);
        return Ok(updated);
    }

    [HttpDelete("{observationTypeID}")]
    [AdminFeature]
    public async Task<IActionResult> Delete([FromRoute] int observationTypeID)
    {
        await TreatmentBMPAssessmentObservationTypes.DeleteAsync(DbContext, observationTypeID);
        return NoContent();
    }
}
