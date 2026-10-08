import * as L from "leaflet";

// Shared Leaflet styles for the entity polygons we draw as WFS vectors, kept in one place so the
// app matches what GeoServer renders for the same layers (NPT-1123). The numbers below are the
// long-standing convention across the layer components: 2px stroke, 0.65 stroke opacity, 0.1 fill.
const POLYGON_BASE = { weight: 2, opacity: 0.65, fillOpacity: 0.1 };

// Fill opacity used while a feature is hovered or linked-hovered from a list row.
export const POLYGON_HIGHLIGHT_FILL_OPACITY = 0.5;
export const POLYGON_DEFAULT_FILL_OPACITY = POLYGON_BASE.fillOpacity;

// Mirrors Neptune.GeoServer/data_dir/styles/water_quality_management_plan.sld, the default style of
// OCStormwater:WaterQualityManagementPlans, so a WQMP drawn as a vector reads the same as the WMS tiles.
export const WQMP_BOUNDARY_STYLE: L.PathOptions = { color: "#de54b2", ...POLYGON_BASE };

// Mirrors Neptune.GeoServer/data_dir/styles/ovta_score.sld, the default style of
// OCStormwater:OnlandVisualTrashAssessmentAreas. Keyed by the WFS feature's Score property;
// "null" covers a feature whose Score came back null.
export const OVTA_AREA_STYLE_BY_SCORE: { [score: string]: L.PathOptions & { graphicFill?: string } } = {
    A: { color: "#00FF00", ...POLYGON_BASE, graphicFill: "Slash" },
    B: { color: "#ebc400", ...POLYGON_BASE },
    C: { color: "#FF7F7F", ...POLYGON_BASE },
    D: { color: "#c500ff", ...POLYGON_BASE },
    "Not Assessed": { color: "#808080", ...POLYGON_BASE },
    null: { color: "#808080", ...POLYGON_BASE },
};

// An OVTA area whose Score is missing entirely still needs a style.
export const OVTA_AREA_FALLBACK_STYLE: L.PathOptions = OVTA_AREA_STYLE_BY_SCORE["Not Assessed"];

export function ovtaAreaStyleForScore(score: string | null | undefined): L.PathOptions {
    return OVTA_AREA_STYLE_BY_SCORE[score as string] ?? OVTA_AREA_FALLBACK_STYLE;
}

// The generic "this feature is selected" yellow used across jurisdictions, parcels, subbasins, LGUs and OVTA areas.
export const SELECTED_FEATURE_STYLE: L.PathOptions = { color: "#fcfc12", ...POLYGON_BASE };
