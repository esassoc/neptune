using Microsoft.EntityFrameworkCore;
using Neptune.EFModels.Entities;
using NetTopologySuite.Geometries;

namespace Neptune.OverlayAPI.Services.Overlay;

/// <summary>
/// The Trash Generating Unit overlay: which records go in, which of two overlapping records wins,
/// and which pieces become units. <see cref="Run"/> touches no database, so the rules can be tested
/// with a handful of squares; <see cref="LoadInputsAsync"/> is the only part that reads one.
/// </summary>
public static class TrashGeneratingUnitOverlay
{
    public sealed record Inputs(
        List<OverlayFeature> Delineations,
        List<OverlayFeature> OnlandVisualTrashAssessmentAreas,
        List<OverlayFeature> WaterQualityManagementPlans,
        List<OverlayFeature> LandUseBlocks);

    /// <param name="Units">One entry per Trash Generating Unit polygon.</param>
    /// <param name="LandUseBlockAreas">Area of each land use block as the overlay saw it (cleaned), by LandUseBlockID.</param>
    public sealed record Result(
        List<(OverlayFeature Feature, Polygon Polygon)> Units,
        Dictionary<int, double> LandUseBlockAreas);

    /// <summary>
    /// What goes into the overlay:
    ///  - verified delineations that are not on a project BMP;
    ///  - OVTA Areas with a Baseline Score or a Progress Score (the view shares its score definitions
    ///    with vTrashGeneratingUnitLoadStatistic, so it stays in SQL; NPT-1144);
    ///  - every WQMP boundary, whatever the plan's status;
    ///  - every land use block.
    /// </summary>
    public static async Task<Inputs> LoadInputsAsync(NeptuneDbContext dbContext)
    {
        var delineations = (await Delineations.ListTrashGeneratingUnitInputs(dbContext)
                .Select(x => new
                {
                    x.DelineationID, x.DelineationGeometry, x.TreatmentBMP.StormwaterJurisdictionID,
                    x.TreatmentBMP.TrashCaptureStatusTypeID, x.TreatmentBMP.TrashCaptureEffectiveness,
                })
                .ToListAsync())
            .Select(x => new OverlayFeature
            {
                Geometry = x.DelineationGeometry,
                DelineationID = x.DelineationID,
                StormwaterJurisdictionID = x.StormwaterJurisdictionID,
                TrashCaptureEffectiveness = TrashCaptureEffectivenessFor(x.TrashCaptureStatusTypeID, x.TrashCaptureEffectiveness),
                TrashCaptureStatusPriority = TrashCaptureStatusPriorityFor(x.TrashCaptureStatusTypeID),
            }).ToList();

        var onlandVisualTrashAssessmentAreas = await dbContext.vPyQgisOnlandVisualTrashAssessmentAreaDateds.AsNoTracking()
            .Select(x => new OverlayFeature
            {
                Geometry = x.OnlandVisualTrashAssessmentAreaGeometry,
                OnlandVisualTrashAssessmentAreaID = x.OVTAID,
                AssessmentDate = x.AssessDate,
            }).ToListAsync();

        var waterQualityManagementPlans = (await dbContext.WaterQualityManagementPlanBoundaries.AsNoTracking()
                .Where(x => x.GeometryNative != null)
                .Select(x => new
                {
                    x.WaterQualityManagementPlanID, x.GeometryNative,
                    x.WaterQualityManagementPlan.TrashCaptureStatusTypeID, x.WaterQualityManagementPlan.TrashCaptureEffectiveness,
                })
                .ToListAsync())
            .Select(x => new OverlayFeature
            {
                Geometry = x.GeometryNative!,
                WaterQualityManagementPlanID = x.WaterQualityManagementPlanID,
                TrashCaptureEffectiveness = TrashCaptureEffectivenessFor(x.TrashCaptureStatusTypeID, x.TrashCaptureEffectiveness),
                TrashCaptureStatusPriority = TrashCaptureStatusPriorityFor(x.TrashCaptureStatusTypeID),
            }).ToList();

        var landUseBlocks = await dbContext.LandUseBlocks.AsNoTracking()
            .Select(x => new OverlayFeature
            {
                Geometry = x.LandUseBlockGeometry,
                LandUseBlockID = x.LandUseBlockID,
                StormwaterJurisdictionID = x.StormwaterJurisdictionID,
            }).ToListAsync();

        return new Inputs(delineations, onlandVisualTrashAssessmentAreas, waterQualityManagementPlans, landUseBlocks);
    }

    /// <summary>
    /// The overlay itself:
    ///  1. Each layer is de-overlapped by its winner rule. Delineations and WQMPs: higher trash
    ///     capture effectiveness wins, then the better capture status, then the higher ID. OVTA
    ///     Areas: the later assessment date wins, then the higher ID.
    ///  2. The layers are unioned: (OVTA Areas with delineations) with WQMPs, then land use blocks
    ///     first, so a piece takes its jurisdiction from the land use block and not the delineation.
    ///  3. A piece with no land use block is not a unit: what is left of a delineation, WQMP or
    ///     OVTA Area over ground with no mapped land use is dropped.
    /// </summary>
    public static Result Run(Inputs inputs)
    {
        var delineations = OverlayEngine.Clean(inputs.Delineations);
        var onlandVisualTrashAssessmentAreas = OverlayEngine.Clean(inputs.OnlandVisualTrashAssessmentAreas);
        var waterQualityManagementPlans = OverlayEngine.Clean(inputs.WaterQualityManagementPlans);
        var landUseBlocks = OverlayEngine.Clean(inputs.LandUseBlocks);

        delineations = OverlayEngine.Flatten(delineations, x => x.DelineationID!.Value,
            (a, b) => LosesByTrashCaptureEffectiveness(a, b, x => x.DelineationID!.Value));
        onlandVisualTrashAssessmentAreas = OverlayEngine.Flatten(onlandVisualTrashAssessmentAreas, x => x.OnlandVisualTrashAssessmentAreaID!.Value,
            LosesByAssessmentDate);
        waterQualityManagementPlans = OverlayEngine.Flatten(waterQualityManagementPlans, x => x.WaterQualityManagementPlanID!.Value,
            (a, b) => LosesByTrashCaptureEffectiveness(a, b, x => x.WaterQualityManagementPlanID!.Value));

        var captureAndAssessment = OverlayEngine.UnionLayers(onlandVisualTrashAssessmentAreas, delineations);
        captureAndAssessment = OverlayEngine.UnionLayers(captureAndAssessment, waterQualityManagementPlans);
        var pieces = OverlayEngine.UnionLayers(landUseBlocks, captureAndAssessment);

        var units = OverlayEngine.ExplodeAndDropSlivers(pieces)
            .Where(x => x.Feature.LandUseBlockID != null && x.Feature.StormwaterJurisdictionID != null)
            .ToList();
        return new Result(units, landUseBlocks.ToDictionary(x => x.LandUseBlockID!.Value, x => x.Geometry.Area));
    }

    /// <summary>
    /// The effectiveness the winner rule compares, in percent. Full capture is 100 and no capture
    /// (or not provided) is 0 whatever was typed into the effectiveness field; only partial capture
    /// uses the field, and a blank counts as 0.
    /// </summary>
    public static int TrashCaptureEffectivenessFor(int trashCaptureStatusTypeID, int? trashCaptureEffectiveness) =>
        (TrashCaptureStatusTypeEnum)trashCaptureStatusTypeID switch
        {
            TrashCaptureStatusTypeEnum.Full => 100,
            TrashCaptureStatusTypeEnum.None or TrashCaptureStatusTypeEnum.NotProvided => 0,
            _ => trashCaptureEffectiveness ?? 0,
        };

    /// <summary>
    /// Rank of a capture status for breaking an effectiveness tie: Full 3, Partial 2, None or Not
    /// Provided 1. Equal effectiveness does not mean equal status: a partial-capture device and an
    /// unscreened inlet can both be 0, and the trash results classify ground by status.
    /// </summary>
    public static int TrashCaptureStatusPriorityFor(int trashCaptureStatusTypeID) =>
        (TrashCaptureStatusTypeEnum)trashCaptureStatusTypeID switch
        {
            TrashCaptureStatusTypeEnum.Full => 3,
            TrashCaptureStatusTypeEnum.Partial => 2,
            _ => 1,
        };

    private static bool LosesByTrashCaptureEffectiveness(OverlayFeature a, OverlayFeature b, Func<OverlayFeature, int> idOf)
    {
        var (effectA, effectB) = (a.TrashCaptureEffectiveness ?? double.MinValue, b.TrashCaptureEffectiveness ?? double.MinValue);
        if (effectA != effectB)
        {
            return effectA < effectB;
        }
        var (priorityA, priorityB) = (a.TrashCaptureStatusPriority ?? 1, b.TrashCaptureStatusPriority ?? 1);
        return priorityA != priorityB ? priorityA < priorityB : idOf(a) < idOf(b);
    }

    private static bool LosesByAssessmentDate(OverlayFeature a, OverlayFeature b)
    {
        var (dateA, dateB) = (a.AssessmentDate ?? DateTime.MinValue, b.AssessmentDate ?? DateTime.MinValue);
        return dateA != dateB
            ? dateA < dateB
            : a.OnlandVisualTrashAssessmentAreaID!.Value < b.OnlandVisualTrashAssessmentAreaID!.Value;
    }
}
