using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.Services;
using Neptune.EFModels.Entities;
using UglyToad.PdfPig;

namespace Neptune.Eval;

/// <summary>
/// Builds eval-set.json. Candidates are WQMPs with a Final WQMP PDF, at least 12 of the 17
/// main fields entered by hand, and parcels. Walks them in a seeded shuffle, downloads each PDF
/// to classify it (pages with under 100 non-space characters of text = no text layer), and fills
/// per-type quotas weighted toward scanned documents, which are most of the real corpus.
/// No Anthropic calls.
/// </summary>
public sealed class EvalSetSelector(NeptuneDbContext db, AzureBlobStorageService blobs)
{
    private const int FinalWqmpDocumentTypeID = 1;
    private const int MinFieldsFilled = 12;
    // Anthropic's per-request PDF limit is 600 pages; leave headroom.
    private const int MaxPages = 550;
    private const int MaxPerJurisdiction = 8;

    // 40 documents: ~half scanned (matches the corpus sample), with enough mixed and digital to
    // tell whether a change helps scans at digital's expense.
    private static readonly Dictionary<string, int> Quota = new() { ["scanned"] = 20, ["mixed"] = 8, ["digital"] = 12 };
    private const int MinAllFourCategories = 15;

    public async Task<EvalSet> SelectAsync(int seed)
    {
        var candidates = await LoadCandidatesAsync();
        Console.WriteLine($"{candidates.Count} candidate WQMPs ({candidates.Count(c => c.AllFour)} with all four categories).");

        var random = new Random(seed);
        // All-four-categories candidates first (shuffled), then the rest (shuffled), so the
        // all-four minimum fills early without starving the type quotas.
        var ordered = candidates.Where(c => c.AllFour).OrderBy(_ => random.Next())
            .Concat(candidates.Where(c => !c.AllFour).OrderBy(_ => random.Next()))
            .ToList();

        var chosen = new List<EvalDocument>();
        var perType = Quota.Keys.ToDictionary(k => k, _ => 0);
        var perJurisdiction = new Dictionary<int, int>();
        var tempDir = Directory.CreateTempSubdirectory("neptune-eval-select-");
        try
        {
            foreach (var c in ordered)
            {
                if (perType.All(kv => kv.Value >= Quota[kv.Key])) break;
                if (perJurisdiction.GetValueOrDefault(c.StormwaterJurisdictionID) >= MaxPerJurisdiction) continue;
                var allFourChosen = chosen.Count(d => d.HasQuickBMPs && d.HasSourceControlBMPs);
                var remainingSlots = Quota.Values.Sum() - chosen.Count;
                if (!c.AllFour && allFourChosen < MinAllFourCategories && remainingSlots <= MinAllFourCategories - allFourChosen) continue;

                var profile = await ClassifyAsync(c, tempDir.FullName);
                if (profile == null || profile.Value.Pages == 0 || profile.Value.Pages > MaxPages) continue;
                var type = profile.Value.Type;
                if (perType[type] >= Quota[type]) continue;

                perType[type]++;
                perJurisdiction[c.StormwaterJurisdictionID] = perJurisdiction.GetValueOrDefault(c.StormwaterJurisdictionID) + 1;
                chosen.Add(new EvalDocument
                {
                    WaterQualityManagementPlanID = c.WaterQualityManagementPlanID,
                    WaterQualityManagementPlanDocumentID = c.WaterQualityManagementPlanDocumentID,
                    PdfType = type,
                    Pages = profile.Value.Pages,
                    SizeMB = (int)(c.ContentLength / (1024 * 1024)),
                    StormwaterJurisdictionID = c.StormwaterJurisdictionID,
                    HasQuickBMPs = c.QuickBMPs > 0,
                    HasSourceControlBMPs = c.SourceControlPresent > 0,
                });
                Console.WriteLine($"  + WQMP {c.WaterQualityManagementPlanID}: {type}, {profile.Value.Pages} pp " +
                                  $"({string.Join(", ", perType.Select(kv => $"{kv.Key} {kv.Value}/{Quota[kv.Key]}"))})");
            }
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }

        Split(chosen);
        return new EvalSet
        {
            Description = $"NPT-1132 extraction eval set. Selected {DateTime.Now:yyyy-MM-dd} with seed {seed} from WQMPs with a Final WQMP PDF, " +
                          $">= {MinFieldsFilled} of 17 hand-entered fields and parcels. PdfType: scanned = >= 80% of pages have no text layer, " +
                          "mixed = 20-79%, digital = < 20%. Regenerate with: dotnet run --project Neptune.Eval -- select",
            Documents = chosen.OrderBy(d => d.Split).ThenBy(d => d.PdfType).ThenBy(d => d.WaterQualityManagementPlanID).ToList(),
        };
    }

    /// <summary>Within each PDF type, 3 of every 5 go to dev and 2 to test, interleaved so both splits span sizes.</summary>
    private static void Split(List<EvalDocument> chosen)
    {
        foreach (var group in chosen.GroupBy(d => d.PdfType))
        {
            var i = 0;
            foreach (var d in group.OrderBy(d => d.Pages))
            {
                d.Split = (i % 5) is 1 or 3 ? "test" : "dev";
                i++;
            }
        }
    }

    private async Task<(string Type, int Pages)?> ClassifyAsync(Candidate c, string tempDir)
    {
        var path = Path.Combine(tempDir, $"{c.WaterQualityManagementPlanDocumentID}.pdf");
        try
        {
            await blobs.DownloadBlobToFileAsync(c.BlobName, path);
            using var pdf = PdfDocument.Open(path);
            var pages = 0;
            var textless = 0;
            foreach (var page in pdf.GetPages())
            {
                pages++;
                if (page.Text.Count(ch => !char.IsWhiteSpace(ch)) < 100) textless++;
            }
            if (pages == 0) return null;
            var share = (double)textless / pages;
            var type = share >= 0.8 ? "scanned" : share >= 0.2 ? "mixed" : "digital";
            return (type, pages);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  - WQMP {c.WaterQualityManagementPlanID}: skipped ({ex.GetType().Name}: {ex.Message.Split('\n')[0]})");
            return null;
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private async Task<List<Candidate>> LoadCandidatesAsync()
    {
        var pdfs = await (from d in db.WaterQualityManagementPlanDocuments.AsNoTracking()
                          join f in db.FileResources.AsNoTracking() on d.FileResourceID equals f.FileResourceID
                          where d.WaterQualityManagementPlanDocumentTypeID == FinalWqmpDocumentTypeID && f.OriginalFileExtension == ".pdf"
                          select new { d.WaterQualityManagementPlanID, d.WaterQualityManagementPlanDocumentID, f.FileResourceGUID, f.ContentLength })
            .ToListAsync();
        // One PDF per WQMP: the largest, which is almost always the full plan rather than an excerpt.
        var pdfByWqmp = pdfs.GroupBy(p => p.WaterQualityManagementPlanID).ToDictionary(g => g.Key, g => g.OrderByDescending(p => p.ContentLength).First());

        var parcelCounts = await db.WaterQualityManagementPlanParcels.AsNoTracking()
            .GroupBy(x => x.WaterQualityManagementPlanID).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var bmpCounts = await db.QuickBMPs.AsNoTracking()
            .GroupBy(x => x.WaterQualityManagementPlanID).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var scCounts = await db.SourceControlBMPs.AsNoTracking().Where(x => x.IsPresent == true)
            .GroupBy(x => x.WaterQualityManagementPlanID).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N);
        var wqmps = await db.WaterQualityManagementPlans.AsNoTracking().Where(w => pdfByWqmp.Keys.Contains(w.WaterQualityManagementPlanID)).ToListAsync();

        return wqmps
            .Where(w => FieldsFilled(w) >= MinFieldsFilled && parcelCounts.GetValueOrDefault(w.WaterQualityManagementPlanID) > 0)
            .Select(w =>
            {
                var pdf = pdfByWqmp[w.WaterQualityManagementPlanID];
                return new Candidate(w.WaterQualityManagementPlanID, pdf.WaterQualityManagementPlanDocumentID, pdf.FileResourceGUID.ToString().ToLower(),
                    pdf.ContentLength, w.StormwaterJurisdictionID, bmpCounts.GetValueOrDefault(w.WaterQualityManagementPlanID),
                    scCounts.GetValueOrDefault(w.WaterQualityManagementPlanID));
            })
            .ToList();
    }

    private static int FieldsFilled(WaterQualityManagementPlan w) => new object?[]
    {
        w.ApprovalDate, w.DateOfConstruction, w.HydrologicSubareaID, w.HydromodificationAppliesTypeID,
        NullIfBlank(w.MaintenanceContactName), NullIfBlank(w.MaintenanceContactOrganization), NullIfBlank(w.MaintenanceContactPhone),
        NullIfBlank(w.MaintenanceContactAddress1), NullIfBlank(w.MaintenanceContactCity), NullIfBlank(w.MaintenanceContactZip),
        w.RecordedWQMPAreaInAcres, NullIfBlank(w.RecordNumber), w.WaterQualityManagementPlanLandUseID, w.WaterQualityManagementPlanPriorityID,
        w.WaterQualityManagementPlanDevelopmentTypeID, w.WaterQualityManagementPlanPermitTermID, w.TrashCaptureStatusTypeID,
    }.Count(v => v != null);

    private static string? NullIfBlank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private sealed record Candidate(int WaterQualityManagementPlanID, int WaterQualityManagementPlanDocumentID, string BlobName,
        long ContentLength, int StormwaterJurisdictionID, int QuickBMPs, int SourceControlPresent)
    {
        public bool AllFour => QuickBMPs > 0 && SourceControlPresent > 0;
    }

    public static void Save(EvalSet set, string path) => File.WriteAllText(path, JsonSerializer.Serialize(set, EvalSet.JsonOptions) + Environment.NewLine);
}
