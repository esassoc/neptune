using System.IO.Compression;
using Azure.Identity;
using Neptune.Common;
using Microsoft.AspNetCore.ResponseCompression;
using Neptune.GDALAPI.Services;
using Serilog;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Same configuration sources as Neptune.API and Neptune.OverlayAPI, so that when this service runs
// beside the API in the devcontainer (`make start`) it resolves the SAME blob storage account the API
// stages files in. Both are no-ops in the deployed pod, whose configmap supplies the connection string
// directly and sets neither SECRET_PATH nor KeyVaultName.
var secretPath = builder.Configuration["SECRET_PATH"];
if (File.Exists(secretPath))
{
    builder.Configuration.AddJsonFile(secretPath, optional: false, reloadOnChange: false);
}

var keyVaultName = builder.Configuration["KeyVaultName"];
if (!string.IsNullOrWhiteSpace(keyVaultName))
{
    var kvUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
    builder.Configuration.AddAzureKeyVault(kvUri, new DefaultAzureCredential(),
        new NeptuneKeyVaultSecretManager());
    builder.Configuration.AddEnvironmentVariables();
}

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var logger = CreateSerilogLogger(builder);
builder.Host.UseSerilog(logger);

// Add services to the container.
builder.Services.AddControllers();

// Emit RFC 7807 ProblemDetails JSON for unhandled exceptions (see app.UseExceptionHandler() below).
builder.Services.AddProblemDetails();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

builder.Services.Configure<GDALAPIConfiguration>(builder.Configuration);
builder.Services.AddScoped<IAzureStorage, AzureStorage>();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromDays(1);
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Load native GDAL at startup so a missing or broken native runtime fails the pod's start (and its
// readiness) rather than the first upload or export.
app.Logger.LogInformation("GDAL loaded in-process: {GdalVersion}", Neptune.Common.GeoSpatial.Gdal.GdalSetup.VersionInfo());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler();
}

app.UseResponseCompression();

app.MapControllers();

app.Run();
return;

Logger CreateSerilogLogger(WebApplicationBuilder webApplicationBuilder)
{
    var outputTemplate = $"[{webApplicationBuilder.Environment.EnvironmentName}] {{Timestamp:yyyy-MM-dd HH:mm:ss zzz}} {{Level}} | {{RequestId}}-{{SourceContext}}: {{Message}}{{NewLine}}{{Exception}}";
    var serilogLogger = new LoggerConfiguration()
        .ReadFrom.Configuration(webApplicationBuilder.Configuration)
        .WriteTo.Console(outputTemplate: outputTemplate);

    return serilogLogger.CreateLogger();
}
