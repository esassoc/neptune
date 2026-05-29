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
            if (!await CurrentUserCanViewJurisdictionAsync(jurisdictionID))
            {
                return StatusCode(403);
            }
            var stormwaterJurisdictionGridDto = await StormwaterJurisdictions.GetByIDAsDtoAsync(DbContext, jurisdictionID);
            return Ok(stormwaterJurisdictionGridDto);
        }

        [HttpPut("{jurisdictionID}")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<StormwaterJurisdictionGridDto>> Update([FromRoute] int jurisdictionID, [FromBody] StormwaterJurisdictionUpsertDto dto)
        {
            if (!await CurrentUserCanManageJurisdictionAsync(jurisdictionID))
            {
                return StatusCode(403);
            }
            var updated = await StormwaterJurisdictions.UpdateAsync(DbContext, jurisdictionID, dto);
            return Ok(updated);
        }

        [HttpPut("{jurisdictionID}/users")]
        [JurisdictionEditFeature]
        public async Task<ActionResult<List<PersonDisplayDto>>> UpdateUsers([FromRoute] int jurisdictionID, [FromBody] StormwaterJurisdictionPersonBulkUpsertDto dto)
        {
            if (!await CurrentUserCanManageJurisdictionAsync(jurisdictionID))
            {
                return StatusCode(403);
            }
            await StormwaterJurisdictionPeople.SetJurisdictionPeopleAsync(DbContext, jurisdictionID, dto.PersonIDs);
            var users = await StormwaterJurisdictionPeople.ListByStormwaterJurisdictionIDAsPersonDto(DbContext, jurisdictionID);
            return Ok(users);
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
            if (!await CurrentUserCanViewJurisdictionAsync(jurisdictionID))
            {
                return StatusCode(403);
            }
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
            if (!await CurrentUserCanViewJurisdictionAsync(jurisdictionID))
            {
                return StatusCode(403);
            }
            var entities = await StormwaterJurisdictionPeople.ListByStormwaterJurisdictionIDAsPersonDto(DbContext, jurisdictionID);
            return Ok(entities);
        }

        // NPT-1061 item 4c: Admin/SitkaAdmin can view any jurisdiction; JurisdictionManager and
        // JurisdictionEditor only the jurisdiction(s) they're assigned to.
        private async Task<bool> CurrentUserCanViewJurisdictionAsync(int jurisdictionID)
        {
            if (CallingUser.RoleID == (int)RoleEnum.Admin || CallingUser.RoleID == (int)RoleEnum.SitkaAdmin)
            {
                return true;
            }
            return await DbContext.StormwaterJurisdictionPeople
                .AnyAsync(x => x.PersonID == CallingUser.PersonID && x.StormwaterJurisdictionID == jurisdictionID);
        }

        // Editing (Basics + assigned Users) is limited to Admin/SitkaAdmin and the
        // JurisdictionManager assigned to the jurisdiction — JurisdictionEditors can view but not edit.
        private async Task<bool> CurrentUserCanManageJurisdictionAsync(int jurisdictionID)
        {
            if (CallingUser.RoleID == (int)RoleEnum.Admin || CallingUser.RoleID == (int)RoleEnum.SitkaAdmin)
            {
                return true;
            }
            if (CallingUser.RoleID == (int)RoleEnum.JurisdictionManager)
            {
                return await DbContext.StormwaterJurisdictionPeople
                    .AnyAsync(x => x.PersonID == CallingUser.PersonID && x.StormwaterJurisdictionID == jurisdictionID);
            }
            return false;
        }
    }
}