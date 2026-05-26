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
import { Observable, shareReplay, tap } from "rxjs";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { AsyncPipe } from "@angular/common";
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

@Component({
    selector: "treatment-bmps",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, HybridMapGridComponent, AsyncPipe, LoadingDirective, RouterModule],
    templateUrl: "./treatment-bmps.component.html",
})
export class TreatmentBmpsComponent {
    private readonly destroyRef = inject(DestroyRef);

    public map: any;
    public layerControl: any;
    private markerClusterLayer: any;
    private markerMap: Map<number, any> = new Map();
    public treatmentBmps$: Observable<TreatmentBMPGridDto[]>;
    public columnDefs: ColDef[];
    public isLoading = true;
    public selectedTreatmentBMPID: number;
    public selectionFromMap: boolean;
    public boundingBox$: Observable<BoundingBoxDto>;
    public customRichTextTypeID = NeptunePageTypeEnum.TreatmentBMP;

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
        this.columnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                const actions: { ActionName: string; ActionIcon?: string; ActionHandler: () => void }[] = [
                    {
                        ActionName: "View",
                        ActionHandler: () => this.router.navigate(["/treatment-bmps", params.data.TreatmentBMPID]),
                    },
                ];
                if (canEdit) {
                    actions.push({
                        ActionName: "Start Field Visit",
                        ActionHandler: () => this.openBeginFieldVisitModal(params.data.TreatmentBMPID),
                    });
                    actions.push({
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
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
            this.utilityFunctionsService.createBasicColumnDef("# of Assessments", "NumberOfAssessments"),
            this.utilityFunctionsService.createDateColumnDef("Last Maintenance Date", "LatestMaintenanceDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createBasicColumnDef("# of Maintenance Events", "NumberOfMaintenanceRecords"),
            this.utilityFunctionsService.createBooleanColumnDef("Benchmark and Threshold Set?", "BenchmarkAndThresholdSet"),
            this.utilityFunctionsService.createBasicColumnDef("Required Lifespan of Installation", "TreatmentBMPLifespanTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Lifespan End Date (if Fixed End Date)", "TreatmentBMPLifespanEndDate"),
            this.utilityFunctionsService.createBasicColumnDef("Required Field Visits/Year", "RequiredFieldVisitsPerYear"),
            this.utilityFunctionsService.createBasicColumnDef("Required Post-Storm Field Visits/Year", "RequiredPostStormFieldVisitsPerYear"),
            this.utilityFunctionsService.createBasicColumnDef("Sizing Basis", "SizingBasisTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Status", "TrashCaptureStatusTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Effectiveness (%)", "TrashCaptureEffectiveness"),
            this.utilityFunctionsService.createBasicColumnDef("Delineation Type", "DelineationTypeDisplayName", { UseCustomDropdownFilter: true }),
            // NPT-1061: Notes was previously mid-grid and dominated visible width on rows with
            // long entries. Moved to the far right + capped at 300px with wrap so long notes flow
            // vertically instead of pushing every other column off-screen.
            {
                ...this.utilityFunctionsService.createBasicColumnDef("Notes", "Notes"),
                maxWidth: 300,
                wrapText: true,
                autoHeight: true,
                cellStyle: { whiteSpace: "normal", lineHeight: "1.3" },
            },
        ];
        this.treatmentBmps$ = this.treatmentBMPService
            .listTreatmentBMP()
            .pipe(
                tap(() => (this.isLoading = false)),
                shareReplay({ bufferSize: 1, refCount: true })
            );
        this.boundingBox$ = this.stormwaterJurisdictionService.getBoundingBoxStormwaterJurisdiction();
    }

    handleMapReady(event: any) {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.addOrUpdateClusterLayer();
    }

    private addOrUpdateClusterLayer() {
        if (!this.map || !this.treatmentBmps$) return;
        this.treatmentBmps$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((bmps) => {
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
