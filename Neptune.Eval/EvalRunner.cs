using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Neptune.API.Services.AI;
using Neptune.Models.DataTransferObjects;

namespace Neptune.Eval;

/// <summary>
/// Calls the production extraction for each document and saves the raw result to
/// runs/&lt;run&gt;/docs/&lt;WQMP id&gt;.json as soon as it finishes, so an interrupted run keeps
/// what it already paid for. Results are NOT written to WaterQualityManagementPlanExtractionResult,
/// so local review-wizard state is untouched.
/// </summary>
public sealed class EvalRunner(IServiceProvider services, string runDirectory, string model, string effort, EvalOptions options)
{
    private readonly object _spendLock = new();
    private decimal _spent;
    private string? _stopReason;

    public async Task<EvalRun> RunAsync(List<EvalDocument> documents)
    {
        Directory.CreateDirectory(Path.Combine(runDirectory, "docs"));
        var run = new EvalRun
        {
            Directory = runDirectory,
            Label = options.Label,
            Model = model,
            Effort = effort,
            MaxCost = options.MaxCost,
            StartedAt = DateTime.UtcNow,
        };
        await SaveRunAsync(run);

        using var gate = new SemaphoreSlim(options.Concurrency);
        var completed = 0;
        var skipped = new System.Collections.Concurrent.ConcurrentBag<int>();
        var tasks = documents.Select(async doc =>
        {
            await gate.WaitAsync();
            try
            {
                // Checked when a slot frees up, so at most `Concurrency` documents in flight can
                // carry spending past the cap.
                var stopReason = StopReason();
                if (stopReason != null)
                {
                    skipped.Add(doc.WaterQualityManagementPlanID);
                    Console.WriteLine($"[skip] WQMP {doc.WaterQualityManagementPlanID}: {stopReason}");
                    return null;
                }

                var result = await ExtractOneAsync(doc);
                await File.WriteAllTextAsync(Path.Combine(runDirectory, "docs", $"{doc.WaterQualityManagementPlanID}.json"),
                    JsonSerializer.Serialize(result, EvalSet.JsonOptions));
                Record(result);
                var n = Interlocked.Increment(ref completed);
                Console.WriteLine($"[{n}/{documents.Count}] WQMP {doc.WaterQualityManagementPlanID} ({doc.PdfType}, {doc.Pages} pp): " +
                                  (result.Succeeded ? $"ok in {result.ElapsedMs / 1000}s" : $"FAILED {result.Error}") +
                                  (result.Hiccups.Count > 0 ? $", {result.Hiccups.Count} hiccup(s)" : "") +
                                  $" · run total ${SpentSoFar():0.00}");
                return result;
            }
            finally
            {
                gate.Release();
            }
        });
        run.Documents = (await Task.WhenAll(tasks)).Where(r => r != null).Select(r => r!).OrderBy(r => r.Document.WaterQualityManagementPlanID).ToList();
        run.Skipped = skipped.OrderBy(x => x).ToList();
        run.StopReason = _stopReason;
        run.FinishedAt = DateTime.UtcNow;
        await SaveRunAsync(run);
        if (run.Skipped.Count > 0)
        {
            Console.WriteLine($"Stopped early ({_stopReason}); {run.Skipped.Count} document(s) not run.");
        }
        return run;
    }

    private Task SaveRunAsync(EvalRun run) =>
        File.WriteAllTextAsync(Path.Combine(runDirectory, "run.json"), JsonSerializer.Serialize(run, EvalSet.JsonOptions));

    private void Record(DocumentResult result)
    {
        lock (_spendLock)
        {
            _spent += Pricing.Cost(model, result.CallUsage);
            if (result.AccountIssue && _stopReason == null)
            {
                // No point continuing: every request will fail the same way until the account is fixed.
                _stopReason = $"Anthropic account issue: {result.Error}";
            }
        }
    }

    private decimal SpentSoFar()
    {
        lock (_spendLock) return _spent;
    }

    private string? StopReason()
    {
        lock (_spendLock)
        {
            if (_stopReason != null) return _stopReason;
            if (options.MaxCost.HasValue && _spent >= options.MaxCost.Value)
            {
                _stopReason = $"spending cap ${options.MaxCost:0.00} reached (${_spent:0.00} spent)";
            }
            return _stopReason;
        }
    }

    private async Task<DocumentResult> ExtractOneAsync(EvalDocument doc)
    {
        var sw = Stopwatch.StartNew();
        await using var scope = services.CreateAsyncScope();
        var extraction = scope.ServiceProvider.GetRequiredService<WqmpExtractionService>();
        try
        {
            var dto = await extraction.ExtractFromDocument(doc.WaterQualityManagementPlanDocumentID, options.PersonID, CancellationToken.None);
            return new DocumentResult
            {
                Document = doc,
                Succeeded = true,
                ElapsedMs = sw.ElapsedMilliseconds,
                FinalOutput = dto.FinalOutput,
                CallUsage = dto.CallUsage,
                Hiccups = dto.Hiccups,
            };
        }
        catch (Exception ex)
        {
            return new DocumentResult
            {
                Document = doc,
                Succeeded = false,
                ElapsedMs = sw.ElapsedMilliseconds,
                Error = $"{ex.GetType().Name}: {Truncate(ex.Message, 400)}",
                AccountIssue = AnthropicAccountIssue.IsAccountIssue(ex),
            };
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max] + "...";
}

public sealed class EvalRun
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string Directory { get; set; } = "";
    public string Label { get; set; } = "";
    public string Model { get; set; } = "";
    /// <summary>ClaudeEffort used, or "default" (the model's default). Older runs: null.</summary>
    public string? Effort { get; set; }
    public decimal? MaxCost { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    /// <summary>WQMPs not run because the spending cap was reached or the account failed.</summary>
    public List<int> Skipped { get; set; } = new();
    public string? StopReason { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public List<DocumentResult> Documents { get; set; } = new();

    public static async Task<EvalRun> LoadAsync(string directory)
    {
        var run = JsonSerializer.Deserialize<EvalRun>(await File.ReadAllTextAsync(Path.Combine(directory, "run.json")), EvalSet.JsonOptions)!;
        run.Directory = directory;
        foreach (var file in System.IO.Directory.GetFiles(Path.Combine(directory, "docs"), "*.json"))
        {
            var result = JsonSerializer.Deserialize<DocumentResult>(await File.ReadAllTextAsync(file), EvalSet.JsonOptions)!;
            // Runs saved before AccountIssue existed: recognize account failures from the error text.
            if (!result.Succeeded && !result.AccountIssue && result.Error != null
                && (result.Error.Contains("usage limit", StringComparison.OrdinalIgnoreCase)
                    || result.Error.Contains("credit balance", StringComparison.OrdinalIgnoreCase)
                    || result.Error.StartsWith("AnthropicUnauthorizedException") || result.Error.StartsWith("AnthropicForbiddenException")))
            {
                result.AccountIssue = true;
            }
            run.Documents.Add(result);
        }
        run.Documents = run.Documents.OrderBy(r => r.Document.WaterQualityManagementPlanID).ToList();
        return run;
    }
}

public sealed class DocumentResult
{
    public EvalDocument Document { get; set; } = new();
    public bool Succeeded { get; set; }
    public string? Error { get; set; }
    /// <summary>Failed for an account reason (credits, usage limit, key), not because of the document.</summary>
    public bool AccountIssue { get; set; }
    public long ElapsedMs { get; set; }
    public string? FinalOutput { get; set; }
    public List<WqmpExtractionCallUsageDto> CallUsage { get; set; } = new();
    public List<string> Hiccups { get; set; } = new();
}
