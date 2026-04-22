import { Component } from "@angular/core";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { HybridMapGridComponent } from "src/app/shared/components/hybrid-map-grid/hybrid-map-grid.component";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable, tap } from "rxjs";
import { LoadGeneratingUnitService } from "src/app/shared/generated/api/load-generating-unit.service";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Map } from "leaflet";
import { NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { LoadGeneratingUnitsLayerComponent } from "src/app/shared/components/leaflet/layers/load-generating-units-layer/load-generating-units-layer.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { LoadGeneratingUnitGridDto } from "src/app/shared/generated/model/models";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";

@Component({
    selector: "load-generating-units",
    imports: [PageHeaderComponent, AlertDisplayComponent, HybridMapGridComponent, AsyncPipe, LoadGeneratingUnitsLayerComponent, RegionalSubbasinsLayerComponent, JurisdictionsLayerComponent, WqmpsLayerComponent, DelineationsLayerComponent],
    templateUrl: "./load-generating-units.component.html",
    styleUrl: "./load-generating-units.component.scss",
})
export class LoadGeneratingUnitsComponent {
    public OverlayMode = OverlayMode;
    public loadGeneratingUnits$: Observable<LoadGeneratingUnitGridDto[]>;
    public columnDefs: ColDef[];
    public map: Map;
    public layerControl: L.Control.Layers;
    public mapIsReady: boolean = false;
    public boundingBox$: Observable<any>; // Replace 'any' with the correct DTO if available
    public selectedLoadGeneratingUnitID: number;
    public isLoading: boolean = true;

    constructor(
        private loadGeneratingUnitService: LoadGeneratingUnitService,
        private jurisdictionService: StormwaterJurisdictionService,
        private utilityFunctionsService: UtilityFunctionsService,
        private alertService: AlertService
    ) {}

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createLinkColumnDef("LGU ID", "LoadGeneratingUnitID", "LoadGeneratingUnitID", {
                InRouterLink: "/load-generating-units/",
            }),
            this.utilityFunctionsService.createLinkColumnDef("TreatmentBMP", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
            }),
            this.utilityFunctionsService.createLinkColumnDef("WaterQualityManagementPlan", "WaterQualityManagementPlanName", "WaterQualityManagementPlanID", {
                InRouterLink: "/water-quality-management-plans/",
                FieldDefinitionType: "WaterQualityManagementPlan",
            }),
            this.utilityFunctionsService.createLinkColumnDef("RegionalSubbasin", "RegionalSubbasinName", "RegionalSubbasinID", {
                InRouterLink: "/regional-subbasins/",
                FieldDefinitionType: "RegionalSubbasin",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Model Basin", "ModelBasinKey"),
            this.utilityFunctionsService.createDateColumnDef("Date HRU Requested", "DateHRURequested", "short"),
            this.utilityFunctionsService.createBooleanColumnDef("Is Empty", "IsEmptyResponseFromHRUService"),
        ];
        this.loadGeneratingUnits$ = this.loadGeneratingUnitService.listLoadGeneratingUnit().pipe(tap(() => (this.isLoading = false)));
        this.boundingBox$ = this.jurisdictionService.getBoundingBoxStormwaterJurisdiction();
    }

    public handleMapReady(event: NeptuneMapInitEvent, boundingBox?: any) {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;
        if (boundingBox && this.map) {
            this.map.fitBounds([
                [boundingBox.Bottom, boundingBox.Left],
                [boundingBox.Top, boundingBox.Right],
            ]);
        }
    }

    public onSelectedLoadGeneratingUnitIDChanged(selectedLoadGeneratingUnitID: number, fromMap: boolean = false) {
        if (this.selectedLoadGeneratingUnitID == selectedLoadGeneratingUnitID) {
            return;
        }
        this.selectedLoadGeneratingUnitID = selectedLoadGeneratingUnitID;
        return this.selectedLoadGeneratingUnitID;
    }
}
