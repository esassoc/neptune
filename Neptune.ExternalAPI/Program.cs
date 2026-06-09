using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using Neptune.ExternalAPI;
using Neptune.ExternalAPI.Filters;
using Neptune.ExternalAPI.Logging;
using Neptune.ExternalAPI.Middleware;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configuration: env vars + secrets JSON (per SECRET_PATH) + appsettings.json
builder.Configuration.AddEnvironmentVariables()
    .AddJsonFile(builder.Configuration["SECRET_PATH"], optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.json", optional: true);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .Enrich.FromLogContext()
        .ReadFrom.Configuration(context.Configuration);
});

builder.Services.Configure<NeptuneExternalAPIConfiguration>(builder.Configuration);
var configuration = builder.Configuration.Get<NeptuneExternalAPIConfiguration>();

builder.Services.AddDbContext<NeptuneDbContext>(c =>
{
    c.UseSqlServer(configuration.DatabaseConnectionString, x =>
    {
        x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
        x.UseNetTopologySuite();
    });
});

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddSingleton(_ => new AzureBlobStorageService(configuration.AzureBlobStorageConnectionString));

// PascalCase JSON property names everywhere — matches the legacy PowerBI controller
// response shape that existing client dashboards parse. Both config blocks are required:
//   - AddControllers().AddJsonOptions → runtime JSON bytes from MVC controller actions.
//   - ConfigureHttpJsonOptions       → OpenAPI schema that Scalar renders.
// ASP.NET Core defaults to JsonSerializerDefaults.Web (camelCase) on both sides; we
// override both to get PascalCase + a schema that matches the runtime bytes.
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null;
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

builder.Services.AddOpenApi(options =>
{
    options.AddScalarTransformers();

    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Orange County Stormwater Tools — External API",
            Version = "1.0",
            Description = "Read-only data endpoints for external consumers (PowerBI dashboards, partner reporting). " +
                          "All requests require an `x-api-key` header containing your personal access token. " +
                          "Generate or rotate your token from the Web Services tab inside the Data Hub of the OC Stormwater Tools site.",
            Contact = new OpenApiContact
            {
                Name = "OC Stormwater Tools Support",
                Email = "support@ocstormwatertools.org"
            }
        };
        return Task.CompletedTask;
    });

    options.AddDocumentTransformer<ApiKeySecuritySchemeTransformer>();
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = WebServiceTokenAuthenticationHandler.SchemeName;
    options.DefaultChallengeScheme = WebServiceTokenAuthenticationHandler.SchemeName;
})
.AddScheme<AuthenticationSchemeOptions, WebServiceTokenAuthenticationHandler>(WebServiceTokenAuthenticationHandler.SchemeName, _ => { });

// AKS ingress terminates TLS and forwards the request to the pod over HTTP, setting
// X-Forwarded-Proto: https. Without this, Request.Scheme stays "http" inside the pod,
// which (a) makes UseHttpsRedirection issue a needless 301 (PowerBI / curl users without
// -L get stuck on it) and (b) makes the OpenAPI auto-detected server URL render as
// "http://qa-api..." in Scalar, so the "try it out" client originates an http request
// that has to follow the same 301. Clearing KnownNetworks/KnownProxies is required because
// the ingress pod's IP varies and isn't on the default loopback trust list.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHealthChecks().AddDbContextCheck<NeptuneDbContext>();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseSerilogRequestLogging(opts =>
{
    opts.EnrichDiagnosticContext = LogHelper.EnrichFromRequest;
    opts.GetLevel = LogHelper.CustomGetLevel;
});

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}

// Must run before UseHttpsRedirection so the redirector sees the original scheme.
app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseRouting();
app.UseCors(policy =>
{
    policy.AllowAnyOrigin();
    policy.AllowAnyHeader();
    policy.AllowAnyMethod();
    policy.WithExposedHeaders("WWW-Authenticate");
});
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AccessDeniedMiddleware>();
app.UseMiddleware<LogHelper>();
app.MapControllers();
app.MapHealthChecks("/healthz");

app.MapOpenApi();
app.MapScalarApiReference("/docs", options =>
{
    options.Title = "OC Stormwater Tools — External API";
    options.ShowSidebar = true;
    options.HideModels = false;
    options.AddPreferredSecuritySchemes(WebServiceTokenAuthenticationHandler.SchemeName);
});

app.Run();
