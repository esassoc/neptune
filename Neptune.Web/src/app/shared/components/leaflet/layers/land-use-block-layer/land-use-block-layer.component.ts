import { AfterViewInit, Component, Input, OnChanges } from "@angular/core";

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
    /** NPT-1135: draw only these jurisdictions' blocks. Omit (or pass []) to draw every block; callers that want
     * none should not render the layer. */
    @Input() stormwaterJurisdictionIDs?: number[];

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
        if (this.stormwaterJurisdictionIDs?.length) {
            (this.wmsOptions as any).cql_filter = `StormwaterJurisdictionID IN (${this.stormwaterJurisdictionIDs.join(",")})`;
        }

        this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", this.wmsOptions);
        this.initLayer();
    }
}
