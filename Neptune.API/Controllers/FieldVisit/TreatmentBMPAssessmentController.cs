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
        var dtos = TreatmentBMPAssessments.ListAllAsGridDtoForJurisdictions(DbContext, stormwaterJurisdictionIDsPersonCanView);
        return Ok(dtos);
    }

    /// <summary>
    /// NPT-984 round 6: dedicated endpoint for the Latest BMP Assessments page — returns one
    /// row per BMP (the most-recent assessment from a wrapped-up visit). Split from the
    /// unfiltered <see cref="List"/> endpoint so the Field Records "Assessments" tab can
    /// continue listing every assessment ever recorded.
    /// </summary>
    [HttpGet("latest-by-bmp")]
    [UserViewFeature]
    public async Task<ActionResult<List<TreatmentBMPAssessmentGridDto>>> ListLatestByBMP()
    {
        var stormwaterJurisdictionIDsPersonCanView = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var dtos = TreatmentBMPAssessments.ListLatestAsGridDtoForJurisdictions(DbContext, stormwaterJurisdictionIDsPersonCanView);
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

    /// <summary>
    /// NPT-1056: per-observation scoring breakdown for the SPA assessment detail page (Threshold
    /// / Observed / Benchmark / Weight / Score columns). Carved into its own endpoint instead of
    /// folding into <see cref="GetByID"/> so the detail page can pay the benchmark-graph load
    /// cost only when the user is actually on a page that renders the breakdown.
    /// </summary>
    [HttpGet("{treatmentBMPAssessmentID}/score-detail")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMPAssessment), "treatmentBMPAssessmentID")]
    public async Task<ActionResult<TreatmentBMPAssessmentScoreDetailDto?>> GetScoreDetail([FromRoute] int treatmentBMPAssessmentID)
    {
        var dto = await TreatmentBMPAssessments.GetScoreDetailAsync(DbContext, treatmentBMPAssessmentID);
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
