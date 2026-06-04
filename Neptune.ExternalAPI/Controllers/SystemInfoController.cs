using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Neptune.ExternalAPI.Logging;

namespace Neptune.ExternalAPI.Controllers;

/// <summary>
/// Anonymous service-info endpoints — health and version checks. Also serves as the target
/// for the kubelet startup/liveness probes via the root `/` route, mirroring Neptune.API's
/// SystemInfoController. Hidden from Scalar so external consumers don't see noise.
/// </summary>
[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class SystemInfoController : ControllerBase
{
    /// <summary>
    /// Service identity ping. Mapped at both <c>/</c> (kubelet startup/liveness probe target)
    /// and <c>/system/info</c> (explicit name for humans hitting the API directly).
    /// </summary>
    [HttpGet("/")]
    [HttpGet("system/info")]
    [LogIgnore]
    public IActionResult Info()
    {
        return Ok(new
        {
            Service = "Neptune.ExternalAPI",
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }
}
