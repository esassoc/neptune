using System;
using System.IO;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Neptune.Common;
using Serilog;

namespace Neptune.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    var configurationRoot = config.Build();
                    var secretPath = configurationRoot["SECRET_PATH"];
                    if (File.Exists(secretPath))
                    {
                        config.AddJsonFile(secretPath);
                    }

                    // Optional Azure Key Vault as the real-secret source. Opt-in: only
                    // wired when KeyVaultName is set (configmap in deployed envs,
                    // .devcontainer/.env locally), so local dev with no vault / no
                    // `az login` is unaffected. DefaultAzureCredential uses the
                    // developer's `az login` identity in dev and the pod's workload
                    // identity in deployed environments.
                    var keyVaultName = configurationRoot["KeyVaultName"];
                    if (!string.IsNullOrWhiteSpace(keyVaultName))
                    {
                        var kvUri = new Uri($"https://{keyVaultName}.vault.azure.net/");
                        config.AddAzureKeyVault(kvUri, new DefaultAzureCredential(),
                            new NeptuneKeyVaultSecretManager());
                        // Re-add env vars after the vault so local overrides still win.
                        config.AddEnvironmentVariables();
                    }
                })
                .UseSerilog((context, services, configuration) =>
                {
                    configuration
                        .Enrich.FromLogContext()
                        .ReadFrom.Configuration(context.Configuration);
                })
                .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
            return hostBuilder;
        }
    }
}
