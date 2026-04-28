import { Component, Input, OnInit, OnChanges, SimpleChanges } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { DatePipe, AsyncPipe, CommonModule } from "@angular/common";
import { Observable, of } from "rxjs";
import { shareReplay, switchMap, tap } from "rxjs/operators";
import { DialogService } from "@ngneat/dialog";
import { ColDef } from "ag-grid-community";
import * as L from "leaflet";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneMapComponent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { ModeledBmpPerformanceComponent } from "src/app/shared/components/modeled-bmp-performance/modeled-bmp-performance.component";
import { LandUseTableComponent } from "src/app/shared/components/land-use-table/land-use-table.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanDto } from "src/app/shared/generated/model/water-quality-management-plan-dto";
import { WaterQualityManagementPlanDocumentDto } from "src/app/shared/generated/model/water-quality-management-plan-document-dto";
import { QuickBMPDto } from "src/app/shared/generated/model/quick-bmp-dto";
import { SourceControlBMPDto } from "src/app/shared/generated/model/source-control-bmp-dto";
import { WaterQualityManagementPlanVerifyGridDto } from "src/app/shared/generated/model/water-quality-management-plan-verify-grid-dto";
import { ProjectLoadReducingResultDto } from "src/app/shared/generated/model/project-load-reducing-result-dto";
import { TreatmentBMPHRUCharacteristicsSummarySimpleDto } from "src/app/shared/generated/model/treatment-bmphru-characteristics-summary-simple-dto";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { TrashCaptureStatusTypeEnum } from "src/app/shared/generated/enum/trash-capture-status-type-enum";
import { WaterQualityManagementPlanStatusEnum } from "src/app/shared/generated/enum/water-quality-management-plan-status-enum";
import {
    WaterQualityManagementPlanModelingApproachEnum,
    WaterQualityManagementPlanModelingApproaches,
} from "src/app/shared/generated/enum/water-quality-management-plan-modeling-approach-enum";
import {
    WaterQualityManagementPlanDocumentTypeEnum,
    WaterQualityManagementPlanDocumentTypes,
} from "src/app/shared/generated/enum/water-quality-management-plan-document-type-enum";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { DryWeatherFlowOverrides } from "src/app/shared/generated/enum/dry-weather-flow-override-enum";
import { WqmpModalComponent } from "src/app/pages/wqmps/wqmp-modal/wqmp-modal.component";
import { EditModelingApproachModalComponent, EditModelingApproachModalContext } from "src/app/pages/wqmps/wqmp-detail/edit-modeling-approach-modal/edit-modeling-approach-modal.component";
import { EditTreatmentBMPsModalComponent, EditTreatmentBMPsModalContext } from "src/app/pages/wqmps/wqmp-detail/edit-treatment-bmps-modal/edit-treatment-bmps-modal.component";
import { GroupByPipe } from "src/app/shared/pipes/group-by.pipe";
import { SumPipe } from "src/app/shared/pipes/sum.pipe";
import { environment } from "src/environments/environment";

@Component({
    selector: "wqmp-detail",
    templateUrl: "./wqmp-detail.component.html",
    styleUrls: ["./wqmp-detail.component.scss"],
    standalone: true,
    imports: [
        CommonModule,
        RouterLink,
        DatePipe,
        AsyncPipe,
        PageHeaderComponent,
        AlertDisplayComponent,
        FieldDefinitionComponent,
        NeptuneMapComponent,
        WqmpsLayerComponent,
        NeptuneGridComponent,
        IconComponent,
        ModeledBmpPerformanceComponent,
        LandUseTableComponent,
        LoadingDirective,
    ],
})
export class WqmpDetailComponent implements OnInit, OnChanges {
    @Input() waterQualityManagementPlanID!: number;

    public OverlayMode = OverlayMode;
    public TrashCaptureStatusTypeEnum = TrashCaptureStatusTypeEnum;
    public WaterQualityManagementPlanModelingApproachEnum = WaterQualityManagementPlanModelingApproachEnum;
    public WaterQualityManagementPlanStatusEnum = WaterQualityManagementPlanStatusEnum;
    public aboutModelingBMPPerformanceUrl = `${environment.ocStormwaterToolsBaseUrl}/Home/AboutModelingBMPPerformance`;
    public apiBaseUrl = environment.mainAppApiUrl;

    wqmp$: Observable<WaterQualityManagementPlanDto>;
    quickBMPs$: Observable<QuickBMPDto[]>;
    quickBMPTotalRow: any[] = [];
    sourceControlBMPs$: Observable<SourceControlBMPDto[]>;
    documents$: Observable<WaterQualityManagementPlanDocumentDto[]>;
    verifications$: Observable<WaterQualityManagementPlanVerifyGridDto[]>;
    modeledPerformance$: Observable<ProjectLoadReducingResultDto | null>;

    // Land use statistics
    hruCharacteristicsSummaries: TreatmentBMPHRUCharacteristicsSummarySimpleDto[] = [];
    hasHRUData = false;

    // Map state
    map: L.Map;
    layerControl: L.Control.Layers;
    mapIsReady = false;
    boundingBox: BoundingBoxDto;
    private treatmentBMPsLayer: L.FeatureGroup;
    private delineationsLayer: L.TileLayer;
    private pendingWqmpForMap: WaterQualityManagementPlanDto;

    // Permissions — read live from AuthenticationService each CD pass via getters/methods so
    // the template re-evaluates after the auth service finishes loading the current user.
    // Using eager fields populated from a fire-and-forget subscribe was timing-fragile and
    // could leave gated buttons hidden when the user resolved after first template render
    // (the JE/JM Begin-button bug from NPT-995's first round).
    currentUser: PersonDto;

    public get currentPersonCanManage(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    public get currentPersonCanEdit(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    }

    public get isAnonymousOrUnassigned(): boolean {
        return !this.currentUser || this.currentUser.RoleID == RoleEnum.Unassigned;
    }

    // Grid column defs
    parcelColumnDefs: ColDef[];
    treatmentBMPColumnDefs: ColDef[];
    quickBMPColumnDefs: ColDef[];
    verificationColumnDefs: ColDef[];

    // Source control BMPs grouped by category
    sourceControlBMPsByCategory: { category: string; bmps: SourceControlBMPDto[] }[] = [];

    // Documents grouped by type
    documentsByType: { typeDisplayName: string; documents: WaterQualityManagementPlanDocumentDto[] }[] = [];

    // Modeling approach descriptions
    modelingApproachDescriptions: { [key: number]: string } = {
        [WaterQualityManagementPlanModelingApproachEnum.Detailed]:
            "This WQMP is modeled by inventorying the associated structural BMPs and defining their delineations. The performance of each BMP is modeled based on its modeling parameters and the attributes of the delineated tributary area.",
        [WaterQualityManagementPlanModelingApproachEnum.Simplified]:
            "This WQMP is modeled by entering simplified structural BMP modeling parameters directly on this WQMP page.",
    };

    constructor(
        private wqmpService: WaterQualityManagementPlanService,
        private utilityFunctionsService: UtilityFunctionsService,
        private authenticationService: AuthenticationService,
        private dialogService: DialogService,
        private alertService: AlertService,
        private groupByPipe: GroupByPipe,
        private sumPipe: SumPipe,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.initColumnDefs();
        this.loadData();

        this.authenticationService.getCurrentUser().subscribe((user) => {
            this.currentUser = user;
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["waterQualityManagementPlanID"] && !changes["waterQualityManagementPlanID"].firstChange) {
            this.loadData();
        }
    }

    private loadData(): void {
        this.boundingBox = undefined;
        this.wqmp$ = this.wqmpService.getWaterQualityManagementPlan(this.waterQualityManagementPlanID).pipe(
            tap((wqmp) => {
                if (wqmp?.WaterQualityManagementPlanBoundaryBBox) {
                    const parts = wqmp.WaterQualityManagementPlanBoundaryBBox.split(",").map(Number);
                    if (parts.length === 4) {
                        this.boundingBox = new BoundingBoxDto({
                            Left: parts[0],
                            Bottom: parts[1],
                            Right: parts[2],
                            Top: parts[3],
                        });
                    }
                }
                if (this.map) {
                    this.addTreatmentBMPsLayer(wqmp);
                } else {
                    this.pendingWqmpForMap = wqmp;
                }
            }),
            shareReplay(1)
        );

        this.quickBMPs$ = this.wqmpService.listQuickBMPsWaterQualityManagementPlan(this.waterQualityManagementPlanID).pipe(
            tap((quickBMPs) => {
                const totalPercentOfSiteTreated = quickBMPs.reduce((sum, bmp) => sum + (bmp.PercentOfSiteTreated ?? 0), 0);
                this.quickBMPTotalRow = [{ QuickBMPName: "Total", PercentOfSiteTreated: totalPercentOfSiteTreated }];
            })
        );
        this.documents$ = this.wqmpService.listDocumentsWaterQualityManagementPlan(this.waterQualityManagementPlanID).pipe(
            tap((docs) => {
                this.documentsByType = WaterQualityManagementPlanDocumentTypes.map((docType) => ({
                    typeDisplayName: docType.DisplayName,
                    documents: docs
                        .filter((d) => d.WaterQualityManagementPlanDocumentTypeID === docType.Value)
                        .sort((a, b) => (a.DisplayName ?? "").localeCompare(b.DisplayName ?? "")),
                }));
            })
        );
        this.verifications$ = this.wqmpService.listVerificationsWaterQualityManagementPlan(this.waterQualityManagementPlanID);

        this.modeledPerformance$ = this.wqmpService.getModeledPerformanceWaterQualityManagementPlan(this.waterQualityManagementPlanID);

        this.wqmpService.listHRUCharacteristicsWaterQualityManagementPlan(this.waterQualityManagementPlanID).subscribe((hruCharacteristics) => {
            const grouped = this.groupByPipe.transform(hruCharacteristics, "HRUCharacteristicLandUseCodeDisplayName");
            this.hruCharacteristicsSummaries = Object.entries(grouped).map(
                ([key, value]) =>
                    new TreatmentBMPHRUCharacteristicsSummarySimpleDto({
                        Area: this.sumPipe.transform(value, "Area"),
                        ImperviousCover: this.sumPipe.transform(value, "ImperviousAcres"),
                        LandUse: key,
                    })
            );
            this.hasHRUData = this.hruCharacteristicsSummaries.length > 0;
        });

        this.sourceControlBMPs$ = this.wqmpService.listSourceControlBMPsWaterQualityManagementPlan(this.waterQualityManagementPlanID).pipe(
            tap((bmps) => {
                const grouped = this.groupByPipe.transform(bmps, "SourceControlBMPAttributeCategoryName");
                this.sourceControlBMPsByCategory = Object.entries(grouped).map(([category, items]) => ({
                    category,
                    bmps: items as SourceControlBMPDto[],
                }));
            })
        );
    }

    private addTreatmentBMPsLayer(wqmp: WaterQualityManagementPlanDto): void {
        // Clear previous layers
        if (this.treatmentBMPsLayer) {
            this.map.removeLayer(this.treatmentBMPsLayer);
            this.layerControl.removeLayer(this.treatmentBMPsLayer);
        }
        if (this.delineationsLayer) {
            this.map.removeLayer(this.delineationsLayer);
            this.layerControl.removeLayer(this.delineationsLayer);
        }

        if (wqmp.TreatmentBMPs?.length) {
            const bmpIds = wqmp.TreatmentBMPs.map((b) => b.TreatmentBMPID);

            // Delineations WMS layer filtered to this WQMP's BMPs
            this.delineationsLayer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", {
                layers: "OCStormwater:Delineations",
                transparent: true,
                format: "image/png",
                tiled: true,
                cql_filter: `TreatmentBMPID IN (${bmpIds.join(",")})`,
                maxZoom: 22,
            } as any);
            this.delineationsLayer.addTo(this.map);
            this.layerControl.addOverlay(this.delineationsLayer, "Delineations");

            // Treatment BMP markers from DTO data
            this.treatmentBMPsLayer = L.featureGroup();
            for (const bmp of wqmp.TreatmentBMPs) {
                if (bmp.Latitude != null && bmp.Longitude != null) {
                    const marker = L.marker([bmp.Latitude, bmp.Longitude], {
                        icon: MarkerHelper.inventoriedTreatmentBMPMarker,
                    });
                    const popupContainer = document.createElement("div");
                    const nameLabel = document.createElement("b");
                    nameLabel.textContent = "Name: ";
                    const nameLink = document.createElement("a");
                    nameLink.href = `/treatment-bmps/${bmp.TreatmentBMPID}`;
                    nameLink.textContent = bmp.TreatmentBMPName;
                    popupContainer.appendChild(nameLabel);
                    popupContainer.appendChild(nameLink);
                    popupContainer.appendChild(document.createElement("br"));
                    const typeLabel = document.createElement("b");
                    typeLabel.textContent = "Type: ";
                    popupContainer.appendChild(typeLabel);
                    popupContainer.appendChild(document.createTextNode(bmp.TreatmentBMPTypeName ?? ""));
                    marker.bindPopup(popupContainer);
                    marker.addTo(this.treatmentBMPsLayer);
                }
            }
            this.treatmentBMPsLayer.addTo(this.map);
            this.layerControl.addOverlay(this.treatmentBMPsLayer, "Treatment BMPs");

            // If no WQMP boundary, zoom to fit the BMP markers
            if (!this.boundingBox && this.treatmentBMPsLayer.getBounds()?.isValid()) {
                this.map.fitBounds(this.treatmentBMPsLayer.getBounds());
            }
        }
    }

    private initColumnDefs(): void {
        this.parcelColumnDefs = [
            this.utilityFunctionsService.createBasicColumnDef("Parcel Number", "ParcelNumber"),
        ];

        this.treatmentBMPColumnDefs = [
            this.utilityFunctionsService.createLinkColumnDef("Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Type", "TreatmentBMPTypeName"),
            this.utilityFunctionsService.createBasicColumnDef("Notes", "Notes", { Width: 200 }),
            this.utilityFunctionsService.createBasicColumnDef("Delineation Status", "DelineationStatus"),
            this.utilityFunctionsService.createDecimalColumnDef("Delineation Area (ac)", "Area", { DecimalPlacesToDisplay: 2 }),
        ];

        const percentFormatter = (params) => (params.value != null ? `${Math.round(params.value)}%` : "");
        this.quickBMPColumnDefs = [
            this.utilityFunctionsService.createBasicColumnDef("Name", "QuickBMPName"),
            this.utilityFunctionsService.createBasicColumnDef("Type", "TreatmentBMPTypeName"),
            this.utilityFunctionsService.createBasicColumnDef("Notes", "QuickBMPNote", { Width: 200 }),
            this.utilityFunctionsService.createBasicColumnDef("# Individual BMPs", "NumberOfIndividualBMPs"),
            { headerName: "% Site Treated", field: "PercentOfSiteTreated", valueFormatter: percentFormatter, cellStyle: { "justify-content": "flex-end" } },
            { headerName: "% Captured", field: "PercentCaptured", valueFormatter: percentFormatter, cellStyle: { "justify-content": "flex-end" } },
            { headerName: "% Retained", field: "PercentRetained", valueFormatter: percentFormatter, cellStyle: { "justify-content": "flex-end" } },
            {
                headerName: "Dry Weather Flow Override",
                field: "DryWeatherFlowOverrideID",
                valueGetter: (params) => {
                    if (params.node?.rowPinned) return "";
                    const id = params.data?.DryWeatherFlowOverrideID;
                    if (id == null) return "";
                    return DryWeatherFlowOverrides.find((x) => x.Value === id)?.DisplayName ?? "No";
                },
            },
        ];

        this.verificationColumnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                const wqmpID = this.waterQualityManagementPlanID;
                const verifyID = params.data.WaterQualityManagementPlanVerifyID;
                const actions: any[] = [
                    {
                        ActionName: "View",
                        ActionHandler: () => this.router.navigate(["/water-quality-management-plans", wqmpID, "verifications", verifyID, "view"]),
                    },
                ];
                if (this.currentPersonCanEdit && params.data.IsDraft) {
                    actions.push({
                        ActionName: "Edit",
                        ActionIcon: "fas fa-edit",
                        ActionHandler: () => this.router.navigate(["/water-quality-management-plans", wqmpID, "verifications", verifyID]),
                    });
                }
                return actions;
            }),
            this.utilityFunctionsService.createDateColumnDef("Verification Date", "VerificationDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createDateColumnDef("Last Edited", "LastEditedDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createBasicColumnDef("Edited By", "LastEditedByPersonFullName"),
            this.utilityFunctionsService.createBasicColumnDef("Type", "WaterQualityManagementPlanVerifyTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Visit Status", "WaterQualityManagementPlanVisitStatusDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Verify Status", "WaterQualityManagementPlanVerifyStatusDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Draft/Finalized", "IsDraft", {
                UseCustomDropdownFilter: true,
                ValueGetter: (params) => (params.data?.IsDraft ? "Draft" : "Finalized"),
            }),
        ];
    }

    public modelingApproaches = WaterQualityManagementPlanModelingApproaches;

    getModelingApproachDisplayName(id: number): string {
        return WaterQualityManagementPlanModelingApproaches.find((x) => x.Value === id)?.DisplayName ?? "";
    }

    handleMapReady(event: any): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;
        if (this.pendingWqmpForMap) {
            this.addTreatmentBMPsLayer(this.pendingWqmpForMap);
            this.pendingWqmpForMap = null;
        }
    }

    openEditModal(wqmp: WaterQualityManagementPlanDto): void {
        const dialogRef = this.dialogService.open(WqmpModalComponent, {
            data: { mode: "edit", wqmp },
            width: "800px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Water Quality Management Plan updated successfully.", AlertContext.Success));
                this.loadData();
            }
        });
    }

    openEditModelingApproachModal(wqmp: WaterQualityManagementPlanDto): void {
        const dialogRef = this.dialogService.open(EditModelingApproachModalComponent, {
            data: { wqmpID: wqmp.WaterQualityManagementPlanID, currentApproachID: wqmp.WaterQualityManagementPlanModelingApproachID } as EditModelingApproachModalContext,
            width: "600px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Modeling approach updated successfully.", AlertContext.Success));
                this.loadData();
            }
        });
    }

    openEditTreatmentBMPsModal(wqmp: WaterQualityManagementPlanDto): void {
        const dialogRef = this.dialogService.open(EditTreatmentBMPsModalComponent, {
            data: {
                wqmpID: wqmp.WaterQualityManagementPlanID,
                currentBMPIDs: wqmp.TreatmentBMPs?.map((b) => b.TreatmentBMPID) ?? [],
            } as EditTreatmentBMPsModalContext,
            width: "700px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Treatment BMP associations updated successfully.", AlertContext.Success));
                this.loadData();
            }
        });
    }

    navigateToEditQuickBMPs(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID, "edit-quick-bmps"]);
    }

    navigateToEditSourceControlBMPs(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID, "edit-source-control-bmps"]);
    }

    onVerificationRowClicked(event: any): void {
        // Navigate based on draft state and edit permission. Explicit action column (Edit/View)
        // is the primary affordance now, but row-click stays as a convenience: drafts → wizard
        // (when the user can edit), finalized → read-only detail.
        const selectedRows = event.api.getSelectedRows();
        if (!selectedRows?.length) return;
        const verifyID = selectedRows[0].WaterQualityManagementPlanVerifyID;
        const isDraft = selectedRows[0].IsDraft;
        if (isDraft && this.currentPersonCanEdit) {
            this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID, "verifications", verifyID]);
        } else {
            this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID, "verifications", verifyID, "view"]);
        }
    }
}
