import { AfterViewInit, Component, Input, OnChanges } from "@angular/core";
import { environment } from "src/environments/environment";
import { MapLayerBase } from "../map-layer-base.component";
import * as L from "leaflet";

// NPT-1126: which score the OVTA Areas are colored by. Each maps to a GeoServer style on the same layer;
// ovta_score (the layer default) colors by the area's most recently completed OVTA.
export type OvtaAreaScoreType = "MostRecent" | "Baseline" | "Progress";

const STYLE_BY_SCORE_TYPE: Record<OvtaAreaScoreType, string> = {
    MostRecent: "ovta_score",
    Baseline: "ovta_baseline_score",
    Progress: "ovta_progress_score",
};

@Component({
    selector: "ovta-areas-layer",
    imports: [],
    templateUrl: "./ovta-areas-layer.component.html",
    styleUrl: "./ovta-areas-layer.component.scss",
})
export class OvtaAreasLayerComponent extends MapLayerBase implements OnChanges, AfterViewInit {
    constructor() {
        super();
    }
    @Input() selectedJurisdictionID: number;
    @Input() scoreType: OvtaAreaScoreType = "MostRecent";
    public wmsOptions: L.WMSOptions;
    public layer;

    ngAfterViewInit(): void {
        this.wmsOptions = {
            layers: "OCStormwater:OnlandVisualTrashAssessmentAreas",
            styles: this.getStyle(),
            transparent: true,
            format: "image/png",
            tiled: true,
            cql_filter: this.getCqlFilter(),
            maxZoom: 22,
        } as any;

        this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", this.wmsOptions);
        this.initLayer();
    }

    ngOnChanges(changes: any): void {
        if (!this.layer) {
            return;
        }
        this.layer.wmsParams.cql_filter = this.getCqlFilter();
        this.layer.wmsParams.styles = this.getStyle();
        this.layer.redraw();
    }

    private getCqlFilter(): string {
        return this.selectedJurisdictionID ? `StormwaterJurisdictionID=${this.selectedJurisdictionID}` : "1=1";
    }

    private getStyle(): string {
        return STYLE_BY_SCORE_TYPE[this.scoreType] ?? STYLE_BY_SCORE_TYPE.MostRecent;
    }
}
