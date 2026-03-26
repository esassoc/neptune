import { Component, Input, OnChanges } from "@angular/core";
import { environment } from "src/environments/environment";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
@Component({
    selector: "trash-generating-unit-loads-layer",
    imports: [],
    templateUrl: "./trash-generating-unit-loads-layer.component.html",
    styleUrls: ["./trash-generating-unit-loads-layer.component.scss"],
})
export class TrashGeneratingUnitLoadsLayerComponent extends MapLayerBase implements OnChanges {
    constructor() {
        super();
    }
    @Input() style: string;
    @Input() selectedJurisdictionID: number;
    public wmsOptions: L.WMSOptions;
    public layer;
    public layerDisplayName: string;

    ngAfterViewInit(): void {
        this.layerDisplayName = this.style == "current_load" ? "Current Net Loading Rate (gal/ac/yr)" : "Net Change in Trash Loading Rate (gal/ac/yr)";
        let cql_filter = `1=1`;
        if (this.selectedJurisdictionID) {
            cql_filter = `StormwaterJurisdictionID=${this.selectedJurisdictionID}`;
        }

        this.wmsOptions = {
            layers: "OCStormwater:TrashGeneratingUnits",
            transparent: true,
            format: "image/png",
            tiled: true,
            styles: this.style,
            cql_filter: cql_filter,
            maxZoom: 22,
        } as any;

        this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", this.wmsOptions);
        if (this.style == "delta_load") {
            (this.layer as any).showEmptyTitle = true; 
        }

        this.initLayer();
    }

    ngOnChanges(changes: any): void {
        if (!this.layer) {
            return;
        }
        this.layer.wmsParams.cql_filter = `StormwaterJurisdictionID=${this.selectedJurisdictionID}`;
        this.layer.redraw();
    }
}
