using System.Text.Json;

namespace Neptune.Eval;

/// <summary>
/// The fixed document list (eval-set.json). Built by select-eval-set.sh from WQMPs whose
/// fields, parcels and BMPs were entered by hand, stratified by PDF type. PDFs themselves are
/// never stored in the repo; the runner reads them through the normal blob path.
/// </summary>
public sealed class EvalSet
{
    public string Description { get; set; } = "";
    public List<EvalDocument> Documents { get; set; } = new();

    public static EvalSet Load(string path)
    {
        var set = JsonSerializer.Deserialize<EvalSet>(File.ReadAllText(path), JsonOptions)
                  ?? throw new InvalidOperationException($"Could not read {path}");
        return set;
    }

    public List<EvalDocument> Select(string split, HashSet<int>? ids, int? limit)
    {
        IEnumerable<EvalDocument> docs = Documents;
        if (ids != null)
        {
            docs = docs.Where(d => ids.Contains(d.WaterQualityManagementPlanID));
        }
        else if (split != "all")
        {
            docs = docs.Where(d => string.Equals(d.Split, split, StringComparison.OrdinalIgnoreCase));
        }
        if (limit.HasValue)
        {
            docs = docs.Take(limit.Value);
        }
        return docs.ToList();
    }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
}

public sealed class EvalDocument
{
    public int WaterQualityManagementPlanID { get; set; }
    public int WaterQualityManagementPlanDocumentID { get; set; }
    /// <summary>scanned (no text layer), mixed (scanned sections), or digital.</summary>
    public string PdfType { get; set; } = "";
    public int Pages { get; set; }
    public int SizeMB { get; set; }
    public int StormwaterJurisdictionID { get; set; }
    public bool HasQuickBMPs { get; set; }
    public bool HasSourceControlBMPs { get; set; }
    /// <summary>dev (iterate against) or test (held out; only for confirming a final choice).</summary>
    public string Split { get; set; } = "dev";
}
