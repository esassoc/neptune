using System.Collections.Concurrent;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Implementation;
using NetTopologySuite.Geometries.Prepared;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.Index.Strtree;
using NetTopologySuite.Operation.Overlay;
using NetTopologySuite.Operation.OverlayNG;
using NetTopologySuite.Operation.RelateNG;
using NetTopologySuite.Operation.Union;

namespace Neptune.QGISAPI.Services.Overlay;

/// <summary>
/// In-process NTS replacement for the retired PyQGIS overlay scripts (NPT-1105 Part 2).
/// Benchmarked 4.3x faster single-threaded than QGIS 3.28 on the golden TGU inputs; the layer
/// union parallelizes per-feature. Design notes that differ from the QGIS behavior on purpose:
///  - deterministic winner tie-breaks in Flatten (QGIS was iteration-order-dependent and did not
///    reproduce its own output run-to-run on contested boundaries);
///  - no self-snap step (qgis:snapgeometries existed to stop GeometryCollection fallout from
///    union; OverlayNGRobust handles robustness, validated against the golden outputs);
///  - all geometries normalized to strictly-2D packed sequences (mixed 2D/3D coordinates make
///    OverlayNG's elevation model throw).
/// </summary>
public static class OverlayEngine
{
    public static readonly GeometryFactory Factory2D = new(
        new PrecisionModel(), Neptune.Common.GeoSpatial.Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
        PackedCoordinateSequenceFactory.DoubleFactory);

    /// <summary>
    /// Make valid + buffer(0) + force polygonal/2D. Replaces the QGIS bufferSnapFix pipeline.
    /// Features whose geometry cleans to empty are dropped.
    /// </summary>
    public static List<OverlayFeature> Clean(IEnumerable<OverlayFeature> features)
    {
        var cleaned = new List<OverlayFeature>();
        foreach (var feature in features)
        {
            var geometry = feature.Geometry;
            if (geometry.IsEmpty) continue;
            if (!geometry.IsValid)
            {
                geometry = GeometryFixer.Fix(geometry);
            }
            geometry = ToPolygonal2D(geometry.Buffer(0));
            if (geometry.IsEmpty) continue;
            feature.Geometry = geometry;
            cleaned.Add(feature);
        }
        return cleaned;
    }

    /// <summary>
    /// De-overlap a single layer in place (the python "Flatten"): exact duplicates, then
    /// containments, then partial overlaps. <paramref name="firstLoses"/> implements the layer's
    /// winner rule and MUST be deterministic including ties (e.g. tie on TCEffect -> lower ID
    /// loses) — this is where QGIS was nondeterministic. <paramref name="idOf"/> supplies the
    /// stable per-feature identity used for pair ordering.
    /// </summary>
    public static List<OverlayFeature> Flatten(List<OverlayFeature> layer, Func<OverlayFeature, int> idOf,
        Func<OverlayFeature, OverlayFeature, bool> firstLoses)
    {
        var alive = layer.ToDictionary(idOf);

        // phase 1: exact geometric duplicates — delete the loser
        foreach (var (idA, idB) in CandidatePairs(alive, idOf))
        {
            if (!alive.TryGetValue(idA, out var a) || !alive.TryGetValue(idB, out var b)) continue;
            if (!Relates(a.Geometry, b.Geometry, RelatePredicate.EqualsTopologically())) continue;
            alive.Remove(firstLoses(a, b) ? idA : idB);
        }

        // phase 2: containment — losing inner is deleted; winning inner is punched out of the outer
        foreach (var (idA, idB) in CandidatePairs(alive, idOf))
        {
            foreach (var (innerId, outerId) in new[] { (idA, idB), (idB, idA) })
            {
                if (!alive.TryGetValue(innerId, out var inner) || !alive.TryGetValue(outerId, out var outer)) continue;
                if (!Relates(inner.Geometry, outer.Geometry, RelatePredicate.Within())) continue;
                if (firstLoses(inner, outer))
                {
                    alive.Remove(innerId);
                }
                else
                {
                    outer.Geometry = Difference(outer.Geometry, inner.Geometry);
                }
            }
        }

        // phase 3: partial overlaps — loser keeps (loser - winner)
        foreach (var (idA, idB) in CandidatePairs(alive, idOf))
        {
            if (!alive.TryGetValue(idA, out var a) || !alive.TryGetValue(idB, out var b)) continue;
            if (!Relates(a.Geometry, b.Geometry, RelatePredicate.Overlaps())) continue;
            if (firstLoses(a, b))
            {
                a.Geometry = Difference(a.Geometry, b.Geometry);
            }
            else
            {
                b.Geometry = Difference(b.Geometry, a.Geometry);
            }
        }

        return alive.Values.Where(x => !x.Geometry.IsEmpty).ToList();
    }

    /// <summary>
    /// Attribute-carrying union of two layers (qgis native:union): intersection pieces merge both
    /// sides' attributes (first layer wins name clashes), remainders keep their own. The dominant
    /// pipeline cost — parallel over the first layer's features (97% of spike wall time).
    /// </summary>
    public static List<OverlayFeature> UnionLayers(IReadOnlyList<OverlayFeature> layerA, IReadOnlyList<OverlayFeature> layerB)
    {
        var tree = new STRtree<int>();
        for (var i = 0; i < layerB.Count; i++)
        {
            tree.Insert(layerB[i].Geometry.EnvelopeInternal, i);
        }
        tree.Build();

        var results = new ConcurrentBag<OverlayFeature>();
        var bIntersectorGeometries = new ConcurrentDictionary<int, ConcurrentBag<Geometry>>();

        Parallel.ForEach(layerA, a =>
        {
            var prepared = PreparedGeometryFactory.Prepare(a.Geometry);
            var hits = new List<int>();
            foreach (var candidateIndex in tree.Query(a.Geometry.EnvelopeInternal))
            {
                if (prepared.Intersects(layerB[candidateIndex].Geometry))
                {
                    hits.Add(candidateIndex);
                }
            }

            if (hits.Count == 0)
            {
                results.Add(a);
                return;
            }

            var hitGeometries = new List<Geometry>(hits.Count);
            foreach (var bIndex in hits)
            {
                var b = layerB[bIndex];
                var piece = Intersection(a.Geometry, b.Geometry);
                if (!piece.IsEmpty)
                {
                    results.Add(a.MergedWith(b, piece));
                }
                hitGeometries.Add(b.Geometry);
                bIntersectorGeometries.GetOrAdd(bIndex, _ => new ConcurrentBag<Geometry>()).Add(a.Geometry);
            }

            var remainder = Difference(a.Geometry, UnaryUnionOp.Union(hitGeometries));
            if (!remainder.IsEmpty)
            {
                results.Add(a.WithGeometry(remainder));
            }
        });

        var final = results.ToList();
        for (var bIndex = 0; bIndex < layerB.Count; bIndex++)
        {
            var b = layerB[bIndex];
            if (!bIntersectorGeometries.TryGetValue(bIndex, out var intersectors))
            {
                final.Add(b);
                continue;
            }
            var remainder = Difference(b.Geometry, UnaryUnionOp.Union(intersectors.ToList()));
            if (!remainder.IsEmpty)
            {
                final.Add(b.WithGeometry(remainder));
            }
        }

        return final;
    }

    /// <summary>
    /// Clip a layer to a boundary geometry (qgis native:clip): features fully inside pass through,
    /// features outside drop, straddlers keep their intersection. Attributes are untouched.
    /// </summary>
    public static List<OverlayFeature> Clip(IReadOnlyList<OverlayFeature> layer, Geometry clipBoundary)
    {
        var prepared = PreparedGeometryFactory.Prepare(clipBoundary);
        var results = new ConcurrentBag<OverlayFeature>();
        Parallel.ForEach(layer, feature =>
        {
            if (!prepared.Intersects(feature.Geometry)) return;
            if (prepared.Contains(feature.Geometry))
            {
                results.Add(feature);
                return;
            }
            var clipped = Intersection(feature.Geometry, clipBoundary);
            if (!clipped.IsEmpty)
            {
                results.Add(feature.WithGeometry(clipped));
            }
        });
        return results.ToList();
    }

    /// <summary>
    /// Keep only features that intersect the filter geometry — in their entirety (the delta-refresh
    /// select-by-location semantics: a piece touching the refresh area is regenerated whole).
    /// </summary>
    public static List<OverlayFeature> FilterIntersecting(IReadOnlyList<OverlayFeature> layer, Geometry filter)
    {
        var prepared = PreparedGeometryFactory.Prepare(filter);
        return layer.Where(x => prepared.Intersects(x.Geometry)).ToList();
    }

    /// <summary>Explode to single polygons and drop slivers (matches the python area&lt;1 cleanup).</summary>
    public static IEnumerable<(OverlayFeature Feature, Polygon Polygon)> ExplodeAndDropSlivers(IEnumerable<OverlayFeature> features, double minimumArea = 1.0)
    {
        foreach (var feature in features)
        {
            foreach (var polygon in PolygonExtracter.GetPolygons(feature.Geometry).Cast<Polygon>())
            {
                if (polygon.Area < minimumArea) continue;
                yield return (feature, polygon);
            }
        }
    }

    /// <summary>
    /// Spatial predicate via RelateNG — the legacy V1 relate throws "side location conflict"
    /// TopologyExceptions on full-precision DB geometries (hit on the first ported TGU run).
    /// Falls back to fixing both operands and retrying if even NG objects.
    /// </summary>
    private static bool Relates(Geometry a, Geometry b, TopologyPredicate predicate)
    {
        try
        {
            return RelateNG.Relate(a, b, predicate);
        }
        catch (TopologyException)
        {
            return RelateNG.Relate(GeometryFixer.Fix(a), GeometryFixer.Fix(b), predicate);
        }
    }

    public static Geometry Difference(Geometry a, Geometry b)
    {
        try
        {
            return ToPolygonal2D(OverlayNGRobust.Overlay(a, b, SpatialFunction.Difference));
        }
        catch (TopologyException)
        {
            return ToPolygonal2D(OverlayNGRobust.Overlay(a.Buffer(0), b.Buffer(0), SpatialFunction.Difference));
        }
    }

    public static Geometry Intersection(Geometry a, Geometry b)
    {
        try
        {
            return ToPolygonal2D(OverlayNGRobust.Overlay(a, b, SpatialFunction.Intersection));
        }
        catch (TopologyException)
        {
            return ToPolygonal2D(OverlayNGRobust.Overlay(a.Buffer(0), b.Buffer(0), SpatialFunction.Intersection));
        }
    }

    /// <summary>
    /// Normalize to polygonal-only geometry built on strictly-2D packed coordinate sequences.
    /// Every overlay result funnels through here: mixed 2D/3D sequences make OverlayNG's
    /// ElevationModel throw ArgumentOutOfRangeException, and DB geometries can carry Z.
    /// </summary>
    public static Geometry ToPolygonal2D(Geometry geometry)
    {
        var polygons = PolygonExtracter.GetPolygons(geometry);
        var rebuilt = new Polygon[polygons.Count];
        for (var i = 0; i < polygons.Count; i++)
        {
            rebuilt[i] = Rebuild2D((Polygon)polygons[i]);
        }
        return rebuilt.Length switch
        {
            0 => Polygon.Empty,
            1 => rebuilt[0],
            _ => Factory2D.CreateMultiPolygon(rebuilt),
        };
    }

    private static Polygon Rebuild2D(Polygon polygon)
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
        var packedSequence = ((PackedCoordinateSequenceFactory)Factory2D.CoordinateSequenceFactory).Create(packed, 2, 0);
        return Factory2D.CreateLinearRing(packedSequence);
    }

    /// <summary>
    /// Envelope-intersecting id pairs (idA &lt; idB), sorted, from the current layer state.
    /// Recomputed per Flatten phase — geometry mutations within a phase are visible to later
    /// pairs (dict reads), matching the python re-fetch semantics; the sort is what makes the
    /// whole flatten deterministic.
    /// </summary>
    private static List<(int A, int B)> CandidatePairs(Dictionary<int, OverlayFeature> alive, Func<OverlayFeature, int> idOf)
    {
        var tree = new STRtree<int>();
        foreach (var (id, feature) in alive)
        {
            tree.Insert(feature.Geometry.EnvelopeInternal, id);
        }
        tree.Build();
        var pairs = new List<(int, int)>();
        foreach (var (id, feature) in alive)
        {
            foreach (var candidateId in tree.Query(feature.Geometry.EnvelopeInternal))
            {
                if (id < candidateId)
                {
                    pairs.Add((id, candidateId));
                }
            }
        }
        pairs.Sort();
        return pairs;
    }
}
