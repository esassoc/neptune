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
        if (!Request.Headers.TryGetValue(ApiKeyName, out var extractedApiKey))
        {
            return AuthenticateResult.Fail("API key was not provided");
        }

        if (!Guid.TryParse(extractedApiKey, out var parsedApiKey))
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
