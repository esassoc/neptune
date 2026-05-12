import { DialogService } from "@ngneat/dialog";
import { FundingEventModalComponent, FundingEventModalContext } from "../funding-event-modal/funding-event-modal.component";
import { BeginFieldVisitModalComponent, BeginFieldVisitModalContext } from "./begin-field-visit-modal/begin-field-visit-modal.component";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { Component, OnInit, OnChanges, SimpleChanges, ViewChild, TemplateRef, Input } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { DatePipe, AsyncPipe, CommonModule } from "@angular/common";
import { BehaviorSubject, Observable } from "rxjs";
import { shareReplay, switchMap, tap } from "rxjs/operators";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import * as L from "leaflet";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPImageByTreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp-image-by-treatment-bmp.service";
import { TreatmentBMPDto } from "src/app/shared/generated/model/treatment-bmp-dto";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { NeptuneMapComponent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { StormwaterNetworkLayerComponent } from "src/app/shared/components/leaflet/layers/stormwater-network-layer/stormwater-network-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { InventoriedBMPsLayerComponent } from "src/app/shared/components/leaflet/layers/inventoried-bmps-layer/inventoried-bmps-layer.component";
import { TreatmentBMPLifespanTypeEnum } from "src/app/shared/generated/enum/treatment-b-m-p-lifespan-type-enum";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { ImageCarouselComponent } from "src/app/shared/components/image-carousel/image-carousel.component";
import { ModeledBmpPerformanceComponent } from "src/app/shared/components/modeled-bmp-performance/modeled-bmp-performance.component";
import { WaterQualityManagementPlanModelingApproachEnum } from "src/app/shared/generated/enum/water-quality-management-plan-modeling-approach-enum";
import { HRUCharacteristicDto } from "src/app/shared/generated/model/hru-characteristic-dto";
import { ColDef } from "ag-grid-community";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { LandUseTableComponent } from "src/app/shared/components/land-use-table/land-use-table.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { CustomAttributesDisplayComponent } from "src/app/shared/components/custom-attributes-display/custom-attributes-display.component";
import {
    CustomAttributeDto,
    PersonDto,
    RegionalSubbasinRevisionRequestDto,
    TreatmentBMPDelineationErrorsDto,
    TreatmentBMPDocumentDto,
    TreatmentBMPDocumentUpdateDto,
    TreatmentBMPHRUCharacteristicsSummarySimpleDto,
    TreatmentBMPImageDto,
    TreatmentBMPParameterizationErrorsDto,
    TreatmentBMPTypeCustomAttributeTypeDto,
    TreatmentBMPUpstreamestErrorsDto,
} from "src/app/shared/generated/model/models";
import { FieldVisitDto } from "src/app/shared/generated/model/field-visit-dto";
import { FundingEventByTreatmentBMPIDService } from "src/app/shared/generated/api/funding-event-by-treatment-bmpid.service";
import { FundingEventDto } from "src/app/shared/generated/model/funding-event-dto";
import { CustomAttributeTypePurposeEnum } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { GroupByPipe } from "src/app/shared/pipes/group-by.pipe";
import { SumPipe } from "src/app/shared/pipes/sum.pipe";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { HruCharacteristicsGridComponent } from "src/app/shared/components/hru-characteristics-grid/hru-characteristics-grid.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { TrashCaptureStatusTypeEnum } from "src/app/shared/generated/enum/trash-capture-status-type-enum";
import {
    TreatmentBmpUpdateTypeModalComponent,
    TreatmentBmpUpdateTypeModalContext,
} from "src/app/pages/treatment-bmps/treatment-bmp-detail/treatment-bmp-update-type-modal/treatment-bmp-update-type-modal.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { FileResourceListComponent, IHaveFileResource } from "src/app/shared/components/file-resource-list/file-resource-list.component";
import { TreatmentBMPDocumentByTreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp-document-by-treatment-bmp.service";
import { FileUploadModalComponent, IFileResourceUpload } from "src/app/shared/components/file-resource-list/file-upload-modal/file-upload-modal.component";
import {
    TreatmentBmpUpdateUpstreamBmpModalComponent,
    TreatmentBmpUpdateUpstreamBmpModalContext,
} from "src/app/pages/treatment-bmps/treatment-bmp-detail/treatment-bmp-update-upstream-bmp-modal/treatment-bmp-update-upstream-bmp-modal.component";
import { RegionalSubbasinRevisionRequestStatusEnum } from "src/app/shared/generated/enum/regional-subbasin-revision-request-status-enum";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { TreatmentBMPBenchmarkAndThresholdService } from "src/app/shared/generated/api/treatment-bmp-benchmark-and-threshold.service";
import { TreatmentBMPBenchmarkAndThresholdDto } from "src/app/shared/generated/model/treatment-bmp-benchmark-and-threshold-dto";
import {
    TreatmentBmpBenchmarkThresholdModalComponent,
    TreatmentBmpBenchmarkThresholdModalContext,
} from "src/app/pages/treatment-bmps/treatment-bmp-detail/treatment-bmp-benchmark-threshold-modal/treatment-bmp-benchmark-threshold-modal.component";

@Component({
    selector: "treatment-bmp-detail",
    templateUrl: "./treatment-bmp-detail.component.html",
    styleUrls: ["./treatment-bmp-detail.component.scss"],
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
        RegionalSubbasinsLayerComponent,
        StormwaterNetworkLayerComponent,
        JurisdictionsLayerComponent,
        WqmpsLayerComponent,
        DelineationsLayerComponent,
        InventoriedBMPsLayerComponent,
        LoadingDirective,
        ImageCarouselComponent,
        ModeledBmpPerformanceComponent,
        LandUseTableComponent,
        NeptuneGridComponent,
        CustomAttributesDisplayComponent,
        HruCharacteristicsGridComponent,
        IconComponent,
        FileResourceListComponent,
    ],
})
export class TreatmentBmpDetailComponent implements OnInit, OnChanges {
    @ViewChild("templateRight", { static: true }) templateRight!: TemplateRef<any>;
    @ViewChild("templateAbove", { static: true }) templateAbove!: TemplateRef<any>;

    @Input() treatmentBMPID!: number;

    public OverlayMode = OverlayMode;

    fundingEvents$: Observable<FundingEventDto[]>;
    /**
     * Stub: Edit URL for Modeling Attributes
     */
    editTreatmentBMPModelingAttributesUrl = "";

    /**
     * Stub: Edit URL for Other Design Attributes
     */
    editTreatmentBMPOtherDesignAttributesUrl = "";
    // Carousel state
    currentImageIndex = 0;

    setImageIndex(idx: number, images: any[]): void {
        if (images && images.length > 0) {
            this.currentImageIndex = (idx + images.length) % images.length;
        }
    }
    // Neptune map integration
    boundingBox: any;
    map: any;
    layerControl: any;
    mapIsReady = false;

    // Observables for async pipe
    treatmentBMP$!: Observable<TreatmentBMPDto>;
    delineationErrors$: Observable<TreatmentBMPDelineationErrorsDto>;
    parameterizationErrors$: Observable<TreatmentBMPParameterizationErrorsDto>;
    upstreamestBMPErrors$: Observable<TreatmentBMPUpstreamestErrorsDto>;
    customAttributes$: Observable<CustomAttributeDto[]>;
    treatmentBMPTypeCustomAttributeTypes$: Observable<TreatmentBMPTypeCustomAttributeTypeDto[]>;
    treatmentBMPImages$: Observable<TreatmentBMPImageDto[]>;
    treatmentBMPDocuments$: Observable<TreatmentBMPDocumentDto[]>;
    refreshTreatmentBMPDocumentsTrigger$: BehaviorSubject<void> = new BehaviorSubject<void>(undefined);
    hruCharacteristics$: Observable<HRUCharacteristicDto[]>;
    fieldVisits$: Observable<FieldVisitDto[]>;
    benchmarkAndThresholds$: Observable<TreatmentBMPBenchmarkAndThresholdDto[]>;
    fieldVisitColumnDefs: Array<ColDef>;

    imagesLoading = true;

    currentUser: PersonDto;
    // Placeholder properties for template bindings
    isAnonymousOrUnassigned = false;
    openRevisionRequest: RegionalSubbasinRevisionRequestDto = null;
    openRevisionRequestDetailUrl = "";
    currentPersonCanManage = false;
    canEditStormwaterJurisdiction = false;
    isAnalyzedInModelingModule = true;
    isSitkaAdmin = false;
    hasModelingAttributes = false;
    upstreamSectionExpanded = false;

    hruCharacteristicsSummaries: TreatmentBMPHRUCharacteristicsSummarySimpleDto[] = [];

    public WaterQualityManagementPlanModelingApproachEnum = WaterQualityManagementPlanModelingApproachEnum;
    public CustomAttributeTypePurposeEnum = CustomAttributeTypePurposeEnum;
    public TrashCaptureStatusTypeEnum = TrashCaptureStatusTypeEnum;
    public TreatmentBMPLifespanTypeEnum = TreatmentBMPLifespanTypeEnum;

    constructor(
        private treatmentBMPService: TreatmentBMPService,
        private treatmentBMPImageByTreatmentBMPService: TreatmentBMPImageByTreatmentBMPService,
        private treatmentBMPDocumentByTreatmentBMPService: TreatmentBMPDocumentByTreatmentBMPService,
        private treatmentBMPTypeService: TreatmentBMPTypeService,
        private utilityFunctionsService: UtilityFunctionsService,
        private groupByPipe: GroupByPipe,
        private sumPipe: SumPipe,
        private fundingEventByTreatmentBMPIDService: FundingEventByTreatmentBMPIDService,
        private dialogService: DialogService,
        private confirmService: ConfirmService,
        private alertService: AlertService,
        private authenticationService: AuthenticationService,
        private benchmarkAndThresholdService: TreatmentBMPBenchmarkAndThresholdService,
        private fieldVisitService: FieldVisitService,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.fieldVisitColumnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                const visit: FieldVisitDto = params.data;
                const inProgress = visit.FieldVisitStatusID === 1; // FieldVisitStatusEnum.InProgress
                const actions: { ActionName: string; ActionIcon?: string; ActionHandler: () => void }[] = [
                    {
                        ActionName: inProgress ? "Continue" : "View",
                        ActionHandler: () => this.router.navigate(["/field-visits", visit.FieldVisitID]),
                    },
                ];
                if (this.currentPersonCanManage) {
                    actions.push({
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
                        ActionHandler: () => this.deleteFieldVisit(params),
                    });
                }
                return actions;
            }),
            this.utilityFunctionsService.createDateColumnDef("Visit Date", "VisitDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createBasicColumnDef("Performed By", "PerformedByPersonName"),
            this.utilityFunctionsService.createBooleanColumnDef("Field Visit Verified", "IsFieldVisitVerified"),
            this.utilityFunctionsService.createBasicColumnDef("Field Visit Status", "FieldVisitStatusDisplayName"),
            this.utilityFunctionsService.createBasicColumnDef("Field Visit Type", "FieldVisitTypeDisplayName"),
            this.utilityFunctionsService.createBooleanColumnDef("Inventory Updated?", "InventoryUpdated"),
            this.utilityFunctionsService.createBooleanColumnDef("Required Attributes Entered?", "RequiredAttributesEntered"),
            this.utilityFunctionsService.createBasicColumnDef("Initial Assessment?", "InitialAssessmentStatus"),
            this.utilityFunctionsService.createDecimalColumnDef("Initial Assessment Score", "AssessmentScoreInitial"),
            this.utilityFunctionsService.createBasicColumnDef("Maintenance Occurred?", "MaintenanceOccurred"),
            this.utilityFunctionsService.createBasicColumnDef("Post-Maintenance Assessment?", "PostMaintenanceAssessmentStatus"),
            this.utilityFunctionsService.createDecimalColumnDef("Post-Maintenance Assessment Score", "AssessmentScorePM"),
        ];
        this.loadData();

        this.authenticationService.getCurrentUser().subscribe((user) => {
            this.currentUser = user;
            this.isAnonymousOrUnassigned = !user || user.RoleID == RoleEnum.Unassigned;
            this.isSitkaAdmin = user.RoleID == RoleEnum.SitkaAdmin;
            this.currentPersonCanManage = this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
            this.canEditStormwaterJurisdiction = this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["treatmentBMPID"] && !changes["treatmentBMPID"].firstChange) {
            this.loadData();
        }
    }

    private loadData(): void {
        // Wire up funding events grid as observable
        this.fundingEvents$ = this.fundingEventByTreatmentBMPIDService.listFundingEventByTreatmentBMPID(this.treatmentBMPID);
        this.hruCharacteristics$ = this.treatmentBMPService.listHRUCharacteristicsTreatmentBMP(this.treatmentBMPID).pipe(
            tap((hruCharacteristics) => {
                const grouped = this.groupByPipe.transform(hruCharacteristics, "HRUCharacteristicLandUseCodeDisplayName");
                this.hruCharacteristicsSummaries = Object.entries(grouped).map(
                    ([key, value]) =>
                        new TreatmentBMPHRUCharacteristicsSummarySimpleDto({
                            Area: this.sumPipe.transform(value, "Area"),
                            ImperviousCover: this.sumPipe.transform(value, "ImperviousAcres"),
                            LandUse: key,
                        })
                );
            })
        );
        // Wire up field visits grid as observable
        this.fieldVisits$ = this.treatmentBMPService.fieldVisitGridJsonDataTreatmentBMP(this.treatmentBMPID);
        this.benchmarkAndThresholds$ = this.benchmarkAndThresholdService.listTreatmentBMPBenchmarkAndThreshold(this.treatmentBMPID).pipe(
            tap((benchmarks) => (this.currentBenchmarks = benchmarks))
        );

        this.treatmentBMP$ = this.treatmentBMPService.getByIDTreatmentBMP(this.treatmentBMPID).pipe(
            tap((bmp) => {
                if (bmp && bmp.Latitude && bmp.Longitude) {
                    this.boundingBox = new BoundingBoxDto({
                        Left: bmp.Longitude - 0.001,
                        Right: bmp.Longitude + 0.001,
                        Bottom: bmp.Latitude - 0.001,
                        Top: bmp.Latitude + 0.001,
                    });
                }

                if (bmp && bmp.RegionalSubbasinRevisionRequests) {
                    this.openRevisionRequest = bmp.RegionalSubbasinRevisionRequests.find(
                        (r) => r.RegionalSubbasinRevisionRequestStatusID == RegionalSubbasinRevisionRequestStatusEnum.Open
                    );
                }

                this.hasSettableBenchmarkAndThresholdValues = bmp?.HasSettableBenchmarkAndThresholdValues ?? false;
                this.canEditBenchmarkAndThresholds = this.currentPersonCanManage && this.hasSettableBenchmarkAndThresholdValues;
            }),
            shareReplay(1)
        );

        this.delineationErrors$ = this.treatmentBMP$.pipe(switchMap((bmp) => this.treatmentBMPService.getDelineationErrorsTreatmentBMP(bmp.TreatmentBMPID)));
        this.parameterizationErrors$ = this.treatmentBMP$.pipe(switchMap((bmp) => this.treatmentBMPService.getParameterizationErrorsTreatmentBMP(bmp.TreatmentBMPID)));
        this.upstreamestBMPErrors$ = this.treatmentBMP$.pipe(
            switchMap((bmp) => this.treatmentBMPService.getUpstreamestErrorsTreatmentBMP(bmp.TreatmentBMPID)),
            shareReplay(1)
        );

        this.customAttributes$ = this.treatmentBMP$.pipe(switchMap((bmp) => this.treatmentBMPService.listCustomAttributesTreatmentBMP(bmp.TreatmentBMPID)));
        this.treatmentBMPTypeCustomAttributeTypes$ = this.treatmentBMP$.pipe(
            switchMap((bmp) =>
                this.treatmentBMPTypeService.listCustomAttributeTypesTreatmentBMPType(bmp.TreatmentBMPTypeID).pipe(
                    tap((attributes) => {
                        this.hasModelingAttributes =
                            Array.isArray(attributes) &&
                            attributes.some((attr) => attr.CustomAttributeType?.CustomAttributeTypePurposeID === CustomAttributeTypePurposeEnum.Modeling);
                    })
                )
            )
        );

        this.treatmentBMPImages$ = this.treatmentBMPImageByTreatmentBMPService.listForCarouselTreatmentBMPImageByTreatmentBMP(this.treatmentBMPID).pipe(
            tap({
                next: () => {
                    this.imagesLoading = false;
                },
                error: () => {
                    this.imagesLoading = false;
                },
                complete: () => {
                    this.imagesLoading = false;
                },
            })
        );

        this.treatmentBMPDocuments$ = this.refreshTreatmentBMPDocumentsTrigger$.pipe(
            switchMap(() => this.treatmentBMPDocumentByTreatmentBMPService.listTreatmentBMPDocumentByTreatmentBMP(this.treatmentBMPID))
        );

        this.refreshTreatmentBMPDocumentsTrigger$.next();
    }

    openUpdateTypeModal(treatmentBMP: TreatmentBMPDto): void {
        this.dialogService
            .open(TreatmentBmpUpdateTypeModalComponent, {
                data: { treatmentBMPID: this.treatmentBMPID, currentTreatmentBMPTypeID: treatmentBMP.TreatmentBMPTypeID } as TreatmentBmpUpdateTypeModalContext,
            })
            .afterClosed$.subscribe((result) => {
                if (result) {
                    this.loadData();
                    this.alertService.pushAlert(new Alert("BMP type updated successfully.", AlertContext.Success));
                }
            });
    }

    openEditUpstreamBMPModal(treatmentBMP: TreatmentBMPDto): void {
        this.dialogService
            .open(TreatmentBmpUpdateUpstreamBmpModalComponent, {
                data: { treatmentBMPID: this.treatmentBMPID, currentUpstreamBMPID: treatmentBMP?.UpstreamBMPID } as TreatmentBmpUpdateUpstreamBmpModalContext,
            })
            .afterClosed$.subscribe((result) => {
                if (result) {
                    this.loadData();
                    this.alertService.pushAlert(new Alert("Upstream BMP updated successfully.", AlertContext.Success));
                }
            });
    }

    private deleteFieldVisit(params: any): void {
        const visit: FieldVisitDto = params.data;
        this.confirmService
            .confirm({
                title: "Delete Field Visit",
                message: `<p>You are about to delete the field visit dated ${new Date(visit.VisitDate).toLocaleDateString()}.</p><p>This will also delete the assessment(s) and maintenance record associated with this visit.</p><p>Are you sure you wish to proceed?</p>`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.fieldVisitService.deleteFieldVisit(visit.FieldVisitID).subscribe(() => {
                    this.alertService.pushAlert(new Alert("Field Visit deleted.", AlertContext.Success));
                    params.api.applyTransaction({ remove: [visit] });
                    this.fieldVisits$ = this.treatmentBMPService.fieldVisitGridJsonDataTreatmentBMP(this.treatmentBMPID);
                });
            });
    }

    openBeginFieldVisitModal(): void {
        this.fieldVisitService.getInProgressForTreatmentBMPFieldVisit(this.treatmentBMPID).subscribe((inProgress) => {
            this.dialogService
                .open(BeginFieldVisitModalComponent, {
                    data: {
                        treatmentBMPID: this.treatmentBMPID,
                        inProgressFieldVisit: inProgress ?? null,
                    } as BeginFieldVisitModalContext,
                })
                .afterClosed$.subscribe((result) => {
                    if (result) {
                        this.router.navigate(["/field-visits", result.FieldVisitID]);
                    }
                });
        });
    }

    openAddFundingEventModal(): void {
        this.dialogService
            .open(FundingEventModalComponent, {
                data: { treatmentBMPID: this.treatmentBMPID } as FundingEventModalContext,
            })
            .afterClosed$.subscribe((result) => {
                if (result) {
                    this.loadData();
                }
            });
    }

    openEditFundingEventModal(fundingEvent: FundingEventDto): void {
        this.dialogService
            .open(FundingEventModalComponent, {
                data: { treatmentBMPID: this.treatmentBMPID, editData: fundingEvent } as FundingEventModalContext,
            })
            .afterClosed$.subscribe((result) => {
                if (result) this.loadData();
            });
    }

    confirmDeleteFundingEvent(fundingEvent: FundingEventDto): void {
        this.confirmService
            .confirm({
                buttonClassYes: "btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
                title: "Delete Funding Event",
                message: `<p>Are you sure you want to delete the Funding Event '<strong>${fundingEvent.DisplayName}</strong>'?</p>`,
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.fundingEventByTreatmentBMPIDService.deleteFundingEventByTreatmentBMPID(this.treatmentBMPID, fundingEvent.FundingEventID).subscribe(() => this.loadData());
                }
            });
    }

    confirmRefreshLandUse(treatmentBMP: TreatmentBMPDto): void {
        this.confirmService
            .confirm({
                buttonClassYes: "btn-primary",
                buttonTextYes: "Refresh Land Use",
                buttonTextNo: "Cancel",
                title: "Refresh Land Use",
                message: `<p>Are you sure you want to refresh the Land Use Area for '${treatmentBMP.TreatmentBMPName}' treatment BMP?</p>`,
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.treatmentBMPService.queueRefreshLandUseTreatmentBMP(treatmentBMP.TreatmentBMPID).subscribe(() => {
                        this.alertService.pushAlert(
                            new Alert(
                                `Successfully queued a Land Use refresh for the Treatment BMP ${treatmentBMP.TreatmentBMPName}. It will run in the background, please check back later to view the results.`,
                                AlertContext.Success
                            )
                        );
                    });
                }
            });
    }

    canEditBenchmarkAndThresholds = false;
    hasSettableBenchmarkAndThresholdValues = false;
    currentBenchmarks: TreatmentBMPBenchmarkAndThresholdDto[] = [];

    // NOTE: TreatmentBMPTypeIsAnalyzedInModelingModule is expected on treatmentBMP DTO. If missing, add to DTO or adjust template logic.

    private delineationLayer: L.GeoJSON | null = null;

    // Handler for Neptune map load event
    handleMapReady(event: any): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;
        // Add marker for BMP location and delineation polygon
        this.treatmentBMP$.subscribe((bmp) => {
            if (bmp && bmp.Latitude && bmp.Longitude && this.map) {
                const marker = L.marker([bmp.Latitude, bmp.Longitude], {
                    icon: MarkerHelper.treatmentBMPMarker,
                });
                marker.addTo(this.map);
                marker.bindPopup(bmp.TreatmentBMPName || "BMP Location");

                // Add delineation polygon (use upstream BMP's delineation if this BMP has one)
                const delineationGeometry = bmp.UpstreamBMP?.Delineation?.Geometry || bmp.Delineation?.Geometry;
                if (delineationGeometry) {
                    try {
                        const geoJson = JSON.parse(delineationGeometry);
                        if (this.delineationLayer) {
                            this.map.removeLayer(this.delineationLayer);
                        }
                        this.delineationLayer = L.geoJSON(geoJson, {
                            style: {
                                color: "#4488CC",
                                weight: 2,
                                fillOpacity: 0.15,
                            },
                        });
                        this.delineationLayer.addTo(this.map);
                        this.layerControl.addOverlay(this.delineationLayer, "Delineation Area");
                        // Fit bounds to include both marker and delineation
                        const bounds = this.delineationLayer.getBounds().extend([bmp.Latitude, bmp.Longitude]);
                        this.map.fitBounds(bounds, { padding: [20, 20] });
                    } catch {
                        // If geometry parsing fails, just center on the marker
                    }
                }
            }
        });
    }

    openEditBenchmarkAndThresholdsModal(benchmarks: TreatmentBMPBenchmarkAndThresholdDto[]): void {
        const dialogRef = this.dialogService.open(TreatmentBmpBenchmarkThresholdModalComponent, {
            data: {
                treatmentBMPID: this.treatmentBMPID,
                benchmarks: benchmarks,
            } as TreatmentBmpBenchmarkThresholdModalContext,
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.loadData();
            }
        });
    }

    getFundingEventTotal(fundingEvent: FundingEventDto): number {
        if (!fundingEvent.FundingEventFundingSources) return 0;
        return fundingEvent.FundingEventFundingSources.reduce((acc, s) => acc + (s.Amount || 0), 0);
    }
    openDocumentUploadModal(): void {
        const dialogRef = this.dialogService.open(FileUploadModalComponent, {
            data: {},
            size: "sm",
        });

        dialogRef.afterClosed$.subscribe((result: IFileResourceUpload) => {
            if (result) {
                this.uploadDocument(result);
            }
        });
    }

    uploadDocument(fileResource: IFileResourceUpload): void {
        this.treatmentBMPDocumentByTreatmentBMPService
            .createTreatmentBMPDocumentByTreatmentBMP(this.treatmentBMPID, fileResource.File, fileResource.DocumentDescription)
            .subscribe((result) => {
                this.alertService.pushAlert(new Alert("Successfully uploaded document.", AlertContext.Success));
                this.refreshTreatmentBMPDocumentsTrigger$.next();
            });
    }

    onDocumentUpdated(fileResource: any): void {
        let updateDto = {
            Description: fileResource.DocumentDescription,
        } as TreatmentBMPDocumentUpdateDto;

        this.treatmentBMPDocumentByTreatmentBMPService
            .updateTreatmentBMPDocumentByTreatmentBMP(this.treatmentBMPID, fileResource.FileResource.TreatmentBMPDocumentID!, updateDto)
            .subscribe(() => {
                this.alertService.pushAlert(new Alert("Successfully updated document.", AlertContext.Success));
                this.refreshTreatmentBMPDocumentsTrigger$.next();
            });
    }

    onDocumentDeleted(treatmentBMPDocument: TreatmentBMPDocumentDto): void {
        this.treatmentBMPDocumentByTreatmentBMPService.deleteTreatmentBMPDocumentByTreatmentBMP(this.treatmentBMPID, treatmentBMPDocument.TreatmentBMPDocumentID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Successfully deleted document.", AlertContext.Success));
            this.refreshTreatmentBMPDocumentsTrigger$.next();
        });
    }
}
