using Microsoft.EntityFrameworkCore;
using Neptune.EFModels.Entities;
using Neptune.QGISAPI.Services;
using Serilog;
using Serilog.Core;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddJsonFile(builder.Configuration["SECRET_PATH"], optional: false, reloadOnChange: true);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var logger = CreateSerilogLogger(builder);
builder.Host.UseSerilog(logger);


// Add services to the container.
builder.Services.AddControllers();

// Emit RFC 7807 ProblemDetails JSON for unhandled exceptions (see app.UseExceptionHandler() below).
builder.Services.AddProblemDetails();

builder.Services.AddScoped<QgisService>();
builder.Services.Configure<QGISAPIConfiguration>(builder.Configuration);
var configuration = builder.Configuration.Get<QGISAPIConfiguration>();

builder.Services.AddDbContext<NeptuneDbContext>(c =>
{
    c.UseSqlServer(configuration.DatabaseConnectionString, x =>
    {
        x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
        x.UseNetTopologySuite();
    });
});

builder.Services.AddScoped<IAzureStorage, AzureStorage>();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromDays(1);
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
