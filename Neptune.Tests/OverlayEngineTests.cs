using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.OverlayAPI.Services.Overlay;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Union;

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

    // A polygon from its corners in order; the ring is closed for you.
    private static Geometry Ring(params (double X, double Y)[] corners)
    {
        return OverlayEngine.Factory2D.CreatePolygon(
            corners.Append(corners[0]).Select(c => new Coordinate(c.X, c.Y)).ToArray());
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
        if (effectA != effectB)
        {
            return effectA < effectB;
        }
        var (priorityA, priorityB) = (a.TrashCaptureStatusPriority ?? 1, b.TrashCaptureStatusPriority ?? 1);
        return priorityA != priorityB ? priorityA < priorityB : a.DelineationID!.Value < b.DelineationID!.Value;
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
    public void Flatten_TieBreaksOnCaptureStatus_BeforeID()
    {
        // The Tustin trash-map regression: an in-stream trash boom (Partial status) and an unscreened
        // inlet (No Capture) both carry TCEffect 0. Status must decide the winner — even when the
        // no-capture feature has the higher ID — or Partial acreage silently becomes Untreated.
        var boom = new OverlayFeature
        {
            Geometry = Square(0, 0, 10),
            DelineationID = 100,               // LOWER id
            TrashCaptureEffectiveness = 0,
            TrashCaptureStatusPriority = 2,    // Partial
        };
        var unscreenedInlet = new OverlayFeature
        {
            Geometry = Square(5, 0, 10),
            DelineationID = 200,               // higher id would win a bare-ID tie-break
            TrashCaptureEffectiveness = 0,
            TrashCaptureStatusPriority = 1,    // No Capture
        };

        var flattened = FlattenDelineations(new List<OverlayFeature> { unscreenedInlet, boom });

        Assert.AreEqual(100, flattened.Single(x => x.DelineationID == 100).Geometry.Area, AreaTolerance, "Partial-status boom keeps its full footprint");
        Assert.AreEqual(50, flattened.Single(x => x.DelineationID == 200).Geometry.Area, AreaTolerance, "No-capture inlet loses the contested strip");
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

    [TestMethod]
    public void Flatten_ResolvesAPairThatBecomesAContainmentAfterAnEarlierCut()
    {
        // NPT-1143: feature 1 pokes 0.3 mm out of feature 3, so the containment phase sees a partial
        // overlap and skips the pair. Cutting feature 2 out of feature 1 then puts feature 1 on the
        // 1 mm overlay grid, which removes the poke and leaves it wholly inside feature 3. The
        // overlap phase must still resolve that pair; it used to skip it because a containment is
        // not a partial overlap, leaving feature 1 whole on feature 3's ground.
        var poking = OverlayEngine.Factory2D.CreatePolygon(new[]
        {
            new Coordinate(8, 8), new Coordinate(20.0003, 8), new Coordinate(20.0003, 12), new Coordinate(8, 12), new Coordinate(8, 8),
        });

        var flattened = FlattenDelineations(new List<OverlayFeature>
        {
            Delineation(1, poking, tcEffect: 0),             // loses to both others
            Delineation(2, Square(5, 5, 5), tcEffect: 50),   // inside 3, overlaps 1, beats both
            Delineation(3, Square(0, 0, 20), tcEffect: 0),   // beats 1 on the ID tie-break
        });

        Assert.IsFalse(flattened.Any(x => x.DelineationID == 1), "feature 1 lies under two winners and must not survive");
        Assert.AreEqual(25, flattened.Single(x => x.DelineationID == 2).Geometry.Area, 0.001);
        Assert.AreEqual(375, flattened.Single(x => x.DelineationID == 3).Geometry.Area, 0.001);
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

    // ---------- NPT-1143: overlay that failed silently at floating precision ----------

    // A block lying wholly inside three overlapping areas: one very large (TCEffect 0) and two small
    // ones (TCEffect 100) that split the block between them. Captured from a case where
    // OverlayNGRobust returned an empty intersection and an untouched difference for the block
    // against the union of the flattened areas, without throwing. The coordinates are translated
    // from the original by a whole number of metres: the failure depends on the exact coordinate
    // bits, which is why the fixture is hex WKB and not rounded WKT.
    private static Geometry NestedBlockFixture(string name)
    {
        var resourceName = $"Neptune.Tests.OverlayFixtures.NestedBlock.{name}.wkb.hex";
        using var stream = typeof(OverlayEngineTests).Assembly.GetManifestResourceStream(resourceName);
        Assert.IsNotNull(stream, $"embedded fixture {resourceName} not found");
        using var reader = new StreamReader(stream);
        var geometry = new WKBReader().Read(WKBReader.HexToBytes(reader.ReadToEnd().Trim()));
        return OverlayEngine.ToPolygonal2D(geometry);
    }

    private static List<OverlayFeature> FlattenedNestedBlockAreas() => FlattenDelineations(OverlayEngine.Clean(new List<OverlayFeature>
    {
        Delineation(1, NestedBlockFixture("Area1"), tcEffect: 0),
        Delineation(2, NestedBlockFixture("Area2"), tcEffect: 100),
        Delineation(3, NestedBlockFixture("Area3"), tcEffect: 100),
    }));

    // Snapping to the 1 mm overlay grid can move the block's ~520 m boundary by under a millimetre,
    // so areas agree to well under 1 m²; the defect was off by the block's whole 9,179 m².
    private const double NestedBlockAreaTolerance = 1.0;

    [TestMethod]
    public void UnionLayers_DoesNotCountABlockTwice_WhenItLiesInsideTheOtherLayer()
    {
        var block = LandUseBlock(10, NestedBlockFixture("Block"), sjid: 7);

        var blockPieces = OverlayEngine.UnionLayers(new List<OverlayFeature> { block }, FlattenedNestedBlockAreas())
            .Where(x => x.LandUseBlockID == 10).ToList();

        Assert.AreEqual(block.Geometry.Area, blockPieces.Sum(x => x.Geometry.Area), NestedBlockAreaTolerance,
            "the block's pieces must tile the block, not cover it twice");
        Assert.AreEqual(0, blockPieces.Where(x => x.DelineationID == null).Sum(x => x.Geometry.Area), NestedBlockAreaTolerance,
            "the areas cover the whole block, so no unattributed remainder is left");
    }

    [TestMethod]
    public void IntersectionAndDifference_AreCorrect_WhenFirstOperandLiesInsideTheSecond()
    {
        var block = NestedBlockFixture("Block");
        var areasUnion = UnaryUnionOp.Union(FlattenedNestedBlockAreas().Select(x => x.Geometry).ToList());
        Assert.IsTrue(areasUnion.Contains(block.InteriorPoint), "fixture sanity: the block sits inside the unioned areas");

        // Both must be checked against known answers: the failing results (0 and the whole block)
        // add up to the block's area, so a conservation check alone passes on the defect.
        Assert.AreEqual(block.Area, OverlayEngine.Intersection(block, areasUnion).Area, NestedBlockAreaTolerance);
        Assert.AreEqual(0, OverlayEngine.Difference(block, areasUnion).Area, NestedBlockAreaTolerance);
    }

    [TestMethod]
    public void FindOverCountedSources_ReturnsOnlySourcesWhosePiecesExceedThemBeyondTolerance()
    {
        var sourceAreas = new Dictionary<int, double> { { 1, 100 }, { 2, 100 }, { 3, 100 } };
        var pieces = new List<(int SourceID, double Area)>
        {
            (1, 60), (1, 40),      // tiles its source exactly
            (2, 100), (2, 100),    // counted twice
            (3, 70), (3, 30.5),    // over, but within tolerance
        };

        var overCounted = OverlayEngine.FindOverCountedSources(pieces, sourceAreas, tolerance: 1);

        Assert.AreEqual(1, overCounted.Count);
        Assert.AreEqual(2, overCounted[0].SourceID);
        Assert.AreEqual(100, overCounted[0].SourceArea, AreaTolerance);
        Assert.AreEqual(200, overCounted[0].PieceArea, AreaTolerance);
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
    public void PlanDeltaRefresh_ReplacesOnlyThePartsNearTheRefreshArea()
    {
        // NPT-1158: one piece in two separate parts, only one of them in the refresh area. Inserting
        // the far part too, while only the near part's stored row was deleted, left one more copy of
        // the far part after every refresh.
        var twoParts = OverlayEngine.Factory2D.CreateMultiPolygon(new[]
        {
            (Polygon)Square(0, 0, 10),     // in the refresh area
            (Polygon)Square(500, 500, 10), // far from it
        });
        var pieces = new List<OverlayFeature>
        {
            Delineation(1, twoParts),
            Delineation(2, Square(5, 5, 10)),   // single part clipping the area's corner: replaced whole
            Delineation(3, Square(50, 50, 10)), // disjoint: left alone
        };
        var stored = new List<(int ID, OverlayFeature Feature)>
        {
            (101, Delineation(1, Square(0, 0, 10))),
            (102, Delineation(1, Square(500, 500, 10))),
            (103, Delineation(2, Square(5, 5, 10))),
            (104, Delineation(3, Square(50, 50, 10))),
        };
        var refreshArea = Square(0, 0, 6);

        Assert.AreEqual(2, OverlayEngine.FilterIntersecting(pieces, refreshArea).Count, "the piece-level filter keeps the two-part piece whole");

        var (deleted, inserted, matchedBeyondCandidateZone) = OverlayEngine.PlanDeltaRefresh(stored, pieces, refreshArea);

        CollectionAssert.AreEquivalent(new[] { 101, 103 }, deleted);
        Assert.AreEqual(2, inserted.Count);
        Assert.IsTrue(inserted.Single(x => x.Feature.DelineationID == 1).Polygon.Intersects(refreshArea), "only the part in the refresh area is inserted");
        Assert.AreEqual(100, inserted.Single(x => x.Feature.DelineationID == 2).Polygon.Area, AreaTolerance, "an inserted polygon is not clipped");
        Assert.AreEqual(0, matchedBeyondCandidateZone, "everything was selected by the reach test");
    }

    [TestMethod]
    public void PlanDeltaRefresh_DeletesAndInsertsTheSamePolygons_WhenStoredCopiesAreSlightlyOff()
    {
        // NPT-1158: the stored rows are the polygons of an earlier run and can sit a fraction of a
        // millimetre from where this run puts them. On the refresh area's own edge, where LGU edges
        // routinely lie, a polygon and its stored copy 0.3 mm away must still be treated as one.
        var refreshArea = Square(0, 0, 10);
        const double storedOffset = 0.0003;
        var ids = new[] { 1, 2, 3, 4 };
        Geometry Shape(int id, double offset) => id switch
        {
            1 => Square(2, 2, 3),                     // inside the area
            2 => Square(10 + offset, 0, 5),           // on the area's edge
            3 => Square(10 + offset, 10 + offset, 5), // on the area's corner
            _ => Square(40, 40, 5),                   // well clear of the area
        };
        var pieces = ids.Select(x => Delineation(x, Shape(x, 0))).ToList();
        var stored = ids.Select(x => (ID: x, Feature: Delineation(x, Shape(x, storedOffset)))).ToList();

        var (deleted, inserted, matchedBeyondCandidateZone) = OverlayEngine.PlanDeltaRefresh(stored, pieces, refreshArea);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, deleted);
        CollectionAssert.AreEquivalent(deleted, inserted.Select(x => x.Feature.DelineationID!.Value).ToList(),
            "what is deleted and what is inserted must be the same polygons");
        Assert.AreEqual(0, matchedBeyondCandidateZone, "everything was selected by the reach test");
    }

    [TestMethod]
    [DataRow("square", 0, 0.0003, DisplayName = "square: new polygon within reach, stored copy just outside")]
    [DataRow("square", 0.0003, 0, DisplayName = "square: stored copy within reach, new polygon just outside")]
    [DataRow("strip", 0, 0.0006, DisplayName = "1 mm strip: new polygon within reach, stored copy just outside")]
    [DataRow("strip", 0.0006, 0, DisplayName = "1 mm strip: stored copy within reach, new polygon just outside")]
    [DataRow("neck", 0, 0.0006, DisplayName = "narrow neck: new polygon within reach, stored copy just outside")]
    [DataRow("neck", 0.0006, 0, DisplayName = "narrow neck: stored copy within reach, new polygon just outside")]
    public void PlanDeltaRefresh_ReconcilesAPolygonOnTheEdgeOfItsReach(string shape, double newOffset, double storedOffset)
    {
        // The reach test alone only moves the sensitive edge half a metre out. A polygon lying exactly
        // on that edge is selected on one side and not the other: left alone, the first case keeps the
        // stored copy next to its replacement (a duplicate) and the second deletes the stored row
        // without replacing it (a gap). Reconciling by shared ground settles both.
        var refreshArea = Square(0, 0, 10);
        var reach = OverlayEngine.DeltaRefreshReach(refreshArea);
        var reachEdge = reach.EnvelopeInternal.MaxX;
        // The strip and the neck are the shapes an interior-point match gets wrong: shifted by more
        // than half their 1 mm width, the copies overlap but neither covers the other's interior point.
        Geometry Shape(double x) => shape switch
        {
            "square" => Square(x, 2, 5),
            // 1 mm wide, 2 km long: 2 m², above the sliver floor
            "strip" => Ring((x, 2), (x + 0.001, 2), (x + 0.001, 2002), (x, 2002)),
            // a 1 mm wide, 2 km tall neck on the reach edge, with a 1 mm tall foot running east
            _ => Ring((x, 2), (x + 3, 2), (x + 3, 2.001), (x + 0.001, 2.001), (x + 0.001, 2002), (x, 2002)),
        };
        var newPolygon = Shape(reachEdge + newOffset);
        var storedPolygon = Shape(reachEdge + storedOffset);
        Assert.IsTrue(newPolygon.Area >= 1 && newPolygon.IsValid, "sanity: the shape is a valid polygon above the sliver floor");
        Assert.AreNotEqual(reach.Intersects(newPolygon), reach.Intersects(storedPolygon), "sanity: the reach test alone treats the two differently");

        var pieces = new List<OverlayFeature>
        {
            Delineation(1, newPolygon),
            Delineation(2, Square(reachEdge + 0.2, 8, 5)), // a different unit just outside reach: not part of this
        };
        var stored = new List<(int ID, OverlayFeature Feature)>
        {
            (101, Delineation(1, storedPolygon)),
            (102, Delineation(2, Square(reachEdge + 0.2, 8, 5))),
        };

        var (deleted, inserted, matchedBeyondCandidateZone) = OverlayEngine.PlanDeltaRefresh(stored, pieces, refreshArea);

        CollectionAssert.AreEquivalent(new[] { 101 }, deleted);
        Assert.AreEqual(1, inserted.Count);
        Assert.AreEqual(1, inserted[0].Feature.DelineationID);
        // The matched polygon runs on past the zone the candidates were loaded from, so anything it
        // overlaps out there was never considered. The plan reports that so the caller can log it.
        Assert.AreEqual(1, matchedBeyondCandidateZone);
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

    // ---------- YieldToTreatingPlans (NPT-1155) ----------

    private static OverlayFeature Wqmp(int id, Geometry geometry, bool treats) => new()
    {
        Geometry = geometry,
        WaterQualityManagementPlanID = id,
        TreatsRunoff = treats,
    };

    private static double AreaOf(IEnumerable<OverlayFeature> plans, int id) =>
        plans.Where(x => x.WaterQualityManagementPlanID == id).Sum(x => x.Geometry.Area);

    [TestMethod]
    public void YieldToTreatingPlans_PlanThatTreatsNothing_GivesUpTheSharedGround()
    {
        // two 10 x 10 squares sharing a 5 x 10 strip
        var plans = new List<OverlayFeature> { Wqmp(1, Square(0, 0, 10), treats: true), Wqmp(2, Square(5, 0, 10), treats: false) };

        var result = OverlayEngine.YieldToTreatingPlans(plans);

        Assert.AreEqual(100, AreaOf(result, 1), AreaTolerance, "the plan that treats keeps all of its ground");
        Assert.AreEqual(50, AreaOf(result, 2), AreaTolerance, "the plan that treats nothing keeps only what the other does not cover");
        var treating = result.Single(x => x.WaterQualityManagementPlanID == 1).Geometry;
        var notTreating = result.Single(x => x.WaterQualityManagementPlanID == 2).Geometry;
        Assert.AreEqual(0, treating.Intersection(notTreating).Area, AreaTolerance);
    }

    [TestMethod]
    public void YieldToTreatingPlans_PlanThatTreatsNothing_InsideATreatingPlan_DropsOut()
    {
        var plans = new List<OverlayFeature> { Wqmp(1, Square(0, 0, 10), treats: true), Wqmp(2, Square(2, 2, 3), treats: false) };

        var result = OverlayEngine.YieldToTreatingPlans(plans);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(1, result.Single().WaterQualityManagementPlanID);
    }

    [TestMethod]
    public void YieldToTreatingPlans_TreatingPlanInsideOneThatTreatsNothing_IsPunchedOut()
    {
        var plans = new List<OverlayFeature> { Wqmp(1, Square(0, 0, 10), treats: false), Wqmp(2, Square(2, 2, 3), treats: true) };

        var result = OverlayEngine.YieldToTreatingPlans(plans);

        Assert.AreEqual(91, AreaOf(result, 1), AreaTolerance);
        Assert.AreEqual(9, AreaOf(result, 2), AreaTolerance);
    }

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void YieldToTreatingPlans_TwoPlansAlike_AreLeftOverlapping(bool bothTreat)
    {
        // nothing says which of two plans that both treat (or both treat nothing) is right
        var plans = new List<OverlayFeature> { Wqmp(1, Square(0, 0, 10), bothTreat), Wqmp(2, Square(5, 0, 10), bothTreat) };

        var result = OverlayEngine.YieldToTreatingPlans(plans);

        Assert.AreEqual(100, AreaOf(result, 1), AreaTolerance);
        Assert.AreEqual(100, AreaOf(result, 2), AreaTolerance);
    }

    [TestMethod]
    public void YieldToTreatingPlans_SharedEdgeOnly_ChangesNothing()
    {
        var plans = new List<OverlayFeature> { Wqmp(1, Square(0, 0, 10), treats: true), Wqmp(2, Square(10, 0, 10), treats: false) };

        var result = OverlayEngine.YieldToTreatingPlans(plans);

        Assert.AreEqual(100, AreaOf(result, 2), AreaTolerance);
        Assert.AreSame(plans[1], result.Single(x => x.WaterQualityManagementPlanID == 2), "an untouched plan is passed through as it came in");
    }

    [TestMethod]
    public void YieldToTreatingPlans_PlanThatTreatsNothing_UnderTwoTreatingPlans_LosesBoth()
    {
        var plans = new List<OverlayFeature>
        {
            Wqmp(1, Square(0, 0, 4), treats: true),
            Wqmp(2, Square(6, 0, 4), treats: true),
            Wqmp(3, Square(0, 0, 10), treats: false),
        };

        var result = OverlayEngine.YieldToTreatingPlans(plans);

        Assert.AreEqual(100 - 16 - 16, AreaOf(result, 3), AreaTolerance);
    }
}
