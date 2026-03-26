import { Component, Input, OnChanges } from "@angular/core";
import { environment } from "src/environments/environment";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
@Component({
    selector: "delineations-layer",
    imports: [],
    templateUrl: "./delineations-layer.component.html",
    styleUrls: ["./delineations-layer.component.scss"],
})
export class DelineationsLayerComponent extends MapLayerBase implements OnChanges {
    constructor() {
        super();
    }
    @Input() isAnalyzedInModelingModule: boolean = true;
    public wmsOptions: L.WMSOptions;
    public layer;

    ngAfterViewInit(): void {
        this.wmsOptions = {
            layers: "OCStormwater:Delineations",
            transparent: true,
            format: "image/png",
            tiled: true,
            cql_filter: this.isAnalyzedInModelingModule ? "DelineationStatus = 'Verified' AND IsAnalyzedInModelingModule = 1" : "DelineationStatus = 'Verified'",
            maxZoom: 22,
        } as any;

        this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", this.wmsOptions);
        this.initLayer();
    }
}
