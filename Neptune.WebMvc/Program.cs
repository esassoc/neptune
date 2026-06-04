using Auth0.AspNetCore.Authentication;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Neptune.Common.Email;
using Neptune.Common.JsonConverters;
using Neptune.Common.Services;
using Neptune.Common.Services.GDAL;
using Neptune.EFModels.Entities;
using Neptune.Jobs;
using Neptune.Jobs.Services;
using Neptune.WebMvc.Common;
using Neptune.WebMvc.Common.OpenID;
using Neptune.WebMvc.Services;
using NetTopologySuite.IO.Converters;
using SendGrid;
using Serilog;
using Serilog.Core;
using System.Text.Json;
using System.Text.Json.Serialization;
using LogHelper = Neptune.WebMvc.Services.Logging.LogHelper;

var builder = WebApplication.CreateBuilder(args);
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(context.Configuration);
    });
    var logger = CreateSerilogLogger(builder);

    // Add services to the container.
    var services = builder.Services;
    services.AddRazorPages();
    services.AddControllersWithViews(options =>
        {
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
        })
        .AddRazorOptions(options =>
        {
            options.ViewLocationFormats.Add("~/Views/Shared/TextControls/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/ExpenditureAndBudgetControls/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/PerformanceMeasureControls/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/ProjectControls/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/JurisdictionControls/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/ProjectWatershedControls/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/ProjectUpdateDiffControls/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/EditAttributes/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/SortOrder/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/Location/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/UserJurisdictions/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/HRUCharacteristics/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/Shared/ModeledPerformance/{0}.cshtml");
            options.ViewLocationFormats.Add("~/Views/FieldVisit/ObservationTypePreview/{0}.cshtml");
        })
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new GeoJsonConverterFactory(false));
            options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
            options.JsonSerializerOptions.Converters.Add(new NullableDateTimeConverter());
            options.JsonSerializerOptions.Converters.Add(new DoubleConverter(10));
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.PropertyNameCaseInsensitive = false;
            options.JsonSerializerOptions.PropertyNamingPolicy = null;
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        });

    builder.Configuration.AddJsonFile(builder.Configuration["SECRET_PATH"], optional: false, reloadOnChange: true);

    services.Configure<WebConfiguration>(builder.Configuration);
    services.Configure<NeptuneJobConfiguration>(builder.Configuration);
    services.Configure<SendGridConfiguration>(builder.Configuration);
    var configuration = builder.Configuration.Get<WebConfiguration>();

    services.AddDbContext<NeptuneDbContext>(c =>
    {
        c.UseSqlServer(configuration.DatabaseConnectionString, x =>
        {
            x.CommandTimeout((int)TimeSpan.FromMinutes(3).TotalSeconds);
            x.UseNetTopologySuite();
        });
    });

    services.AddHttpClient<NereidService>(c =>
    {
        c.BaseAddress = new Uri(configuration.NereidUrl);
        c.Timeout = TimeSpan.FromDays(1);
    }).ConfigurePrimaryHttpMessageHandler(() =>
    {
        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
        httpClientHandler.ServerCertificateCustomValidationCallback =
            (_, _, _, _) => true;

        return httpClientHandler;
    });

    services.AddHttpClient<OCGISService>(c =>
    {
        c.BaseAddress = new Uri(configuration.OCGISBaseUrl);
        c.Timeout = TimeSpan.FromDays(1);
    }).ConfigurePrimaryHttpMessageHandler(() =>
    {
        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
        httpClientHandler.ServerCertificateCustomValidationCallback =
            (_, _, _, _) => true;

        return httpClientHandler;
    });

    services.AddHttpClient<GDALAPIService>(c =>
    {
        c.BaseAddress = new Uri(configuration.GDALAPIBaseUrl);
        c.Timeout = TimeSpan.FromDays(1);
    }).ConfigurePrimaryHttpMessageHandler(() =>
    {
        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
        httpClientHandler.ServerCertificateCustomValidationCallback =
            (_, _, _, _) => true;

        return httpClientHandler;
    });

    services.AddHttpClient<QGISAPIService>(c =>
    {
        c.BaseAddress = new Uri(configuration.QGISAPIBaseUrl);
        c.Timeout = TimeSpan.FromDays(1);
    }).ConfigurePrimaryHttpMessageHandler(() =>
    {
        var httpClientHandler = new HttpClientHandler();
        httpClientHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
        httpClientHandler.ServerCertificateCustomValidationCallback =
            (_, _, _, _) => true;

        return httpClientHandler;
    });

    // Register SendGrid client from official SDK (not the Extensions DI package)
    services.AddSingleton<ISendGridClient>(_ => new SendGridClient(configuration.SendGridApiKey));
    services.AddSingleton<SitkaSmtpClientService>();

    #region Hangfire
    services.AddHangfire(x => x
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(configuration.DatabaseConnectionString, new SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));

    services.AddHangfireServer(x =>
    {
        x.WorkerCount = 1;
    });
    #endregion

    #region Auth0 authentication
    services.Configure<CookiePolicyOptions>(options =>
    {
        options.MinimumSameSitePolicy = SameSiteMode.None;
        options.Secure = CookieSecurePolicy.Always;
    });
    
    services.AddAuth0WebAppAuthentication(options =>
    {
        options.Domain = configuration.Auth0Domain;
        options.ClientId = configuration.Auth0ClientID;
        options.Scope = "openid profile email";
        options.CallbackPath = "/callback";

        options.OpenIdConnectEvents = new OpenIdConnectEvents
        {
            OnTokenValidated = context =>
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<NeptuneDbContext>();
                var sitkaSmtpClientService = context.HttpContext.RequestServices.GetRequiredService<SitkaSmtpClientService>();

                if (context.Principal?.Identity?.IsAuthenticated == true)
                {
                    AuthenticationHelper.ProcessLoginFromAuth0(context, dbContext, configuration, logger, sitkaSmtpClientService);
                }

                return Task.CompletedTask;
            },

            OnRemoteFailure = context =>
            {
                context.HandleResponse();

                // The useful info is on context.Failure (often an AuthenticationFailureException
                // wrapping an OpenIdConnectProtocolException)
                var ex = context.Failure;
                var msg = ex?.ToString() ?? "";

                // Common patterns depending on stack version:
                // - "access_denied"
                // - "Please verify your email before continuing."
                // - inner OpenIdConnectProtocolException
                if (msg.Contains("access_denied", StringComparison.OrdinalIgnoreCase) &&
                    msg.Contains("verify your email", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect("/Account/VerifyEmailRequired");
                    return Task.CompletedTask;
                }

                // Optional: log the full failure for diagnostics
                // logger.LogError(ex, "Auth0 remote failure");

                context.Response.Redirect("/Home/Error");
                return Task.CompletedTask;
            }
        };
    });

    services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ReturnUrlParameter = "returnUrl";
        options.AccessDeniedPath = "/Account/NotAuthorized"; // or whatever you prefer
    });
    #endregion


    services.AddHttpContextAccessor();
    services.AddScoped<AzureBlobStorageService>(sp =>
        new AzureBlobStorageService(sp.GetRequiredService<IOptions<WebConfiguration>>().Value.AzureBlobStorageConnectionString));
    services.AddScoped<FileResourceService>();
    services.AddHealthChecks().AddDbContextCheck<NeptuneDbContext>();
}


var app = builder.Build();
{
    app.UseSerilogRequestLogging(opts =>
    {
        opts.EnrichDiagnosticContext = LogHelper.EnrichFromRequest;
        opts.GetLevel = LogHelper.CustomGetLevel;
    });

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    // app.MapGet("/debug/routes", (IEnumerable<EndpointDataSource> endpointSources) => string.Join("\n", endpointSources.SelectMany(source => source.Endpoints)));

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseRouting();

    app.UseCookiePolicy();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.MapHealthChecks("/healthz");

    app.Run();
}
return;

Logger CreateSerilogLogger(WebApplicationBuilder webApplicationBuilder)
{
    var outputTemplate = $"[{webApplicationBuilder.Environment.EnvironmentName}] {{Timestamp:yyyy-MM-dd HH:mm:ss zzz}} {{Level}} | {{RequestId}}-{{SourceContext}}: {{Message}}{{NewLine}}{{Exception}}";
    var serilogLogger = new LoggerConfiguration()
        .ReadFrom.Configuration(webApplicationBuilder.Configuration)
        .WriteTo.Console(outputTemplate: outputTemplate);

    return serilogLogger.CreateLogger();
}