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
[Route("field-visits")]
public class FieldVisitController(NeptuneDbContext dbContext, ILogger<FieldVisitController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<FieldVisitController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [UserViewFeature]
    public async Task<ActionResult<List<FieldVisitDto>>> List()
    {
        var stormwaterJurisdictionIDsPersonCanView = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var dtos = FieldVisits.ListAsDtoForJurisdictions(DbContext, stormwaterJurisdictionIDsPersonCanView);
        return Ok(dtos);
    }

    [HttpGet("{fieldVisitID}")]
    [UserViewFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public ActionResult<FieldVisitWorkflowDto> GetByID([FromRoute] int fieldVisitID)
    {
        return Ok(FieldVisits.GetByIDAsWorkflowDto(DbContext, fieldVisitID));
    }

    [HttpGet("/treatment-bmps/{treatmentBMPID}/field-visits/in-progress")]
    [UserViewFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public ActionResult<FieldVisitDto?> GetInProgressForTreatmentBMP([FromRoute] int treatmentBMPID)
    {
        var inProgress = FieldVisits.GetInProgressForTreatmentBMPIfAnyAsDto(DbContext, treatmentBMPID);
        return Ok(inProgress);
    }

    [HttpPost("/treatment-bmps/{treatmentBMPID}/field-visits")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(TreatmentBMP), "treatmentBMPID")]
    public async Task<ActionResult<FieldVisitDto>> Create([FromRoute] int treatmentBMPID, [FromBody] FieldVisitCreateDto createDto)
    {
        var dto = await FieldVisits.CreateAsync(DbContext, treatmentBMPID, createDto, CallingUser.PersonID);
        return Ok(dto);
    }

    [HttpPut("{fieldVisitID}/date-and-type")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<FieldVisitDto>> UpdateDateAndType([FromRoute] int fieldVisitID, [FromBody] FieldVisitUpsertDto upsertDto)
    {
        var dto = await FieldVisits.UpdateDateAndTypeAsync(DbContext, fieldVisitID, upsertDto);
        return Ok(dto);
    }

    [HttpPut("{fieldVisitID}/inventory-updated")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<FieldVisitDto>> UpdateInventoryUpdated([FromRoute] int fieldVisitID, [FromBody] FieldVisitInventoryUpdatedDto dto)
    {
        var result = await FieldVisits.UpdateInventoryUpdatedAsync(DbContext, fieldVisitID, dto.InventoryUpdated);
        return Ok(result);
    }

    // NPT-984: Verify / Mark Provisional / Return to Edit are Manager-only attestation actions
    // — they signal the field visit's regulatory record is finalized. JurisdictionEditor can
    // perform field visits and edit them while in progress but cannot attest to the result.
    [HttpPost("{fieldVisitID}/verify")]
    [JurisdictionManageFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<FieldVisitDto>> Verify([FromRoute] int fieldVisitID)
    {
        var dto = await FieldVisits.VerifyAsync(DbContext, fieldVisitID);
        return Ok(dto);
    }

    [HttpPost("{fieldVisitID}/mark-provisional")]
    [JurisdictionManageFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<FieldVisitDto>> MarkProvisional([FromRoute] int fieldVisitID)
    {
        var dto = await FieldVisits.MarkProvisionalAsync(DbContext, fieldVisitID);
        return Ok(dto);
    }

    [HttpPost("{fieldVisitID}/return-to-edit")]
    [JurisdictionManageFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<FieldVisitDto>> ReturnToEdit([FromRoute] int fieldVisitID)
    {
        var dto = await FieldVisits.ReturnToEditAsync(DbContext, fieldVisitID);
        return Ok(dto);
    }

    // Save & Wrap Up — Editor performs the visit and wraps it up themselves; only the
    // post-wrap-up attestation actions (Verify / Mark Provisional / Return to Edit) need the
    // Manager gate.
    [HttpPost("{fieldVisitID}/finalize")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<FieldVisitDto>> Finalize([FromRoute] int fieldVisitID)
    {
        var dto = await FieldVisits.FinalizeAsync(DbContext, fieldVisitID);
        return Ok(dto);
    }

    // NPT-984: Delete Field Visit is Manager-only — destructive, removes the field record.
    [HttpDelete("{fieldVisitID}")]
    [JurisdictionManageFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<IActionResult> Delete([FromRoute] int fieldVisitID)
    {
        await FieldVisits.DeleteAsync(DbContext, fieldVisitID);
        return NoContent();
    }
}
