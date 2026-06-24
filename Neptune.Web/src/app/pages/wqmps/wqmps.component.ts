import { AsyncPipe } from "@angular/common";
import { Component } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { ColDef } from "ag-grid-community";
import { BehaviorSubject, map, Observable, shareReplay, switchMap, tap } from "rxjs";
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
import { WqmpModalComponent } from "./wqmp-modal/wqmp-modal.component";
import { WqmpUploadModalComponent } from "./wqmp-upload-modal/wqmp-upload-modal.component";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

@Component({
    selector: "wqmps",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, HybridMapGridComponent, AsyncPipe, WqmpsLayerComponent, ParcelLayerComponent, DelineationsLayerComponent, JurisdictionsLayerComponent, DropdownToggleDirective, RouterLink],
    templateUrl: "./wqmps.component.html",
})
export class WqmpsComponent {
    public OverlayMode = OverlayMode;
    public customRichTextTypeID = NeptunePageTypeEnum.WaterQualityMaintenancePlan;
    public wqmps$: Observable<WaterQualityManagementPlanGridDto[]>;
    public columnDefs$: Observable<ColDef[]>;
    public isLoading = true;
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public boundingBox$: Observable<BoundingBoxDto>;
    public selectedWaterQualityManagementPlanID: number;
    public zoomOnNextSelection = true;
    public mapIsReady = false;
    public wqmpJurisdictionIDs: number[];
    public currentPersonCanEdit$: Observable<boolean>;
    // NPT-984: Create-from-PDF (the AI workflow entry point) is Manager-level — the backend
    // upload endpoint is gated on [JurisdictionManageFeature]. Editor-level users see the
    // rest of the Actions menu (Add WQMP, Bulk Uploads via the Data Hub) but not Create from PDF.
    public currentPersonCanManage$: Observable<boolean>;
    private wqmps: WaterQualityManagementPlanGridDto[] = [];
    private reload$ = new BehaviorSubject<void>(undefined);
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

    // Match against headerName (not `field`) because utilityFunctionsService.createBasicColumnDef
    // doesn't populate ColDef.field — it uses a valueGetter against fieldName instead. A filter
    // keyed on c.field was a silent no-op (every c.field undefined → nothing removed) which is
    // why the maintenance-contact columns stayed visible to the public despite the earlier fix.
    private maintenanceContactHeaders = new Set([
        "Maintenance Contact Organization",
        "Maintenance Contact Name",
        "Maintenance Contact Address",
        "Maintenance Contact Phone",
    ]);

    ngOnInit(): void {
        // Derive columnDefs from the current user so the grid never renders with sensitive
        // Maintenance Contact columns for anonymous users. Sharing a single getCurrentUser()
        // emission avoids multiple independent auth lookups and, via switchMap below, keeps
        // wqmps$ self-contained so the template subscription order no longer matters.
        const currentUser$ = this.authenticationService.getCurrentUser().pipe(shareReplay(1));

        this.columnDefs$ = currentUser$.pipe(
            map((user) => {
                const isAnonymousOrUnassigned = !user || this.authenticationService.isUserUnassigned(user);
                return this.buildColumnDefs(isAnonymousOrUnassigned);
            }),
            shareReplay(1)
        );

        this.currentPersonCanEdit$ = currentUser$.pipe(
            map(() => this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission())
        );
        this.currentPersonCanManage$ = currentUser$.pipe(
            map(() => this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission())
        );

        this.wqmps$ = this.reload$.pipe(switchMap(() => this.loadWqmps$()));
        this.boundingBox$ = this.stormwaterJurisdictionService.getBoundingBoxStormwaterJurisdiction();
    }

    // Loads the WQMP grid data AND derives wqmpJurisdictionIDs for the map's CQL filter from
    // the same data. Keyed on the current user so the "should we filter" decision and the
    // list fetch are atomic — no class-field side-effect reliance, and works for both the
    // initial load and the post-add refresh in openAddModal().
    private loadWqmps$(): Observable<WaterQualityManagementPlanGridDto[]> {
        return this.authenticationService.getCurrentUser().pipe(
            switchMap((user) => {
                const isAnonymousOrUnassigned = !user || this.authenticationService.isUserUnassigned(user);
                const shouldFilterMapByJurisdiction =
                    !isAnonymousOrUnassigned && !this.authenticationService.isCurrentUserAnAdministrator();
                return this.waterQualityManagementPlanService.listAsGridDtoWaterQualityManagementPlan().pipe(
                    tap((wqmps) => {
                        this.wqmps = wqmps;
                        this.wqmpJurisdictionIDs = shouldFilterMapByJurisdiction
                            ? [
                                  ...new Set(
                                      wqmps
                                          .map((w) => w.StormwaterJurisdictionID)
                                          .filter((id): id is number => typeof id === "number" && Number.isFinite(id))
                                  ),
                              ]
                            : undefined;
                        this.isLoading = false;
                    })
                );
            })
        );
    }

    private buildColumnDefs(isAnonymousOrUnassigned: boolean): ColDef[] {
        const defs: ColDef[] = [
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

        return isAnonymousOrUnassigned
            ? defs.filter((c) => !this.maintenanceContactHeaders.has(c.headerName as string))
            : defs;
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
                // Re-use loadWqmps$ so the map's wqmpJurisdictionIDs is recomputed after add —
                // otherwise adding a WQMP in a new jurisdiction wouldn't show up on the filtered map.
                this.reload$.next();
            }
        });
    }

    // NPT-1051 rework: the modal now owns file selection so the user sees the upload
    // requirements before committing to a file. Previously a hidden <input type="file">
    // fired on Create-from-PDF click, which meant the OS file picker opened before the
    // user saw any constraints — out-of-order UX.
    public openCreateFromPdfModal(): void {
        const dialogRef = this.dialogService.open(WqmpUploadModalComponent, {
            width: "600px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result?.wqmpID) {
                this.router.navigate(["/water-quality-management-plans", result.wqmpID, "review"]);
            }
        });
    }
}
