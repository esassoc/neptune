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

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("funding-sources")]
    public class FundingSourceController(
        NeptuneDbContext dbContext,
        ILogger<FundingSourceController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration)
        : SitkaController<FundingSourceController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpGet]
        [AdminFeature]
        public async Task<ActionResult<IEnumerable<FundingSourceDto>>> List()
        {
            var sources = await FundingSources.ListAsDtoAsync(DbContext);
            return Ok(sources);
        }

        [HttpGet("{fundingSourceID}")]
        // NPT-999: loosened from AdminFeature to UserViewFeature so the SPA FundingSource
        // detail page is reachable by any authenticated user, matching the legacy MVC's
        // FundingSourceController.Detail behavior. List stays admin-only above.
        [UserViewFeature]
        [EntityNotFoundAttribute(typeof(FundingSource), "fundingSourceID")]
        public async Task<ActionResult<FundingSourceDto>> Get([FromRoute] int fundingSourceID)
        {
            var entity = await FundingSources.GetByIDAsDtoAsync(DbContext, fundingSourceID);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        [HttpGet("{fundingSourceID}/treatment-bmps")]
        [UserViewFeature]
        [EntityNotFoundAttribute(typeof(FundingSource), "fundingSourceID")]
        public async Task<ActionResult<List<FundingSourceTreatmentBMPFundingDto>>> ListTreatmentBMPFunding([FromRoute] int fundingSourceID)
        {
            var rows = await FundingSources.ListTreatmentBMPFundingByIDAsync(DbContext, fundingSourceID);
            return Ok(rows);
        }

        [HttpPost]
        [AdminFeature]
        public async Task<ActionResult<FundingSourceDto>> Create([FromBody] FundingSourceUpsertDto dto)
        {
            var created = await FundingSources.CreateAsync(DbContext, dto);
            return CreatedAtAction(nameof(Get), new { fundingSourceID = created.FundingSourceID }, created);
        }

        [HttpPut("{fundingSourceID}")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(FundingSource), "fundingSourceID")]
        public async Task<ActionResult<FundingSourceDto>> Update([FromRoute] int fundingSourceID, [FromBody] FundingSourceUpsertDto dto)
        {
            var updated = await FundingSources.UpdateAsync(DbContext, fundingSourceID, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{fundingSourceID}")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(FundingSource), "fundingSourceID")]
        public async Task<IActionResult> Delete([FromRoute] int fundingSourceID)
        {
            var deleted = await FundingSources.DeleteAsync(DbContext, fundingSourceID);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }
}
