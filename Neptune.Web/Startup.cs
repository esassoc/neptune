using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Serilog;
using System.Linq;

namespace Neptune.Web
{
    public class Startup
    {
        private readonly IWebHostEnvironment _environment;
        public IConfiguration Configuration { get; set; }

        public Startup(IWebHostEnvironment environment)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
                .SetBasePath(currentDirectory)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            _environment = environment;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Adding response compression which greatly reduces size of static content delivery.
            // https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-5.0
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
                // Appending any extra MIME types to compress.
                // Default list is here: https://docs.microsoft.com/en-us/aspnet/core/performance/response-compression?view=aspnetcore-5.0#mime-types-1
                // NOTE: It's not recommended to compress images or other binary files
                options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
                {
                    "image/svg+xml"
                });
                options.EnableForHttps = true; 
            });
            var logger = GetSerilogLogger();
            services.AddSingleton(logger);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHostApplicationLifetime applicationLifetime, Serilog.ILogger logger)
        {
            loggerFactory.AddSerilog(logger);
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                var options = new RewriteOptions().AddRedirectToHttps(301, 9001);
                app.UseRewriter(options);
            }
            
            app.Use(async (context, next) =>
            {
                await next();

                if (context.Response.StatusCode == 404 && !Path.HasExtension(context.Request.Path.Value))
                {
                    context.Request.Path = "/index.html";
                    context.Response.StatusCode = 200;
                    await next();
                }
            });

            app.UseResponseCompression();
            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = SetSpaCacheHeaders });
        }

        // Hashed Angular bundles can be cached forever because their URLs change every build.
        // Stable URLs, especially index.html, must revalidate so the browser does not keep
        // a stale SPA shell that points at deleted bundle filenames.
        // First alternative `-[A-Z0-9]{8,}` matches esbuild's `name-HASH.ext` (uppercase base32).
        // Second alternative `\.[a-f0-9]{16,}` matches classic webpack's `name.HASH.ext` (lowercase
        // hex, default 16 chars). Both alternatives are narrow enough to avoid false-positives on
        // ordinary lowercase asset names (e.g., `account-activity-screenshot.png`).
        private static readonly Regex HashedAssetPattern = new(@"(?:-[A-Z0-9]{8,}|\.[a-f0-9]{16,})\.[a-z0-9]+$", RegexOptions.Compiled);

        private static void SetSpaCacheHeaders(StaticFileResponseContext context)
        {
            var headers = context.Context.Response.GetTypedHeaders();
            var fileName = Path.GetFileName(context.File.Name);
            if (HashedAssetPattern.IsMatch(fileName))
            {
                headers.CacheControl = new CacheControlHeaderValue
                {
                    Public = true,
                    MaxAge = TimeSpan.FromDays(365),
                    Extensions = { new NameValueHeaderValue("immutable") }
                };
            }
            else
            {
                headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            }
        }

        private Serilog.ILogger GetSerilogLogger()
        {
            var serilogLogger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration);

            return serilogLogger.CreateLogger();
        }
    }
    
}
