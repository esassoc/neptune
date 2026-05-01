import { AsyncPipe } from "@angular/common";
import { Component } from "@angular/core";
import { ColDef } from "ag-grid-community";
import { Observable, shareReplay } from "rxjs";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { HybridMapGridComponent } from "src/app/shared/components/hybrid-map-grid/hybrid-map-grid.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { ParcelsLayerComponent } from "src/app/shared/components/leaflet/layers/parcels-layer/parcels-layer.component";
import { NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { ParcelService } from "src/app/shared/generated/api/parcel.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { ParcelGridDto } from "src/app/shared/generated/model/parcel-grid-dto";

@Component({
    selector: "parcels",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, HybridMapGridComponent, ParcelsLayerComponent, AsyncPipe],
    templateUrl: "./parcels.component.html",
})
export class ParcelsComponent {
    public OverlayMode = OverlayMode;
    public parcels$: Observable<ParcelGridDto[]>;
    public boundingBox$: Observable<BoundingBoxDto>;
    public columnDefs: ColDef[];
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public selectedParcelID: number;
    public zoomOnNextSelection = true;

    constructor(
        private parcelService: ParcelService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private utilityFunctionsService: UtilityFunctionsService
    ) {}

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createBasicColumnDef("APN", "ParcelNumber"),
            this.utilityFunctionsService.createBasicColumnDef("Address", "ParcelAddress"),
            this.utilityFunctionsService.createBasicColumnDef("City/State", "ParcelCityState"),
            this.utilityFunctionsService.createBasicColumnDef("ZIP", "ParcelZipCode"),
            this.utilityFunctionsService.createDecimalColumnDef("Area (acres)", "ParcelAreaInAcres", { DecimalPlacesToDisplay: 2 }),
        ];

        this.parcels$ = this.parcelService.listParcel().pipe(shareReplay(1));
        this.boundingBox$ = this.stormwaterJurisdictionService.getBoundingBoxStormwaterJurisdiction();
    }

    public handleMapReady(event: NeptuneMapInitEvent, boundingBox?: BoundingBoxDto): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        if (boundingBox && this.map) {
            this.map.fitBounds([
                [boundingBox.Bottom, boundingBox.Left],
                [boundingBox.Top, boundingBox.Right],
            ]);
        }
    }

    public onSelectedParcelIDChanged(selectedID: number, fromMap: boolean = false): void {
        // No early-return on same ID: re-clicking a grid row after panning the map should
        // re-zoom (when fromMap=false flips zoomOnNextSelection back to true, the layer's
        // ngOnChanges fires and re-runs fitBounds even though selectedID didn't change).
        this.zoomOnNextSelection = !fromMap;
        this.selectedParcelID = selectedID;
    }
}
