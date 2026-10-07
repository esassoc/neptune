using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Neptune.EFModels.Entities;

namespace Neptune.Eval;

public enum FieldOutcome
{
    /// <summary>Extracted value equals the hand-entered value (after the wizard's normalization).</summary>
    Correct,
    /// <summary>Text field that's nearly right (e.g. "Suite 100" missing); the reviewer would tweak it.</summary>
    Close,
    Wrong,
    /// <summary>The WQMP has a value; the extraction gave nothing usable (including a lookup label the wizard can't map).</summary>
    Missed,
    /// <summary>The WQMP has no value but the extraction produced one. Not scored: the hand-entered data may simply be incomplete.</summary>
    Extra,
    BothEmpty,
}

public sealed record FieldScore(string Field, FieldOutcome Outcome, string? Expected, string? Extracted, bool Unmapped);

public sealed record SetScore(int Expected, int Extracted, int Matched)
{
    /// <summary>Category has no usable ground truth for this WQMP; excluded from precision/recall.</summary>
    public static readonly SetScore NotScored = new(0, 0, 0);

    public double? Precision => Extracted == 0 ? null : (double)Matched / Extracted;
    public double? Recall => Expected == 0 ? null : (double)Matched / Expected;
}

public sealed class DocumentScore
{
    public DocumentResult Result { get; init; } = null!;
    public List<FieldScore> Fields { get; init; } = new();
    public SetScore Parcels { get; init; } = new(0, 0, 0);
    public SetScore QuickBmpTypes { get; init; } = new(0, 0, 0);
    public int QuickBmpCountExpected { get; init; }
    public int QuickBmpCountExtracted { get; init; }
    public SetScore SourceControlPresent { get; init; } = new(0, 0, 0);
    public int UnmappedSourceControlNames { get; init; }
    public decimal CostUsd { get; init; }
}

/// <summary>
/// Scores an extraction the way the review wizard (wqmp-review.component.ts makeField /
/// buildSourceControlRowsFromParsed) would apply it, against the WQMP's hand-entered values.
/// Lookups only count when the extracted label matches an option label case-insensitively,
/// because that's the only way the wizard auto-fills them.
/// </summary>
public static class Scorer
{
    public const int MaxScorableParcels = 10;

    public static async Task<List<DocumentScore>> ScoreRunAsync(NeptuneDbContext db, EvalRun run)
    {
        var lookups = await Lookups.LoadAsync(db);
        var scores = new List<DocumentScore>();
        // Account failures (credits, usage limit, key) say nothing about extraction; the report
        // lists them separately instead of scoring them as misses.
        foreach (var result in run.Documents.Where(r => !r.AccountIssue))
        {
            var truth = await GroundTruth.LoadAsync(db, result.Document.WaterQualityManagementPlanID);
            scores.Add(Score(result, truth, lookups, run.Model));
        }
        return scores;
    }

    private static DocumentScore Score(DocumentResult result, GroundTruth truth, Lookups lookups, string model)
    {
        var cost = Pricing.Cost(model, result.CallUsage);
        if (!result.Succeeded || string.IsNullOrEmpty(result.FinalOutput))
        {
            // A failed extraction misses everything the WQMP has.
            return new DocumentScore
            {
                Result = result,
                Fields = EvalFields.All.Select(f => f.Score(truth, null, lookups)).ToList(),
                Parcels = truth.Apns.Count is > 0 and <= MaxScorableParcels ? new SetScore(truth.Apns.Count, 0, 0) : SetScore.NotScored,
                QuickBmpTypes = truth.QuickBmpTypeIDs.Count > 0 ? new SetScore(truth.QuickBmpTypeIDs.Count, 0, 0) : SetScore.NotScored,
                QuickBmpCountExpected = truth.QuickBmpTypeIDs.Count,
                SourceControlPresent = truth.SourceControlPresentIDs.Count > 0 ? new SetScore(truth.SourceControlPresentIDs.Count, 0, 0) : SetScore.NotScored,
                CostUsd = cost,
            };
        }

        using var doc = JsonDocument.Parse(result.FinalOutput);
        var root = doc.RootElement;
        var wqmp = root.TryGetProperty("WQMP", out var w) ? w : default;

        var fields = EvalFields.All.Select(f => f.Score(truth, ExtractedValue(wqmp, f.Key), lookups)).ToList();

        // Exact (trimmed) string match, like the wizard's Parcels.LookupByParcelNumbers: an APN
        // in a format the lookup can't resolve is a miss for the reviewer too.
        var extractedApns = Items(root, "Parcels")
            .Select(p => ExtractedValue(p, "ParcelNumber")?.Trim())
            .Where(a => !string.IsNullOrEmpty(a))
            .Select(a => a!)
            .Distinct()
            .ToList();
        // WQMPs with many parcels usually got them from post-construction parcel splits the plan
        // predates (1830: 55 APNs on the record, none in its 12-page PDF), so only score parcels
        // where the record has a list a plan could plausibly contain. Even then, recall is
        // info-only (see ReportWriter): records often list APNs the plan never mentions (WQMP
        // 2760's three APNs appear nowhere in its text), so only precision drives decisions.
        var parcels = truth.Apns.Count is > 0 and <= MaxScorableParcels ? SetMatch(truth.Apns, extractedApns) : SetScore.NotScored;

        var extractedBmpTypeIDs = new List<int>();
        var extractedBmpCount = 0;
        foreach (var bmp in Items(root, "QuickBMPs"))
        {
            extractedBmpCount++;
            var typeLabel = ExtractedValue(bmp, "TreatmentBMPType");
            if (typeLabel != null && lookups.TreatmentBmpTypeIDByName.TryGetValue(typeLabel.Trim(), out var typeID))
            {
                extractedBmpTypeIDs.Add(typeID);
            }
        }
        // No hand-entered records = nothing to score against (the data entry may simply never
        // have covered this category), not 0% precision.
        var bmpTypes = truth.QuickBmpTypeIDs.Count > 0 ? MultisetMatch(truth.QuickBmpTypeIDs, extractedBmpTypeIDs, extractedBmpCount) : SetScore.NotScored;

        var presentIDs = new List<int>();
        var unmappedSc = 0;
        foreach (var sc in Items(root, "SourceControlBMPs"))
        {
            var name = ExtractedValue(sc, "SourceControlBMPAttribute");
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!lookups.SourceControlAttributeIDByName.TryGetValue(name.Trim(), out var attributeID))
            {
                unmappedSc++;
                continue;
            }
            var isPresent = ExtractedValue(sc, "IsPresent")?.Trim().ToLowerInvariant();
            if (isPresent is "yes" or "true")
            {
                presentIDs.Add(attributeID);
            }
        }
        var scPresent = truth.SourceControlPresentIDs.Count > 0 ? SetMatch(truth.SourceControlPresentIDs, presentIDs.Distinct().ToList()) : SetScore.NotScored;

        return new DocumentScore
        {
            Result = result,
            Fields = fields,
            Parcels = parcels,
            QuickBmpTypes = bmpTypes,
            QuickBmpCountExpected = truth.QuickBmpTypeIDs.Count,
            QuickBmpCountExtracted = extractedBmpCount,
            SourceControlPresent = scPresent,
            UnmappedSourceControlNames = unmappedSc,
            CostUsd = cost,
        };
    }

    /// <summary>The "Value" of an ExtractedValue object, or null.</summary>
    private static string? ExtractedValue(JsonElement parent, string key)
    {
        if (parent.ValueKind != JsonValueKind.Object || !parent.TryGetProperty(key, out var node) || node.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        if (!node.TryGetProperty("Value", out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? null : value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
            _ => null,
        };
    }

    private static IEnumerable<JsonElement> Items(JsonElement root, string key) =>
        root.TryGetProperty(key, out var arr) && arr.ValueKind == JsonValueKind.Array ? arr.EnumerateArray() : [];

    private static SetScore SetMatch(IReadOnlyCollection<string> expected, IReadOnlyCollection<string> extracted) =>
        new(expected.Count, extracted.Count, extracted.Count(expected.Contains));

    private static SetScore SetMatch(IReadOnlyCollection<int> expected, IReadOnlyCollection<int> extracted) =>
        new(expected.Count, extracted.Count, extracted.Count(expected.Contains));

    /// <summary>Multiset intersection; unmapped extracted entries still count against precision.</summary>
    private static SetScore MultisetMatch(List<int> expected, List<int> extractedMapped, int extractedTotal)
    {
        var remaining = expected.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        var matched = 0;
        foreach (var id in extractedMapped)
        {
            if (remaining.TryGetValue(id, out var n) && n > 0)
            {
                remaining[id] = n - 1;
                matched++;
            }
        }
        return new SetScore(expected.Count, extractedTotal, matched);
    }

    internal static string Digits(string? s) => s == null ? "" : Regex.Replace(s, "[^0-9]", "");

    internal static string NormalizeText(string s) =>
        Regex.Replace(Regex.Replace(s.ToLowerInvariant(), @"[^\p{L}\p{N}\s]", " "), @"\s+", " ").Trim();
}

/// <summary>One scored WQMP field: how to read the truth and how the wizard would map the extracted text.</summary>
public sealed class FieldDefinition
{
    public string Key { get; init; } = "";
    public Func<GroundTruth, Lookups, string?> Expected { get; init; } = null!;
    /// <summary>Wizard mapping of the raw extracted text; returns (value, unmapped).</summary>
    public Func<string, Lookups, (string? Value, bool Unmapped)> Map { get; init; } = (s, _) => (s, false);
    public Func<string, string, FieldOutcome> Compare { get; init; } = (a, b) => a == b ? FieldOutcome.Correct : FieldOutcome.Wrong;

    public FieldScore Score(GroundTruth truth, string? rawExtracted, Lookups lookups)
    {
        var expected = Expected(truth, lookups);
        string? extracted = null;
        var unmapped = false;
        if (rawExtracted != null)
        {
            (extracted, unmapped) = Map(rawExtracted, lookups);
        }

        var hasExpected = !string.IsNullOrWhiteSpace(expected);
        var hasExtracted = !string.IsNullOrWhiteSpace(extracted);
        var outcome = (hasExpected, hasExtracted) switch
        {
            (false, false) => FieldOutcome.BothEmpty,
            (false, true) => FieldOutcome.Extra,
            (true, false) => FieldOutcome.Missed,
            _ => Compare(expected!, extracted!),
        };
        return new FieldScore(Key, outcome, expected, unmapped ? $"(unmapped) {rawExtracted}" : extracted ?? rawExtracted, unmapped);
    }
}

public static class EvalFields
{
    /// <summary>
    /// Reported but excluded from accuracy: the hand-entered data doesn't follow one convention
    /// (Record Number holds the WQMP number, a grading permit number or a project name; the
    /// maintenance address is sometimes the site, sometimes the contact's mailing address), so a
    /// disagreement often isn't an extraction error. Priority: Neptune's High / Low may not be the
    /// document's "Priority / Non-Priority Project" designation (WQMP 3239's cover says
    /// Non-Priority, its record says High). Revisit once the conventions are settled.
    /// </summary>
    public static readonly HashSet<string> InfoOnly =
    [
        "RecordNumber", "WaterQualityManagementPlanPriority", "MaintenanceContactAddress1", "MaintenanceContactAddress2", "MaintenanceContactCity",
        "MaintenanceContactState", "MaintenanceContactZip",
    ];

    public static bool IsScored(FieldScore f) => !InfoOnly.Contains(f.Field);

    // The 18 fields the review wizard fills from the extraction (locationFields + basicsFields,
    // minus Jurisdiction / WQMP Name, which the uploader enters and the wizard overrides, and
    // Modeling Approach / Trash Capture Effectiveness, which the extraction doesn't produce).
    public static readonly List<FieldDefinition> All =
    [
        Lookup("HydrologicSubarea", (t, l) => t.Wqmp.HydrologicSubareaID, l => l.HydrologicSubareaIDByName),
        Acres("RecordedWQMPAreaInAcres"),
        Lookup("WaterQualityManagementPlanPriority", (t, _) => t.Wqmp.WaterQualityManagementPlanPriorityID, l => l.PriorityIDByName),
        Lookup("WaterQualityManagementPlanDevelopmentType", (t, _) => t.Wqmp.WaterQualityManagementPlanDevelopmentTypeID, l => l.DevelopmentTypeIDByName),
        Lookup("WaterQualityManagementPlanLandUse", (t, _) => t.Wqmp.WaterQualityManagementPlanLandUseID, l => l.LandUseIDByName),
        Lookup("WaterQualityManagementPlanPermitTerm", (t, _) => t.Wqmp.WaterQualityManagementPlanPermitTermID, l => l.PermitTermIDByName),
        Date("ApprovalDate", t => t.Wqmp.ApprovalDate),
        Date("DateOfConstruction", t => t.Wqmp.DateOfConstruction),
        Lookup("HydromodificationAppliesType", (t, _) => t.Wqmp.HydromodificationAppliesTypeID, l => l.HydromodificationIDByName),
        Text("RecordNumber", t => t.Wqmp.RecordNumber),
        TrashCaptureStatus(),
        Text("MaintenanceContactName", t => t.Wqmp.MaintenanceContactName),
        Text("MaintenanceContactOrganization", t => t.Wqmp.MaintenanceContactOrganization),
        new FieldDefinition
        {
            Key = "MaintenanceContactPhone",
            Expected = (t, _) => LastTen(Scorer.Digits(t.Wqmp.MaintenanceContactPhone)),
            Map = (s, _) => (LastTen(Scorer.Digits(s)), false),
        },
        Text("MaintenanceContactAddress1", t => t.Wqmp.MaintenanceContactAddress1),
        Text("MaintenanceContactAddress2", t => t.Wqmp.MaintenanceContactAddress2),
        Text("MaintenanceContactCity", t => t.Wqmp.MaintenanceContactCity),
        new FieldDefinition
        {
            Key = "MaintenanceContactState",
            Expected = (t, _) => UsStates.ToCode(t.Wqmp.MaintenanceContactState),
            Map = (s, _) => UsStates.ToCode(s) is { } code ? (code, false) : (null, true),
        },
        new FieldDefinition
        {
            Key = "MaintenanceContactZip",
            Expected = (t, _) => FirstFive(Scorer.Digits(t.Wqmp.MaintenanceContactZip)),
            Map = (s, _) => (FirstFive(Scorer.Digits(s)), false),
        },
    ];

    private static FieldDefinition Lookup(string key, Func<GroundTruth, Lookups, int?> truthID, Func<Lookups, Dictionary<string, int>> options) => new()
    {
        Key = key,
        Expected = (t, l) => truthID(t, l)?.ToString(),
        Map = (s, l) => options(l).TryGetValue(s.Trim(), out var id) ? (id.ToString(), false) : (null, true),
    };

    /// <summary>
    /// "No Trash Capture" and "Not Provided" score as the same answer (no trash capture device in
    /// the plan): the records use them interchangeably. Plans with only LID/bioretention are
    /// recorded either way (3132 vs 1752), so penalizing the model's choice would measure the
    /// data entry, not the extraction. Decided 2026-10-07.
    /// </summary>
    private static FieldDefinition TrashCaptureStatus()
    {
        var noDevice = new HashSet<string> { ((int)TrashCaptureStatusTypeEnum.None).ToString(), ((int)TrashCaptureStatusTypeEnum.NotProvided).ToString() };
        var field = Lookup("TrashCaptureStatusType", (t, _) => t.Wqmp.TrashCaptureStatusTypeID, l => l.TrashCaptureStatusIDByName);
        return new FieldDefinition
        {
            Key = field.Key,
            Expected = field.Expected,
            Map = field.Map,
            Compare = (expected, extracted) => expected == extracted || (noDevice.Contains(expected) && noDevice.Contains(extracted))
                ? FieldOutcome.Correct
                : FieldOutcome.Wrong,
        };
    }

    private static FieldDefinition Date(string key, Func<GroundTruth, DateTime?> truth) => new()
    {
        Key = key,
        Expected = (t, _) => truth(t)?.ToString("yyyy-MM-dd"),
        Map = (s, _) => DateTime.TryParse(s, CultureInfo.GetCultureInfo("en-US"), DateTimeStyles.AllowWhiteSpaces, out var d)
            ? (d.ToString("yyyy-MM-dd"), false)
            : (null, true),
    };

    private static FieldDefinition Acres(string key) => new()
    {
        Key = key,
        Expected = (t, _) => t.Wqmp.RecordedWQMPAreaInAcres?.ToString("0.##", CultureInfo.InvariantCulture),
        // Wizard rounds to 2 decimals (decimal(6,2) column).
        Map = (s, _) => decimal.TryParse(Regex.Replace(s, "[^0-9.]", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var v)
            ? (Math.Round(v, 2).ToString("0.##", CultureInfo.InvariantCulture), false)
            : (null, true),
        // Compare at the precision the record was entered with: "2.4" on the WQMP vs "2.44" in
        // the PDF is the same area, entered to one decimal.
        Compare = (expected, extracted) =>
        {
            var e = decimal.Parse(expected, CultureInfo.InvariantCulture);
            var x = decimal.Parse(extracted, CultureInfo.InvariantCulture);
            var decimals = expected.Contains('.') ? expected.Length - expected.IndexOf('.') - 1 : 0;
            return Math.Round(x, decimals, MidpointRounding.AwayFromZero) == e || Math.Abs(x - e) <= 0.01m
                ? FieldOutcome.Correct
                : FieldOutcome.Wrong;
        },
    };

    private static FieldDefinition Text(string key, Func<GroundTruth, string?> truth) => new()
    {
        Key = key,
        Expected = (t, _) => truth(t),
        Compare = CompareText,
    };

    private static FieldOutcome CompareText(string expected, string extracted)
    {
        var a = Scorer.NormalizeText(expected);
        var b = Scorer.NormalizeText(extracted);
        if (a == b) return FieldOutcome.Correct;
        if (a.Length > 0 && b.Length > 0 && (a.Contains(b) || b.Contains(a))) return FieldOutcome.Close;
        var ta = a.Split(' ').ToHashSet();
        var tb = b.Split(' ').ToHashSet();
        var jaccard = (double)ta.Intersect(tb).Count() / ta.Union(tb).Count();
        return jaccard >= 0.6 ? FieldOutcome.Close : FieldOutcome.Wrong;
    }

    private static string? LastTen(string digits) => digits.Length >= 10 ? digits[^10..] : (digits.Length == 0 ? null : digits);
    private static string? FirstFive(string digits) => digits.Length >= 5 ? digits[..5] : (digits.Length == 0 ? null : digits);
}

/// <summary>Hand-entered values for one WQMP.</summary>
public sealed class GroundTruth
{
    public WaterQualityManagementPlan Wqmp { get; init; } = null!;
    public List<string> Apns { get; init; } = new();
    public List<int> QuickBmpTypeIDs { get; init; } = new();
    public List<int> SourceControlPresentIDs { get; init; } = new();

    public static async Task<GroundTruth> LoadAsync(NeptuneDbContext db, int wqmpID)
    {
        var wqmp = await db.WaterQualityManagementPlans.AsNoTracking().SingleAsync(x => x.WaterQualityManagementPlanID == wqmpID);
        var parcelIDs = await db.WaterQualityManagementPlanParcels.AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == wqmpID).Select(x => x.ParcelID).ToListAsync();
        var apns = await db.Parcels.AsNoTracking().Where(x => parcelIDs.Contains(x.ParcelID)).Select(x => x.ParcelNumber).ToListAsync();
        return new GroundTruth
        {
            Wqmp = wqmp,
            Apns = apns.Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a!.Trim()).Distinct().ToList(),
            QuickBmpTypeIDs = await db.QuickBMPs.AsNoTracking().Where(x => x.WaterQualityManagementPlanID == wqmpID).Select(x => x.TreatmentBMPTypeID).ToListAsync(),
            SourceControlPresentIDs = await db.SourceControlBMPs.AsNoTracking()
                .Where(x => x.WaterQualityManagementPlanID == wqmpID && x.IsPresent == true)
                .Select(x => x.SourceControlBMPAttributeID).Distinct().ToListAsync(),
        };
    }
}

/// <summary>Option label → ID maps, case-insensitive like the wizard's dropdown matching.</summary>
public sealed class Lookups
{
    public Dictionary<string, int> HydrologicSubareaIDByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> TreatmentBmpTypeIDByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> SourceControlAttributeIDByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> PriorityIDByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> DevelopmentTypeIDByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> LandUseIDByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> PermitTermIDByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> HydromodificationIDByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> TrashCaptureStatusIDByName { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<Lookups> LoadAsync(NeptuneDbContext db)
    {
        static Dictionary<string, int> Map<T>(IEnumerable<T> items, Func<T, string?> name, Func<T, int> id) =>
            items.Where(x => !string.IsNullOrWhiteSpace(name(x)))
                .GroupBy(x => name(x)!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => id(g.First()), StringComparer.OrdinalIgnoreCase);

        return new Lookups
        {
            HydrologicSubareaIDByName = Map(await db.HydrologicSubareas.AsNoTracking().ToListAsync(), x => x.HydrologicSubareaName, x => x.HydrologicSubareaID),
            TreatmentBmpTypeIDByName = Map(await db.TreatmentBMPTypes.AsNoTracking().ToListAsync(), x => x.TreatmentBMPTypeName, x => x.TreatmentBMPTypeID),
            SourceControlAttributeIDByName = Map(await db.SourceControlBMPAttributes.AsNoTracking().ToListAsync(), x => x.SourceControlBMPAttributeName, x => x.SourceControlBMPAttributeID),
            PriorityIDByName = Map(WaterQualityManagementPlanPriority.All, x => x.WaterQualityManagementPlanPriorityDisplayName, x => x.WaterQualityManagementPlanPriorityID),
            DevelopmentTypeIDByName = Map(WaterQualityManagementPlanDevelopmentType.All, x => x.WaterQualityManagementPlanDevelopmentTypeDisplayName, x => x.WaterQualityManagementPlanDevelopmentTypeID),
            LandUseIDByName = Map(WaterQualityManagementPlanLandUse.All, x => x.WaterQualityManagementPlanLandUseDisplayName, x => x.WaterQualityManagementPlanLandUseID),
            PermitTermIDByName = Map(WaterQualityManagementPlanPermitTerm.All, x => x.WaterQualityManagementPlanPermitTermDisplayName, x => x.WaterQualityManagementPlanPermitTermID),
            HydromodificationIDByName = Map(HydromodificationAppliesType.All, x => x.HydromodificationAppliesTypeDisplayName, x => x.HydromodificationAppliesTypeID),
            TrashCaptureStatusIDByName = Map(TrashCaptureStatusType.All, x => x.TrashCaptureStatusTypeDisplayName, x => x.TrashCaptureStatusTypeID),
        };
    }
}

/// <summary>The wizard's MaintenanceContactState coercion: 2-letter code or full state name.</summary>
public static class UsStates
{
    private static readonly Dictionary<string, string> CodeByName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alabama"] = "AL", ["Alaska"] = "AK", ["Arizona"] = "AZ", ["Arkansas"] = "AR", ["California"] = "CA", ["Colorado"] = "CO",
        ["Connecticut"] = "CT", ["Delaware"] = "DE", ["District of Columbia"] = "DC", ["Florida"] = "FL", ["Georgia"] = "GA",
        ["Hawaii"] = "HI", ["Idaho"] = "ID", ["Illinois"] = "IL", ["Indiana"] = "IN", ["Iowa"] = "IA", ["Kansas"] = "KS",
        ["Kentucky"] = "KY", ["Louisiana"] = "LA", ["Maine"] = "ME", ["Maryland"] = "MD", ["Massachusetts"] = "MA",
        ["Michigan"] = "MI", ["Minnesota"] = "MN", ["Mississippi"] = "MS", ["Missouri"] = "MO", ["Montana"] = "MT",
        ["Nebraska"] = "NE", ["Nevada"] = "NV", ["New Hampshire"] = "NH", ["New Jersey"] = "NJ", ["New Mexico"] = "NM",
        ["New York"] = "NY", ["North Carolina"] = "NC", ["North Dakota"] = "ND", ["Ohio"] = "OH", ["Oklahoma"] = "OK",
        ["Oregon"] = "OR", ["Pennsylvania"] = "PA", ["Rhode Island"] = "RI", ["South Carolina"] = "SC", ["South Dakota"] = "SD",
        ["Tennessee"] = "TN", ["Texas"] = "TX", ["Utah"] = "UT", ["Vermont"] = "VT", ["Virginia"] = "VA", ["Washington"] = "WA",
        ["West Virginia"] = "WV", ["Wisconsin"] = "WI", ["Wyoming"] = "WY",
    };

    public static string? ToCode(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        var t = s.Trim().TrimEnd('.');
        if (t.Length == 2 && CodeByName.ContainsValue(t.ToUpperInvariant())) return t.ToUpperInvariant();
        return CodeByName.TryGetValue(t, out var code) ? code : null;
    }
}
