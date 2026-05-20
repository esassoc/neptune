using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers;

[ApiController]
[Route("organizations")]
public class OrganizationController(NeptuneDbContext dbContext, ILogger<OrganizationController> logger, IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<OrganizationController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet]
    [AdminFeature]
    public async Task<ActionResult<List<OrganizationDto>>> List()
    {
        var organizations = await Organizations.ListAsDtoAsync(DbContext);
        return Ok(organizations);
    }

    [HttpGet("{organizationID}")]
    // NPT-999: loosened from AdminFeature to UserViewFeature so the SPA org-detail page
    // is reachable by any authenticated user, matching the legacy MVC's [OrganizationViewFeature].
    // The List endpoint above stays AdminFeature — broad enumeration is still admin-only.
    [UserViewFeature]
    [EntityNotFound(typeof(Organization), "organizationID")]
    public async Task<ActionResult<OrganizationDto>> Get([FromRoute] int organizationID)
    {
        var organization = await Organizations.GetByIDAsDtoAsync(DbContext, organizationID);
        if (organization == null) return NotFound();
        return Ok(organization);
    }

    [HttpGet("{organizationID}/funding-sources")]
    [UserViewFeature]
    [EntityNotFound(typeof(Organization), "organizationID")]
    public async Task<ActionResult<List<FundingSourceSimpleDto>>> ListFundingSources([FromRoute] int organizationID)
    {
        var fundingSources = await Organizations.ListFundingSourcesByOrganizationIDAsync(DbContext, organizationID);
        return Ok(fundingSources);
    }

    [HttpGet("{organizationID}/users")]
    [UserViewFeature]
    [EntityNotFound(typeof(Organization), "organizationID")]
    public async Task<ActionResult<List<PersonSimpleDto>>> ListUsers([FromRoute] int organizationID)
    {
        var users = await Organizations.ListActivePeopleByOrganizationIDAsync(DbContext, organizationID);
        return Ok(users);
    }

    [HttpPost]
    [AdminFeature]
    public async Task<ActionResult<OrganizationDto>> Create([FromBody] OrganizationUpsertDto dto)
    {
        var created = await Organizations.CreateAsync(DbContext, dto);
        return CreatedAtAction(nameof(Get), new { organizationID = created.OrganizationID }, created);
    }

    [HttpPut("{organizationID}")]
    [AdminFeature]
    [EntityNotFound(typeof(Organization), "organizationID")]
    public async Task<ActionResult<OrganizationDto>> Update([FromRoute] int organizationID, [FromBody] OrganizationUpsertDto dto)
    {
        var updated = await Organizations.UpdateAsync(DbContext, organizationID, dto);
        if (updated == null) return NotFound();
        return Ok(updated);
    }

    [HttpDelete("{organizationID}")]
    [AdminFeature]
    [EntityNotFound(typeof(Organization), "organizationID")]
    public async Task<IActionResult> Delete([FromRoute] int organizationID)
    {
        var deleted = await Organizations.DeleteAsync(DbContext, organizationID);
        if (!deleted) return NotFound();
        return NoContent();
    }
}