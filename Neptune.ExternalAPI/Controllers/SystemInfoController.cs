using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neptune.ExternalAPI.Logging;

namespace Neptune.ExternalAPI.Controllers;

/// <summary>
/// Anonymous service-info endpoints — health and version checks.
/// </summary>
[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("system")]
public class SystemInfoController : ControllerBase
{
    /// <summary>
    /// Service identity ping.
    /// </summary>
    [HttpGet("info")]
    [LogIgnore]
    [EndpointSummary("Service information")]
    [EndpointDescription("Returns the service name and environment for sanity checks.")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [Produces("application/json")]
    public IActionResult Info()
    {
        return Ok(new
        {
            Service = "Neptune.ExternalAPI",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }
}
