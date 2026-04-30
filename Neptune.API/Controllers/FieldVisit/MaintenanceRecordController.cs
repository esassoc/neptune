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
[Route("maintenance-records")]
public class MaintenanceRecordController(NeptuneDbContext dbContext, ILogger<MaintenanceRecordController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<MaintenanceRecordController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [UserViewFeature]
    public async Task<ActionResult<List<MaintenanceRecordGridDto>>> List()
    {
        var stormwaterJurisdictionIDsPersonCanView = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var dtos = MaintenanceRecords.ListAsGridDtoForJurisdictions(DbContext, stormwaterJurisdictionIDsPersonCanView);
        return Ok(dtos);
    }

    [HttpGet("{maintenanceRecordID}")]
    [UserViewFeature]
    [EntityNotFound(typeof(MaintenanceRecord), "maintenanceRecordID")]
    public async Task<ActionResult<MaintenanceRecordDetailDto?>> GetByID([FromRoute] int maintenanceRecordID)
    {
        var dto = await MaintenanceRecords.GetByIDAsDetailDtoAsync(DbContext, maintenanceRecordID);
        return Ok(dto);
    }

    [HttpGet("/field-visits/{fieldVisitID}/maintenance-record")]
    [UserViewFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<MaintenanceRecordDetailDto?>> GetByFieldVisit([FromRoute] int fieldVisitID)
    {
        var dto = await MaintenanceRecords.GetByFieldVisitIDAsDetailDtoAsync(DbContext, fieldVisitID);
        return Ok(dto);
    }

    [HttpPost("/field-visits/{fieldVisitID}/maintenance-record")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(FieldVisit), "fieldVisitID")]
    public async Task<ActionResult<MaintenanceRecordDetailDto>> CreateForFieldVisit([FromRoute] int fieldVisitID)
    {
        var dto = await MaintenanceRecords.CreateForFieldVisitAsync(DbContext, fieldVisitID, CallingUser.PersonID);
        return Ok(dto);
    }

    [HttpPut("{maintenanceRecordID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(MaintenanceRecord), "maintenanceRecordID")]
    public async Task<ActionResult<MaintenanceRecordDetailDto>> Update([FromRoute] int maintenanceRecordID, [FromBody] MaintenanceRecordUpsertDto upsertDto)
    {
        var dto = await MaintenanceRecords.UpdateAsync(DbContext, maintenanceRecordID, upsertDto, CallingUser.PersonID);
        return Ok(dto);
    }

    [HttpDelete("{maintenanceRecordID}")]
    [JurisdictionEditFeature]
    [EntityNotFound(typeof(MaintenanceRecord), "maintenanceRecordID")]
    public async Task<IActionResult> Delete([FromRoute] int maintenanceRecordID)
    {
        await MaintenanceRecords.DeleteAsync(DbContext, maintenanceRecordID);
        return NoContent();
    }
}
