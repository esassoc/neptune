using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Neptune.ExternalAPI.Filters;

internal sealed class ApiKeySecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == WebServiceTokenAuthenticationHandler.SchemeName))
        {
            var requirements = new Dictionary<string, IOpenApiSecurityScheme>
            {
                [WebServiceTokenAuthenticationHandler.SchemeName] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = WebServiceTokenAuthenticationHandler.SchemeName,
                    In = ParameterLocation.Header,
                    Name = WebServiceTokenAuthenticationHandler.ApiKeyName
                }
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes = requirements;

            foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
            {
                operation.Value.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Value.Security.Add(new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference(WebServiceTokenAuthenticationHandler.SchemeName)] = new List<string>()
                });
            }
        }
    }
}
