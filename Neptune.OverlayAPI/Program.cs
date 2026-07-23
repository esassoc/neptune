extern alias AzureIdentity;
using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.EFModels.Entities;
using Neptune.OverlayAPI.Services;
using Serilog;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Guarded so a missing secrets file does not throw when Key Vault is the source
// (deployed pods have no mounted secret file).
var secretPath = builder.Configuration["SECRET_PATH"];
if (File.Exists(secretPath))
{
    builder.Configuration.AddJsonFile(secretPath, optional: false, reloadOnChange: true);
}

// Opt-in Azure Key Vault: only wired when KeyVaultName is set, so local dev with
// no vault / no `az login` is unaffected. DefaultAzureCredential uses the pod's
// workload identity in AKS and the developer's `az login` identity locally.
var keyVaultName = builder.Configuration["KeyVaultName"];
if (!string.IsNullOrWhiteSpace(keyVaultName))
{
    var kvUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
    // Alias-qualified: DefaultAzureCredential is type-forwarded between Azure.Core
    // and Azure.Identity, so an unaliased name is ambiguous.
    builder.Configuration.AddAzureKeyVault(kvUri, new AzureIdentity::Azure.Identity.DefaultAzureCredential(),
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

builder.Services.Configure<OverlayAPIConfiguration>(builder.Configuration);
var configuration = builder.Configuration.Get<OverlayAPIConfiguration>();

builder.Services.AddDbContext<NeptuneDbContext>(c =>
{
    c.UseSqlServer(configuration.DatabaseConnectionString, x =>
    {
        // headroom for the p*MakeValid stored procs over ~150K freshly-inserted rows
        x.CommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds);
        x.UseNetTopologySuite();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

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
