import { AfterViewInit, Component, OnChanges } from "@angular/core";

import * as L from "leaflet";
import { environment } from "src/environments/environment";
import { MapLayerBase } from "../map-layer-base.component";

@Component({
    selector: "land-use-block-layer",
    imports: [],
    templateUrl: "./land-use-block-layer.component.html",
    styleUrl: "./land-use-block-layer.component.scss",
})
export class LandUseBlockLayerComponent extends MapLayerBase implements OnChanges, AfterViewInit {
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
            maxZoom: 22,
        } as any;

        this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", this.wmsOptions);
        this.initLayer();
    }
}
