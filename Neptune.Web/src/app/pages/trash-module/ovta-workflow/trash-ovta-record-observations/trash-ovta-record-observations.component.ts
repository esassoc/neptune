import { ApplicationRef, Component } from "@angular/core";
import * as L from "leaflet";
import { PageHeaderComponent } from "../../../../shared/components/page-header/page-header.component";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "../../../../shared/components/leaflet/neptune-map/neptune-map.component";
import { AsyncPipe } from "@angular/common";
import { Observable, switchMap, tap } from "rxjs";
import { Router } from "@angular/router";
import { Input } from "@angular/core";
import { OnlandVisualTrashAssessmentDetailDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-detail-dto";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { FormArray, FormBuilder, FormControl, FormGroup, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "../../../../shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { OvtaWorkflowProgressService } from "src/app/shared/services/ovta-workflow-progress.service";
import {
    OnlandVisualTrashAssessmentObservationWithPhotoDto,
    OnlandVisualTrashAssessmentObservationWithPhotoDtoForm,
} from "src/app/shared/generated/model/onland-visual-trash-assessment-observation-with-photo-dto";
import { environment } from "src/environments/environment";
import { OnlandVisualTrashAssessmentObservationService } from "src/app/shared/generated/api/onland-visual-trash-assessment-observation.service";
import { WorkflowBodyComponent } from "src/app/shared/components/workflow-body/workflow-body.component";
import { WfsService } from "src/app/shared/services/wfs.service";
import { LandUseBlockLayerComponent } from "../../../../shared/components/leaflet/layers/land-use-block-layer/land-use-block-layer.component";
import { ParcelLayerComponent } from "../../../../shared/components/leaflet/layers/parcel-layer/parcel-layer.component";
import { OvtaAreaLayerComponent } from "../../../../shared/components/leaflet/layers/ovta-area-layer/ovta-area-layer.component";
import { TransectLineLayerComponent } from "src/app/shared/components/leaflet/layers/transect-line-layer/transect-line-layer.component";

@Component({
    selector: "trash-ovta-record-observations",
    imports: [
        PageHeaderComponent,
        NeptuneMapComponent,
        AsyncPipe,
        FormFieldComponent,
        ReactiveFormsModule,
        FormsModule,
        AlertDisplayComponent,
        WorkflowBodyComponent,
        LandUseBlockLayerComponent,
        ParcelLayerComponent,
        TransectLineLayerComponent,
        OvtaAreaLayerComponent,
    ],
    templateUrl: "./trash-ovta-record-observations.component.html",
    styleUrl: "./trash-ovta-record-observations.component.scss",
})
export class TrashOvtaRecordObservationsComponent {
    public FormFieldType = FormFieldType;
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public mapIsReady = false;
    public isLoadingSubmit = false;
    public ovtaObservationLayer: L.GeoJSON<any>;
    public uploadFormField: FormControl<Blob> = new FormControl<Blob>(null);
    public formGroup: FormGroup<OnlandVisualTrashAssessmentObservationsUpsertDtoCustomForm> = new FormGroup<OnlandVisualTrashAssessmentObservationsUpsertDtoCustomForm>({
        Observations: new FormArray<FormGroup<OnlandVisualTrashAssessmentObservationWithPhotoDtoForm>>([]),
    });

    public selectedOnlandVisualTrashAssessmentObservationID: number;
    public newObservationIDIndex: number = -1;
    public isAddingObservation = false;
    public isEditingLocation = false;
    public editingObservationIndex: number = null;

    public onlandVisualTrashAssessmentObservations$: Observable<OnlandVisualTrashAssessmentObservationWithPhotoDto[]>;

    public onlandVisualTrashAssessment$: Observable<OnlandVisualTrashAssessmentDetailDto>;

    @Input() onlandVisualTrashAssessmentID!: number;

    constructor(
        private onlandVisualTrashAssessmentService: OnlandVisualTrashAssessmentService,
        private onlandVisualTrashAssessmentObservationService: OnlandVisualTrashAssessmentObservationService,
        private alertService: AlertService,
        private ovtaWorkflowProgressService: OvtaWorkflowProgressService,
        private router: Router,
        private wfsService: WfsService,
        private formBuilder: FormBuilder,
        private appRef: ApplicationRef
    ) {}

    ngOnInit() {
        this.onlandVisualTrashAssessment$ = this.onlandVisualTrashAssessmentService.getOnlandVisualTrashAssessment(this.onlandVisualTrashAssessmentID);
        this.onlandVisualTrashAssessmentObservations$ = this.onlandVisualTrashAssessment$.pipe(
            switchMap((onlandVisualTrashAssessment) => {
                return this.onlandVisualTrashAssessmentObservationService.listByOnlandVisualTrashAssessmentIDOnlandVisualTrashAssessmentObservation(
                    onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID
                );
            }),
            tap((onlandVisualTrashAssessmentObservations) => {
                const formArray = this.formGroup.controls.Observations as FormArray;
                onlandVisualTrashAssessmentObservations.forEach((onlandVisualTrashAssessmentObservation) => {
                    let observation = this.formBuilder.group<OnlandVisualTrashAssessmentObservationWithPhotoDto>({
                        OnlandVisualTrashAssessmentObservationID: onlandVisualTrashAssessmentObservation.OnlandVisualTrashAssessmentObservationID,
                        OnlandVisualTrashAssessmentID: onlandVisualTrashAssessmentObservation.OnlandVisualTrashAssessmentID,
                        Note: onlandVisualTrashAssessmentObservation.Note,
                        Latitude: onlandVisualTrashAssessmentObservation.Latitude,
                        Longitude: onlandVisualTrashAssessmentObservation.Longitude,
                        FileResourceID: onlandVisualTrashAssessmentObservation.FileResourceID,
                        FileResourceGUID: onlandVisualTrashAssessmentObservation.FileResourceGUID,
                    });
                    formArray.push(observation);
                });
            })
        );
    }

    public handleMapReady(event: NeptuneMapInitEvent, onlandVisualTrashAssessment: OnlandVisualTrashAssessmentDetailDto): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;
        if (this.formGroup.controls.Observations.length > 0) {
            this.addObservationPointsLayersToMap();
            this.map.fitBounds(this.ovtaObservationLayer.getBounds());
        } else if (onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID) {
            this.wfsService
                .getGeoserverWFSLayerWithCQLFilter(
                    "OCStormwater:OnlandVisualTrashAssessmentAreas",
                    `OnlandVisualTrashAssessmentAreaID = ${onlandVisualTrashAssessment.OnlandVisualTrashAssessmentAreaID}`,
                    "OnlandVisualTrashAssessmentAreaID"
                )
                .subscribe((response) => {
                    this.map.fitBounds(L.geoJson(response as any).getBounds());
                });
        } else {
            this.wfsService
                .getGeoserverWFSLayerWithCQLFilter(
                    "OCStormwater:Jurisdictions",
                    `StormwaterJurisdictionID = ${onlandVisualTrashAssessment.StormwaterJurisdictionID}`,
                    "StormwaterJurisdictionID"
                )
                .subscribe((response) => {
                    this.map.fitBounds(L.geoJson(response as any).getBounds());
                });
        }

        // Ensure the view updates immediately in zoneless mode.
        // (Output emissions and Leaflet callbacks don't always schedule a render on their own.)
        Promise.resolve().then(() => this.appRef.tick());
    }

    public cancelEditMode() {
        this.map.off("click");
        this.isAddingObservation = false;
        this.isEditingLocation = false;
        this.map.getContainer().style.cursor = "grab";
        const selectedMarker = this.getSelectedMarker();
        if (selectedMarker) {
            selectedMarker.off("dragend");
            selectedMarker.dragging.disable();
        }
    }

    public addObservationMarker() {
        this.isAddingObservation = true;
        this.map.getContainer().style.cursor = "crosshair";
        this.map.on("click", (e: L.LeafletMouseEvent) => {
            this.addObservation(e.latlng);
            this.map.off("click");
            this.isAddingObservation = false;
            this.map.getContainer().style.cursor = "grab";
        });
    }

    public addObservation(latlng: L.LatLng) {
        this.uploadFormField.reset();
        let observation = this.formBuilder.group<OnlandVisualTrashAssessmentObservationWithPhotoDto>({
            OnlandVisualTrashAssessmentObservationID: this.newObservationIDIndex,
            OnlandVisualTrashAssessmentID: this.onlandVisualTrashAssessmentID,
            Note: null,
            Latitude: latlng.lat,
            Longitude: latlng.lng,
            FileResourceID: null,
            FileResourceGUID: null,
        });
        const formArray = this.formGroup.controls.Observations as FormArray;
        formArray.push(observation);
        this.selectedOnlandVisualTrashAssessmentObservationID = observation.controls.OnlandVisualTrashAssessmentObservationID.value;
        this.addObservationPointsLayersToMap();
        this.newObservationIDIndex--;
    }

    public addObservationPointsLayersToMap(): void {
        if (this.ovtaObservationLayer) {
            this.map.removeLayer(this.ovtaObservationLayer);
            this.layerControl.removeLayer(this.ovtaObservationLayer);
        }
        const ovtaObservationGeoJSON = this.mapObservationsToGeoJson();
        this.ovtaObservationLayer = new L.GeoJSON(ovtaObservationGeoJSON as any, {
            pointToLayer: (feature, latlng) => {
                return L.marker(latlng, {
                    icon:
                        feature.properties.OnlandVisualTrashAssessmentObservationID === this.selectedOnlandVisualTrashAssessmentObservationID
                            ? MarkerHelper.selectedMarker
                            : MarkerHelper.treatmentBMPMarker,
                });
            },
            onEachFeature: (feature, layer) => {
                layer.on("click", (e) => {
                    this.selectedOnlandVisualTrashAssessmentObservationID = feature.properties.OnlandVisualTrashAssessmentObservationID;
                    this.selectOnlandVisualTrashAssessmentObservation();
                });
            },
        });
        this.ovtaObservationLayer["sortOrder"] = 100;
        this.ovtaObservationLayer.addTo(this.map);
    }

    save(andContinue: boolean = false) {
        this.onlandVisualTrashAssessmentObservationService
            .updateObservationsOnlandVisualTrashAssessmentObservation(this.onlandVisualTrashAssessmentID, this.formGroup.value)
            .subscribe(() => {
                this.isLoadingSubmit = false;
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Your observations were successfully updated.", AlertContext.Success));
                this.ovtaWorkflowProgressService.updateProgress(this.onlandVisualTrashAssessmentID);

                if (andContinue) {
                    this.onlandVisualTrashAssessmentService.getWorkflowProgressOnlandVisualTrashAssessment(this.onlandVisualTrashAssessmentID).subscribe((response) => {
                        if (response.Steps.RefineAssessmentArea.Disabled) {
                            this.router.navigate([`/trash/onland-visual-trash-assessments/edit/${this.onlandVisualTrashAssessmentID}/review-and-finalize`]);
                        } else {
                            this.router.navigate([`/trash/onland-visual-trash-assessments/edit/${this.onlandVisualTrashAssessmentID}/add-or-remove-parcels`]);
                        }
                    });
                }
            });
    }

    private mapObservationsToGeoJson() {
        return {
            type: "FeatureCollection",
            features: this.formGroup.controls.Observations.value.map((x) => {
                let observationGeoJson = {
                    type: "Feature",
                    geometry: {
                        type: "Point",
                        coordinates: [x.Longitude ?? 0, x.Latitude ?? 0],
                    },
                    properties: {
                        OnlandVisualTrashAssessmentObservationID: x.OnlandVisualTrashAssessmentObservationID,
                        Latitude: x.Latitude,
                        Longitude: x.Longitude,
                    },
                };
                return observationGeoJson;
            }),
        };
    }

    public selectOnlandVisualTrashAssessmentObservation() {
        this.uploadFormField.reset();
        this.ovtaObservationLayer.eachLayer((layer: L.Marker) => {
            if (layer.feature.properties.OnlandVisualTrashAssessmentObservationID === this.selectedOnlandVisualTrashAssessmentObservationID) {
                layer.setIcon(MarkerHelper.selectedMarker);
            } else {
                layer.setIcon(MarkerHelper.treatmentBMPMarker);
            }
        });
    }

    public editObservationLocation(index: number) {
        this.editingObservationIndex = index;
        this.isEditingLocation = true;
        this.map.getContainer().style.cursor = "crosshair";

        // Find the selected marker and enable dragging
        const selectedMarker = this.getSelectedMarker();
        if (selectedMarker) {
            selectedMarker.dragging.enable();
        }

        const updateObservation = (latlng: L.LatLng) => {
            const observation = this.formGroup.controls.Observations.controls[this.editingObservationIndex].value;
            observation.Latitude = latlng.lat;
            observation.Longitude = latlng.lng;
            this.formGroup.controls.Observations.controls[this.editingObservationIndex].patchValue(observation);
            this.addObservationPointsLayersToMap();
            // Re-enable dragging on the new marker after layer rebuild
            const newMarker = this.getSelectedMarker();
            if (newMarker) {
                newMarker.dragging.enable();
                newMarker.on("dragend", onDragEnd);
            }
        };

        const onMapClick = (e: L.LeafletMouseEvent) => {
            updateObservation(e.latlng);
        };

        const onDragEnd = (e: L.DragEndEvent) => {
            updateObservation((e.target as L.Marker).getLatLng());
        };

        this.map.on("click", onMapClick);
        if (selectedMarker) {
            selectedMarker.on("dragend", onDragEnd);
        }
    }

    private getSelectedMarker(): L.Marker | null {
        let selectedMarker: L.Marker = null;
        if (this.ovtaObservationLayer) {
            this.ovtaObservationLayer.eachLayer((layer: L.Marker) => {
                if (layer.feature?.properties?.OnlandVisualTrashAssessmentObservationID === this.selectedOnlandVisualTrashAssessmentObservationID) {
                    selectedMarker = layer;
                }
            });
        }
        return selectedMarker;
    }

    public deleteObservation(index: number) {
        this.formGroup.controls.Observations.removeAt(index);
        this.selectedOnlandVisualTrashAssessmentObservationID = null;
        this.addObservationPointsLayersToMap();
    }

    public goToCurrentLocation() {
        this.map.locate({ setView: true }).on("locationerror", function (e) {
            alert("Location access has been denied.");
        });
    }

    public getFile(index: number) {
        if (typeof this.uploadFormField.value != typeof "string") {
            this.isLoadingSubmit = true;
            this.onlandVisualTrashAssessmentObservationService
                .stageObservationPhotoOnlandVisualTrashAssessmentObservation(this.onlandVisualTrashAssessmentID, this.uploadFormField.value)
                .subscribe((response) => {
                    const observation = this.formGroup.controls.Observations.controls[index].value;
                    observation.FileResourceID = response.FileResourceID;
                    observation.FileResourceGUID = response.FileResourceGUID;
                    this.formGroup.controls.Observations.controls[index].patchValue(observation);
                    this.isLoadingSubmit = false;
                });
        }
    }

    public getUrl(fileResourceGUID) {
        return environment.ocStormwaterToolsBaseUrl + "/FileResource/DisplayResource/" + fileResourceGUID;
    }

    public deletePhotoFromSelectedObservation(index: number) {
        const observation = this.formGroup.controls.Observations.controls[index].value;
        if (observation.FileResourceID) {
            this.onlandVisualTrashAssessmentObservationService
                .deleteObservationPhotoOnlandVisualTrashAssessmentObservation(this.onlandVisualTrashAssessmentID, observation.FileResourceID)
                .subscribe((x) => {
                    observation.FileResourceID = null;
                    observation.FileResourceGUID = null;
                    this.formGroup.controls.Observations.controls[index].patchValue(observation);
                });
        }
    }
}

export class OnlandVisualTrashAssessmentObservationsUpsertDtoCustomForm {
    Observations: FormArray<FormGroup<OnlandVisualTrashAssessmentObservationWithPhotoDtoForm>>;
}
