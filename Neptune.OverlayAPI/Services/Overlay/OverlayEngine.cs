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

namespace Neptune.OverlayAPI.Services.Overlay;

/// <summary>
/// In-process NTS replacement for the retired PyQGIS overlay scripts (NPT-1105 Part 2).
/// Benchmarked 4.3x faster single-threaded than QGIS 3.28 on the golden TGU inputs; the layer
/// union parallelizes per-feature. Design notes that differ from the QGIS behavior on purpose:
///  - deterministic winner tie-breaks in Flatten (QGIS was iteration-order-dependent and did not
///    reproduce its own output run-to-run on contested boundaries);
///  - no self-snap step (qgis:snapgeometries existed to stop GeometryCollection fallout from
///    union); intersections and differences are noded on a fixed 1 mm grid instead (see Overlay);
///  - all geometries normalized to strictly-2D packed sequences (mixed 2D/3D coordinates make
///    OverlayNG's elevation model throw).
/// </summary>
public static class OverlayEngine
{
    public static readonly GeometryFactory Factory2D = new(
        new PrecisionModel(), Neptune.Common.GeoSpatial.Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
        PackedCoordinateSequenceFactory.DoubleFactory);

    // Leave one core free for Kestrel. Unbounded Parallel.ForEach saturated every thread-pool
    // worker with long CPU-bound iterations for the entire multi-minute run, so the pod's
    // health endpoints couldn't get a thread — the kubelet liveness probe timed out repeatedly
    // and SIGKILLed the container mid-refresh (QA nightly TGU, 2026-07-09 and -10; the API
    // sees it as "response ended prematurely"). Costs ~1/8 of throughput on the QA nodes;
    // keeps the pod responsive to probes and concurrent overlay calls.
    private static readonly ParallelOptions ParallelOpts = new()
    {
        MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1),
    };

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

        // phase 3: every remaining pair whose interiors share ground — loser keeps (loser - winner).
        // Deliberately not just Overlaps(): that predicate is false when one polygon contains the
        // other, and a pair can be a partial overlap when phase 2 looks at it and a containment by
        // the time phase 3 does (NPT-1143). Results come back on the 1 mm overlay grid, so a
        // neighbour that was exactly inside an untouched polygon pokes out of the cut one by a
        // fraction of a millimetre, then sits inside it again once it has been cut itself. Phase 2
        // skipped such a pair as "not within" and Overlaps() skipped it as "not a partial overlap",
        // leaving both features whole on the same ground.
        foreach (var (idA, idB) in CandidatePairs(alive, idOf))
        {
            if (!alive.TryGetValue(idA, out var a) || !alive.TryGetValue(idB, out var b)) continue;
            if (!Relates(a.Geometry, b.Geometry, InteriorsIntersect)) continue;
            var (loserId, loser, winner) = firstLoses(a, b) ? (idA, a, b) : (idB, b, a);
            loser.Geometry = Difference(loser.Geometry, winner.Geometry);
            if (loser.Geometry.IsEmpty)
            {
                alive.Remove(loserId);
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

        Parallel.ForEach(layerA, ParallelOpts, a =>
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
        Parallel.ForEach(layer, ParallelOpts, feature =>
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

    // How far past the refresh area a delta refresh reaches, in metres.
    private const double DeltaRefreshReachMeters = 0.5;

    // How far past its reach a delta refresh looks for polygons to reconcile, in metres.
    private const double DeltaRefreshCandidateMarginMeters = 1;

    /// <summary>
    /// The ground a delta refresh replaces: the refresh area grown by half a metre (NPT-1158).
    /// Refresh areas are built from the edited shapes, so LGU edges routinely lie exactly on the
    /// area's own edge; half a metre out, very little does.
    /// </summary>
    public static Geometry DeltaRefreshReach(Geometry refreshArea) => ToPolygonal2D(refreshArea.Buffer(DeltaRefreshReachMeters));

    /// <summary>
    /// Where a delta refresh looks for candidates: its reach plus a margin. Load the stored rows that
    /// intersect this, and filter the overlay pieces with it, before calling PlanDeltaRefresh.
    /// </summary>
    public static Geometry DeltaRefreshCandidateZone(Geometry refreshArea) =>
        ToPolygonal2D(refreshArea.Buffer(DeltaRefreshReachMeters + DeltaRefreshCandidateMarginMeters));

    /// <summary>
    /// Decides which stored rows a delta refresh deletes and which new polygons it inserts (NPT-1158).
    /// The two used to be chosen independently and did not agree, so ground gained one more stored
    /// copy on every refresh:
    ///  - whole pieces were inserted when any part touched the area, while only the touching part's
    ///    stored row was deleted. Here both sides are single polygons;
    ///  - a stored polygon and its recomputed twin can differ by under a millimetre, so on the edge
    ///    of the test area one was selected and the other not. Here the two sets are reconciled after
    ///    the reach test: a stored row holding the same ground as a polygon being inserted is deleted
    ///    too, and a new polygon holding the same ground as a row being deleted is inserted too,
    ///    repeated until nothing changes.
    /// </summary>
    /// <param name="stored">Stored rows intersecting DeltaRefreshCandidateZone, as single polygons with their attributes.</param>
    /// <param name="pieces">Overlay pieces intersecting DeltaRefreshCandidateZone.</param>
    public static (List<int> StoredIDsToDelete, List<(OverlayFeature Feature, Polygon Polygon)> PolygonsToInsert) PlanDeltaRefresh(
        IReadOnlyList<(int ID, OverlayFeature Feature)> stored, IEnumerable<OverlayFeature> pieces, Geometry refreshArea,
        double minimumArea = 1.0)
    {
        var reach = PreparedGeometryFactory.Prepare(DeltaRefreshReach(refreshArea));
        var candidateZone = PreparedGeometryFactory.Prepare(DeltaRefreshCandidateZone(refreshArea));
        // parts of a multi-part piece that lie outside the zone have no stored row among the candidates
        // to reconcile against, so they are left alone
        var replacements = ExplodeAndDropSlivers(pieces, minimumArea).Where(x => candidateZone.Intersects(x.Polygon)).ToList();

        var delete = stored.Select(x => reach.Intersects(x.Feature.Geometry)).ToArray();
        var insert = replacements.Select(x => reach.Intersects(x.Polygon)).ToArray();

        // Same attributes and interiors that share ground. Not an interior-point test: a strip a
        // millimetre wide and its copy shifted by half that overlap without either covering the
        // other's interior point.
        bool HoldSameGround(int storedIndex, int replacementIndex)
        {
            var storedFeature = stored[storedIndex].Feature;
            var (replacementFeature, replacementPolygon) = replacements[replacementIndex];
            return storedFeature.RegionalSubbasinID == replacementFeature.RegionalSubbasinID
                   && storedFeature.DelineationID == replacementFeature.DelineationID
                   && storedFeature.WaterQualityManagementPlanID == replacementFeature.WaterQualityManagementPlanID
                   && storedFeature.Geometry.EnvelopeInternal.Intersects(replacementPolygon.EnvelopeInternal)
                   && Relates(storedFeature.Geometry, replacementPolygon, InteriorsIntersect);
        }

        bool changed;
        do
        {
            changed = false;
            for (var s = 0; s < stored.Count; s++)
            {
                if (delete[s]) continue;
                for (var r = 0; r < replacements.Count; r++)
                {
                    if (!insert[r] || !HoldSameGround(s, r)) continue;
                    delete[s] = true;
                    changed = true;
                    break;
                }
            }
            for (var r = 0; r < replacements.Count; r++)
            {
                if (insert[r]) continue;
                for (var s = 0; s < stored.Count; s++)
                {
                    if (!delete[s] || !HoldSameGround(s, r)) continue;
                    insert[r] = true;
                    changed = true;
                    break;
                }
            }
        } while (changed);

        return (stored.Where((_, i) => delete[i]).Select(x => x.ID).ToList(),
            replacements.Where((_, i) => insert[i]).ToList());
    }

    /// <summary>
    /// Spatial predicate via RelateNG — the legacy V1 relate throws "side location conflict"
    /// TopologyExceptions on full-precision DB geometries (hit on the first ported TGU run).
    /// Falls back to fixing both operands and retrying if even NG objects.
    /// </summary>
    // DE-9IM "T********": the two interiors have at least a point in common. For polygons that is
    // any shared ground — partial overlap, containment either way, or equality — but not a shared edge.
    private static TopologyPredicate InteriorsIntersect => RelatePredicate.Matches("T********");

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

    public static Geometry Difference(Geometry a, Geometry b) => Overlay(a, b, SpatialFunction.Difference);

    public static Geometry Intersection(Geometry a, Geometry b) => Overlay(a, b, SpatialFunction.Intersection);

    /// <summary>
    /// Every overlay is noded on a fixed 1 mm grid (snap-rounding), not at floating precision
    /// (NPT-1143). OverlayNGRobust tries floating-precision noding first and only falls back to
    /// snapping when that attempt THROWS; on some inputs it instead returns a wrong answer without
    /// throwing — an empty intersection and an untouched difference for a polygon lying wholly
    /// inside the other operand — which made UnionLayers emit a block's area twice. The two wrong
    /// answers still add up to the first operand's area, so comparing
    /// area(A ∩ B) + area(A − B) with area(A) cannot detect it; snap-rounding avoids the failure
    /// instead of trying to spot it. Result vertices land on the grid, so they can sit up to
    /// ~0.7 mm from the input's.
    /// </summary>
    private static Geometry Overlay(Geometry a, Geometry b, SpatialFunction operation)
    {
        try
        {
            return ToPolygonal2D(OverlayNG.Overlay(a, b, operation, OverlayPrecisionModel));
        }
        catch (TopologyException)
        {
            return ToPolygonal2D(OverlayNG.Overlay(a.Buffer(0), b.Buffer(0), operation, OverlayPrecisionModel));
        }
    }

    // Grid cells per coordinate unit. The overlay inputs are in metres (NAD83(HARN) California
    // zone VI), so 1000 is a 1 mm grid: three orders of magnitude finer than the 1 m² sliver floor.
    private static readonly PrecisionModel OverlayPrecisionModel = new(1000);

    /// <summary>
    /// Post-run check for a layer union whose pieces must tile their source features (TGUs tile
    /// their land use block): returns each source whose pieces total more than the source's own
    /// area by more than <paramref name="tolerance"/>, i.e. ground that was counted twice.
    /// </summary>
    public static List<(int SourceID, double SourceArea, double PieceArea)> FindOverCountedSources(
        IEnumerable<(int SourceID, double Area)> pieces, IReadOnlyDictionary<int, double> sourceAreas, double tolerance)
    {
        return pieces.GroupBy(x => x.SourceID)
            .Select(x => (SourceID: x.Key, SourceArea: sourceAreas.GetValueOrDefault(x.Key), PieceArea: x.Sum(y => y.Area)))
            .Where(x => x.PieceArea - x.SourceArea > tolerance)
            .OrderBy(x => x.SourceID)
            .ToList();
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
