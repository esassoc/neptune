using System.Collections.Generic;
using System.Threading.Tasks;
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
[Route("treatment-bmp-types")]
public class TreatmentBMPTypeController(
    NeptuneDbContext dbContext,
    ILogger<TreatmentBMPTypeController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<TreatmentBMPTypeController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<List<TreatmentBMPTypeWithModelingAttributesDto>> List()
    {
        var treatmentBMPTypeWithModelingAttributesDtos = TreatmentBMPs.ListWithModelingAttributesAsDto(DbContext);
        return Ok(treatmentBMPTypeWithModelingAttributesDtos);
    }

    [HttpGet("{treatmentBMPTypeID}/custom-attribute-types")]
    [AllowAnonymous]
    public ActionResult<List<TreatmentBMPTypeCustomAttributeTypeDto>> ListCustomAttributeTypes([FromRoute] int treatmentBMPTypeID)
    {
        var treatmentBMPTypeCustomAttributeTypeDtos = TreatmentBMPTypeCustomAttributeTypes.ListByTreatmentBMPTypeAsDto(DbContext, treatmentBMPTypeID);
        return treatmentBMPTypeCustomAttributeTypeDtos;
    }

    [HttpGet("grid")]
    [AdminFeature]
    public async Task<ActionResult<List<TreatmentBMPTypeGridDto>>> ListAsGridDto()
    {
        var dtos = await TreatmentBMPTypesAdmin.ListAsGridDtoAsync(DbContext);
        return Ok(dtos);
    }

    [HttpGet("cards")]
    [AllowAnonymous]
    public async Task<ActionResult<List<TreatmentBMPTypeDetailDto>>> ListAsDetailDto()
    {
        var dtos = await TreatmentBMPTypesAdmin.ListAsDetailDtoAsync(DbContext);
        return Ok(dtos);
    }

    [HttpGet("{treatmentBMPTypeID}/detail")]
    [AllowAnonymous]
    public async Task<ActionResult<TreatmentBMPTypeDetailDto>> GetDetail([FromRoute] int treatmentBMPTypeID)
    {
        var dto = await TreatmentBMPTypesAdmin.GetByIDAsDtoAsync(DbContext, treatmentBMPTypeID);
        if (dto == null) return NotFound();
        return Ok(dto);
    }

    /// <summary>
    /// NPT-1038 round 4: rows for the "Treatment BMPs of this Type" grid on the SPA
    /// Treatment BMP Type detail page. Filtered to the calling user's viewable jurisdictions
    /// (mirrors legacy MVC TreatmentBMPTypeController.TreatmentBMPsInTreatmentBMPTypeGridJsonData).
    /// </summary>
    [HttpGet("{treatmentBMPTypeID}/treatment-bmps")]
    [UserViewFeature]
    public async Task<ActionResult<List<TreatmentBMPByTypeGridDto>>> ListBMPsByType([FromRoute] int treatmentBMPTypeID)
    {
        var stormwaterJurisdictionIDsPersonCanView = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
        var dtos = await TreatmentBMPs.ListByTypeAsGridDtoForJurisdictionsAsync(DbContext, treatmentBMPTypeID, stormwaterJurisdictionIDsPersonCanView);
        return Ok(dtos);
    }

    [HttpPost]
    [AdminFeature]
    public async Task<ActionResult<TreatmentBMPTypeDetailDto>> Create([FromBody] TreatmentBMPTypeUpsertDto dto)
    {
        var created = await TreatmentBMPTypesAdmin.CreateAsync(DbContext, dto);
        return Ok(created);
    }

    [HttpPut("{treatmentBMPTypeID}")]
    [AdminFeature]
    public async Task<ActionResult<TreatmentBMPTypeDetailDto>> Update([FromRoute] int treatmentBMPTypeID, [FromBody] TreatmentBMPTypeUpsertDto dto)
    {
        var updated = await TreatmentBMPTypesAdmin.UpdateAsync(DbContext, treatmentBMPTypeID, dto);
        return Ok(updated);
    }

    [HttpDelete("{treatmentBMPTypeID}")]
    [AdminFeature]
    public async Task<IActionResult> Delete([FromRoute] int treatmentBMPTypeID)
    {
        await TreatmentBMPTypesAdmin.DeleteAsync(DbContext, treatmentBMPTypeID);
        return NoContent();
    }
}