using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.QGISAPI.Services.Overlay;
using NetTopologySuite.Geometries;

namespace Neptune.Tests;

/// <summary>
/// Unit tests for the NTS overlay engine that replaced the PyQGIS scripts (NPT-1105 Part 2).
/// Focus areas, in order of the confidence they buy:
///  - Flatten winner rules and the DETERMINISTIC tie-break (the deliberate improvement over QGIS,
///    which was iteration-order-dependent) — including input-order independence;
///  - geometric invariants: flatten never double-counts or loses ground; layer union conserves area;
///  - the 2D-normalization regression (mixed 2D/3D coordinates crashed OverlayNG's elevation model
///    on the first ported run against raw DB geometries);
///  - attribute-merge semantics that reproduce the retired QGIS union's field-clash behavior.
/// </summary>
[TestClass]
public class OverlayEngineTests
{
    private const double AreaTolerance = 1e-6;

    // ---------- helpers ----------

    private static Geometry Square(double minX, double minY, double size)
    {
        return OverlayEngine.Factory2D.CreatePolygon(new[]
        {
            new Coordinate(minX, minY),
            new Coordinate(minX + size, minY),
            new Coordinate(minX + size, minY + size),
            new Coordinate(minX, minY + size),
            new Coordinate(minX, minY),
        });
    }

    private static OverlayFeature Delineation(int id, Geometry geometry, double tcEffect = 0, int? sjid = null) => new()
    {
        Geometry = geometry,
        DelineationID = id,
        StormwaterJurisdictionID = sjid,
        TrashCaptureEffectiveness = tcEffect,
    };

    private static OverlayFeature LandUseBlock(int id, Geometry geometry, int sjid) => new()
    {
        Geometry = geometry,
        LandUseBlockID = id,
        StormwaterJurisdictionID = sjid,
    };

    // Matches the controller's winner rule: higher TCEffect wins; tie -> higher ID wins.
    private static bool LosesByTcEffect(OverlayFeature a, OverlayFeature b)
    {
        var (effectA, effectB) = (a.TrashCaptureEffectiveness ?? double.MinValue, b.TrashCaptureEffectiveness ?? double.MinValue);
        return effectA != effectB ? effectA < effectB : a.DelineationID!.Value < b.DelineationID!.Value;
    }

    private static List<OverlayFeature> FlattenDelineations(List<OverlayFeature> layer) =>
        OverlayEngine.Flatten(layer, x => x.DelineationID!.Value, LosesByTcEffect);

    // ---------- Clean ----------

    [TestMethod]
    public void Clean_FixesInvalidGeometry_AndKeepsPolygonalArea()
    {
        // classic bowtie: self-intersecting ring, invalid
        var bowtie = OverlayEngine.Factory2D.CreatePolygon(new[]
        {
            new Coordinate(0, 0), new Coordinate(10, 10), new Coordinate(10, 0), new Coordinate(0, 10), new Coordinate(0, 0),
        });
        Assert.IsFalse(bowtie.IsValid);

        var cleaned = OverlayEngine.Clean(new List<OverlayFeature> { Delineation(1, bowtie) });

        Assert.AreEqual(1, cleaned.Count);
        Assert.IsTrue(cleaned[0].Geometry.IsValid);
        // the bowtie resolves to two 25-unit triangles
        Assert.AreEqual(50, cleaned[0].Geometry.Area, 0.001);
    }

    [TestMethod]
    public void Clean_DropsEmptyGeometries()
    {
        var cleaned = OverlayEngine.Clean(new List<OverlayFeature>
        {
            Delineation(1, Polygon.Empty),
            Delineation(2, Square(0, 0, 10)),
        });

        Assert.AreEqual(1, cleaned.Count);
        Assert.AreEqual(2, cleaned[0].DelineationID);
    }

    [TestMethod]
    public void Clean_ForcesStrictly2D_SoOverlayDoesNotThrow()
    {
        // Regression: DB geometries can carry Z; mixed 2D/3D sequences made OverlayNG's
        // ElevationModel throw ArgumentOutOfRangeException on the first ported TGU run.
        var factory3D = new GeometryFactory(new PrecisionModel(), 2771);
        var withZ = factory3D.CreatePolygon(new[]
        {
            new CoordinateZ(0, 0, 5), new CoordinateZ(10, 0, 5), new CoordinateZ(10, 10, 5), new CoordinateZ(0, 10, 5), new CoordinateZ(0, 0, 5),
        });

        var cleaned = OverlayEngine.Clean(new List<OverlayFeature> { Delineation(1, withZ) });

        Assert.IsFalse(cleaned[0].Geometry.Coordinates.Any(c => !double.IsNaN(c.Z)), "Z ordinates should be stripped");
        // and the cleaned geometry must be usable in an overlay against a plain 2D geometry
        var difference = OverlayEngine.Difference(cleaned[0].Geometry, Square(5, 5, 10));
        Assert.AreEqual(75, difference.Area, 0.001);
    }

    // ---------- Flatten: winner rules ----------

    [TestMethod]
    public void Flatten_RemovesExactDuplicate_KeepingHigherTcEffect()
    {
        var flattened = FlattenDelineations(new List<OverlayFeature>
        {
            Delineation(1, Square(0, 0, 10), tcEffect: 80),
            Delineation(2, Square(0, 0, 10), tcEffect: 20),
        });

        Assert.AreEqual(1, flattened.Count);
        Assert.AreEqual(1, flattened[0].DelineationID);
    }

    [TestMethod]
    public void Flatten_DeletesContainedInner_WhenInnerLoses()
    {
        var flattened = FlattenDelineations(new List<OverlayFeature>
        {
            Delineation(1, Square(0, 0, 20), tcEffect: 80), // outer, wins
            Delineation(2, Square(5, 5, 5), tcEffect: 20),  // inner, loses
        });

        Assert.AreEqual(1, flattened.Count);
        Assert.AreEqual(1, flattened[0].DelineationID);
        Assert.AreEqual(400, flattened[0].Geometry.Area, AreaTolerance);
    }

    [TestMethod]
    public void Flatten_PunchesWinningInnerOutOfOuter()
    {
        var flattened = FlattenDelineations(new List<OverlayFeature>
        {
            Delineation(1, Square(0, 0, 20), tcEffect: 20), // outer, loses
            Delineation(2, Square(5, 5, 5), tcEffect: 80),  // inner, wins
        });

        Assert.AreEqual(2, flattened.Count);
        var outer = flattened.Single(x => x.DelineationID == 1);
        var inner = flattened.Single(x => x.DelineationID == 2);
        Assert.AreEqual(400 - 25, outer.Geometry.Area, AreaTolerance); // donut
        Assert.AreEqual(25, inner.Geometry.Area, AreaTolerance);
    }

    [TestMethod]
    public void Flatten_OverlapLoserKeepsOnlyItsDifference()
    {
        // two 10x10 squares overlapping by a 5x10 strip
        var flattened = FlattenDelineations(new List<OverlayFeature>
        {
            Delineation(1, Square(0, 0, 10), tcEffect: 80),  // winner keeps everything
            Delineation(2, Square(5, 0, 10), tcEffect: 20),  // loser keeps 5x10
        });

        Assert.AreEqual(100, flattened.Single(x => x.DelineationID == 1).Geometry.Area, AreaTolerance);
        Assert.AreEqual(50, flattened.Single(x => x.DelineationID == 2).Geometry.Area, AreaTolerance);
    }

    [TestMethod]
    public void Flatten_TieBreaksOnHigherID_RegardlessOfInputOrder()
    {
        // The determinism guarantee: equal TCEffect (the real-world channel-delineation case) must
        // resolve identically no matter how the input list is ordered. QGIS failed exactly this.
        List<OverlayFeature> Build(bool reversed)
        {
            var features = new List<OverlayFeature>
            {
                Delineation(100, Square(0, 0, 10), tcEffect: 0),
                Delineation(200, Square(5, 0, 10), tcEffect: 0),
            };
            if (reversed) features.Reverse();
            return features;
        }

        foreach (var reversed in new[] { false, true })
        {
            var flattened = FlattenDelineations(Build(reversed));
            var winner = flattened.Single(x => x.DelineationID == 200);
            var loser = flattened.Single(x => x.DelineationID == 100);
            Assert.AreEqual(100, winner.Geometry.Area, AreaTolerance, $"reversed={reversed}: higher ID must win the tie");
            Assert.AreEqual(50, loser.Geometry.Area, AreaTolerance, $"reversed={reversed}");
        }
    }

    [TestMethod]
    public void Flatten_ResultIsNonOverlapping_AndCoversTheOriginalGround()
    {
        // three mutually-overlapping squares with mixed rules -> pieces must tile the original
        // footprint exactly: sum of areas == area of the unioned inputs (no loss, no double count)
        var inputs = new List<OverlayFeature>
        {
            Delineation(1, Square(0, 0, 10), tcEffect: 50),
            Delineation(2, Square(5, 0, 10), tcEffect: 50),
            Delineation(3, Square(2, 2, 10), tcEffect: 90),
        };
        var expectedFootprint = Square(0, 0, 10).Union(Square(5, 0, 10)).Union(Square(2, 2, 10)).Area;

        var flattened = FlattenDelineations(inputs);

        var totalArea = flattened.Sum(x => x.Geometry.Area);
        Assert.AreEqual(expectedFootprint, totalArea, 0.001, "flatten must neither lose ground nor double-count");
        for (var i = 0; i < flattened.Count; i++)
        {
            for (var j = i + 1; j < flattened.Count; j++)
            {
                var overlapArea = OverlayEngine.Intersection(flattened[i].Geometry, flattened[j].Geometry).Area;
                Assert.AreEqual(0, overlapArea, 0.001, $"pieces {i} and {j} must not overlap");
            }
        }
    }

    // ---------- UnionLayers ----------

    [TestMethod]
    public void UnionLayers_ProducesIntersectionPieceWithMergedAttributes_AndRemainders()
    {
        var landUseBlocks = new List<OverlayFeature> { LandUseBlock(10, Square(0, 0, 10), sjid: 7) };
        var delineations = new List<OverlayFeature> { Delineation(1, Square(5, 0, 10), sjid: 99) };

        var pieces = OverlayEngine.UnionLayers(landUseBlocks, delineations);

        Assert.AreEqual(3, pieces.Count);
        var merged = pieces.Single(x => x.LandUseBlockID == 10 && x.DelineationID == 1);
        var lubOnly = pieces.Single(x => x.LandUseBlockID == 10 && x.DelineationID == null);
        var delinOnly = pieces.Single(x => x.LandUseBlockID == null && x.DelineationID == 1);
        Assert.AreEqual(50, merged.Geometry.Area, AreaTolerance);
        Assert.AreEqual(50, lubOnly.Geometry.Area, AreaTolerance);
        Assert.AreEqual(50, delinOnly.Geometry.Area, AreaTolerance);
        // first layer (LUB) wins the SJID clash — the retired QGIS union's SJID/SJID_2 behavior
        Assert.AreEqual(7, merged.StormwaterJurisdictionID);
    }

    [TestMethod]
    public void UnionLayers_PassesThroughDisjointFeatures()
    {
        var layerA = new List<OverlayFeature> { LandUseBlock(10, Square(0, 0, 10), sjid: 7) };
        var layerB = new List<OverlayFeature> { Delineation(1, Square(100, 100, 10)) };

        var pieces = OverlayEngine.UnionLayers(layerA, layerB);

        Assert.AreEqual(2, pieces.Count);
        Assert.AreEqual(100, pieces.Single(x => x.LandUseBlockID == 10).Geometry.Area, AreaTolerance);
        Assert.AreEqual(100, pieces.Single(x => x.DelineationID == 1).Geometry.Area, AreaTolerance);
    }

    [TestMethod]
    public void UnionLayers_ConservesTotalArea_WithManyOverlaps()
    {
        // a 3x3 grid of blocks overlaid by two straddling delineations: the union pieces must
        // tile the combined footprint exactly (the invariant that makes trash areas trustworthy)
        var blocks = new List<OverlayFeature>();
        var blockId = 1;
        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                blocks.Add(LandUseBlock(blockId++, Square(x * 10, y * 10, 10), sjid: 7));
            }
        }
        var delineations = new List<OverlayFeature>
        {
            Delineation(1, Square(5, 5, 12)),
            Delineation(2, Square(-5, 20, 12)),
        };
        var expectedFootprint = 900 + Square(-5, 20, 12).Difference(Square(0, 0, 30)).Area;

        var pieces = OverlayEngine.UnionLayers(blocks, delineations);

        // delineation 1 sits fully inside the grid, so only delineation 2 leaves a remainder
        Assert.AreEqual(expectedFootprint, pieces.Sum(x => x.Geometry.Area), 0.001);
    }

    // ---------- Clip / FilterIntersecting / ExplodeAndDropSlivers ----------

    [TestMethod]
    public void Clip_KeepsInside_DropsOutside_CutsStraddlers()
    {
        var clipBoundary = Square(0, 0, 20);
        var clipped = OverlayEngine.Clip(new List<OverlayFeature>
        {
            Delineation(1, Square(5, 5, 5)),    // fully inside
            Delineation(2, Square(100, 100, 5)), // fully outside
            Delineation(3, Square(15, 0, 10)),   // straddles: 5x10 inside
        }, clipBoundary);

        Assert.AreEqual(2, clipped.Count);
        Assert.AreEqual(25, clipped.Single(x => x.DelineationID == 1).Geometry.Area, AreaTolerance);
        Assert.AreEqual(50, clipped.Single(x => x.DelineationID == 3).Geometry.Area, AreaTolerance);
    }

    [TestMethod]
    public void FilterIntersecting_KeepsTouchedPiecesWhole()
    {
        // the delta-refresh semantics: a piece touching the refresh area is kept in its entirety
        var refreshArea = Square(0, 0, 6);
        var filtered = OverlayEngine.FilterIntersecting(new List<OverlayFeature>
        {
            Delineation(1, Square(5, 5, 10)),   // clips the corner -> kept WHOLE
            Delineation(2, Square(50, 50, 10)), // disjoint -> dropped
        }, refreshArea);

        Assert.AreEqual(1, filtered.Count);
        Assert.AreEqual(100, filtered[0].Geometry.Area, AreaTolerance, "intersecting piece must be kept whole, not clipped");
    }

    [TestMethod]
    public void ExplodeAndDropSlivers_ExplodesMultiPolygons_AndDropsSubMinimumPieces()
    {
        var multi = OverlayEngine.Factory2D.CreateMultiPolygon(new[]
        {
            (Polygon)Square(0, 0, 10),
            (Polygon)Square(50, 50, 0.5), // 0.25 m^2 sliver — below the 1 m^2 floor
            (Polygon)Square(100, 100, 3),
        });

        var exploded = OverlayEngine.ExplodeAndDropSlivers(new List<OverlayFeature> { Delineation(1, multi) }).ToList();

        Assert.AreEqual(2, exploded.Count);
        Assert.IsTrue(exploded.All(x => x.Polygon.Area >= 1));
        Assert.IsTrue(exploded.All(x => x.Feature.DelineationID == 1), "exploded pieces keep their source attributes");
    }
}
