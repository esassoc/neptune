using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neptune.API.Services;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;
using System.Linq;
using System.Threading.Tasks;

namespace Neptune.API.Controllers;

[ApiController]
[Route("user-claims")]
public class UserClaimsController(
    NeptuneDbContext dbContext,
    ILogger<UserClaimsController> logger,
    IOptions<NeptuneConfiguration> neptuneConfiguration)
    : SitkaController<UserClaimsController>(dbContext, logger, neptuneConfiguration)
{
    [HttpGet("{globalID}")]
    [Authorize]
    public async Task<ActionResult<PersonDto>> GetByGlobalID([FromRoute] string globalID)
    {
        var userDto = await People.GetByGlobalIDAsDtoAsync(DbContext, globalID);
        if (userDto == null)
        {
            var notFoundMessage = $"User with GlobalID {globalID} does not exist!";
            Logger.LogError(notFoundMessage);
            return NotFound(notFoundMessage);
        }

        return Ok(userDto);
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PersonDto>> PostUserClaims([FromServices] HttpContext httpContext)
    {
        var claimsPrincipal = httpContext.User;
        if (!claimsPrincipal.Claims.Any())  // Updating user based on claims does not work when there are no claims
        {
            return BadRequest();
        }

        await People.UpdateClaims(DbContext, claimsPrincipal);
        // Return CallingUser (effective user) so a SPA refresh during an impersonation
        // session correctly returns the impersonated PersonDto, not the authenticated user.
        return Ok(CallingUser);
    }
}