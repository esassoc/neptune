using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.AI;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("water-quality-management-plan-documents")]
    public class WaterQualityManagementPlanDocumentController(
        NeptuneDbContext dbContext,
        ILogger<WaterQualityManagementPlanDocumentController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration,
        AnthropicFileService anthropicFileService)
        : SitkaController<WaterQualityManagementPlanDocumentController>(dbContext, logger,
            neptuneConfiguration)
    {
        [HttpGet]
        [AdminFeature]
        public async Task<ActionResult<IEnumerable<WaterQualityManagementPlanDocumentDto>>> List()
        {
            var docs = await WaterQualityManagementPlanDocuments.ListAsDtoAsync(DbContext);
            return Ok(docs);
        }

        [HttpGet("{waterQualityManagementPlanDocumentID}")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(WaterQualityManagementPlanDocument), "waterQualityManagementPlanDocumentID")]
        public async Task<ActionResult<WaterQualityManagementPlanDocumentDto>> Get([FromRoute] int waterQualityManagementPlanDocumentID)
        {
            var entity = await WaterQualityManagementPlanDocuments.GetByIDAsDtoAsync(DbContext, waterQualityManagementPlanDocumentID);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        [HttpPost]
        [AdminFeature]
        public async Task<ActionResult<WaterQualityManagementPlanDocumentDto>> Create([FromBody] WaterQualityManagementPlanDocumentUpsertDto dto)
        {
            var created = await WaterQualityManagementPlanDocuments.CreateAsync(DbContext, dto);
            return CreatedAtAction(nameof(Get), new { waterQualityManagementPlanDocumentID = created.WaterQualityManagementPlanDocumentID }, created);
        }

        [HttpPut("{waterQualityManagementPlanDocumentID}")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(WaterQualityManagementPlanDocument), "waterQualityManagementPlanDocumentID")]
        public async Task<ActionResult<WaterQualityManagementPlanDocumentDto>> Update([FromRoute] int waterQualityManagementPlanDocumentID, [FromBody] WaterQualityManagementPlanDocumentUpsertDto dto)
        {
            // Pointing the row at a different FileResource orphans the cached Anthropic
            // upload (UpdateFromUpsertDto clears the id) — capture it first so we can
            // reclaim it upstream. NPT-1121.
            var existing = WaterQualityManagementPlanDocuments.GetByID(DbContext, waterQualityManagementPlanDocumentID);
            var replacedAnthropicFileID = existing.FileResourceID != dto.FileResourceID
                ? existing.AnthropicFileID
                : null;

            var updated = await WaterQualityManagementPlanDocuments.UpdateAsync(DbContext, waterQualityManagementPlanDocumentID, dto);
            if (updated == null) return NotFound();

            await anthropicFileService.DeleteRemoteFileAsync(replacedAnthropicFileID, CancellationToken.None);
            return Ok(updated);
        }

        [HttpDelete("{waterQualityManagementPlanDocumentID}")]
        [AdminFeature]
        [EntityNotFoundAttribute(typeof(WaterQualityManagementPlanDocument), "waterQualityManagementPlanDocumentID")]
        public async Task<IActionResult> Delete([FromRoute] int waterQualityManagementPlanDocumentID)
        {
            // Capture before the row goes away — it is the only record of the upload (NPT-1121).
            var existing = WaterQualityManagementPlanDocuments.GetByID(DbContext, waterQualityManagementPlanDocumentID);
            var anthropicFileID = existing.AnthropicFileID;

            var deleted = await WaterQualityManagementPlanDocuments.DeleteAsync(DbContext, waterQualityManagementPlanDocumentID);
            if (!deleted) return NotFound();

            await anthropicFileService.DeleteRemoteFileAsync(anthropicFileID, CancellationToken.None);
            return NoContent();
        }
    }
}
