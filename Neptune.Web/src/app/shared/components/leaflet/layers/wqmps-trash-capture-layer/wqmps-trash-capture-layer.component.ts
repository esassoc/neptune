import { Component, OnChanges } from "@angular/core";
import { environment } from "src/environments/environment";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";

@Component({
    selector: "wqmps-trash-capture-layer",
    imports: [],
    templateUrl: "./wqmps-trash-capture-layer.component.html",
    styleUrls: ["./wqmps-trash-capture-layer.component.scss"],
})
export class WqmpsTrashCaptureLayerComponent extends MapLayerBase implements OnChanges {
    public iconSrc: string;
    public legendName: string;

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
            styles: "wqmp_trash_capture_status",
            maxZoom: 22,
        } as any;
        this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", this.wmsOptions);
        this.initLayer();
    }

    public ocstBaseUrl(): string {
        return environment.ocStormwaterToolsBaseUrl;
    }
}
