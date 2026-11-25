using Microsoft.EntityFrameworkCore;
using Neptune.EFModels.Entities;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Union;

namespace Neptune.OverlayAPI.Services.Overlay;

/// <summary>
/// The Load Generating Unit overlay, shared by the existing-conditions run (LGUs) and a project's
/// run (PLGUs): which records go in and how they are combined. <see cref="Run"/> touches no
/// database, so the rules can be tested with a handful of squares; the two Load methods are the
/// only parts that read one.
/// </summary>
public static class LoadGeneratingUnitOverlay
{
    /// <param name="ModelBasins">Geometry only: the model basins bound the overlay and give no attributes.</param>
    public sealed record Inputs(
        List<OverlayFeature> RegionalSubbasins,
        List<OverlayFeature> WaterQualityManagementPlans,
        List<OverlayFeature> Delineations,
        List<OverlayFeature> ModelBasins);

    /// <summary>
    /// What goes into the existing-conditions overlay: every regional subbasin, the WQMPs that are
    /// modeled as a unit (see <see cref="LoadWaterQualityManagementPlansAsync"/>), and the verified
    /// distributed delineations on modeled BMP types.
    /// </summary>
    public static async Task<Inputs> LoadInputsAsync(NeptuneDbContext dbContext)
    {
        var regionalSubbasins = await ToRegionalSubbasinFeatures(dbContext.RegionalSubbasins.AsNoTracking()).ToListAsync();
        var delineations = await ToDelineationFeatures(Delineations.ListLoadGeneratingUnitInputs(dbContext)).ToListAsync();
        return new Inputs(regionalSubbasins, await LoadWaterQualityManagementPlansAsync(dbContext), delineations, await LoadModelBasinsAsync(dbContext));
    }

    /// <summary>
    /// What goes into a project's overlay: the regional subbasins the project touches, the same
    /// WQMPs as existing conditions, and the existing delineation inputs plus the project's own.
    /// </summary>
    public static async Task<Inputs> LoadProjectInputsAsync(NeptuneDbContext dbContext, int projectID, ICollection<int> regionalSubbasinIDs)
    {
        var regionalSubbasins = await ToRegionalSubbasinFeatures(dbContext.RegionalSubbasins.AsNoTracking()
            .Where(x => regionalSubbasinIDs.Contains(x.RegionalSubbasinID))).ToListAsync();
        var delineations = await ToDelineationFeatures(Delineations.ListProjectLoadGeneratingUnitInputs(dbContext, projectID)).ToListAsync();
        return new Inputs(regionalSubbasins, await LoadWaterQualityManagementPlansAsync(dbContext), delineations, await LoadModelBasinsAsync(dbContext));
    }

    /// <summary>
    /// The overlay itself:
    ///  1. A WQMP that treats nothing gives up the ground it shares with one that treats (NPT-1155).
    ///     WQMPs and delineations are otherwise left as drawn; overlaps among them are not resolved.
    ///  2. Everything is clipped to the modeling extent, the union of the model basins. A project's
    ///     run is clipped to the same extent, not to its own subbasins.
    ///  3. Regional subbasins are unioned with WQMPs, then with delineations, so each piece carries
    ///     the subbasin, WQMP and delineation that cover it.
    /// For a delta refresh, pass the zone around the refresh area to get back only the pieces that
    /// touch it. They come back whole; OverlayEngine.PlanDeltaRefresh narrows them to single polygons.
    /// </summary>
    public static List<OverlayFeature> Run(Inputs inputs, Geometry? keepOnlyPiecesTouching = null)
    {
        var regionalSubbasins = OverlayEngine.Clean(inputs.RegionalSubbasins);
        var delineations = OverlayEngine.Clean(inputs.Delineations);
        var modelBasins = OverlayEngine.Clean(inputs.ModelBasins);
        var waterQualityManagementPlans = OverlayEngine.YieldToTreatingPlans(OverlayEngine.Clean(inputs.WaterQualityManagementPlans));

        var modelingExtent = OverlayEngine.ToPolygonal2D(UnaryUnionOp.Union(modelBasins.Select(x => x.Geometry).ToList()));
        var clippedRegionalSubbasins = OverlayEngine.Clip(regionalSubbasins, modelingExtent);
        var clippedDelineations = OverlayEngine.Clip(delineations, modelingExtent);
        var clippedWaterQualityManagementPlans = OverlayEngine.Clip(waterQualityManagementPlans, modelingExtent);

        var regionalSubbasinsAndPlans = OverlayEngine.UnionLayers(clippedRegionalSubbasins, clippedWaterQualityManagementPlans);
        var pieces = OverlayEngine.UnionLayers(regionalSubbasinsAndPlans, clippedDelineations);

        return keepOnlyPiecesTouching != null ? OverlayEngine.FilterIntersecting(pieces, keepOnlyPiecesTouching) : pieces;
    }

    /// <summary>The single polygons that become units: slivers dropped, and only ground inside a regional subbasin.</summary>
    public static IEnumerable<(OverlayFeature Feature, Polygon Polygon)> ToUnits(IEnumerable<OverlayFeature> pieces) =>
        InARegionalSubbasin(OverlayEngine.ExplodeAndDropSlivers(pieces));

    /// <summary>A polygon outside every regional subbasin is not a unit, even under a WQMP or a delineation.</summary>
    public static IEnumerable<(OverlayFeature Feature, Polygon Polygon)> InARegionalSubbasin(IEnumerable<(OverlayFeature Feature, Polygon Polygon)> polygons) =>
        polygons.Where(x => x.Feature.RegionalSubbasinID != null);

    /// <summary>
    /// The WQMP inputs are the Active plans that are not Detailed. That rule stays in vPyQgisWaterQualityManagementPlanLGUInput
    /// because the WQMP Boundary Overlap Report view reads the same view. Whether a plan treats
    /// runoff is the solve's own test for a simple BMP it sends to the model, so the two agree.
    /// </summary>
    private static async Task<List<OverlayFeature>> LoadWaterQualityManagementPlansAsync(NeptuneDbContext dbContext)
    {
        var waterQualityManagementPlanIDsThatTreat = (await dbContext.QuickBMPs.AsNoTracking()
                .Where(QuickBMPs.IsSentToModel)
                .Select(x => x.WaterQualityManagementPlanID)
                .Distinct()
                .ToListAsync())
            .ToHashSet();

        return (await dbContext.vPyQgisWaterQualityManagementPlanLGUInputs.AsNoTracking()
                .Where(x => x.WaterQualityManagementPlanBoundary != null)
                .Select(x => new { x.WQMPID, x.WaterQualityManagementPlanBoundary })
                .ToListAsync())
            .Select(x => new OverlayFeature
            {
                Geometry = x.WaterQualityManagementPlanBoundary!,
                WaterQualityManagementPlanID = x.WQMPID,
                TreatsRunoff = waterQualityManagementPlanIDsThatTreat.Contains(x.WQMPID),
            }).ToList();
    }

    private static Task<List<OverlayFeature>> LoadModelBasinsAsync(NeptuneDbContext dbContext) =>
        dbContext.ModelBasins.AsNoTracking().Select(x => new OverlayFeature { Geometry = x.ModelBasinGeometry }).ToListAsync();

    private static IQueryable<OverlayFeature> ToRegionalSubbasinFeatures(IQueryable<RegionalSubbasin> regionalSubbasins) =>
        regionalSubbasins.Select(x => new OverlayFeature
        {
            Geometry = x.CatchmentGeometry, RegionalSubbasinID = x.RegionalSubbasinID, ModelBasinID = x.ModelBasinID,
        });

    private static IQueryable<OverlayFeature> ToDelineationFeatures(IQueryable<Delineation> delineations) =>
        delineations.Select(x => new OverlayFeature { Geometry = x.DelineationGeometry, DelineationID = x.DelineationID });
}
