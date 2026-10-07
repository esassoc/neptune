using System.Globalization;
using System.Text;

namespace Neptune.Eval;

/// <summary>Writes scorecard.md (headline + per-field + per-document) and fields.csv (every comparison).</summary>
public static class ReportWriter
{
    private static readonly string[] PdfTypes = ["scanned", "mixed", "digital"];

    public static string Write(EvalRun run, List<DocumentScore> scores)
    {
        var md = new StringBuilder();
        md.AppendLine($"# Extraction eval: {run.Label}");
        md.AppendLine();
        md.AppendLine($"Model `{run.Model}` · effort `{run.Effort ?? "default"}` · started {run.StartedAt:yyyy-MM-dd HH:mm} UTC · {scores.Count} documents scored · scored {DateTime.Now:yyyy-MM-dd HH:mm}");
        var accountFailures = run.Documents.Where(r => r.AccountIssue).Select(r => r.Document.WaterQualityManagementPlanID).ToList();
        if (accountFailures.Count > 0 || run.Skipped.Count > 0)
        {
            md.AppendLine();
            md.AppendLine($"> **Incomplete run.** Not scored: {accountFailures.Count} document(s) failed for an Anthropic account reason " +
                          $"({string.Join(", ", accountFailures)}){(run.Skipped.Count > 0 ? $"; {run.Skipped.Count} not run ({string.Join(", ", run.Skipped)})" : "")}. " +
                          $"{run.StopReason ?? ""} Compare only the documents that ran.");
        }
        if (!Pricing.IsKnown(run.Model))
        {
            md.AppendLine();
            md.AppendLine($"> Cost shows $0: no price entry for `{run.Model}` in Pricing.cs.");
        }
        md.AppendLine();

        md.AppendLine("## Headline");
        md.AppendLine();
        md.AppendLine("Field accuracy = correct ÷ fields the WQMP has a value for, over the scored fields (info-only fields excluded; see Per field). " +
                      "Lenient also counts near-miss text (Close). Parcels, BMP types and source control BMPs are precision / recall against the hand-entered records, " +
                      $"scored only where the WQMP has records (parcels: 1-{Scorer.MaxScorableParcels}).");
        md.AppendLine();
        md.AppendLine("| Slice | Docs | Failed | Field accuracy | Lenient | Missed | Parcels P (R, info) | QuickBMP types P / R | SC present P / R | Hiccups | Avg cost | Avg time |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
        md.AppendLine(Headline("All", scores));
        foreach (var type in PdfTypes)
        {
            var slice = scores.Where(s => s.Result.Document.PdfType == type).ToList();
            if (slice.Count > 0) md.AppendLine(Headline(type, slice));
        }
        md.AppendLine();
        md.AppendLine($"Total cost **{Usd(scores.Sum(s => s.CostUsd))}** · QuickBMP count off by {Avg(scores.Select(s => (double)Math.Abs(s.QuickBmpCountExtracted - s.QuickBmpCountExpected))):0.0} on average · " +
                      $"{scores.Sum(s => s.UnmappedSourceControlNames)} source control names the wizard couldn't match to an attribute");
        md.AppendLine();

        md.AppendLine("## Per field");
        md.AppendLine();
        md.AppendLine("| Field | Has value | Correct | Close | Wrong | Missed | …unmapped | Extra | Accuracy | Scanned | Digital |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var field in EvalFields.All.Select(f => f.Key))
        {
            var all = scores.SelectMany(s => s.Fields.Where(f => f.Field == field)).ToList();
            var scorable = all.Count(f => f.Outcome is FieldOutcome.Correct or FieldOutcome.Close or FieldOutcome.Wrong or FieldOutcome.Missed);
            var label = EvalFields.InfoOnly.Contains(field) ? $"{field} *(info only)*" : field;
            md.AppendLine($"| {label} | {scorable} | {Count(all, FieldOutcome.Correct)} | {Count(all, FieldOutcome.Close)} | {Count(all, FieldOutcome.Wrong)} | " +
                          $"{Count(all, FieldOutcome.Missed)} | {all.Count(f => f.Unmapped && f.Outcome == FieldOutcome.Missed)} | {Count(all, FieldOutcome.Extra)} | " +
                          $"{Pct(FieldAccuracy(all, lenient: false))} | {Pct(FieldAccuracy(SliceFields(scores, "scanned", field), false))} | {Pct(FieldAccuracy(SliceFields(scores, "digital", field), false))} |");
        }
        md.AppendLine();

        var withHiccups = scores.Where(s => s.Result.Hiccups.Count > 0 || !s.Result.Succeeded).ToList();
        md.AppendLine("## Hiccups and failures");
        md.AppendLine();
        if (withHiccups.Count == 0)
        {
            md.AppendLine("None.");
        }
        foreach (var s in withHiccups)
        {
            var items = s.Result.Hiccups.ToList();
            if (!s.Result.Succeeded) items.Insert(0, $"**FAILED** {s.Result.Error}");
            md.AppendLine($"- WQMP {s.Result.Document.WaterQualityManagementPlanID} ({s.Result.Document.PdfType}, {s.Result.Document.Pages} pp): {string.Join("; ", items)}");
        }
        md.AppendLine();

        md.AppendLine("## Per document");
        md.AppendLine();
        md.AppendLine("| WQMP | Type | Pages | Field accuracy | Parcels P (R, info) | BMP types P / R | BMPs (exp/got) | SC P / R | Hiccups | Cost | Time | Stop reasons |");
        md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
        foreach (var s in scores)
        {
            var d = s.Result.Document;
            md.AppendLine($"| {d.WaterQualityManagementPlanID} | {d.PdfType} | {d.Pages} | {Pct(FieldAccuracy(s.Fields.Where(EvalFields.IsScored).ToList(), false))} | {PrInfoRecall(s.Parcels)} | {Pr(s.QuickBmpTypes)} | " +
                          $"{s.QuickBmpCountExpected}/{s.QuickBmpCountExtracted} | {Pr(s.SourceControlPresent)} | {s.Result.Hiccups.Count + (s.Result.Succeeded ? 0 : 1)} | " +
                          $"{Usd(s.CostUsd)} | {s.Result.ElapsedMs / 1000}s | {string.Join(", ", s.Result.CallUsage.Select(c => $"{c.Category}:{c.StopReason?.Trim('"')}"))} |");
        }

        var mdPath = Path.Combine(run.Directory, "scorecard.md");
        File.WriteAllText(mdPath, md.ToString());
        WriteFieldsCsv(Path.Combine(run.Directory, "fields.csv"), scores);
        return mdPath;
    }

    private static string Headline(string label, List<DocumentScore> slice)
    {
        var fields = slice.SelectMany(s => s.Fields).Where(EvalFields.IsScored).ToList();
        var scorable = fields.Count(f => f.Outcome is FieldOutcome.Correct or FieldOutcome.Close or FieldOutcome.Wrong or FieldOutcome.Missed);
        var missed = scorable == 0 ? (double?)null : (double)Count(fields, FieldOutcome.Missed) / scorable;
        return $"| {label} | {slice.Count} | {slice.Count(s => !s.Result.Succeeded)} | {Pct(FieldAccuracy(fields, false))} | {Pct(FieldAccuracy(fields, true))} | {Pct(missed)} | " +
               $"{PrInfoRecall(Sum(slice.Select(s => s.Parcels)))} | {Pr(Sum(slice.Select(s => s.QuickBmpTypes)))} | {Pr(Sum(slice.Select(s => s.SourceControlPresent)))} | " +
               $"{slice.Sum(s => s.Result.Hiccups.Count)} | {Usd(slice.Count == 0 ? 0 : slice.Average(s => s.CostUsd))} | {Avg(slice.Select(s => s.Result.ElapsedMs / 1000.0)):0}s |";
    }

    private static List<FieldScore> SliceFields(List<DocumentScore> scores, string type, string field) =>
        scores.Where(s => s.Result.Document.PdfType == type).SelectMany(s => s.Fields.Where(f => f.Field == field)).ToList();

    private static double? FieldAccuracy(List<FieldScore> fields, bool lenient)
    {
        var scorable = fields.Count(f => f.Outcome is FieldOutcome.Correct or FieldOutcome.Close or FieldOutcome.Wrong or FieldOutcome.Missed);
        if (scorable == 0) return null;
        var good = Count(fields, FieldOutcome.Correct) + (lenient ? Count(fields, FieldOutcome.Close) : 0);
        return (double)good / scorable;
    }

    private static SetScore Sum(IEnumerable<SetScore> sets)
    {
        var list = sets.ToList();
        return new SetScore(list.Sum(s => s.Expected), list.Sum(s => s.Extracted), list.Sum(s => s.Matched));
    }

    private static int Count(IEnumerable<FieldScore> fields, FieldOutcome outcome) => fields.Count(f => f.Outcome == outcome);
    private static string Pct(double? v) => v.HasValue ? v.Value.ToString("P0", CultureInfo.InvariantCulture) : "–";
    // Explicit "$": the :C format renders "¤" under the invariant culture Linux containers default to.
    private static string Usd(decimal v) => "$" + v.ToString("0.00", CultureInfo.InvariantCulture);
    // Parcels: recall is info-only, since records often list APNs the plan never mentions.
    private static string PrInfoRecall(SetScore s) => $"{Pct(s.Precision)} ({Pct(s.Recall)})";
    private static string Pr(SetScore s) => $"{Pct(s.Precision)} / {Pct(s.Recall)}";
    private static double Avg(IEnumerable<double> values) { var l = values.ToList(); return l.Count == 0 ? 0 : l.Average(); }

    private static void WriteFieldsCsv(string path, List<DocumentScore> scores)
    {
        static string Esc(string? s) => s == null ? "" : "\"" + s.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
        var csv = new StringBuilder("WaterQualityManagementPlanID,PdfType,Pages,Field,Outcome,Expected,Extracted\n");
        foreach (var s in scores)
        foreach (var f in s.Fields)
        {
            var d = s.Result.Document;
            csv.AppendLine($"{d.WaterQualityManagementPlanID},{d.PdfType},{d.Pages},{f.Field},{f.Outcome},{Esc(f.Expected)},{Esc(f.Extracted)}");
        }
        File.WriteAllText(path, csv.ToString());
    }
}
