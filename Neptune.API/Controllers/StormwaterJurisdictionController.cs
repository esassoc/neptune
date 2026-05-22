using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Attributes;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("jurisdictions")]
    public class StormwaterJurisdictionController(
        NeptuneDbContext dbContext,
        ILogger<StormwaterJurisdictionController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration)
        : SitkaController<StormwaterJurisdictionController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpGet]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<StormwaterJurisdictionGridDto>>> List()
        {
            var stormwaterJurisdictionDtos = await StormwaterJurisdictions.ListAsDtoAsync(DbContext);
            return Ok(stormwaterJurisdictionDtos);
        }

        [HttpGet("user-viewable")]
        [AllowAnonymous]
        [OptionalAuth]
        public async Task<ActionResult<List<StormwaterJurisdictionDisplayDto>>> ListViewable()
        {
            var stormwaterJurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(DbContext, CallingUser.PersonID);
            var stormwaterJurisdictionDisplayDtos = await StormwaterJurisdictions.ListByIDsAsDisplayDtoAsync(DbContext, stormwaterJurisdictionIDs);
            return Ok(stormwaterJurisdictionDisplayDtos);
        }

        [HttpGet("bounding-box")]
        [AllowAnonymous]
        [OptionalAuth]
        public async Task<ActionResult<BoundingBoxDto>> GetBoundingBox()
        {
            var boundingBoxDto = await StormwaterJurisdictions.GetBoundingBoxDtoByPersonAsync(DbContext, CallingUser);
            return Ok(boundingBoxDto);
        }

        [HttpGet("{jurisdictionID}")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<StormwaterJurisdictionGridDto>> Get([FromRoute] int jurisdictionID)
        {
            var stormwaterJurisdictionGridDto = await StormwaterJurisdictions.GetByIDAsDtoAsync(DbContext, jurisdictionID);
            return Ok(stormwaterJurisdictionGridDto);
        }

        [HttpGet("{jurisdictionID}/bounding-box")]
        [AllowAnonymous]
        public ActionResult<BoundingBoxDto> GetBoundingBoxByJurisdictionID([FromRoute] int jurisdictionID)
        {
            var boundingBoxDto = StormwaterJurisdictions.GetBoundingBoxDtoByJurisdictionID(DbContext, jurisdictionID);
            return Ok(boundingBoxDto);
        }

        [HttpGet("{jurisdictionID}/treatment-bmps")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<TreatmentBMPGridDto>>> ListTreatmentBMPs([FromRoute] int jurisdictionID)
        {
            var entities = await DbContext.vTreatmentBMPDetaileds
                .Where(x => x.StormwaterJurisdictionID == jurisdictionID)
                .ToListAsync();
            var treatmentBMPGridDtos = entities.Select(x => x.AsGridDto())
                .ToList();
            return Ok(treatmentBMPGridDtos);
        }

        [HttpGet("{jurisdictionID}/users")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<PersonDisplayDto>>> ListUsers([FromRoute] int jurisdictionID)
        {
            var entities = await StormwaterJurisdictionPeople.ListByStormwaterJurisdictionIDAsPersonDto(DbContext, jurisdictionID);
            return Ok(entities);
        }
    }
}