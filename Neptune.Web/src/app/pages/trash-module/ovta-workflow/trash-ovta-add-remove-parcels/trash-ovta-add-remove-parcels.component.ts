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
import { FormsModule } from "@angular/forms";
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
import { SelectedLandUseBlockLayerComponent } from "../../../../shared/components/leaflet/layers/selected-land-use-block-layer/selected-land-use-block-layer.component";
import { WorkflowBodyComponent } from "../../../../shared/components/workflow-body/workflow-body.component";
import { TransectLineLayerComponent } from "../../../../shared/components/leaflet/layers/transect-line-layer/transect-line-layer.component";
import { ParcelLayerComponent } from "../../../../shared/components/leaflet/layers/parcel-layer/parcel-layer.component";
import { OvtaAreaSourceTypeEnum } from "src/app/shared/generated/enum/ovta-area-source-type-enum";
import {
    BtnGroupRadioInputComponent,
    IBtnGroupRadioInputOption,
} from "src/app/shared/components/inputs/btn-group-radio-input/btn-group-radio-input.component";

@Component({
    selector: "trash-ovta-add-remove-parcels",
    imports: [
        PageHeaderComponent,
        AlertDisplayComponent,
        NeptuneMapComponent,
        AsyncPipe,
        FormsModule,
        BtnGroupRadioInputComponent,
        OvtaObservationLayerComponent,
        SelectedLandUseBlockLayerComponent,
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
    public sourceTypeOptions: IBtnGroupRadioInputOption[] = [];
    public selectedParcelIDs: number[] = [];
    public selectedLandUseBlockIDs: number[] = [];
    /** Yellow-highlight overlay for selected parcels. The LUB selection is drawn directly by
     * the vector <selected-land-use-block-layer>; only parcel mode uses this overlay because
     * the parcel layer is a WMS tile and parcels are too numerous to render as vectors. */
    public parcelSelectionLayer: L.GeoJSON<any>;

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
        this.sourceTypeOptions = [
            { label: "Parcels", value: OvtaAreaSourceTypeEnum.Parcel },
            { label: "Land Use Blocks", value: OvtaAreaSourceTypeEnum.LandUseBlock, disabled: !context.JurisdictionHasLandUseBlocks },
        ];
    }

    public handleMapReady(event: NeptuneMapInitEvent, context: OnlandVisualTrashAssessmentSelectAreaContextDto): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        // Initial load: fit to the parcel selection so the user starts zoomed at the right scale.
        // (LUB selection fitting is handled by the vector layer if needed.)
        this.refreshParcelSelectionLayer(true);
        this.enableDisableMapClickEvent(context);
        this.mapIsReady = true;
    }

    public onSourceTypeChange(newSourceTypeID: OvtaAreaSourceTypeEnum) {
        // ngModelChange fires with the new value already; keep this guard in case it ever fires
        // with the same value (idempotent).
        if (this.sourceTypeID === newSourceTypeID) {
            return;
        }
        // Clear the inactive list so we don't accidentally save a stale union of mixed sources.
        // The user can still re-pick from the now-active layer; nothing is lost on the server
        // until they click Save.
        if (newSourceTypeID === OvtaAreaSourceTypeEnum.Parcel) {
            this.selectedLandUseBlockIDs = [];
        } else {
            this.selectedParcelIDs = [];
        }
        this.sourceTypeID = newSourceTypeID;
        this.refreshParcelSelectionLayer();
    }

    /** Called by the vector LUB layer when the user clicks a polygon. Toggles membership in
     * selectedLandUseBlockIDs; we replace the array (rather than mutating) so Angular fires
     * ngOnChanges on the layer and it can re-style. */
    public onLandUseBlockClicked(landUseBlockID: number) {
        if (this.selectedLandUseBlockIDs.includes(landUseBlockID)) {
            this.selectedLandUseBlockIDs = this.selectedLandUseBlockIDs.filter((id) => id !== landUseBlockID);
        } else {
            this.selectedLandUseBlockIDs = [...this.selectedLandUseBlockIDs, landUseBlockID];
        }
    }

    private refreshParcelSelectionLayer(shouldFitBounds: boolean = false) {
        // Can be invoked from onSourceTypeChange before <neptune-map> finishes initializing —
        // bail until handleMapReady runs. handleMapReady will call refreshParcelSelectionLayer
        // itself, so the deferred refresh isn't lost.
        if (!this.map) return;
        if (this.parcelSelectionLayer) {
            this.map.removeLayer(this.parcelSelectionLayer);
            this.parcelSelectionLayer = null;
        }
        if (this.sourceTypeID !== OvtaAreaSourceTypeEnum.Parcel) return;
        if (this.selectedParcelIDs.length === 0) return;
        const cql = `ParcelID in (${this.selectedParcelIDs.join(",")})`;
        this.wfsService.getGeoserverWFSLayerWithCQLFilter("OCStormwater:Parcels", cql, "ParcelID").subscribe((response) => {
            this.parcelSelectionLayer = L.geoJSON(response as any, { style: this.highlightStyle });
            this.parcelSelectionLayer.addTo(this.map);
            if (shouldFitBounds) {
                this.map.fitBounds(this.parcelSelectionLayer.getBounds());
            }
        });
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
                    this.refreshParcelSelectionLayer(true);
                    this.enableDisableMapClickEvent(context);
                    this.selectAreaContext$ = of(context);
                });
        });
    }

    /** Map-level click handler — used only for parcel mode (coordinate-query against WMS). LUB
     * mode handles per-feature clicks directly on the vector layer. Stays active even when the
     * area was manually refined: re-picking is a valid way for the user to discard the
     * refinement (matches the LUB layer's behavior and the Refresh button's intent). */
    private enableDisableMapClickEvent(_context: OnlandVisualTrashAssessmentSelectAreaContextDto) {
        this.map.off("click");
        this.map.on("click", (event: L.LeafletMouseEvent): void => {
            if (this.sourceTypeID !== OvtaAreaSourceTypeEnum.Parcel) {
                return;
            }
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
                this.refreshParcelSelectionLayer();
            });
        });
    }
}
