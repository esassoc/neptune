// NPT-1105 Part 2 — NTS overlay benchmark spike.
// Reproduces ComputeTrashGeneratingUnits.py (clean -> flatten -> union -> cleanup) in
// NetTopologySuite against the captured golden TGU inputs. Goal: wall time + memory vs the
// QGIS 3.28 baseline (~29 min python phase). Parity here is approximate (no self-snap,
// different tie-break iteration order) — exact parity engineering is the port's job; the
// compare_geojson.py envelope tells us how close we land.
//
// Usage: dotnet run -c Release -- <inputFolder> <runPrefix> <outputGeoJsonPath>
//   inputFolder: folder containing <prefix>{delineation,ovta,wqmp,landUseBlock}Layer.geojson

using System.Diagnostics;
using System.Text.Json;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.IO.Converters;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;

var totalStopwatch = Stopwatch.StartNew();

if (args.Length != 3)
{
    Console.Error.WriteLine("Usage: NtsOverlaySpike <inputFolder> <runPrefix> <outputGeoJsonPath>");
    return 2;
}
var (inputFolder, prefix, outputPath) = (args[0], args[1], args[2]);

var serializerOptions = new JsonSerializerOptions { Converters = { new GeoJsonConverterFactory() } };

FeatureCollection LoadLayer(string suffix)
{
    var path = Path.Combine(inputFolder, $"{prefix}{suffix}.geojson");
    using var stream = File.OpenRead(path);
    return JsonSerializer.Deserialize<FeatureCollection>(stream, serializerOptions)
           ?? throw new InvalidOperationException($"Could not parse {path}");
}

var stage = Stopwatch.StartNew();
var delineations = LoadLayer("delineationLayer");
var ovtas = LoadLayer("ovtaLayer");
var wqmps = LoadLayer("wqmpLayer");
var landUseBlocks = LoadLayer("landUseBlockLayer");
Report("Load GeoJSON", stage, $"delin={delineations.Count:N0} ovta={ovtas.Count:N0} wqmp={wqmps.Count:N0} lub={landUseBlocks.Count:N0}");

// ---------- stage: clean (QGIS bufferSnapFix ~= fix validity + buffer(0); self-snap omitted in spike) ----------
stage.Restart();
var delinEntries = Clean(delineations, "DelinID");
var ovtaEntries = Clean(ovtas, "OVTAID");
var wqmpEntries = Clean(wqmps, "WQMPID");
var lubEntries = Clean(landUseBlocks, "LUBID");
Report("Clean/fix geometries", stage, $"delin={delinEntries.Count:N0} ovta={ovtaEntries.Count:N0} wqmp={wqmpEntries.Count:N0} lub={lubEntries.Count:N0}");

// ---------- stage: flatten (de-overlap within layer, per business winner rules) ----------
stage.Restart();
// Delineations/WQMPs: higher TCEffect wins. OVTAs: later AssessDate wins. Ties: "left" loses (matches python).
Flatten(delinEntries, LosesByTcEffect);
Report("Flatten delineations", stage, $"remaining={delinEntries.Count:N0}");
stage.Restart();
Flatten(ovtaEntries, LosesByAssessDate);
Report("Flatten OVTAs", stage, $"remaining={ovtaEntries.Count:N0}");
stage.Restart();
Flatten(wqmpEntries, LosesByTcEffect);
Report("Flatten WQMPs", stage, $"remaining={wqmpEntries.Count:N0}");

// ---------- stage: layer unions (attribute-carrying, like qgis native:union) ----------
// python: odw = union(union(ovta, delin), wqmp); final = union(landUseBlock, odw) — LUB fields win name clashes.
stage.Restart();
var odw = UnionLayers(ovtaEntries, delinEntries);
odw = UnionLayers(odw, wqmpEntries);
Report("Union OVTA+Delin+WQMP", stage, $"odw pieces={odw.Count:N0}");
stage.Restart();
var final = UnionLayers(lubEntries, odw);
Report("Union LUB+ODW", stage, $"pieces={final.Count:N0}");

// ---------- stage: cleanup (explode multiparts, drop area<1 / non-polygons / null LUBID or SJID) ----------
stage.Restart();
var outputFeatures = new FeatureCollection();
foreach (var entry in final)
{
    if (entry.Attrs.TryGetValue("LUBID", out var lubid) is false || lubid is null) continue;
    if (entry.Attrs.TryGetValue("SJID", out var sjid) is false || sjid is null) continue;
    foreach (var polygon in ExtractPolygons(entry.Geometry))
    {
        if (polygon.Area < 1) continue;
        var attributes = new AttributesTable
        {
            { "DelinID", entry.Attrs.GetValueOrDefault("DelinID") },
            { "OVTAID", entry.Attrs.GetValueOrDefault("OVTAID") },
            { "WQMPID", entry.Attrs.GetValueOrDefault("WQMPID") },
            { "LUBID", lubid },
            { "SJID", sjid },
        };
        outputFeatures.Add(new Feature(polygon, attributes));
    }
}
Report("Cleanup + explode", stage, $"features={outputFeatures.Count:N0}");

stage.Restart();
await using (var outStream = File.Create(outputPath))
{
    await JsonSerializer.SerializeAsync(outStream, outputFeatures, serializerOptions);
}
Report("Write output GeoJSON", stage, $"{new FileInfo(outputPath).Length / (1024 * 1024)} MB");

using var process = Process.GetCurrentProcess();
Console.WriteLine($"\nTOTAL: {totalStopwatch.Elapsed:mm\\:ss\\.f}   peak working set: {process.PeakWorkingSet64 / (1024.0 * 1024 * 1024):F2} GiB");
return 0;

// ==================== helpers ====================

static void Report(string name, Stopwatch sw, string detail) =>
    Console.WriteLine($"{name,-28} {sw.Elapsed.TotalSeconds,8:F1}s   {detail}");

// Clean: make valid + buffer(0), keep only polygonal parts. Approximates bufferSnapFix minus the self-snap.
static List<Entry> Clean(FeatureCollection layer, string idAttribute)
{
    var entries = new List<Entry>(layer.Count);
    var nextSyntheticId = -1;
    foreach (var feature in layer)
    {
        var geometry = feature.Geometry;
        if (geometry is null || geometry.IsEmpty) continue;
        if (!geometry.IsValid)
        {
            geometry = NetTopologySuite.Geometries.Utilities.GeometryFixer.Fix(geometry);
        }
        geometry = geometry.Buffer(0);
        geometry = ToPolygonal(geometry);
        if (geometry.IsEmpty) continue;

        var attrs = new Dictionary<string, object?>();
        foreach (var name in feature.Attributes.GetNames())
        {
            attrs[name] = Unwrap(feature.Attributes[name]);
        }
        var id = attrs.GetValueOrDefault(idAttribute) as long? ?? Convert.ToInt64(attrs.GetValueOrDefault(idAttribute) ?? nextSyntheticId--);
        entries.Add(new Entry(id, geometry, attrs));
    }
    return entries;
}

// GeoJSON4STJ hands attribute values back as JsonElement — unwrap to CLR primitives.
static object? Unwrap(object? value) => value switch
{
    JsonElement e => e.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.Number => e.TryGetInt64(out var l) ? l : e.GetDouble(),
        JsonValueKind.String => e.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => e.ToString(),
    },
    _ => value,
};

// Everything flows through here after any overlay/buffer, so this is where we normalize:
// polygonal-only AND strictly-2D packed sequences (mixed 2D/3D inputs make OverlayNG's
// ElevationModel PopulateZFilter throw ArgumentOutOfRangeException on plain Coordinates).
static Geometry ToPolygonal(Geometry geometry)
{
    var polygons = PolygonExtracter.GetPolygons(geometry);
    var rebuilt = new List<Geometry>(polygons.Count);
    foreach (var polygon in polygons.Cast<Polygon>())
    {
        rebuilt.Add(GeoUtil.Rebuild2D(polygon));
    }
    return rebuilt.Count switch
    {
        0 => Polygon.Empty,
        1 => rebuilt[0],
        _ => GeoUtil.Factory2D.CreateMultiPolygon(rebuilt.Cast<Polygon>().ToArray()),
    };
}

static IEnumerable<Polygon> ExtractPolygons(Geometry geometry) =>
    PolygonExtracter.GetPolygons(geometry).Cast<Polygon>();

// True when `left` loses to `right` (python compareFeatures semantics; ties -> left loses).
static bool LosesByTcEffect(Entry left, Entry right)
{
    var l = AsDouble(left.Attrs.GetValueOrDefault("TCEffect"));
    var r = AsDouble(right.Attrs.GetValueOrDefault("TCEffect"));
    return l <= r;
}

static bool LosesByAssessDate(Entry left, Entry right)
{
    var l = AsDate(left.Attrs.GetValueOrDefault("AssessDate"));
    var r = AsDate(right.Attrs.GetValueOrDefault("AssessDate"));
    return l <= r;
}

static double AsDouble(object? value) => value switch
{
    null => double.MinValue,
    long l => l,
    double d => d,
    string s when double.TryParse(s, out var d) => d,
    _ => double.MinValue,
};

static DateTime AsDate(object? value) => value switch
{
    string s when DateTime.TryParse(s, out var d) => d,
    DateTime d => d,
    _ => DateTime.MinValue,
};

// De-overlap a layer in place: equals -> within -> overlaps, matching the python Flatten's
// three phases. Pairs discovered per phase via STRtree on the phase-start snapshot; geometry
// mutations are visible to later pairs within the phase (dict reads), like the python re-fetch.
static void Flatten(List<Entry> entries, Func<Entry, Entry, bool> leftLoses)
{
    var alive = entries.ToDictionary(e => e.Id);

    // ---- phase 1: exact duplicates ----
    foreach (var (a, b) in Pairs(alive, orderedOnly: true))
    {
        if (!a.Geometry.EqualsTopologically(b.Geometry)) continue;
        var loser = leftLoses(a, b) ? a : b;
        alive.Remove(loser.Id);
    }

    // ---- phase 2: containment (inner within outer) ----
    foreach (var (inner, outer) in Pairs(alive, orderedOnly: false))
    {
        if (!alive.ContainsKey(inner.Id) || !alive.ContainsKey(outer.Id)) continue;
        var innerGeom = alive[inner.Id].Geometry;
        var outerGeom = alive[outer.Id].Geometry;
        if (!innerGeom.Within(outerGeom)) continue;
        if (leftLoses(alive[inner.Id], alive[outer.Id]))
        {
            alive.Remove(inner.Id);
        }
        else
        {
            alive[outer.Id] = alive[outer.Id] with { Geometry = Difference(outerGeom, innerGeom) };
        }
    }

    // ---- phase 3: partial overlaps ----
    foreach (var (a, b) in Pairs(alive, orderedOnly: true))
    {
        if (!alive.ContainsKey(a.Id) || !alive.ContainsKey(b.Id)) continue;
        var currentA = alive[a.Id];
        var currentB = alive[b.Id];
        if (!currentA.Geometry.Overlaps(currentB.Geometry)) continue;
        if (leftLoses(currentA, currentB))
        {
            alive[a.Id] = currentA with { Geometry = Difference(currentA.Geometry, currentB.Geometry) };
        }
        else
        {
            alive[b.Id] = currentB with { Geometry = Difference(currentB.Geometry, currentA.Geometry) };
        }
    }

    entries.Clear();
    entries.AddRange(alive.Values.Where(e => !e.Geometry.IsEmpty));
}

// Candidate pairs by envelope intersection from a snapshot of the current geometries.
// orderedOnly: emit each unordered pair once (id_a < id_b), matching the python "<" join filter.
static List<(Entry, Entry)> Pairs(Dictionary<long, Entry> alive, bool orderedOnly)
{
    var tree = new STRtree<Entry>();
    foreach (var entry in alive.Values)
    {
        tree.Insert(entry.Geometry.EnvelopeInternal, entry);
    }
    tree.Build();
    var pairs = new List<(Entry, Entry)>();
    foreach (var entry in alive.Values)
    {
        foreach (var candidate in tree.Query(entry.Geometry.EnvelopeInternal))
        {
            if (candidate.Id == entry.Id) continue;
            if (orderedOnly && entry.Id >= candidate.Id) continue;
            pairs.Add((entry, candidate));
        }
    }
    return pairs;
}

static Geometry Difference(Geometry a, Geometry b)
{
    try
    {
        return ToPolygonal(OverlayNGRobust.Overlay(a, b, SpatialFunction.Difference));
    }
    catch (TopologyException)
    {
        return ToPolygonal(OverlayNGRobust.Overlay(a.Buffer(0), b.Buffer(0), SpatialFunction.Difference));
    }
}

static Geometry Intersection(Geometry a, Geometry b)
{
    try
    {
        return ToPolygonal(OverlayNGRobust.Overlay(a, b, SpatialFunction.Intersection));
    }
    catch (TopologyException)
    {
        return ToPolygonal(OverlayNGRobust.Overlay(a.Buffer(0), b.Buffer(0), SpatialFunction.Intersection));
    }
}

// Attribute-carrying union of two layers (qgis native:union): A-pieces intersected with B carry
// both attribute sets; A remainders carry A's; B remainders carry B's. A's attributes win name clashes.
static List<Entry> UnionLayers(List<Entry> layerA, List<Entry> layerB)
{
    var result = new List<Entry>(layerA.Count + layerB.Count);
    var tree = new STRtree<Entry>();
    foreach (var b in layerB)
    {
        tree.Insert(b.Geometry.EnvelopeInternal, b);
    }
    tree.Build();

    var bIntersectors = new Dictionary<long, List<Geometry>>();
    long pieceId = 0;

    foreach (var a in layerA)
    {
        var candidates = tree.Query(a.Geometry.EnvelopeInternal);
        var prepared = PreparedGeometryFactory.Prepare(a.Geometry);
        var hits = new List<Entry>();
        foreach (var b in candidates)
        {
            if (prepared.Intersects(b.Geometry)) hits.Add(b);
        }

        if (hits.Count == 0)
        {
            result.Add(new Entry(pieceId++, a.Geometry, a.Attrs));
            continue;
        }

        var bGeometries = new List<Geometry>(hits.Count);
        foreach (var b in hits)
        {
            var piece = Intersection(a.Geometry, b.Geometry);
            if (!piece.IsEmpty)
            {
                result.Add(new Entry(pieceId++, piece, MergeAttrs(a.Attrs, b.Attrs)));
            }
            bGeometries.Add(b.Geometry);
            if (!bIntersectors.TryGetValue(b.Id, out var list))
            {
                bIntersectors[b.Id] = list = new List<Geometry>();
            }
            list.Add(a.Geometry);
        }

        var remainder = Difference(a.Geometry, NetTopologySuite.Operation.Union.UnaryUnionOp.Union(bGeometries));
        if (!remainder.IsEmpty)
        {
            result.Add(new Entry(pieceId++, remainder, a.Attrs));
        }
    }

    foreach (var b in layerB)
    {
        if (!bIntersectors.TryGetValue(b.Id, out var intersectors))
        {
            result.Add(new Entry(pieceId++, b.Geometry, b.Attrs));
            continue;
        }
        var remainder = Difference(b.Geometry, NetTopologySuite.Operation.Union.UnaryUnionOp.Union(intersectors));
        if (!remainder.IsEmpty)
        {
            result.Add(new Entry(pieceId++, remainder, b.Attrs));
        }
    }

    return result;
}

// A's values win name clashes (matches qgis union renaming the second layer's clash to *_2,
// which the downstream C# reader ignores).
static Dictionary<string, object?> MergeAttrs(Dictionary<string, object?> a, Dictionary<string, object?> b)
{
    var merged = new Dictionary<string, object?>(b);
    foreach (var (key, value) in a)
    {
        merged[key] = value;
    }
    return merged;
}

internal sealed record Entry(long Id, Geometry Geometry, Dictionary<string, object?> Attrs);

internal static class GeoUtil
{
    public static readonly GeometryFactory Factory2D = new(
        new PrecisionModel(), 2771, NetTopologySuite.Geometries.Implementation.PackedCoordinateSequenceFactory.DoubleFactory);

    public static Polygon Rebuild2D(Polygon polygon)
    {
        var shell = Ring2D(polygon.ExteriorRing);
        var holes = new LinearRing[polygon.NumInteriorRings];
        for (var i = 0; i < polygon.NumInteriorRings; i++)
        {
            holes[i] = Ring2D(polygon.GetInteriorRingN(i));
        }
        return Factory2D.CreatePolygon(shell, holes);
    }

    private static LinearRing Ring2D(LineString ring)
    {
        var sequence = ring.CoordinateSequence;
        var packed = new double[sequence.Count * 2];
        for (var i = 0; i < sequence.Count; i++)
        {
            packed[2 * i] = sequence.GetX(i);
            packed[2 * i + 1] = sequence.GetY(i);
        }
        var packedSequence = ((NetTopologySuite.Geometries.Implementation.PackedCoordinateSequenceFactory)Factory2D.CoordinateSequenceFactory)
            .Create(packed, 2, 0);
        return Factory2D.CreateLinearRing(packedSequence);
    }
}
