using NetTopologySuite.Geometries;

namespace Neptune.OverlayAPI.Services.Overlay;

/// <summary>
/// A geometry plus the (sparse) business identity it carries through the overlay pipeline.
/// Layers populate only their own fields (delineations: DelinID/SJID/TCEffect, land use blocks:
/// LUBID/SJID, etc.); layer unions merge them with first-layer-wins semantics — matching the
/// retired QGIS union, whose second-layer name clashes were suffixed (SJID_2) and ignored downstream.
/// </summary>
public sealed class OverlayFeature
{
    public required Geometry Geometry { get; set; }

    public int? DelineationID { get; init; }
    public int? OnlandVisualTrashAssessmentAreaID { get; init; }
    public int? WaterQualityManagementPlanID { get; init; }
    public int? LandUseBlockID { get; init; }
    public int? StormwaterJurisdictionID { get; init; }
    public int? RegionalSubbasinID { get; init; }
    public int? ModelBasinID { get; init; }

    // flatten winner-rule inputs; never carried into outputs
    public double? TrashCaptureEffectiveness { get; init; }
    public DateTime? AssessmentDate { get; init; }

    /// <summary>
    /// Merge for the attribute-carrying layer union: this feature's values win, the other side
    /// fills the gaps. (Fields are disjoint between layers except StormwaterJurisdictionID,
    /// where first-layer-wins reproduces the QGIS SJID/SJID_2 behavior.)
    /// </summary>
    public OverlayFeature MergedWith(OverlayFeature other, Geometry geometry) => new()
    {
        Geometry = geometry,
        DelineationID = DelineationID ?? other.DelineationID,
        OnlandVisualTrashAssessmentAreaID = OnlandVisualTrashAssessmentAreaID ?? other.OnlandVisualTrashAssessmentAreaID,
        WaterQualityManagementPlanID = WaterQualityManagementPlanID ?? other.WaterQualityManagementPlanID,
        LandUseBlockID = LandUseBlockID ?? other.LandUseBlockID,
        StormwaterJurisdictionID = StormwaterJurisdictionID ?? other.StormwaterJurisdictionID,
        RegionalSubbasinID = RegionalSubbasinID ?? other.RegionalSubbasinID,
        ModelBasinID = ModelBasinID ?? other.ModelBasinID,
    };

    public OverlayFeature WithGeometry(Geometry geometry) => new()
    {
        Geometry = geometry,
        DelineationID = DelineationID,
        OnlandVisualTrashAssessmentAreaID = OnlandVisualTrashAssessmentAreaID,
        WaterQualityManagementPlanID = WaterQualityManagementPlanID,
        LandUseBlockID = LandUseBlockID,
        StormwaterJurisdictionID = StormwaterJurisdictionID,
        RegionalSubbasinID = RegionalSubbasinID,
        ModelBasinID = ModelBasinID,
        TrashCaptureEffectiveness = TrashCaptureEffectiveness,
        AssessmentDate = AssessmentDate,
    };
}
