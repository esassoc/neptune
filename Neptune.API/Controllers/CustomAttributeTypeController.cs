using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Neptune.API.Controllers;

[ApiController]
[Route("custom-attribute-types")]
public class CustomAttributeTypeController(
    NeptuneDbContext dbContext,
    ILogger<CustomAttributeTypeController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<CustomAttributeTypeController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [AllowAnonymous]
    public ActionResult<List<CustomAttributeTypeDto>> List()
    {
        var customAttributeTypeDtos = CustomAttributeTypes.ListAsDto(DbContext);
        return customAttributeTypeDtos;
    }


    [HttpGet("{customAttributeTypeID}")]
    [AllowAnonymous]
    public ActionResult<CustomAttributeTypeDto> Get([FromRoute] int customAttributeTypeID)
    {
        var customAttributeTypeDto = CustomAttributeTypes.GetByIDAsDto(DbContext, customAttributeTypeID);
        return RequireNotNullThrowNotFound(customAttributeTypeDto, "CustomAttributeType", customAttributeTypeID);
    }

    [HttpGet("/purpose/{customAttributeTypePurposeID}")]
    [AllowAnonymous]
    public ActionResult<List<CustomAttributeTypeWithTreatmentBMPTypeIDsDto>> GetByCustomAttributeTypePurposeID(
        [FromRoute] int customAttributeTypePurposeID)
    {
        var customAttributeTypeDtos =
            CustomAttributeTypes.GetByCustomAttributeTypePurposeAsWithTreatmentBMPTypeIDsDto(DbContext, customAttributeTypePurposeID);

        return customAttributeTypeDtos;
    }

    [HttpPost]
    [AdminFeature]
    public async Task<ActionResult<CustomAttributeTypeDto>> Create([FromBody] CustomAttributeTypeUpsertDto dto)
    {
        // NPT-1038 rework: reject Modeling-purpose creates server-side. The SPA filters
        // the Purpose dropdown to hide Modeling on create — this is defense in depth so
        // a future direct-API call can't slip a phantom modeling attribute past us.
        var validationError = CustomAttributeTypes.ValidateForCreate(dto);
        if (validationError != null)
        {
            return BadRequest(validationError);
        }

        var created = await CustomAttributeTypes.CreateAsync(DbContext, dto);
        return Ok(created);
    }

    [HttpPut("{customAttributeTypeID}")]
    [AdminFeature]
    public async Task<ActionResult<CustomAttributeTypeDto>> Update([FromRoute] int customAttributeTypeID, [FromBody] CustomAttributeTypeUpsertDto dto)
    {
        var updated = await CustomAttributeTypes.UpdateAsync(DbContext, customAttributeTypeID, dto);
        return Ok(updated);
    }

    [HttpDelete("{customAttributeTypeID}")]
    [AdminFeature]
    public async Task<IActionResult> Delete([FromRoute] int customAttributeTypeID)
    {
        await CustomAttributeTypes.DeleteAsync(DbContext, customAttributeTypeID);
        return NoContent();
    }
}