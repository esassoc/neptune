import { Component, OnChanges } from "@angular/core";
import { environment } from "src/environments/environment";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
@Component({
    selector: "wqmps-layer",
    imports: [],
    templateUrl: "./wqmps-layer.component.html",
    styleUrls: ["./wqmps-layer.component.scss"],
})
export class WqmpsLayerComponent extends MapLayerBase implements OnChanges {
    constructor() {
        super();
    }
    public wmsOptions: L.WMSOptions;
    public layer;

    ngAfterViewInit(): void {
        this.wmsOptions = {
            layers: "OCStormwater:WaterQualityManagementPlans",
            transparent: true,
            format: "image/png",
            tiled: true,
            maxZoom: 22,
        } as any;

        this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", this.wmsOptions);
        this.initLayer();
    }
}
