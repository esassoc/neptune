import { Component, Input, OnInit, OnChanges, SimpleChanges } from "@angular/core";
import { RouterLink } from "@angular/router";
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
import {
    WaterQualityManagementPlanModelingApproachEnum,
    WaterQualityManagementPlanModelingApproaches,
} from "src/app/shared/generated/enum/water-quality-management-plan-modeling-approach-enum";
import {
    WaterQualityManagementPlanDocumentTypeEnum,
    WaterQualityManagementPlanDocumentTypes,
} from "src/app/shared/generated/enum/water-quality-management-plan-document-type-enum";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { WqmpModalComponent } from "src/app/pages/wqmps/wqmp-modal/wqmp-modal.component";
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
    public aboutModelingBMPPerformanceUrl = `${environment.ocStormwaterToolsBaseUrl}/Home/AboutModelingBMPPerformance`;

    wqmp$: Observable<WaterQualityManagementPlanDto>;
    quickBMPs$: Observable<QuickBMPDto[]>;
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

    // Permissions
    currentUser: PersonDto;
    currentPersonCanManage = false;
    isAnonymousOrUnassigned = true;

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
        private sumPipe: SumPipe
    ) {}

    ngOnInit(): void {
        this.initColumnDefs();
        this.loadData();

        this.authenticationService.getCurrentUser().subscribe((user) => {
            this.currentUser = user;
            this.isAnonymousOrUnassigned = !user || user.RoleID == RoleEnum.Unassigned;
            this.currentPersonCanManage = this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["waterQualityManagementPlanID"] && !changes["waterQualityManagementPlanID"].firstChange) {
            this.loadData();
        }
    }

    private loadData(): void {
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

        this.quickBMPs$ = this.wqmpService.listQuickBMPsWaterQualityManagementPlan(this.waterQualityManagementPlanID);
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
            this.utilityFunctionsService.createBasicColumnDef("Notes", "Notes"),
            this.utilityFunctionsService.createBasicColumnDef("Delineation Status", "DelineationStatus"),
            this.utilityFunctionsService.createDecimalColumnDef("Delineation Area (ac)", "Area", { DecimalPlacesToDisplay: 2 }),
        ];

        this.quickBMPColumnDefs = [
            this.utilityFunctionsService.createBasicColumnDef("Name", "QuickBMPName"),
            this.utilityFunctionsService.createBasicColumnDef("Type", "TreatmentBMPTypeName"),
            this.utilityFunctionsService.createBasicColumnDef("Notes", "QuickBMPNote"),
            this.utilityFunctionsService.createBasicColumnDef("# Individual BMPs", "NumberOfIndividualBMPs"),
            this.utilityFunctionsService.createDecimalColumnDef("% Site Treated", "PercentOfSiteTreated", { DecimalPlacesToDisplay: 2 }),
            this.utilityFunctionsService.createDecimalColumnDef("% Captured", "PercentCaptured", { DecimalPlacesToDisplay: 2 }),
            this.utilityFunctionsService.createDecimalColumnDef("% Retained", "PercentRetained", { DecimalPlacesToDisplay: 2 }),
        ];

        this.verificationColumnDefs = [
            this.utilityFunctionsService.createDateColumnDef("Verification Date", "VerificationDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createDateColumnDef("Last Edited", "LastEditedDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createBasicColumnDef("Edited By", "LastEditedByPersonFullName"),
            this.utilityFunctionsService.createBasicColumnDef("Type", "WaterQualityManagementPlanVerifyTypeDisplayName"),
            this.utilityFunctionsService.createBasicColumnDef("Visit Status", "WaterQualityManagementPlanVisitStatusDisplayName"),
            this.utilityFunctionsService.createBasicColumnDef("Verify Status", "WaterQualityManagementPlanVerifyStatusDisplayName"),
            this.utilityFunctionsService.createBasicColumnDef("Draft/Finalized", "IsDraft", {
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
}
