import { Component } from "@angular/core";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { HybridMapGridComponent } from "src/app/shared/components/hybrid-map-grid/hybrid-map-grid.component";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable, tap } from "rxjs";
import { RegionalSubbasinService } from "src/app/shared/generated/api/regional-subbasin.service";
import { RegionalSubbasinDto } from "src/app/shared/generated/model/regional-subbasin-dto";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { Map } from "leaflet";
import { NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

@Component({
    selector: "regional-subbasins",
    imports: [PageHeaderComponent, HybridMapGridComponent, AsyncPipe, RegionalSubbasinsLayerComponent, AlertDisplayComponent],
    templateUrl: "./regional-subbasins.component.html",
    styleUrl: "./regional-subbasins.component.scss",
})
export class RegionalSubbasinsComponent {
    public OverlayMode = OverlayMode;
    public regionalSubbasins$: Observable<RegionalSubbasinDto[]>;
    public columnDefs: ColDef[];
    public map: Map;
    public layerControl: L.Control.Layers;
    public mapIsReady: boolean = false;
    public boundingBox$: Observable<BoundingBoxDto>;
    public selectedRegionalSubbasinID: number;
    public isLoading: boolean = true;
    public isSitkaAdmin = false;

    constructor(
        private regionalSubbasinService: RegionalSubbasinService,
        private jurisdictionService: StormwaterJurisdictionService,
        private utilityFunctionsService: UtilityFunctionsService,
        private authenticationService: AuthenticationService,
        private confirmService: ConfirmService,
        private alertService: AlertService
    ) {}

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createLinkColumnDef("RSB ID", "RegionalSubbasinID", "RegionalSubbasinID", {
                InRouterLink: "/regional-subbasins/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Drain ID", "DrainID"),
            this.utilityFunctionsService.createBasicColumnDef("Watershed", "Watershed"),
            this.utilityFunctionsService.createBasicColumnDef("Area (sq mi)", "AreaSqMi"),
            this.utilityFunctionsService.createBasicColumnDef("Catchment ID in OC Survey", "OCSurveyCatchmentID"),
            this.utilityFunctionsService.createBasicColumnDef("Downstream Catchment ID in OC Survey", "OCSurveyDownstreamCatchmentID"),
        ];
        this.regionalSubbasins$ = this.regionalSubbasinService.listRegionalSubbasin().pipe(tap(() => (this.isLoading = false)));
        this.boundingBox$ = this.jurisdictionService.getBoundingBoxStormwaterJurisdiction();
        this.authenticationService.getCurrentUser().subscribe((user) => {
            this.isSitkaAdmin = user?.RoleID === RoleEnum.SitkaAdmin;
        });
    }

    public handleMapReady(event: NeptuneMapInitEvent, boundingBox?: BoundingBoxDto) {
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

    refreshFromOCSurvey(): void {
        this.confirmService
            .confirm({
                title: "Refresh Regional Subbasins",
                message: "Are you sure you want to refresh the Regional Subbasins from OC Survey? This can take a while to run.",
                buttonTextYes: "Refresh",
                buttonTextNo: "Cancel",
                buttonClassYes: "btn-primary",
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.regionalSubbasinService.enqueueRefreshRegionalSubbasin().subscribe(() => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("Regional Subbasins refresh will run in the background.", AlertContext.Success));
                    });
                }
            });
    }

    public onSelectedRegionalSubbasinIDChanged(selectedRegionalSubbasinID: number, fromMap: boolean = false) {
        if (this.selectedRegionalSubbasinID == selectedRegionalSubbasinID) {
            return;
        }
        this.selectedRegionalSubbasinID = selectedRegionalSubbasinID;
        return this.selectedRegionalSubbasinID;
    }
}
