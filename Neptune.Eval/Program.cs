using Anthropic;
using Azure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Neptune.API.Services;
using Neptune.API.Services.AI;
using Neptune.Common;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;

namespace Neptune.Eval;

/// <summary>
/// NPT-1132: WQMP AI extraction eval. Runs the production <see cref="WqmpExtractionService"/>
/// against a fixed set of WQMPs whose fields were entered by hand (the ground truth) and scores
/// the output the way the review wizard would apply it. See README.md.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var options = EvalOptions.Parse(args);
        if (options == null)
        {
            Console.WriteLine(EvalOptions.Usage);
            return 1;
        }

        var repoRoot = FindRepoRoot();
        var evalDir = Path.Combine(repoRoot, "Neptune.Eval");

        if (options.Command == "select")
        {
            using var selectHost = BuildHost(options, repoRoot);
            await using var scope = selectHost.Services.CreateAsyncScope();
            var selector = new EvalSetSelector(scope.ServiceProvider.GetRequiredService<NeptuneDbContext>(),
                scope.ServiceProvider.GetRequiredService<AzureBlobStorageService>());
            var set = await selector.SelectAsync(options.Seed);
            var path = Path.Combine(evalDir, "eval-set.json");
            EvalSetSelector.Save(set, path);
            Console.WriteLine($"Wrote {set.Documents.Count} documents to {path}: " +
                              string.Join(", ", set.Documents.GroupBy(d => (d.Split, d.PdfType)).OrderBy(g => g.Key).Select(g => $"{g.Key.Split}/{g.Key.PdfType} {g.Count()}")));
            return 0;
        }

        if (options.Command == "score")
        {
            using var scoreHost = BuildHost(options, repoRoot);
            await using var scope = scoreHost.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NeptuneDbContext>();
            var run = await EvalRun.LoadAsync(options.RunDirectory!);
            var scored = await Scorer.ScoreRunAsync(dbContext, run);
            var path = ReportWriter.Write(run, scored);
            Console.WriteLine($"Scorecard: {path}");
            return 0;
        }

        var evalSet = EvalSet.Load(Path.Combine(evalDir, "eval-set.json"));
        var documents = evalSet.Select(options.Split, options.Ids, options.Limit);
        if (documents.Count == 0)
        {
            Console.WriteLine("No documents matched the selection.");
            return 1;
        }

        using var host = BuildHost(options, repoRoot);
        var neptuneConfiguration = host.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<NeptuneConfiguration>>().Value;
        var model = neptuneConfiguration.ClaudeModelId;
        var effort = string.IsNullOrWhiteSpace(neptuneConfiguration.ClaudeEffort) ? "default" : neptuneConfiguration.ClaudeEffort;
        WqmpExtractionService.BuildOutputConfig(neptuneConfiguration.ClaudeEffort); // fail fast on a bad --effort
        Console.WriteLine($"Selected {documents.Count} document(s) ({string.Join(", ", documents.GroupBy(d => d.PdfType).Select(g => $"{g.Count()} {g.Key}"))}), " +
                          $"{documents.Sum(d => d.Pages)} pages total, model {model}, effort {effort}" +
                          (options.MaxCost.HasValue ? $", spending cap ${options.MaxCost:0.00}." : ", no spending cap."));
        if (!options.Yes)
        {
            Console.WriteLine("This calls the Anthropic API and spends credits on the shared key. Re-run with --yes to proceed.");
            return 2;
        }

        var runDirectory = Path.Combine(evalDir, "runs", $"{DateTime.Now:yyyyMMdd-HHmm}-{options.Label}");
        var runner = new EvalRunner(host.Services, runDirectory, model, effort, options);
        var result = await runner.RunAsync(documents);

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NeptuneDbContext>();
            var scored = await Scorer.ScoreRunAsync(dbContext, result);
            var path = ReportWriter.Write(result, scored);
            Console.WriteLine($"Scorecard: {path}");
        }
        return 0;
    }

    private static IHost BuildHost(EvalOptions options, string repoRoot)
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((_, config) =>
            {
                // Same sources and order as Neptune.API/Program.cs: SECRET_PATH file, then the
                // Key Vault (when KeyVaultName is set), then environment variables.
                var configurationRoot = config.Build();
                var secretPath = configurationRoot["SECRET_PATH"];
                if (File.Exists(secretPath))
                {
                    config.AddJsonFile(secretPath);
                }
                var keyVaultName = configurationRoot["KeyVaultName"];
                if (!string.IsNullOrWhiteSpace(keyVaultName))
                {
                    config.AddAzureKeyVault(new Uri($"https://{keyVaultName}.vault.azure.net/"), new DefaultAzureCredential(),
                        new NeptuneKeyVaultSecretManager());
                    config.AddEnvironmentVariables();
                }
                if (!string.IsNullOrWhiteSpace(options.Model))
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?> { ["ClaudeModelId"] = options.Model });
                }
                if (!string.IsNullOrWhiteSpace(options.Effort))
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?> { ["ClaudeEffort"] = options.Effort });
                }
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddSimpleConsole(o => o.SingleLine = true);
                logging.SetMinimumLevel(options.Verbose ? LogLevel.Information : LogLevel.Warning);
                logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
            })
            .ConfigureServices((context, services) =>
            {
                services.Configure<NeptuneConfiguration>(context.Configuration);
                var configuration = context.Configuration.Get<NeptuneConfiguration>()!;

                services.AddDbContext<NeptuneDbContext>(c => c.UseSqlServer(configuration.DatabaseConnectionString, x =>
                {
                    x.CommandTimeout((int)TimeSpan.FromMinutes(10).TotalSeconds);
                    x.UseNetTopologySuite();
                }));
                services.AddHttpClient();
                services.AddScoped(_ => new AzureBlobStorageService(configuration.AzureBlobStorageConnectionString));
                // Read prompts straight from the repo so prompt edits take effect without a rebuild.
                services.AddSingleton<IPromptTemplateService>(_ =>
                    new PromptTemplateService(Path.Combine(repoRoot, "Neptune.API", "Services", "AI", "Prompts")));
                services.AddScoped<AnthropicFileService>();
                services.AddScoped<WqmpExtractionService>();
                // Same client options as Neptune.API/Startup.cs (no HttpClient timeout; the service
                // bounds each category call with its own 4-minute token).
                services.AddSingleton(_ => new AnthropicClient(new Anthropic.Core.ClientOptions
                {
                    ApiKey = configuration.AnthropicApiKey,
                    Timeout = TimeSpan.FromMinutes(5),
                    HttpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan },
                }));
            })
            .Build();
    }

    // Current directory first: with --artifacts-path the binaries live outside the repo.
    private static string FindRepoRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(start);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Neptune.sln")))
            {
                dir = dir.Parent;
            }
            if (dir != null) return dir.FullName;
        }
        throw new InvalidOperationException("Could not find Neptune.sln; run from inside the repo.");
    }
}

public sealed class EvalOptions
{
    public const string Usage = """
        Usage:
          dotnet run --project Neptune.Eval -- run [--split dev|test|all] [--ids 123,456] [--limit N]
                                                   [--label name] [--model claude-...] [--effort low|medium|high|xhigh|max]
                                                   [--max-cost 25] [--concurrency N] [--person-id N] [--verbose] --yes
          dotnet run --project Neptune.Eval -- score <run directory>
          dotnet run --project Neptune.Eval -- select [--seed N]

        run     Extract each selected document with the production WqmpExtractionService, save the
                raw output under Neptune.Eval/runs/, then score it. Spends Anthropic credits: --yes required.
                --max-cost stops starting documents once the run has spent that many dollars; an
                Anthropic account problem (credits, usage limit, key) stops the run immediately.
        score   Re-score a saved run against the current ground truth (no API calls).
        select  Rebuild eval-set.json: classify candidate PDFs (downloads them from blob storage;
                no API calls). Overwrites the checked-in set, so only run it to change the set.
        """;

    public string Command { get; private set; } = "";
    public string Split { get; private set; } = "dev";
    public HashSet<int>? Ids { get; private set; }
    public int? Limit { get; private set; }
    public string Label { get; private set; } = "run";
    public string? Model { get; private set; }
    public string? Effort { get; private set; }
    public decimal? MaxCost { get; private set; }
    public int Concurrency { get; private set; } = 2;
    public int PersonID { get; private set; } = 1;
    public bool Yes { get; private set; }
    public bool Verbose { get; private set; }
    public string? RunDirectory { get; private set; }
    public int Seed { get; private set; } = 1132;

    public static EvalOptions? Parse(string[] args)
    {
        if (args.Length == 0) return null;
        var o = new EvalOptions { Command = args[0] };
        if (o.Command == "select")
        {
            if (args.Length == 3 && args[1] == "--seed") o.Seed = int.Parse(args[2]);
            else if (args.Length != 1) return null;
            return o;
        }
        if (o.Command == "score")
        {
            if (args.Length < 2) return null;
            o.RunDirectory = args[1];
            return o;
        }
        if (o.Command != "run") return null;

        for (var i = 1; i < args.Length; i++)
        {
            string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{args[i]} needs a value");
            switch (args[i])
            {
                case "--split": o.Split = Next(); break;
                case "--ids": o.Ids = Next().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToHashSet(); break;
                case "--limit": o.Limit = int.Parse(Next()); break;
                case "--label": o.Label = Next(); break;
                case "--model": o.Model = Next(); break;
                case "--effort": o.Effort = Next(); break;
                case "--max-cost": o.MaxCost = decimal.Parse(Next(), System.Globalization.CultureInfo.InvariantCulture); break;
                case "--concurrency": o.Concurrency = Math.Max(1, int.Parse(Next())); break;
                case "--person-id": o.PersonID = int.Parse(Next()); break;
                case "--yes": o.Yes = true; break;
                case "--verbose": o.Verbose = true; break;
                default: return null;
            }
        }
        return o;
    }
}
