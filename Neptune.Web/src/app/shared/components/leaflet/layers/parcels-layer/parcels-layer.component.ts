import { Component, EventEmitter, Input, Output } from "@angular/core";
import { GenericWmsWfsLayerComponent } from "../generic-wms-wfs-layer/generic-wms-wfs-layer.component";
import { OverlayMode } from "../generic-wms-wfs-layer/overlay-mode.enum";

@Component({
    selector: "parcels-layer",
    imports: [GenericWmsWfsLayerComponent],
    templateUrl: "./parcels-layer.component.html",
})
export class ParcelsLayerComponent {
    readonly WFS_FEATURE_TYPE = "OCStormwater:Parcels";
    readonly WMS_LAYER_NAME = "OCStormwater:Parcels";
    readonly IDENTIFIER_PROPERTY = "ParcelID";
    readonly OVERLAY_LABEL = "Parcels";
    readonly WMS_STYLE = "parcel";
    readonly DEFAULT_SELECTED_STYLE: L.PathOptions = {
        color: "#fcfc12",
        weight: 2,
        opacity: 0.65,
        fillOpacity: 0.1,
    };

    @Input() mode: OverlayMode = OverlayMode.ReferenceOnly;
    @Input() map: L.Map;
    @Input() layerControl: any;
    @Input() sortOrder: number = 1;
    @Input() selectedID: number;
    @Input() displayOnLoad: boolean = false;
    @Input() fitBoundsOnSelect: boolean = true;
    @Output() selected = new EventEmitter<number>();

    wfsFeatureType: string = this.WFS_FEATURE_TYPE;
    identifierProperty: string = this.IDENTIFIER_PROPERTY;
    overlayLabel: string = this.OVERLAY_LABEL;
    wmsStyle: string = this.WMS_STYLE;
    selectedStyle: L.PathOptions = this.DEFAULT_SELECTED_STYLE;
    cqlFilter: string = "1=1";
    interactive: boolean = false;
    addToLayerControl: boolean = true;
    wmsLayerName: string = this.WMS_LAYER_NAME;

    ngOnChanges(): void {
        switch (this.mode) {
            case OverlayMode.Single:
                this.displayOnLoad = true;
                this.interactive = false;
                this.addToLayerControl = false;
                this.wmsLayerName = null;
                this.cqlFilter = "1=0";
                break;
            case OverlayMode.ReferenceOnly:
                if (this.displayOnLoad === undefined || this.displayOnLoad === null) {
                    this.displayOnLoad = false;
                }
                this.interactive = false;
                this.addToLayerControl = true;
                this.wmsLayerName = this.WMS_LAYER_NAME;
                this.cqlFilter = "1=1";
                break;
            case OverlayMode.ReferenceWithInteractivity:
                this.displayOnLoad = true;
                this.interactive = true;
                this.addToLayerControl = true;
                this.wmsLayerName = this.WMS_LAYER_NAME;
                this.cqlFilter = "1=1";
                break;
        }
    }
}
