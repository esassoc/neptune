import { AfterViewInit, Component, Input, OnChanges } from "@angular/core";
import * as L from "leaflet";
import { environment } from "src/environments/environment";
import { MapLayerBase } from "../map-layer-base.component";

@Component({
    selector: "parcel-layer",
    imports: [],
    templateUrl: "./parcel-layer.component.html",
    styleUrl: "./parcel-layer.component.scss",
})
export class ParcelLayerComponent extends MapLayerBase implements OnChanges, AfterViewInit {
    @Input() styles: string = "parcel";

    constructor() {
        super();
    }
    public wmsOptions: L.WMSOptions;
    public layer;

    ngAfterViewInit(): void {
        this.wmsOptions = {
            layers: "OCStormwater:Parcels",
            transparent: true,
            format: "image/png",
            tiled: true,
            styles: this.styles,
            maxZoom: 22,
        } as any;

        this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", this.wmsOptions);
        this.initLayer();
    }
}
