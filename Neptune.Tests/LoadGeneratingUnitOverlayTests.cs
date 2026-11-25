using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neptune.OverlayAPI.Services.Overlay;
using NetTopologySuite.Geometries;

namespace Neptune.Tests;

/// <summary>
/// NPT-1157: the Load Generating Unit overlay as a whole, from inputs to units, with no database.
/// The same code runs for existing conditions (LGUs) and for a project (PLGUs); only the inputs
/// differ, and ModelingInputViewTests covers those.
/// </summary>
[TestClass]
public class LoadGeneratingUnitOverlayTests
{
    private const double AreaTolerance = 1e-6;

    private static Geometry Rectangle(double minX, double minY, double maxX, double maxY) =>
        OverlayEngine.Factory2D.CreatePolygon([
            new Coordinate(minX, minY), new Coordinate(maxX, minY), new Coordinate(maxX, maxY), new Coordinate(minX, maxY), new Coordinate(minX, minY),
        ]);

    private static OverlayFeature RegionalSubbasin(int id, Geometry geometry, int? modelBasinID = 1) => new()
    {
        Geometry = geometry, RegionalSubbasinID = id, ModelBasinID = modelBasinID,
    };

    private static OverlayFeature WaterQualityManagementPlan(int id, Geometry geometry, bool treatsRunoff = true) => new()
    {
        Geometry = geometry, WaterQualityManagementPlanID = id, TreatsRunoff = treatsRunoff,
    };

    private static OverlayFeature Delineation(int id, Geometry geometry) => new() { Geometry = geometry, DelineationID = id };

    private static OverlayFeature ModelBasin(Geometry geometry) => new() { Geometry = geometry };

    private static List<(OverlayFeature Feature, Polygon Polygon)> RunToUnits(
        IEnumerable<OverlayFeature> regionalSubbasins, IEnumerable<OverlayFeature> modelBasins,
        IEnumerable<OverlayFeature>? waterQualityManagementPlans = null, IEnumerable<OverlayFeature>? delineations = null) =>
        LoadGeneratingUnitOverlay.ToUnits(LoadGeneratingUnitOverlay.Run(new LoadGeneratingUnitOverlay.Inputs(
            regionalSubbasins.ToList(), (waterQualityManagementPlans ?? []).ToList(), (delineations ?? []).ToList(), modelBasins.ToList()))).ToList();

    private static double AreaWhere(List<(OverlayFeature Feature, Polygon Polygon)> units, Func<OverlayFeature, bool> predicate) =>
        units.Where(x => predicate(x.Feature)).Sum(x => x.Polygon.Area);

    [TestMethod]
    public void ARegionalSubbasinWithNothingOverIt_IsOneUnit()
    {
        var units = RunToUnits([RegionalSubbasin(1, Rectangle(0, 0, 100, 100), modelBasinID: 5)], [ModelBasin(Rectangle(0, 0, 100, 100))]);

        Assert.AreEqual(1, units.Count);
        var unit = units.Single().Feature;
        Assert.AreEqual(1, unit.RegionalSubbasinID);
        Assert.AreEqual(5, unit.ModelBasinID);
        Assert.IsNull(unit.WaterQualityManagementPlanID);
        Assert.IsNull(unit.DelineationID);
        Assert.AreEqual(10000, units.Single().Polygon.Area, AreaTolerance);
    }

    [TestMethod]
    public void GroundOutsideTheModelBasins_IsDropped()
    {
        // the subbasin is twice the width of the model basin
        var units = RunToUnits([RegionalSubbasin(1, Rectangle(0, 0, 200, 100))], [ModelBasin(Rectangle(0, 0, 100, 100))]);

        Assert.AreEqual(10000, units.Sum(x => x.Polygon.Area), AreaTolerance);
    }

    [TestMethod]
    public void AUnitCarriesTheSubbasinPlanAndDelineationThatCoverIt()
    {
        // plan over the right 60, delineation over the top 50: four combinations
        var units = RunToUnits(
            [RegionalSubbasin(1, Rectangle(0, 0, 100, 100))], [ModelBasin(Rectangle(0, 0, 100, 100))],
            waterQualityManagementPlans: [WaterQualityManagementPlan(20, Rectangle(40, 0, 100, 100))],
            delineations: [Delineation(10, Rectangle(0, 50, 100, 100))]);

        Assert.IsTrue(units.All(x => x.Feature.RegionalSubbasinID == 1));
        Assert.AreEqual(3000, AreaWhere(units, x => x.WaterQualityManagementPlanID == 20 && x.DelineationID == 10), AreaTolerance);
        Assert.AreEqual(3000, AreaWhere(units, x => x.WaterQualityManagementPlanID == 20 && x.DelineationID == null), AreaTolerance);
        Assert.AreEqual(2000, AreaWhere(units, x => x.WaterQualityManagementPlanID == null && x.DelineationID == 10), AreaTolerance);
        Assert.AreEqual(2000, AreaWhere(units, x => x.WaterQualityManagementPlanID == null && x.DelineationID == null), AreaTolerance);
    }

    [TestMethod]
    public void GroundOutsideEveryRegionalSubbasin_IsNotAUnit()
    {
        // the delineation runs past the subbasin but stays inside the model basin
        var units = RunToUnits(
            [RegionalSubbasin(1, Rectangle(0, 0, 100, 100))], [ModelBasin(Rectangle(0, 0, 200, 100))],
            delineations: [Delineation(10, Rectangle(50, 0, 150, 100))]);

        Assert.IsTrue(units.All(x => x.Feature.RegionalSubbasinID == 1));
        Assert.AreEqual(10000, units.Sum(x => x.Polygon.Area), AreaTolerance);
        Assert.AreEqual(5000, AreaWhere(units, x => x.DelineationID == 10), AreaTolerance);
    }

    [TestMethod]
    public void APlanThatTreatsNothing_GivesUpTheGroundItSharesWithOneThatTreats()
    {
        // 20 (treats nothing) and 21 (treats) share the strip x 40..60
        var units = RunToUnits(
            [RegionalSubbasin(1, Rectangle(0, 0, 100, 100))], [ModelBasin(Rectangle(0, 0, 100, 100))],
            waterQualityManagementPlans:
            [
                WaterQualityManagementPlan(20, Rectangle(0, 0, 60, 100), treatsRunoff: false),
                WaterQualityManagementPlan(21, Rectangle(40, 0, 100, 100), treatsRunoff: true),
            ]);

        Assert.AreEqual(4000, AreaWhere(units, x => x.WaterQualityManagementPlanID == 20), AreaTolerance);
        Assert.AreEqual(6000, AreaWhere(units, x => x.WaterQualityManagementPlanID == 21), AreaTolerance);
        Assert.AreEqual(10000, units.Sum(x => x.Polygon.Area), AreaTolerance);
    }

    [TestMethod]
    public void TwoPlansThatBothTreat_AreLeftOverlapping_SoTheSharedGroundIsCountedTwice()
    {
        // the known limitation carried on NPT-1161: nothing says which plan is right
        var units = RunToUnits(
            [RegionalSubbasin(1, Rectangle(0, 0, 100, 100))], [ModelBasin(Rectangle(0, 0, 100, 100))],
            waterQualityManagementPlans:
            [
                WaterQualityManagementPlan(20, Rectangle(0, 0, 60, 100)),
                WaterQualityManagementPlan(21, Rectangle(40, 0, 100, 100)),
            ]);

        Assert.AreEqual(6000, AreaWhere(units, x => x.WaterQualityManagementPlanID == 20), AreaTolerance);
        Assert.AreEqual(6000, AreaWhere(units, x => x.WaterQualityManagementPlanID == 21), AreaTolerance);
        Assert.AreEqual(12000, units.Sum(x => x.Polygon.Area), AreaTolerance);
    }

    [TestMethod]
    public void ADeltaRun_ReturnsOnlyThePiecesTouchingTheZone_Whole()
    {
        var inputs = new LoadGeneratingUnitOverlay.Inputs(
            [RegionalSubbasin(1, Rectangle(0, 0, 100, 100)), RegionalSubbasin(2, Rectangle(100, 0, 200, 100))],
            [], [], [ModelBasin(Rectangle(0, 0, 200, 100))]);

        var pieces = LoadGeneratingUnitOverlay.Run(inputs, keepOnlyPiecesTouching: Rectangle(10, 10, 20, 20));

        Assert.AreEqual(1, pieces.Count);
        Assert.AreEqual(1, pieces.Single().RegionalSubbasinID);
        Assert.AreEqual(10000, pieces.Single().Geometry.Area, AreaTolerance);
    }
}
