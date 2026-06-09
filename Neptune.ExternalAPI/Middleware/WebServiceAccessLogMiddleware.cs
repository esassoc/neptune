using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Neptune.EFModels.Entities;

namespace Neptune.ExternalAPI.Middleware;

// NPT-1078: writes an audit row to dbo.WebServiceAccessLog after each authenticated
// request resolves and denormalizes the timestamp onto Person.LastWebServiceAccessDate.
// Drives compliance ("who accessed what, when") and token-pruning ("who hasn't used
// theirs lately") use cases.
//
// Runs AFTER UseAuthentication/UseAuthorization so context.User reflects the resolved
// identity, and AFTER the response pipeline finishes so we capture the actual status
// code. Audit-write failures are logged-and-swallowed because we never want a logging
// hiccup to fail a successful API call from a consumer's perspective.
public class WebServiceAccessLogMiddleware(RequestDelegate next, ILogger<WebServiceAccessLogMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, NeptuneDbContext dbContext)
    {
        await next(context);

        // Skip anon traffic (healthz, /openapi, /docs). The auth handler returns Fail for
        // protected endpoints when no key is provided, so an unauthenticated identity here
        // also means "the request never reached an audited endpoint."
        if (context.User?.Identity?.IsAuthenticated != true) return;

        var personIDClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(personIDClaim, out var personID)) return;

        try
        {
            var endpoint = context.Request.Path.Value ?? string.Empty;
            // Cap the endpoint string at the column's max length so a pathologically long
            // URL doesn't fail the insert. The dbo.WebServiceAccessLog.Endpoint column is
            // VARCHAR(200); truncate defensively here so the audit row still gets written.
            if (endpoint.Length > 200) endpoint = endpoint[..200];

            await WebServiceAccessLogs.CreateAsync(
                dbContext,
                personID,
                endpoint,
                context.Request.Method,
                context.Response.StatusCode);
        }
        catch (Exception ex)
        {
            // Log but don't rethrow — the consumer already has their response and we don't
            // want a logging-side hiccup to turn a 200 into a 500.
            logger.LogError(ex, "Failed to write WebServiceAccessLog row for PersonID {PersonID}", personID);
        }
    }
}
