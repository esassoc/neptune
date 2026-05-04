import { Component } from "@angular/core";
import { PageHeaderComponent } from "../../../../shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "../../../../shared/components/alert-display/alert-display.component";
import { Router } from "@angular/router";
import { Input } from "@angular/core";
import { Observable, of, tap } from "rxjs";
import { NeptuneMapInitEvent, NeptuneMapComponent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { WfsService } from "src/app/shared/services/wfs.service";
import * as L from "leaflet";
import { AsyncPipe } from "@angular/common";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";
import { OvtaObservationLayerComponent } from "../../../../shared/components/leaflet/layers/ovta-observation-layer/ovta-observation-layer.component";
import {
    OnlandVisualTrashAssessmentAddRemoveParcelsDto,
    OnlandVisualTrashAssessmentSelectAreaContextDto,
} from "src/app/shared/generated/model/models";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { OvtaWorkflowProgressService } from "src/app/shared/services/ovta-workflow-progress.service";
import { LandUseBlockLayerComponent } from "../../../../shared/components/leaflet/layers/land-use-block-layer/land-use-block-layer.component";
import { WorkflowBodyComponent } from "../../../../shared/components/workflow-body/workflow-body.component";
import { TransectLineLayerComponent } from "../../../../shared/components/leaflet/layers/transect-line-layer/transect-line-layer.component";
import { ParcelLayerComponent } from "../../../../shared/components/leaflet/layers/parcel-layer/parcel-layer.component";
import { OvtaAreaSourceTypeEnum } from "src/app/shared/generated/enum/ovta-area-source-type-enum";

@Component({
    selector: "trash-ovta-add-remove-parcels",
    imports: [
        PageHeaderComponent,
        AlertDisplayComponent,
        NeptuneMapComponent,
        AsyncPipe,
        OvtaObservationLayerComponent,
        LandUseBlockLayerComponent,
        WorkflowBodyComponent,
        TransectLineLayerComponent,
        ParcelLayerComponent,
    ],
    templateUrl: "./trash-ovta-add-remove-parcels.component.html",
    styleUrl: "./trash-ovta-add-remove-parcels.component.scss",
})
export class TrashOvtaAddRemoveParcelsComponent {
    @Input() onlandVisualTrashAssessmentID!: number;
    public selectAreaContext$: Observable<OnlandVisualTrashAssessmentSelectAreaContextDto>;
    public selectAreaContext: OnlandVisualTrashAssessmentSelectAreaContextDto;
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public mapIsReady = false;
    public isLoadingSubmit = false;
    public isLoading: boolean = false;

    public OvtaAreaSourceTypeEnum = OvtaAreaSourceTypeEnum;
    public sourceTypeID: OvtaAreaSourceTypeEnum = OvtaAreaSourceTypeEnum.Parcel;
    public selectedParcelIDs: number[] = [];
    public selectedLandUseBlockIDs: number[] = [];
    public selectionLayer: L.GeoJSON<any>;

    private highlightStyle = {
        color: "#fcfc12",
        weight: 2,
        opacity: 0.65,
        fillOpacity: 0.1,
    };

    constructor(
        private router: Router,
        private onlandVisualTrashAssessmentService: OnlandVisualTrashAssessmentService,
        private wfsService: WfsService,
        private alertService: AlertService,
        private ovtaWorkflowProgressService: OvtaWorkflowProgressService
    ) {}

    ngOnInit(): void {
        this.isLoading = true;
        this.selectAreaContext$ = this.onlandVisualTrashAssessmentService
            .getSelectAreaContextOnlandVisualTrashAssessment(this.onlandVisualTrashAssessmentID)
            .pipe(
                tap((context) => {
                    this.applyContext(context);
                    this.isLoading = false;
                })
            );
    }

    private applyContext(context: OnlandVisualTrashAssessmentSelectAreaContextDto) {
        this.selectAreaContext = context;
        this.sourceTypeID = context.OvtaAreaSourceTypeID as OvtaAreaSourceTypeEnum;
        this.selectedParcelIDs = context.SelectedParcelIDs ?? [];
        this.selectedLandUseBlockIDs = context.SelectedLandUseBlockIDs ?? [];
    }

    public handleMapReady(event: NeptuneMapInitEvent, context: OnlandVisualTrashAssessmentSelectAreaContextDto): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        // Initial load: fit to the selection so the user starts zoomed at the right scale.
        this.refreshSelectionLayer(true);
        this.enableDisableMapClickEvent(context);
        this.mapIsReady = true;
    }

    public onSourceTypeChange(newSourceTypeID: OvtaAreaSourceTypeEnum) {
        if (this.sourceTypeID === newSourceTypeID) {
            return;
        }
        const switchingAwayFromExistingSelection =
            (this.sourceTypeID === OvtaAreaSourceTypeEnum.Parcel && this.selectedParcelIDs.length > 0) ||
            (this.sourceTypeID === OvtaAreaSourceTypeEnum.LandUseBlock && this.selectedLandUseBlockIDs.length > 0);
        if (switchingAwayFromExistingSelection) {
            const ok = window.confirm("Switching the area source will clear your current selection. Continue?");
            if (!ok) return;
        }
        // Clear the inactive list so we don't accidentally save a stale union of mixed sources.
        if (newSourceTypeID === OvtaAreaSourceTypeEnum.Parcel) {
            this.selectedLandUseBlockIDs = [];
        } else {
            this.selectedParcelIDs = [];
        }
        this.sourceTypeID = newSourceTypeID;
        this.refreshSelectionLayer();
    }

    private refreshSelectionLayer(shouldFitBounds: boolean = false) {
        if (this.selectionLayer) {
            this.map.removeLayer(this.selectionLayer);
            this.selectionLayer = null;
        }

        if (this.sourceTypeID === OvtaAreaSourceTypeEnum.LandUseBlock) {
            if (this.selectedLandUseBlockIDs.length === 0) return;
            const cql = `LandUseBlockID in (${this.selectedLandUseBlockIDs.join(",")})`;
            this.wfsService.getGeoserverWFSLayerWithCQLFilter("OCStormwater:LandUseBlocks", cql, "LandUseBlockID").subscribe((response) => {
                this.selectionLayer = L.geoJSON(response as any, { style: this.highlightStyle });
                this.selectionLayer.addTo(this.map);
                if (shouldFitBounds) {
                    this.map.fitBounds(this.selectionLayer.getBounds());
                }
            });
        } else {
            if (this.selectedParcelIDs.length === 0) return;
            const cql = `ParcelID in (${this.selectedParcelIDs.join(",")})`;
            this.wfsService.getGeoserverWFSLayerWithCQLFilter("OCStormwater:Parcels", cql, "ParcelID").subscribe((response) => {
                this.selectionLayer = L.geoJSON(response as any, { style: this.highlightStyle });
                this.selectionLayer.addTo(this.map);
                if (shouldFitBounds) {
                    this.map.fitBounds(this.selectionLayer.getBounds());
                }
            });
        }
    }

    public save(andContinue: boolean = false) {
        const dto: OnlandVisualTrashAssessmentAddRemoveParcelsDto = {
            OnlandVisualTrashAssessmentID: this.onlandVisualTrashAssessmentID,
            OnlandVisualTrashAssessmentAreaID: null,
            StormwaterJurisdictionID: this.selectAreaContext.StormwaterJurisdictionID,
            IsDraftGeometryManuallyRefined: false,
            OvtaAreaSourceTypeID: this.sourceTypeID,
            SelectedParcelIDs: this.selectedParcelIDs ?? [],
            SelectedLandUseBlockIDs: this.selectedLandUseBlockIDs ?? [],
        };
        this.onlandVisualTrashAssessmentService
            .updateOnlandVisualTrashAssessmentWithParcelsOnlandVisualTrashAssessment(this.onlandVisualTrashAssessmentID, dto)
            .subscribe(() => {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Your assessment area was successfully updated.", AlertContext.Success));
                this.ovtaWorkflowProgressService.updateProgress(this.onlandVisualTrashAssessmentID);
                if (andContinue) {
                    this.router.navigate([`/trash/onland-visual-trash-assessments/edit/${this.onlandVisualTrashAssessmentID}/refine-assessment-area`]);
                }
            });
    }

    public refreshParcels() {
        this.onlandVisualTrashAssessmentService.refreshOnlandVisualTrashAssessmentParcelsOnlandVisualTrashAssessment(this.onlandVisualTrashAssessmentID).subscribe(() => {
            // After clearing DraftGeometry, re-fetch the context so the saved source type and
            // freshly-recomputed selection (from the transect line) come back together.
            this.onlandVisualTrashAssessmentService
                .getSelectAreaContextOnlandVisualTrashAssessment(this.onlandVisualTrashAssessmentID)
                .subscribe((context) => {
                    this.applyContext(context);
                    this.refreshSelectionLayer(true);
                    this.enableDisableMapClickEvent(context);
                    this.selectAreaContext$ = of(context);
                });
        });
    }

    private enableDisableMapClickEvent(context: OnlandVisualTrashAssessmentSelectAreaContextDto) {
        this.map.off("click");
        if (context.IsDraftGeometryManuallyRefined) {
            return;
        }
        this.map.on("click", (event: L.LeafletMouseEvent): void => {
            if (this.sourceTypeID === OvtaAreaSourceTypeEnum.LandUseBlock) {
                this.wfsService.getLandUseBlockByCoordinate(event.latlng.lng, event.latlng.lat).subscribe((featureCollection: GeoJSON.FeatureCollection) => {
                    featureCollection.features.forEach((feature: GeoJSON.Feature) => {
                        const id = feature.properties.LandUseBlockID;
                        const idx = this.selectedLandUseBlockIDs.indexOf(id);
                        if (idx >= 0) {
                            this.selectedLandUseBlockIDs.splice(idx, 1);
                        } else {
                            this.selectedLandUseBlockIDs.push(id);
                        }
                    });
                    this.refreshSelectionLayer();
                });
            } else {
                this.wfsService.getParcelByCoordinate(event.latlng.lng, event.latlng.lat).subscribe((parcelsFeatureCollection: GeoJSON.FeatureCollection) => {
                    parcelsFeatureCollection.features.forEach((feature: GeoJSON.Feature) => {
                        const parcelID = feature.properties.ParcelID;
                        const idx = this.selectedParcelIDs.indexOf(parcelID);
                        if (idx >= 0) {
                            this.selectedParcelIDs.splice(idx, 1);
                        } else {
                            this.selectedParcelIDs.push(parcelID);
                        }
                    });
                    this.refreshSelectionLayer();
                });
            }
        });
    }
}
