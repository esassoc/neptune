import { Component, EventEmitter, Input, OnChanges, Output } from "@angular/core";
import * as L from "leaflet";
import { GenericWmsWfsLayerComponent } from "../generic-wms-wfs-layer/generic-wms-wfs-layer.component";

/**
 * NPT-981 Round 2: refactored to wrap GenericWmsWfsLayerComponent (parcels-layer pattern)
 * so the delineations layer can emit a `selected` event when the user clicks a polygon.
 * Round 1 only added a passive WMS tile with no click handler — KE flagged that delineations
 * were visible but couldn't populate the side panel.
 *
 * Two layer-control entries get emitted via two parent instances:
 *   <delineations-layer [delineationStatus]="'Verified'" ...></delineations-layer>
 *   <delineations-layer [delineationStatus]="'Provisional'" ...></delineations-layer>
 * Each has its own cql_filter and overlay label.
 */
@Component({
    selector: "delineations-layer",
    imports: [GenericWmsWfsLayerComponent],
    templateUrl: "./delineations-layer.component.html",
    styleUrls: ["./delineations-layer.component.scss"],
})
export class DelineationsLayerComponent implements OnChanges {
    readonly WFS_FEATURE_TYPE = "OCStormwater:Delineations";
    readonly WMS_LAYER_NAME = "OCStormwater:Delineations";
    readonly IDENTIFIER_PROPERTY = "DelineationID";
    readonly WMS_STYLE = "delineation";

    @Input() delineationStatus: "Verified" | "Provisional" = "Verified";
    @Input() isAnalyzedInModelingModule: boolean = true;
    @Input() map: L.Map;
    @Input() layerControl: any;
    @Input() sortOrder: number = 1;
    @Input() displayOnLoad: boolean = true;
    /** Emits the clicked DelineationID. Selection of the topmost feature is handled by the
     *  WMS GetFeatureInfo response in GenericWmsWfsLayerComponent — the API returns features
     *  in draw order, so `features[0]` is the topmost. */
    @Output() selected = new EventEmitter<number>();

    public cqlFilter: string = "1=1";
    public overlayLabel: string = "Delineations";
    public legendHtml: string = "";

    ngOnChanges(): void {
        let cqlFilter = `DelineationStatus = '${this.delineationStatus}'`;
        if (this.delineationStatus === "Verified" && this.isAnalyzedInModelingModule) {
            cqlFilter += " AND IsAnalyzedInModelingModule = 1";
        }
        this.cqlFilter = cqlFilter;
        this.overlayLabel = `${this.delineationStatus} Delineations`;
        // NPT-981 r2 KE feedback: build inline HTML swatches rather than serving Geoserver's
        // GetLegendGraphic (which emits 6 SLD rules including WQMP Boundary + status-suffixed
        // labels) and rather than the bundled static PNGs (which bake the "(Verified)" /
        // "(Provisional)" suffix into the image). The group header above ("Verified Delineations"
        // / "Provisional Delineations") already conveys status, so the swatches just need
        // Centralized + Distributed labels. Colors approximate the legacy MVC delineation map
        // palette: verified gets the saturated purple / blue pair; provisional uses the
        // lighter magenta / light-blue pair so the two statuses are visually distinct.
        this.legendHtml = this.delineationStatus === "Verified"
            ? this.buildSwatchHtml("#d4b8e4", "#7e4d8a", "#b8c5e0", "#3a55a0")
            : this.buildSwatchHtml("#e6bdd4", "#b04880", "#c5d4ea", "#6378b8");
    }

    private buildSwatchHtml(centralizedFill: string, centralizedStroke: string, distributedFill: string, distributedStroke: string): string {
        const swatch = (fill: string, stroke: string, label: string) =>
            `<div style="display:flex;align-items:center;margin-bottom:3px;font-size:12px;line-height:1;">` +
            `<span style="display:inline-block;width:14px;height:14px;background:${fill};border:2px solid ${stroke};vertical-align:middle;margin-right:6px;"></span>` +
            `${label}</div>`;
        return swatch(centralizedFill, centralizedStroke, "Centralized") + swatch(distributedFill, distributedStroke, "Distributed");
    }
}
