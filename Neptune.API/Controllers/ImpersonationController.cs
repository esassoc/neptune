using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.API.Services.Authorization;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.API.Controllers
{
    [ApiController]
    [Route("impersonation")]
    public class ImpersonationController(
        NeptuneDbContext dbContext,
        ILogger<ImpersonationController> logger,
        IOptions<NeptuneConfiguration> neptuneConfiguration,
        ImpersonationService impersonationService)
        : SitkaController<ImpersonationController>(dbContext, logger, neptuneConfiguration)
    {
        [HttpPost("{personID}")]
        [ImpersonateUserFeature]
        public async Task<ActionResult<PersonDto>> ImpersonateUser([FromRoute] int personID)
        {
            var targetUser = People.GetByID(DbContext, personID);
            if (targetUser == null)
            {
                return NotFound($"Person with ID {personID} does not exist.");
            }

            if (targetUser.PersonID == CallingUser.PersonID)
            {
                return BadRequest("Cannot impersonate yourself.");
            }

            var impersonatedUser = await impersonationService.ImpersonateUserAsync(DbContext, HttpContext, personID);
            return Ok(impersonatedUser);
        }

        [HttpPost("stop")]
        [StopImpersonationFeature]
        public async Task<ActionResult<PersonDto>> StopImpersonation()
        {
            var originalUser = await impersonationService.StopImpersonationAsync(DbContext, HttpContext);
            return Ok(originalUser);
        }
    }
}
