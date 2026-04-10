import { AsyncPipe } from "@angular/common";
import { Component } from "@angular/core";
import { Router } from "@angular/router";
import { ColDef } from "ag-grid-community";
import { map, Observable, shareReplay, tap } from "rxjs";
import { DialogService } from "@ngneat/dialog";
import { AuthenticationService } from "src/app/services/authentication.service";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { HybridMapGridComponent } from "src/app/shared/components/hybrid-map-grid/hybrid-map-grid.component";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { ParcelLayerComponent } from "src/app/shared/components/leaflet/layers/parcel-layer/parcel-layer.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanGridDto } from "src/app/shared/generated/model/water-quality-management-plan-grid-dto";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { DropdownToggleDirective } from "src/app/shared/directives/dropdown-toggle.directive";
import { environment } from "src/environments/environment";
import { WqmpModalComponent } from "./wqmp-modal/wqmp-modal.component";
import { WqmpUploadModalComponent } from "./wqmp-upload-modal/wqmp-upload-modal.component";

@Component({
    selector: "wqmps",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, HybridMapGridComponent, AsyncPipe, WqmpsLayerComponent, ParcelLayerComponent, DelineationsLayerComponent, JurisdictionsLayerComponent, DropdownToggleDirective],
    templateUrl: "./wqmps.component.html",
})
export class WqmpsComponent {
    public OverlayMode = OverlayMode;
    public wqmps$: Observable<WaterQualityManagementPlanGridDto[]>;
    public columnDefs: ColDef[];
    public isLoading = true;
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public boundingBox$: Observable<BoundingBoxDto>;
    public selectedWaterQualityManagementPlanID: number;
    public zoomOnNextSelection = true;
    public mapIsReady = false;
    public wqmpJurisdictionIDs: number[];
    private shouldFilterMapByJurisdiction = false;
    public siteUrl = environment.ocStormwaterToolsBaseUrl;
    public currentPersonCanEdit$: Observable<boolean>;
    private wqmps: WaterQualityManagementPlanGridDto[] = [];
    private static NO_BOUNDARY_ALERT = "WqmpNoBoundary";

    constructor(
        private waterQualityManagementPlanService: WaterQualityManagementPlanService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private authenticationService: AuthenticationService,
        private utilityFunctionsService: UtilityFunctionsService,
        private alertService: AlertService,
        private dialogService: DialogService,
        private router: Router
    ) {}

    private maintenanceContactFields = ["MaintenanceContactOrganization", "MaintenanceContactName", "MaintenanceContactAddress", "MaintenanceContactPhone"];

    ngOnInit(): void {
        this.currentPersonCanEdit$ = this.authenticationService.getCurrentUser().pipe(
            map((user) => {
                const isAnonymousOrUnassigned = !user || this.authenticationService.isUserUnassigned(user);
                if (isAnonymousOrUnassigned) {
                    this.columnDefs = this.columnDefs.filter((c) => !this.maintenanceContactFields.includes(c.field));
                }
                this.shouldFilterMapByJurisdiction = !isAnonymousOrUnassigned && !this.authenticationService.isCurrentUserAnAdministrator();
                return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
            }),
            tap((canEdit) => {
                if (canEdit && !this.columnDefs.some((c) => c.field === "IsDraft")) {
                    this.columnDefs = [...this.columnDefs, this.utilityFunctionsService.createBasicColumnDef("Is Draft", "IsDraft", {
                        ValueGetter: (params) => params.data?.IsDraft ? "Yes" : "No",
                        UseCustomDropdownFilter: true,
                    })];
                }
            }),
            shareReplay(1)
        );

        this.columnDefs = [
            this.utilityFunctionsService.createLinkColumnDef("Name", "WaterQualityManagementPlanName", "WaterQualityManagementPlanID", {
                InRouterLink: "/water-quality-management-plans/",
                FieldDefinitionType: "WaterQualityManagementPlan",
                FieldDefinitionLabelOverride: "Name",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "/jurisdictions/",
                FieldDefinitionType: "Jurisdiction",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Priority", "WaterQualityManagementPlanPriorityDisplayName", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctionsService.createBasicColumnDef("Status", "WaterQualityManagementPlanStatusDisplayName", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctionsService.createBasicColumnDef("Development Type", "WaterQualityManagementPlanDevelopmentTypeDisplayName", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctionsService.createBasicColumnDef("Land Use", "WaterQualityManagementPlanLandUseDisplayName", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctionsService.createBasicColumnDef("Permit Term", "WaterQualityManagementPlanPermitTermDisplayName", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctionsService.createDateColumnDef("Approval Date", "ApprovalDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createDateColumnDef("Date of Construction", "DateOfConstruction", "MM/dd/yyyy"),
            this.utilityFunctionsService.createBasicColumnDef("Hydromodification Applies", "HydromodificationAppliesTypeDisplayName", {
                UseCustomDropdownFilter: true,
                FieldDefinitionType: "HydromodificationApplies",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Hydrologic Subarea", "HydrologicSubareaName", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctionsService.createBasicColumnDef("Maintenance Contact Organization", "MaintenanceContactOrganization"),
            this.utilityFunctionsService.createBasicColumnDef("Maintenance Contact Name", "MaintenanceContactName"),
            this.utilityFunctionsService.createBasicColumnDef("Maintenance Contact Address", "MaintenanceContactAddress"),
            this.utilityFunctionsService.createBasicColumnDef("Maintenance Contact Phone", "MaintenanceContactPhone"),
            this.utilityFunctionsService.createDecimalColumnDef("# of Inventoried BMPs", "TreatmentBMPCount", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createDecimalColumnDef("# of Simplified BMPs", "QuickBMPCount", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createBasicColumnDef("Modeling Approach", "WaterQualityManagementPlanModelingApproachDisplayName", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctionsService.createDecimalColumnDef("# of Documents", "DocumentCount", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createBasicColumnDef("Has Required Documents", "HasRequiredDocuments", {
                FieldDefinitionType: "HasAllRequiredDocuments",
                UseCustomDropdownFilter: true,
                ValueGetter: (params) => {
                    const value = params.data?.HasRequiredDocuments;
                    if (value == null) return "";
                    return value ? "Yes" : "No";
                },
            }),
            this.utilityFunctionsService.createBasicColumnDef("Record Number", "RecordNumber"),
            this.utilityFunctionsService.createDecimalColumnDef("Recorded Parcel Acreage", "RecordedWQMPAreaInAcres", { DecimalPlacesToDisplay: 2 }),
            this.utilityFunctionsService.createDecimalColumnDef("Calculated Boundary Acreage", "CalculatedWQMPAcreage", {
                DecimalPlacesToDisplay: 1,
            }),
            this.utilityFunctionsService.createBasicColumnDef("Associated APNs", "AssociatedAPNs"),
            this.utilityFunctionsService.createDateColumnDef("Latest O&M Verification", "VerificationDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Status", "TrashCaptureStatusTypeDisplayName", {
                UseCustomDropdownFilter: true,
                FieldDefinitionType: "TrashCaptureStatus",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Trash Capture Effectiveness (%)", "TrashCaptureEffectiveness", { DecimalPlacesToDisplay: 0 }),
        ];

        this.wqmps$ = this.waterQualityManagementPlanService.listAsGridDtoWaterQualityManagementPlan().pipe(
            tap((wqmps) => {
                this.wqmps = wqmps;
                if (this.shouldFilterMapByJurisdiction) {
                    this.wqmpJurisdictionIDs = [...new Set(wqmps.map((w) => w.StormwaterJurisdictionID))];
                }
                this.isLoading = false;
            })
        );
        this.boundingBox$ = this.stormwaterJurisdictionService.getBoundingBoxStormwaterJurisdiction();
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

    public onSelectedWaterQualityManagementPlanIDChanged(selectedID: number, fromMap: boolean = false) {
        if (this.selectedWaterQualityManagementPlanID == selectedID) return;
        this.zoomOnNextSelection = !fromMap;
        this.selectedWaterQualityManagementPlanID = selectedID;

        this.alertService.removeAlertByUniqueCode(WqmpsComponent.NO_BOUNDARY_ALERT);
        if (!fromMap && selectedID) {
            const wqmp = this.wqmps.find((x) => x.WaterQualityManagementPlanID === selectedID);
            if (wqmp && !wqmp.HasBoundary) {
                this.alertService.pushAlert(
                    new Alert("This WQMP does not have a boundary defined and cannot be shown on the map.", AlertContext.Info, true, WqmpsComponent.NO_BOUNDARY_ALERT)
                );
            }
        }
    }

    public openAddModal(): void {
        const dialogRef = this.dialogService.open(WqmpModalComponent, {
            data: { mode: "add" },
            width: "800px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Water Quality Management Plan created successfully.", AlertContext.Success));
                this.wqmps$ = this.waterQualityManagementPlanService.listAsGridDtoWaterQualityManagementPlan().pipe(
                    tap((wqmps) => {
                        this.wqmps = wqmps;
                    })
                );
            }
        });
    }

    public onPdfSelected(event: Event): void {
        const input = event.target as HTMLInputElement;
        if (!input.files?.length) return;
        const file = input.files[0];
        input.value = "";

        if (!file.name.toLowerCase().endsWith(".pdf")) {
            this.alertService.pushAlert(new Alert("Only PDF files are accepted.", AlertContext.Danger));
            return;
        }

        const dialogRef = this.dialogService.open(WqmpUploadModalComponent, {
            data: { file },
            width: "600px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result?.wqmpID) {
                this.router.navigate(["/water-quality-management-plans", result.wqmpID, "review"]);
            }
        });
    }
}
