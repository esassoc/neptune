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
public sealed class EvalRunner(IServiceProvider services, string runDirectory, string model, EvalOptions options)
{
    public async Task<EvalRun> RunAsync(List<EvalDocument> documents)
    {
        Directory.CreateDirectory(Path.Combine(runDirectory, "docs"));
        var run = new EvalRun
        {
            Directory = runDirectory,
            Label = options.Label,
            Model = model,
            StartedAt = DateTime.UtcNow,
        };
        await File.WriteAllTextAsync(Path.Combine(runDirectory, "run.json"), JsonSerializer.Serialize(run, EvalSet.JsonOptions));

        using var gate = new SemaphoreSlim(options.Concurrency);
        var completed = 0;
        var tasks = documents.Select(async doc =>
        {
            await gate.WaitAsync();
            try
            {
                var result = await ExtractOneAsync(doc);
                await File.WriteAllTextAsync(Path.Combine(runDirectory, "docs", $"{doc.WaterQualityManagementPlanID}.json"),
                    JsonSerializer.Serialize(result, EvalSet.JsonOptions));
                var n = Interlocked.Increment(ref completed);
                Console.WriteLine($"[{n}/{documents.Count}] WQMP {doc.WaterQualityManagementPlanID} ({doc.PdfType}, {doc.Pages} pp): " +
                                  (result.Succeeded ? $"ok in {result.ElapsedMs / 1000}s" : $"FAILED {result.Error}") +
                                  (result.Hiccups.Count > 0 ? $", {result.Hiccups.Count} hiccup(s)" : ""));
                return result;
            }
            finally
            {
                gate.Release();
            }
        });
        run.Documents = (await Task.WhenAll(tasks)).OrderBy(r => r.Document.WaterQualityManagementPlanID).ToList();
        run.FinishedAt = DateTime.UtcNow;
        await File.WriteAllTextAsync(Path.Combine(runDirectory, "run.json"),
            JsonSerializer.Serialize(new EvalRun
            {
                Directory = run.Directory, Label = run.Label, Model = run.Model, StartedAt = run.StartedAt, FinishedAt = run.FinishedAt,
            }, EvalSet.JsonOptions));
        return run;
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
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public List<DocumentResult> Documents { get; set; } = new();

    public static async Task<EvalRun> LoadAsync(string directory)
    {
        var run = JsonSerializer.Deserialize<EvalRun>(await File.ReadAllTextAsync(Path.Combine(directory, "run.json")), EvalSet.JsonOptions)!;
        run.Directory = directory;
        foreach (var file in System.IO.Directory.GetFiles(Path.Combine(directory, "docs"), "*.json"))
        {
            run.Documents.Add(JsonSerializer.Deserialize<DocumentResult>(await File.ReadAllTextAsync(file), EvalSet.JsonOptions)!);
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
    public long ElapsedMs { get; set; }
    public string? FinalOutput { get; set; }
    public List<WqmpExtractionCallUsageDto> CallUsage { get; set; } = new();
    public List<string> Hiccups { get; set; } = new();
}
