import { Component, DestroyRef, inject } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { DialogService } from "@ngneat/dialog";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { HybridMapGridComponent } from "src/app/shared/components/hybrid-map-grid/hybrid-map-grid.component";
import "leaflet.markercluster";
import * as L from "leaflet";
import { ColDef } from "ag-grid-community";
import { BehaviorSubject, combineLatest, map, Observable, shareReplay, tap } from "rxjs";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { AsyncPipe } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { NgSelectModule } from "@ng-select/ng-select";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { TreatmentBMPGridDto } from "src/app/shared/generated/model/treatment-bmp-grid-dto";
import {
    BeginFieldVisitModalComponent,
    BeginFieldVisitModalContext,
} from "./treatment-bmp-detail/begin-field-visit-modal/begin-field-visit-modal.component";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { StormwaterNetworkLayerComponent } from "src/app/shared/components/leaflet/layers/stormwater-network-layer/stormwater-network-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { ZoomToMyLocationControlComponent } from "src/app/shared/components/leaflet/features/zoom-to-my-location-control/zoom-to-my-location-control.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";

interface FilterOption {
    ID: number;
    Name: string;
}

@Component({
    selector: "treatment-bmps",
    standalone: true,
    imports: [
        PageHeaderComponent,
        AlertDisplayComponent,
        HybridMapGridComponent,
        AsyncPipe,
        LoadingDirective,
        RouterModule,
        FormsModule,
        NgSelectModule,
        RegionalSubbasinsLayerComponent,
        StormwaterNetworkLayerComponent,
        JurisdictionsLayerComponent,
        ZoomToMyLocationControlComponent,
    ],
    templateUrl: "./treatment-bmps.component.html",
    styleUrl: "./treatment-bmps.component.scss",
})
export class TreatmentBmpsComponent {
    private readonly destroyRef = inject(DestroyRef);

    public map: any;
    public layerControl: any;
    private markerClusterLayer: any;
    private markerMap: Map<number, any> = new Map();
    private clusterLayerSubscribed = false;
    public treatmentBmps$: Observable<TreatmentBMPGridDto[]>;
    public filteredTreatmentBmps$: Observable<TreatmentBMPGridDto[]>;
    public columnDefs: ColDef[];
    public isLoading = true;
    public selectedTreatmentBMPID: number;
    public selectionFromMap: boolean;
    public boundingBox$: Observable<BoundingBoxDto>;
    public customRichTextTypeID = NeptunePageTypeEnum.TreatmentBMP;

    public OverlayMode = OverlayMode;

    // Find-a-BMP filter bar: empty selection = show all. Drives both the grid rowData and the map markers.
    public selectedTypeIDs: number[] = [];
    public selectedJurisdictionIDs: number[] = [];
    private filter$ = new BehaviorSubject<{ typeIDs: number[]; jurisdictionIDs: number[] }>({ typeIDs: [], jurisdictionIDs: [] });
    public typeOptions$: Observable<FilterOption[]>;
    public jurisdictionOptions$: Observable<FilterOption[]>;

    constructor(
        private treatmentBMPService: TreatmentBMPService,
        private fieldVisitService: FieldVisitService,
        private utilityFunctionsService: UtilityFunctionsService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private authenticationService: AuthenticationService,
        private dialogService: DialogService,
        private router: Router,
        private confirmService: ConfirmService,
        private alertService: AlertService
    ) {}

    ngOnInit(): void {
        const canEdit = this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
        const isAnonymousOrUnassigned = this.authenticationService.isCurrentUserAnonymousOrUnassigned();
        const columnDefs: ColDef[] = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                const actions: { ActionName: string; ActionIcon?: string; ActionHandler: () => void }[] = [
                    {
                        ActionName: "View",
                        ActionIcon: "fas fa-file-alt",
                        ActionHandler: () => this.router.navigate(["/treatment-bmps", params.data.TreatmentBMPID]),
                    },
                ];
                if (canEdit) {
                    actions.push({
                        ActionName: "Start Field Visit",
                        ActionIcon: "fas fa-clipboard-check",
                        ActionHandler: () => this.openBeginFieldVisitModal(params.data.TreatmentBMPID),
                    });
                    actions.push({
                        ActionName: "Delete",
                        ActionIcon: "fas fa-trash text-danger",
                        ActionHandler: () => this.deleteModal(params),
                    });
                }
                return actions;
            }),
            this.utilityFunctionsService.createLinkColumnDef("Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
                FieldDefinitionLabelOverride: "Name",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "/jurisdictions/",
                FieldDefinitionType: "Jurisdiction",
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctionsService.createBasicColumnDef("Owner Organization", "OwnerOrganizationName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Type", "TreatmentBMPTypeName", {
                FieldDefinitionType: "TreatmentBMPType",
                FieldDefinitionLabelOverride: "Type",
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctionsService.createBasicColumnDef("Year Built", "YearBuilt"),
            this.utilityFunctionsService.createDateColumnDef("Last Assessment Date", "LatestAssessmentDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createBasicColumnDef("Last Assessed Score", "LatestAssessmentScore"),
            this.utilityFunctionsService.createDecimalColumnDef("# of Assessments", "NumberOfAssessments", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createDateColumnDef("Last Maintenance Date", "LatestMaintenanceDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createDecimalColumnDef("# of Maintenance Events", "NumberOfMaintenanceRecords", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createBooleanColumnDef("Benchmark and Threshold Set?", "BenchmarkAndThresholdSet", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Required Lifespan of Installation", "TreatmentBMPLifespanTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createDateColumnDef("Lifespan End Date (if Fixed End Date)", "TreatmentBMPLifespanEndDate", "MM/dd/yyyy", { IgnoreLocalTimezone: true }),
            this.utilityFunctionsService.createBasicColumnDef("Required Field Visits/Year", "RequiredFieldVisitsPerYear"),
            this.utilityFunctionsService.createBasicColumnDef("Required Post-Storm Field Visits/Year", "RequiredPostStormFieldVisitsPerYear"),
            this.utilityFunctionsService.createBasicColumnDef("Sizing Basis", "SizingBasisTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Status", "TrashCaptureStatusTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Effectiveness (%)", "TrashCaptureEffectiveness"),
            this.utilityFunctionsService.createBasicColumnDef("Delineation Type", "DelineationTypeDisplayName", { UseCustomDropdownFilter: true }),
            // NPT-1061: Notes was previously mid-grid and dominated visible width on rows with long
            // entries. Moved to the far right + capped at 300px. Kept on a single line (truncated
            // with an ellipsis) rather than wrapping with autoHeight — wrapping grew rows unbounded
            // and made tall, uneven rows. The grid's default tooltipValueGetter shows the full note
            // on hover, so nothing is lost.
            {
                ...this.utilityFunctionsService.createBasicColumnDef("Notes", "Notes"),
                maxWidth: 300,
            },
        ];
        // NPT-1079: public (anonymous) + unassigned users see only the fields the legacy "Find a
        // BMP" map exposed — Name and Type. Hide the actions column and all operational columns
        // (assessments, maintenance, lifespan, sizing, trash, notes, jurisdiction, owner).
        const publicHeaders = new Set(["Name", "Type"]);
        this.columnDefs = isAnonymousOrUnassigned ? columnDefs.filter((c) => publicHeaders.has(c.headerName as string)) : columnDefs;

        this.treatmentBmps$ = this.treatmentBMPService
            .listTreatmentBMP()
            .pipe(
                tap(() => (this.isLoading = false)),
                shareReplay({ bufferSize: 1, refCount: true })
            );

        this.filteredTreatmentBmps$ = combineLatest([this.treatmentBmps$, this.filter$]).pipe(
            map(([bmps, f]) =>
                bmps.filter(
                    (b) =>
                        (f.typeIDs.length === 0 || f.typeIDs.includes(b.TreatmentBMPTypeID)) &&
                        (f.jurisdictionIDs.length === 0 || f.jurisdictionIDs.includes(b.StormwaterJurisdictionID))
                )
            ),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        // Derive the dropdown options from the loaded data so they always match what's shown.
        this.typeOptions$ = this.treatmentBmps$.pipe(
            map((bmps) => this.distinctOptions(bmps, (b) => b.TreatmentBMPTypeID, (b) => b.TreatmentBMPTypeName))
        );
        this.jurisdictionOptions$ = this.treatmentBmps$.pipe(
            map((bmps) => this.distinctOptions(bmps, (b) => b.StormwaterJurisdictionID, (b) => b.StormwaterJurisdictionName))
        );

        this.boundingBox$ = this.stormwaterJurisdictionService.getBoundingBoxStormwaterJurisdiction();
    }

    private distinctOptions(bmps: TreatmentBMPGridDto[], idSelector: (b: TreatmentBMPGridDto) => number, nameSelector: (b: TreatmentBMPGridDto) => string): FilterOption[] {
        const byID = new Map<number, string>();
        bmps.forEach((b) => {
            const id = idSelector(b);
            if (id != null && !byID.has(id)) {
                byID.set(id, nameSelector(b));
            }
        });
        return Array.from(byID, ([ID, Name]) => ({ ID, Name })).sort((a, b) => (a.Name ?? "").localeCompare(b.Name ?? ""));
    }

    onFilterChange(): void {
        // ng-select sets the model to null when the clear-all (x) button is used; normalize to []
        // and clone so the emitted filter state stays immutable and the length checks never throw.
        this.filter$.next({
            typeIDs: [...(this.selectedTypeIDs ?? [])],
            jurisdictionIDs: [...(this.selectedJurisdictionIDs ?? [])],
        });
    }

    handleMapReady(event: any) {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.addOrUpdateClusterLayer();
    }

    private addOrUpdateClusterLayer() {
        if (!this.map || !this.filteredTreatmentBmps$ || this.clusterLayerSubscribed) return;
        this.clusterLayerSubscribed = true;
        this.filteredTreatmentBmps$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((bmps) => {
            if (this.markerClusterLayer) {
                this.map.removeLayer(this.markerClusterLayer);
                this.layerControl.removeLayer(this.markerClusterLayer);
            }
            this.markerMap.clear();
            this.markerClusterLayer = L.markerClusterGroup({
                iconCreateFunction: function (cluster) {
                    var childCount = cluster.getChildCount();
                    return L.divIcon({
                        html: "<div><span>" + childCount + "</span></div>",
                        className: "treatment-bmp-cluster",
                        iconSize: L.point(40, 40),
                    });
                },
            });
            bmps.forEach((bmp) => {
                if (bmp.Latitude && bmp.Longitude) {
                    const marker = L.marker([bmp.Latitude, bmp.Longitude], {
                        icon: this.getMarkerIcon(bmp.TreatmentBMPID === this.selectedTreatmentBMPID),
                    });
                    marker.bindPopup(`<b>Name:</b> ${bmp.TreatmentBMPName}<br><b>Type:</b> ${bmp.TreatmentBMPTypeName}`);
                    marker.on("click", () => {
                        this.selectionFromMap = true;
                        this.selectedTreatmentBMPID = bmp.TreatmentBMPID;
                        this.highlightAndZoomToMarker(bmp.TreatmentBMPID);
                    });
                    this.markerMap.set(bmp.TreatmentBMPID, marker);
                    this.markerClusterLayer.addLayer(marker);
                }
            });
            this.layerControl.addOverlay(this.markerClusterLayer, "Treatment BMPs");
            this.map.addLayer(this.markerClusterLayer);
        });
    }

    private getMarkerIcon(isSelected: boolean) {
        return L.icon({
            iconUrl: isSelected ? "assets/main/map-icons/marker-icon-blue.png" : "assets/main/map-icons/marker-icon-orange.png",
            iconSize: [25, 41],
            iconAnchor: [12, 41],
            popupAnchor: [1, -34],
            shadowUrl: "assets/main/map-icons/marker-shadow.png",
            shadowSize: [41, 41],
        });
    }

    private highlightAndZoomToMarker(treatmentBMPID: number) {
        this.markerMap.forEach((marker, id) => {
            marker.setIcon(this.getMarkerIcon(id === treatmentBMPID));
        });
        const marker = this.markerMap.get(treatmentBMPID);
        if (marker) {
            marker.openPopup();
            // Zoom to max zoom level to ensure marker is not clustered
            const maxZoom = this.map.getMaxZoom ? this.map.getMaxZoom() : 18;
            this.map.setView(marker.getLatLng(), maxZoom, { animate: true });
            // Optionally, spiderfy the cluster if needed
            if (this.markerClusterLayer && this.markerClusterLayer.spiderfy) {
                const cluster = this.markerClusterLayer.getVisibleParent(marker);
                if (cluster && cluster.spiderfy) {
                    cluster.spiderfy();
                }
            }
        }
    }

    private openBeginFieldVisitModal(treatmentBMPID: number): void {
        // Mirror treatment-bmp-detail.openBeginFieldVisitModal: pre-fetch the in-progress visit so
        // the modal renders the Continue/New radio when one already exists.
        this.fieldVisitService.getInProgressForTreatmentBMPFieldVisit(treatmentBMPID).subscribe((inProgress) => {
            this.dialogService
                .open(BeginFieldVisitModalComponent, {
                    data: {
                        treatmentBMPID,
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

    private deleteModal(params: any) {
        const confirmOptions = {
            title: "Delete BMP",
            message: `<p>You are about to delete ${params.data.TreatmentBMPName ?? "this BMP"}.</p><p>Are you sure you wish to proceed?</p>`,
            buttonClassYes: "btn btn-danger",
            buttonTextYes: "Delete",
            buttonTextNo: "Cancel",
        };
        this.confirmService.confirm(confirmOptions).then((confirmed) => {
            if (confirmed) {
                this.treatmentBMPService.deleteTreatmentBMP(params.data.TreatmentBMPID).subscribe(() => {
                    this.alertService.pushAlert(new Alert("Successfully deleted BMP", AlertContext.Success));
                    params.api.applyTransaction({ remove: [params.data] });
                });
            }
        });
    }

    onSelectedTreatmentBMPChangedFromGrid(selectedTreatmentBMPID: number) {
        if (this.selectedTreatmentBMPID === selectedTreatmentBMPID) return;
        this.selectedTreatmentBMPID = selectedTreatmentBMPID;
        this.selectionFromMap = false;
        // Update all marker icons to reflect the new selection
        this.markerMap.forEach((marker, id) => {
            marker.setIcon(this.getMarkerIcon(id === selectedTreatmentBMPID));
        });
        this.highlightAndZoomToMarker(selectedTreatmentBMPID);
        return this.selectedTreatmentBMPID;
    }
}
