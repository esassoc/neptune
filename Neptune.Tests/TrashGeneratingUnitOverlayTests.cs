using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.EFModels.Entities;
using Neptune.OverlayAPI.Services.Overlay;
using NetTopologySuite.Geometries;

namespace Neptune.Tests;

/// <summary>
/// NPT-1157: the Trash Generating Unit overlay as a whole, from inputs to units, with no database.
/// OverlayEngineTests covers the geometry operations; these cover the rules built on them: which
/// record wins an overlap, whose jurisdiction a unit takes, and which pieces are units at all.
/// </summary>
[TestClass]
public class TrashGeneratingUnitOverlayTests
{
    private const double AreaTolerance = 1e-6;
    private const int Full = (int)TrashCaptureStatusTypeEnum.Full;
    private const int Partial = (int)TrashCaptureStatusTypeEnum.Partial;
    private const int None = (int)TrashCaptureStatusTypeEnum.None;
    private const int NotProvided = (int)TrashCaptureStatusTypeEnum.NotProvided;

    private static Geometry Rectangle(double minX, double minY, double maxX, double maxY) =>
        OverlayEngine.Factory2D.CreatePolygon([
            new Coordinate(minX, minY), new Coordinate(maxX, minY), new Coordinate(maxX, maxY), new Coordinate(minX, maxY), new Coordinate(minX, minY),
        ]);

    private static OverlayFeature LandUseBlock(int id, Geometry geometry, int stormwaterJurisdictionID = 1) => new()
    {
        Geometry = geometry, LandUseBlockID = id, StormwaterJurisdictionID = stormwaterJurisdictionID,
    };

    private static OverlayFeature Delineation(int id, Geometry geometry, int trashCaptureStatusTypeID, int? effectiveness = null, int stormwaterJurisdictionID = 1) => new()
    {
        Geometry = geometry,
        DelineationID = id,
        StormwaterJurisdictionID = stormwaterJurisdictionID,
        TrashCaptureEffectiveness = TrashGeneratingUnitOverlay.TrashCaptureEffectivenessFor(trashCaptureStatusTypeID, effectiveness),
        TrashCaptureStatusPriority = TrashGeneratingUnitOverlay.TrashCaptureStatusPriorityFor(trashCaptureStatusTypeID),
    };

    private static OverlayFeature WaterQualityManagementPlan(int id, Geometry geometry, int trashCaptureStatusTypeID, int? effectiveness = null) => new()
    {
        Geometry = geometry,
        WaterQualityManagementPlanID = id,
        TrashCaptureEffectiveness = TrashGeneratingUnitOverlay.TrashCaptureEffectivenessFor(trashCaptureStatusTypeID, effectiveness),
        TrashCaptureStatusPriority = TrashGeneratingUnitOverlay.TrashCaptureStatusPriorityFor(trashCaptureStatusTypeID),
    };

    private static OverlayFeature OnlandVisualTrashAssessmentArea(int id, Geometry geometry, DateTime assessmentDate) => new()
    {
        Geometry = geometry, OnlandVisualTrashAssessmentAreaID = id, AssessmentDate = assessmentDate,
    };

    private static TrashGeneratingUnitOverlay.Result Run(
        IEnumerable<OverlayFeature>? landUseBlocks = null, IEnumerable<OverlayFeature>? delineations = null,
        IEnumerable<OverlayFeature>? waterQualityManagementPlans = null, IEnumerable<OverlayFeature>? onlandVisualTrashAssessmentAreas = null) =>
        TrashGeneratingUnitOverlay.Run(new TrashGeneratingUnitOverlay.Inputs(
            (delineations ?? []).ToList(), (onlandVisualTrashAssessmentAreas ?? []).ToList(),
            (waterQualityManagementPlans ?? []).ToList(), (landUseBlocks ?? []).ToList()));

    private static double AreaWhere(TrashGeneratingUnitOverlay.Result result, Func<OverlayFeature, bool> predicate) =>
        result.Units.Where(x => predicate(x.Feature)).Sum(x => x.Polygon.Area);

    [TestMethod]
    public void ALandUseBlockWithNothingOverIt_IsOneUnit()
    {
        var result = Run(landUseBlocks: [LandUseBlock(1, Rectangle(0, 0, 100, 100), stormwaterJurisdictionID: 7)]);

        Assert.AreEqual(1, result.Units.Count);
        var unit = result.Units.Single().Feature;
        Assert.AreEqual(1, unit.LandUseBlockID);
        Assert.AreEqual(7, unit.StormwaterJurisdictionID);
        Assert.IsNull(unit.DelineationID);
        Assert.IsNull(unit.WaterQualityManagementPlanID);
        Assert.IsNull(unit.OnlandVisualTrashAssessmentAreaID);
        Assert.AreEqual(10000, result.LandUseBlockAreas[1], AreaTolerance);
    }

    [TestMethod]
    public void GroundWithNoLandUseBlock_IsNotAUnit()
    {
        // the delineation covers the right half of the block and as much again beyond it
        var result = Run(
            landUseBlocks: [LandUseBlock(1, Rectangle(0, 0, 100, 100))],
            delineations: [Delineation(10, Rectangle(50, 0, 150, 100), Full)]);

        Assert.IsTrue(result.Units.All(x => x.Feature.LandUseBlockID == 1));
        Assert.AreEqual(10000, result.Units.Sum(x => x.Polygon.Area), AreaTolerance);
        Assert.AreEqual(5000, AreaWhere(result, x => x.DelineationID == 10), AreaTolerance);
        Assert.AreEqual(5000, AreaWhere(result, x => x.DelineationID == null), AreaTolerance);
    }

    [TestMethod]
    public void APieceCarriesEveryLayerThatCoversIt()
    {
        var everything = Rectangle(0, 0, 100, 100);
        var result = Run(
            landUseBlocks: [LandUseBlock(1, everything)],
            delineations: [Delineation(10, everything, Partial, 40)],
            waterQualityManagementPlans: [WaterQualityManagementPlan(20, everything, Full)],
            onlandVisualTrashAssessmentAreas: [OnlandVisualTrashAssessmentArea(30, everything, new DateTime(2025, 1, 1))]);

        Assert.AreEqual(1, result.Units.Count);
        var unit = result.Units.Single().Feature;
        Assert.AreEqual(1, unit.LandUseBlockID);
        Assert.AreEqual(10, unit.DelineationID);
        Assert.AreEqual(20, unit.WaterQualityManagementPlanID);
        Assert.AreEqual(30, unit.OnlandVisualTrashAssessmentAreaID);
    }

    [TestMethod]
    public void AUnitTakesItsJurisdictionFromTheLandUseBlock_NotTheDelineation()
    {
        var result = Run(
            landUseBlocks: [LandUseBlock(1, Rectangle(0, 0, 100, 100), stormwaterJurisdictionID: 7)],
            delineations: [Delineation(10, Rectangle(0, 0, 100, 100), Full, stormwaterJurisdictionID: 8)]);

        Assert.AreEqual(7, result.Units.Single().Feature.StormwaterJurisdictionID);
    }

    [TestMethod]
    public void OverlappingDelineations_TheMoreEffectiveOneTakesTheSharedGround()
    {
        // 10 and 11 share the strip x 40..60
        var result = Run(
            landUseBlocks: [LandUseBlock(1, Rectangle(0, 0, 100, 100))],
            delineations:
            [
                Delineation(10, Rectangle(0, 0, 60, 100), Partial, 30),
                Delineation(11, Rectangle(40, 0, 100, 100), Partial, 80),
            ]);

        Assert.AreEqual(4000, AreaWhere(result, x => x.DelineationID == 10), AreaTolerance);
        Assert.AreEqual(6000, AreaWhere(result, x => x.DelineationID == 11), AreaTolerance);
    }

    [TestMethod]
    public void OverlappingDelineations_FullCaptureBeatsPartial_WhateverWasTypedIn()
    {
        // full capture counts as 100 even with a lower number in the effectiveness field
        var result = Run(
            landUseBlocks: [LandUseBlock(1, Rectangle(0, 0, 100, 100))],
            delineations:
            [
                Delineation(10, Rectangle(0, 0, 60, 100), Full, 10),
                Delineation(11, Rectangle(40, 0, 100, 100), Partial, 90),
            ]);

        Assert.AreEqual(6000, AreaWhere(result, x => x.DelineationID == 10), AreaTolerance);
        Assert.AreEqual(4000, AreaWhere(result, x => x.DelineationID == 11), AreaTolerance);
    }

    [TestMethod]
    public void OverlappingDelineations_OnAnEffectivenessTie_TheBetterStatusWins_ThenTheHigherID()
    {
        var landUseBlocks = new[] { LandUseBlock(1, Rectangle(0, 0, 100, 100)) };

        // both count as 0: a partial-capture device with nothing typed in, and no capture; the lower ID has the better status
        var byStatus = Run(landUseBlocks,
            delineations: [Delineation(10, Rectangle(0, 0, 60, 100), Partial), Delineation(11, Rectangle(40, 0, 100, 100), None)]);
        Assert.AreEqual(6000, AreaWhere(byStatus, x => x.DelineationID == 10), AreaTolerance);

        var byID = Run([LandUseBlock(1, Rectangle(0, 0, 100, 100))],
            delineations: [Delineation(10, Rectangle(0, 0, 60, 100), None), Delineation(11, Rectangle(40, 0, 100, 100), None)]);
        Assert.AreEqual(6000, AreaWhere(byID, x => x.DelineationID == 11), AreaTolerance);
    }

    [TestMethod]
    public void OverlappingWaterQualityManagementPlans_FollowTheSameRule()
    {
        var result = Run(
            landUseBlocks: [LandUseBlock(1, Rectangle(0, 0, 100, 100))],
            waterQualityManagementPlans:
            [
                WaterQualityManagementPlan(20, Rectangle(0, 0, 60, 100), Full),
                WaterQualityManagementPlan(21, Rectangle(40, 0, 100, 100), NotProvided),
            ]);

        Assert.AreEqual(6000, AreaWhere(result, x => x.WaterQualityManagementPlanID == 20), AreaTolerance);
        Assert.AreEqual(4000, AreaWhere(result, x => x.WaterQualityManagementPlanID == 21), AreaTolerance);
    }

    [TestMethod]
    public void OverlappingOvtaAreas_TheMoreRecentlyAssessedOneTakesTheSharedGround()
    {
        var result = Run(
            landUseBlocks: [LandUseBlock(1, Rectangle(0, 0, 100, 100))],
            onlandVisualTrashAssessmentAreas:
            [
                OnlandVisualTrashAssessmentArea(30, Rectangle(0, 0, 60, 100), new DateTime(2025, 6, 1)),
                OnlandVisualTrashAssessmentArea(31, Rectangle(40, 0, 100, 100), new DateTime(2024, 6, 1)),
            ]);

        Assert.AreEqual(6000, AreaWhere(result, x => x.OnlandVisualTrashAssessmentAreaID == 30), AreaTolerance);
        Assert.AreEqual(4000, AreaWhere(result, x => x.OnlandVisualTrashAssessmentAreaID == 31), AreaTolerance);
    }

    [TestMethod]
    public void ADelineationAndAWqmpOnTheSameGround_AreBothKept()
    {
        // winner rules apply within a layer only; across layers a unit carries both
        var result = Run(
            landUseBlocks: [LandUseBlock(1, Rectangle(0, 0, 100, 100))],
            delineations: [Delineation(10, Rectangle(0, 0, 60, 100), Full)],
            waterQualityManagementPlans: [WaterQualityManagementPlan(20, Rectangle(40, 0, 100, 100), Full)]);

        Assert.AreEqual(2000, AreaWhere(result, x => x.DelineationID == 10 && x.WaterQualityManagementPlanID == 20), AreaTolerance);
        Assert.AreEqual(10000, result.Units.Sum(x => x.Polygon.Area), AreaTolerance);
    }

    [TestMethod]
    public void TheUnitsOfALandUseBlock_NeverTotalMoreThanTheBlock()
    {
        var result = Run(
            landUseBlocks: [LandUseBlock(1, Rectangle(0, 0, 100, 100)), LandUseBlock(2, Rectangle(100, 0, 200, 100))],
            delineations: [Delineation(10, Rectangle(50, 0, 150, 100), Full), Delineation(11, Rectangle(120, 20, 180, 80), Partial, 50)],
            waterQualityManagementPlans: [WaterQualityManagementPlan(20, Rectangle(0, 40, 200, 60), Partial, 20)],
            onlandVisualTrashAssessmentAreas: [OnlandVisualTrashAssessmentArea(30, Rectangle(80, 0, 130, 100), new DateTime(2025, 1, 1))]);

        foreach (var (landUseBlockID, landUseBlockArea) in result.LandUseBlockAreas)
        {
            Assert.AreEqual(landUseBlockArea, AreaWhere(result, x => x.LandUseBlockID == landUseBlockID), AreaTolerance);
        }
    }

    [TestMethod]
    [DataRow(Full, null, 100)]
    [DataRow(Full, 25, 100)]
    [DataRow(Partial, 25, 25)]
    [DataRow(Partial, null, 0)]
    [DataRow(None, 25, 0)]
    [DataRow(NotProvided, 25, 0)]
    public void TrashCaptureEffectiveness_ComesFromTheStatusFirst(int trashCaptureStatusTypeID, int? typedIn, int expected)
    {
        Assert.AreEqual(expected, TrashGeneratingUnitOverlay.TrashCaptureEffectivenessFor(trashCaptureStatusTypeID, typedIn));
    }
}
