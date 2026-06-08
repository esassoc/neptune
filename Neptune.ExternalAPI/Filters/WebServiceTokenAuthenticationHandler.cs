using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Neptune.EFModels.Entities;

namespace Neptune.ExternalAPI.Filters;

public class WebServiceTokenAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    NeptuneDbContext dbContext)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string ApiKeyName = "x-api-key";
    public const string SchemeName = "ApiKeyScheme";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Header is the documented primary path. Query string is an undocumented PowerBI
        // escape hatch — PowerBI's data source UI doesn't make custom headers easy, so
        // users paste the token into a fully self-contained URL the same way the legacy
        // MVC PowerBIController accepted it as a route segment. Tokens in URLs get logged
        // in access logs / browser history / error reports; consumers should prefer the
        // header path whenever they control the request headers.
        string? rawApiKey = null;
        if (Request.Headers.TryGetValue(ApiKeyName, out var headerValue))
        {
            rawApiKey = headerValue.ToString();
        }
        else if (Request.Query.TryGetValue(ApiKeyName, out var queryValue))
        {
            rawApiKey = queryValue.ToString();
        }

        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            return AuthenticateResult.Fail("API key was not provided");
        }

        if (!Guid.TryParse(rawApiKey, out var parsedApiKey))
        {
            return AuthenticateResult.Fail("API key is not valid");
        }

        var person = await People.GetByWebServiceAccessTokenAsync(dbContext, parsedApiKey);
        if (person == null)
        {
            return AuthenticateResult.Fail("API key is not valid");
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, person.PersonID.ToString()),
            new Claim(ClaimTypes.Name, person.Email ?? string.Empty),
            new Claim("RoleID", person.RoleID.ToString())
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
