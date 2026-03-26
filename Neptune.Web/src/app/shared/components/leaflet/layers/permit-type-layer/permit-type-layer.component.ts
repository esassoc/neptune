import { AfterViewInit, Component, OnChanges } from "@angular/core";

import * as L from "leaflet";
import { environment } from "src/environments/environment";
import { MapLayerBase } from "../map-layer-base.component";

@Component({
    selector: "permit-type-layer",
    imports: [],
    templateUrl: "./permit-type-layer.component.html",
    styleUrl: "./permit-type-layer.component.scss",
})
export class PermitTypeLayerComponent extends MapLayerBase implements OnChanges, AfterViewInit {
    constructor() {
        super();
    }
    public wmsOptions: L.WMSOptions;
    public layer;

    ngAfterViewInit(): void {
        this.wmsOptions = {
            layers: "OCStormwater:LandUseBlocks",
            transparent: true,
            format: "image/png",
            tiled: true,
            styles: "permit_type",
            maxZoom: 22,
        } as any;

        this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", this.wmsOptions);
        this.initLayer();
    }
}
