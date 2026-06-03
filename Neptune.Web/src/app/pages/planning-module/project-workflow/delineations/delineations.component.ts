import * as turf from "@turf/turf";
import { Component, ElementRef, EventEmitter, OnInit, Output, ViewChild } from "@angular/core";
import { Router } from "@angular/router";
import { Input } from "@angular/core";
import L, { PM } from "leaflet";
import "@geoman-io/leaflet-geoman-free";
import { BehaviorSubject, distinctUntilChanged, forkJoin, map, Observable } from "rxjs";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { DelineationUpsertDto } from "src/app/shared/generated/model/delineation-upsert-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { ProjectService } from "src/app/shared/generated/api/project.service";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { DelineationTypeEnum } from "src/app/shared/generated/enum/delineation-type-enum";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { TreatmentBMPDisplayDto } from "src/app/shared/generated/model/treatment-bmp-display-dto";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { AsyncPipe } from "@angular/common";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WorkflowBodyComponent } from "src/app/shared/components/workflow-body/workflow-body.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { ProjectWorkflowProgressService } from "src/app/shared/services/project-workflow-progress.service";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { StormwaterNetworkLayerComponent } from "src/app/shared/components/leaflet/layers/stormwater-network-layer/stormwater-network-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { InventoriedBMPsLayerComponent } from "src/app/shared/components/leaflet/layers/inventoried-bmps-layer/inventoried-bmps-layer.component";
import { LeafletHelperService } from "src/app/shared/services/leaflet-helper.service";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";

@Component({
    selector: "delineations",
    templateUrl: "./delineations.component.html",
    styleUrls: ["./delineations.component.scss"],
    imports: [
        CustomRichTextComponent,
        AsyncPipe,
        FieldDefinitionComponent,
        PageHeaderComponent,
        WorkflowBodyComponent,
        AlertDisplayComponent,
        NeptuneMapComponent,
        RegionalSubbasinsLayerComponent,
        DelineationsLayerComponent,
        JurisdictionsLayerComponent,
        WqmpsLayerComponent,
        StormwaterNetworkLayerComponent,
        InventoriedBMPsLayerComponent,
    ],
})
export class DelineationsComponent implements OnInit {
    public OverlayMode = OverlayMode;
    @ViewChild("mapContainer") mapContainer: ElementRef;
    public mapIsReady: boolean = false;

    public drawMapClicked: boolean = false;
    public delineations: DelineationUpsertDto[];
    private readonly delineationsSubject = new BehaviorSubject<DelineationUpsertDto[]>([]);
    public readonly delineations$ = this.delineationsSubject.asObservable();
    private originalDelineations: string;
    public zoomMapToDefaultExtent: boolean = true;
    public mapHeight: string = "750px";
    public defaultFitBoundsOptions?: L.FitBoundsOptions = null;
    public onEachFeatureCallback?: (feature, layer) => void;

    @Output()
    public afterSetControl: EventEmitter<L.Control.Layers> = new EventEmitter();

    @Output()
    public afterLoadMap: EventEmitter<L.LeafletEvent> = new EventEmitter();

    @Output()
    public onMapMoveEnd: EventEmitter<L.LeafletEvent> = new EventEmitter();

    public map: L.Map;
    public featureLayer: any;
    public delineationFeatureGroup: L.FeatureGroup = new L.FeatureGroup();
    public layerControl: L.Control.Layers;
    public boundingBox$: Observable<BoundingBoxDto>;
    private squareMetersToAcreDivisor: number = 4047;
    public selectedListItem: number;
    public selectedObjectMarker: L.Layer;
    public selectedDelineation: DelineationUpsertDto;
    public selectedTreatmentBMP: TreatmentBMPDisplayDto;
    private readonly selectedTreatmentBMPSubject = new BehaviorSubject<TreatmentBMPDisplayDto | null>(null);
    public readonly selectedTreatmentBMP$ = this.selectedTreatmentBMPSubject.asObservable();
    public readonly selectedTreatmentBMPID$ = this.selectedTreatmentBMP$.pipe(
        map((bmp) => bmp?.TreatmentBMPID ?? null),
        distinctUntilChanged()
    );
    public treatmentBMPsLayer: L.GeoJSON<any>;
    public selectedListItemDetails: { [key: string]: any } = {};
    private delineationDefaultStyle = {
        color: "blue",
        fillOpacity: 0.2,
        opacity: 0,
    };
    private delineationSelectedStyle = {
        color: "yellow",
        fillOpacity: 0.2,
        opacity: 1,
    };
    @Input() projectID!: number;
    public customRichTextTypeID = NeptunePageTypeEnum.HippocampDelineations;

    public isPerformingDrawAction: boolean = false;
    private newDelineationID: number = -1;
    private readonly isLoadingSubmitSubject = new BehaviorSubject<boolean>(false);
    public readonly isLoadingSubmit$ = this.isLoadingSubmitSubject.asObservable();
    public isEditingLocation = false;

    public projectTreatmentBMPs: Array<TreatmentBMPDisplayDto>;
    private readonly projectTreatmentBMPsSubject = new BehaviorSubject<TreatmentBMPDisplayDto[]>([]);
    public readonly projectTreatmentBMPs$ = this.projectTreatmentBMPsSubject.asObservable();

    // 1. Initialization & Lifecycle

    constructor(
        private treatmentBMPService: TreatmentBMPService,
        private router: Router,
        private alertService: AlertService,
        private projectWorkflowProgressService: ProjectWorkflowProgressService,
        private projectService: ProjectService,
        private leafletHelperService: LeafletHelperService
    ) {}

    public ngOnInit(): void {
        this.boundingBox$ = this.projectService.getBoundingBoxByProjectIDProject(this.projectID);
    }

    canExit() {
        let currentDelineations = JSON.stringify(this.mapDelineationsToGeoJson(this.delineations));
        return this.originalDelineations == currentDelineations;
    }

    // 2. Map Setup & Control
    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;

        if (this.projectID) {
            forkJoin({
                project: this.projectService.getProject(this.projectID),
                treatmentBMPs: this.projectService.listTreatmentBMPsByProjectIDProject(this.projectID),
                delineations: this.projectService.listDelineationsByProjectIDProject(this.projectID),
            }).subscribe(({ project, treatmentBMPs, delineations }) => {
                // redirect to review step if project is shared with OCTA grant program
                if (project.ShareOCTAM2Tier2Scores) {
                    this.router.navigateByUrl(`/planning/projects/edit/${this.projectID}/review-and-share`);
                }
                if (treatmentBMPs.length == 0) {
                    this.router.navigateByUrl(`/planning/projects/edit/${this.projectID}`);
                }

                this.projectTreatmentBMPs = treatmentBMPs;
                this.projectTreatmentBMPsSubject.next(this.projectTreatmentBMPs);
                this.delineations = delineations;
                this.delineationsSubject.next(this.delineations ?? []);
                this.originalDelineations = JSON.stringify(this.mapDelineationsToGeoJson(this.delineations));

                this.initializeMap();
            });
        }

        this.registerClickEvents();
    }

    public initializeMap(): void {
        this.delineationFeatureGroup.addTo(this.map);
        this.resetDelineationFeatureGroups();

        const treatmentBMPsGeoJson = this.mapTreatmentBMPsToGeoJson(this.projectTreatmentBMPs);
        this.treatmentBMPsLayer = new L.GeoJSON(treatmentBMPsGeoJson as any, {
            pointToLayer: (feature, latlng) => {
                return L.marker(latlng, { icon: MarkerHelper.treatmentBMPMarker });
            },
        });
        this.selectTreatmentBMP(this.projectTreatmentBMPs[0].TreatmentBMPID);
        this.treatmentBMPsLayer.addTo(this.map);
        this.setControl();
        this.selectFeatureImpl(this.selectedTreatmentBMP.TreatmentBMPID);
    }

    public setControl(): void {
        // Only keep global pm:create for new delineations
        this.map
            .on("pm:create", (event: { shape: PM.SUPPORTED_SHAPES; layer: L.Polygon & { toGeoJSON: () => GeoJSON.Feature } }) => {
                this.isPerformingDrawAction = false;
                const layer = event.layer;
                var delineationUpsertDto = this.delineations.find((x) => this.selectedTreatmentBMP.TreatmentBMPID == x.TreatmentBMPID);
                if (delineationUpsertDto == null) {
                    delineationUpsertDto = new DelineationUpsertDto({
                        DelineationID: this.newDelineationID,
                        TreatmentBMPID: this.selectedTreatmentBMP.TreatmentBMPID,
                    });
                    this.delineations = this.delineations.concat(delineationUpsertDto);
                    this.delineationsSubject.next(this.delineations);
                    this.newDelineationID--;
                }
                delineationUpsertDto.DelineationTypeID = DelineationTypeEnum.Distributed;
                const geometry = layer.toGeoJSON();
                geometry.properties = {
                    TreatmentBMPID: this.selectedTreatmentBMP.TreatmentBMPID,
                    DelineationID: this.newDelineationID,
                };
                delineationUpsertDto.Geometry = JSON.stringify(geometry);
                delineationUpsertDto.DelineationArea = this.calculateAcreage(layer.toGeoJSON());
                // Remove the drawn layer to prevent duplicates
                if (layer && layer.remove) {
                    layer.remove();
                }
                this.resetDelineationFeatureGroups();
                this.delineationsSubject.next(this.delineations);

                this.selectFeatureImpl(this.selectedTreatmentBMP.TreatmentBMPID, true);
            })
            .on("pm:globaleditmodetoggled", (e: any) => {
                if (e.enabled) {
                    // When global edit mode is enabled, only allow editing for the selected delineation
                    this.delineationFeatureGroup.eachLayer((layer: L.Path & { feature?: GeoJSON.Feature }) => {
                        if (this.selectedDelineation == null || this.selectedDelineation.TreatmentBMPID != layer.feature.properties.TreatmentBMPID) {
                            if (layer.pm) layer.pm.disable();
                        } else {
                            if (layer.pm) layer.pm.enable();
                        }
                    });
                    return;
                }
                // Edit mode was just exited, user is done editing
                // Sync delineation data with edited layers
                this.getFullyQualifiedJSONGeometryForDelineations(this.delineations);
                this.resetDelineationFeatureGroups();
                if (this.selectedTreatmentBMP) {
                    this.selectFeatureImpl(this.selectedTreatmentBMP.TreatmentBMPID);
                }
            })
            .on("pm:globalremovalmodetoggled", (e: any) => {
                if (e.enabled) {
                    var delineationUpsertDto = this.delineations.find((x) => this.selectedTreatmentBMP.TreatmentBMPID == x.TreatmentBMPID);
                    delineationUpsertDto.Geometry = null;
                    delineationUpsertDto.DelineationArea = null;
                    delineationUpsertDto.DelineationTypeID = null;
                    this.resetDelineationFeatureGroups();
                    this.map.pm.toggleGlobalRemovalMode();
                }
                // Delete mode was just exited, user is done editing
                // Sync delineation data with edited layers
                this.getFullyQualifiedJSONGeometryForDelineations(this.delineations);
                this.resetDelineationFeatureGroups();
                if (this.selectedTreatmentBMP) {
                    this.selectFeatureImpl(this.selectedTreatmentBMP.TreatmentBMPID);
                }
            });
        this.addOrRemoveGeomanControl(true);
        this.afterSetControl.emit(this.layerControl);
    }

    // 3. Data Mapping & Layer Management
    private mapTreatmentBMPsToGeoJson(treatmentBMPs: TreatmentBMPDisplayDto[]) {
        return this.mapToFeatureCollection(
            treatmentBMPs,
            (x) => ({ type: "Point", coordinates: [x.Longitude ?? 0, x.Latitude ?? 0] }),
            (x) => ({
                TreatmentBMPID: x.TreatmentBMPID,
                TreatmentBMPName: x.TreatmentBMPName,
                TreatmentBMPTypeName: x.TreatmentBMPTypeName,
                Latitude: x.Latitude,
                Longitude: x.Longitude,
            })
        );
    }

    private mapDelineationsToGeoJson(delineations: DelineationUpsertDto[]) {
        return delineations.filter((x) => x.Geometry != null).map((x) => JSON.parse(x.Geometry));
    }

    public addFeatureCollectionToFeatureGroup(featureJsons: any, featureGroup: L.FeatureGroup) {
        if (!featureJsons || featureJsons.length == 0) {
            return;
        }
        L.geoJson(featureJsons, {
            onEachFeature: (feature, layer) => {
                this.addLayersToFeatureGroup(layer, featureGroup);
                layer.on("click", (e) => {
                    if (this.isEditingLocation || this.map.pm.globalRemovalModeEnabled() || this.map.pm.globalEditModeEnabled()) {
                        return;
                    }
                    this.selectFeatureImpl(feature.properties.TreatmentBMPID);
                });
                // Attach Geoman events per-layer
                layer.on("pm:edit", (event) => {
                    const editedLayer = event.layer as L.Path & { feature?: GeoJSON.Feature; toGeoJSON: () => GeoJSON.Feature };
                    var delineationUpsertDto = this.delineations.find((x) => editedLayer.feature.properties.TreatmentBMPID == x.TreatmentBMPID);
                    // Always set delineation type to distributed when edited
                    delineationUpsertDto.DelineationTypeID = DelineationTypeEnum.Distributed;
                    this.selectedDelineation.DelineationTypeID = DelineationTypeEnum.Distributed;
                    delineationUpsertDto.Geometry = JSON.stringify(editedLayer.toGeoJSON());
                    delineationUpsertDto.DelineationArea = this.calculateAcreage(editedLayer.toGeoJSON());
                });
            },
        });
    }

    private addLayersToFeatureGroup(layer: any, featureGroup: L.FeatureGroup) {
        if (layer.getLayers) {
            layer.getLayers().forEach((l) => {
                featureGroup.addLayer(l);
            });
        } else {
            featureGroup.addLayer(layer);
        }
    }

    private resetDelineationFeatureGroups() {
        this.delineationFeatureGroup.clearLayers();
        this.addFeatureCollectionToFeatureGroup(this.mapDelineationsToGeoJson(this.delineations), this.delineationFeatureGroup);
    }

    public toggleIsEditingLocation() {
        if (this.isEditingLocation) {
            this.save();
        }
        this.isEditingLocation = !this.isEditingLocation;
        document.querySelector(".leaflet-interactive").setAttribute("style", "cursor: crosshair");
        this.updateTreatmentBMPsLayer();
        this.isEditingLocation ? this.addOrRemoveGeomanControl(false) : this.addOrRemoveGeomanControl(true);
    }

    // 4. Selection & Interaction

    public selectFeatureImpl(treatmentBMPID: number, skipDrawCheck: boolean = false) {
        if (this.isPerformingGeomanAction(skipDrawCheck)) {
            return;
        }

        let hasFlownToSelectedObject = false;

        this.map.pm.removeControls();

        if (this.selectedListItem) {
            this.selectedListItem = null;
        }

        if (this.selectedDelineation) {
            this.selectedDelineation = null;
        }

        this.selectedDelineation = this.delineations?.find((x) => x.TreatmentBMPID == treatmentBMPID);
        this.delineationFeatureGroup.eachLayer((layer: L.Polygon) => {
            const isSelected = this.selectedDelineation != null && this.selectedDelineation.TreatmentBMPID == layer.feature.properties.TreatmentBMPID;
            // Only enable edit mode if global edit mode is active
            const shouldEdit = isSelected && this.map.pm.globalEditModeEnabled();
            this.setLayerEditState(layer, isSelected ? this.delineationSelectedStyle : this.delineationDefaultStyle, shouldEdit);
            if (isSelected) {
                layer.bringToFront();
                this.map.flyToBounds(layer.getBounds(), { padding: new L.Point(50, 50) });
                hasFlownToSelectedObject = true;
            }
        });

        this.selectedListItem = treatmentBMPID;
        this.selectedTreatmentBMP = this.projectTreatmentBMPs.find((x) => x.TreatmentBMPID == treatmentBMPID);
        this.selectedTreatmentBMPSubject.next(this.selectedTreatmentBMP ?? null);
        this.treatmentBMPsLayer?.eachLayer((layer: L.Marker) => {
            if (this.selectedTreatmentBMP == null || this.selectedTreatmentBMP.TreatmentBMPID != layer.feature.properties.TreatmentBMPID) {
                layer.setIcon(MarkerHelper.treatmentBMPMarker).setZIndexOffset(1000);
                return;
            }
            layer.setIcon(MarkerHelper.selectedMarker);
            layer.setZIndexOffset(10000);
            if (!hasFlownToSelectedObject) {
                this.map.flyTo(layer.getLatLng(), 18);
            }
        });
        this.addOrRemoveGeomanControl(true);

        if (this.drawMapClicked) {
            if (this.isEditingLocation) {
                return;
            }
            if (this.selectedDelineation?.Geometry != null) {
                this.map.pm.toggleGlobalEditMode();
            } else {
                this.map.pm.enableDraw("Polygon");
            }
            this.scrollMapIntoView();
        }
        this.drawMapClicked = false;
    }

    public registerClickEvents(): void {
        this.map.on("click", (event: L.LeafletMouseEvent) => {
            if (!this.isEditingLocation || this.isPerformingGeomanAction()) {
                return;
            }
            if (this.selectedObjectMarker) {
                this.map.removeLayer(this.selectedObjectMarker);
            }
            this.selectedObjectMarker = L.marker(event.latlng, { icon: MarkerHelper.selectedMarker, zIndexOffset: 1000 });

            this.selectedObjectMarker.addTo(this.map);

            this.selectedTreatmentBMP.Latitude = event.latlng.lat;
            this.selectedTreatmentBMP.Longitude = event.latlng.lng;
            this.selectedTreatmentBMPSubject.next(this.selectedTreatmentBMP);
            this.projectTreatmentBMPsSubject.next(this.projectTreatmentBMPs);
            this.updateTreatmentBMPsLayer();
        });
    }

    public selectTreatmentBMP(treatmentBMPID: number) {
        this.isEditingLocation = false;
        this.selectTreatmentBMPImpl(treatmentBMPID);
        this.updateTreatmentBMPsLayer();
    }

    private selectTreatmentBMPImpl(treatmentBMPID: number) {
        this.clearSelectedItem();

        this.selectedListItem = treatmentBMPID;
        let selectedNumber = null;
        let selectedAttributes = null;
        this.selectedTreatmentBMP = this.projectTreatmentBMPs.find((x) => x.TreatmentBMPID == treatmentBMPID);
        this.selectedTreatmentBMPSubject.next(this.selectedTreatmentBMP ?? null);
        selectedAttributes = [
            `<strong>Type:</strong> ${this.selectedTreatmentBMP.TreatmentBMPTypeName}`,
            `<strong>Latitude:</strong> ${this.selectedTreatmentBMP.Latitude}`,
            `<strong>Longitude:</strong> ${this.selectedTreatmentBMP.Longitude}`,
        ];

        if (this.selectedTreatmentBMP && this.selectedTreatmentBMP.Latitude && this.selectedTreatmentBMP.Longitude) {
            this.selectedListItemDetails.title = `${selectedNumber}`;
            this.selectedListItemDetails.attributes = selectedAttributes;
        }
    }

    scrollMapIntoView(): void {
        let mapElement = document.getElementById(this.mapContainer["mapID"]);
        if (mapElement) {
            mapElement.scrollIntoView({ behavior: "smooth", block: "center" });
        }
    }

    // 5. Delineation Actions
    public drawDelineationForTreatmentBMP(treatmentBMPID: number) {
        this.drawMapClicked = true;

        let delineationForTreatmentBMP = this.delineations.find((x) => x.TreatmentBMPID == treatmentBMPID);
        if (delineationForTreatmentBMP) {
            this.selectFeatureImpl(treatmentBMPID);
            return;
        }

        var newDelineation = new DelineationUpsertDto({
            DelineationTypeID: DelineationTypeEnum.Distributed,
            TreatmentBMPID: treatmentBMPID,
        });

        this.delineations = this.delineations.concat(newDelineation);
        this.delineationsSubject.next(this.delineations);
        this.selectFeatureImpl(treatmentBMPID);
    }

    public getFullyQualifiedJSONGeometryForDelineations(delineations: DelineationUpsertDto[]) {
        //We need a fully qualified geojson string and above we are just getting the geometry
        //Possible can remove the update above if we are always going to do it here
        this.delineationFeatureGroup.eachLayer((layer: L.Path & { feature?: GeoJSON.Feature; toGeoJSON: () => GeoJSON.Feature }) => {
            var delineationUpsertDto = delineations.find((x) => x.TreatmentBMPID == layer.feature.properties.TreatmentBMPID);
            delineationUpsertDto.Geometry = JSON.stringify(layer.toGeoJSON());
        });
    }

    public getUpstreamRSBCatchmentForTreatmentBMP(treatmentBMPID: number) {
        this.treatmentBMPService.getUpstreamRSBCatchmentGeoJSONForTreatmentBMPTreatmentBMP(treatmentBMPID).subscribe((result) => {
            let currentDelineationForTreatmentBMP = this.delineations.find((x) => x.TreatmentBMPID == treatmentBMPID);
            if (currentDelineationForTreatmentBMP == null) {
                currentDelineationForTreatmentBMP = new DelineationUpsertDto({
                    TreatmentBMPID: treatmentBMPID,
                });
                this.delineations = this.delineations.concat(currentDelineationForTreatmentBMP);
                this.delineationsSubject.next(this.delineations);
            }
            currentDelineationForTreatmentBMP.DelineationTypeID = DelineationTypeEnum.Centralized;
            currentDelineationForTreatmentBMP.Geometry = result.GeometryGeoJSON;
            currentDelineationForTreatmentBMP.DelineationArea = result.Area;
            this.resetDelineationFeatureGroups();
            this.delineationsSubject.next(this.delineations);
            this.selectFeatureImpl(this.selectedTreatmentBMP.TreatmentBMPID);
        });
    }

    public save(continueToNextStep?: boolean) {
        this.isLoadingSubmitSubject.next(true);
        this.alertService.clearAlerts();
        this.getFullyQualifiedJSONGeometryForDelineations(this.delineations);
        var updatedDelineations = this.delineations.filter((x) => x.Geometry != null);
        forkJoin({
            delineations: this.projectService.mergeDelineationsForProjectProject(this.projectID, updatedDelineations),
            treatmentBMPs: this.projectService.mergeTreatmentBMPLocationsProject(this.projectID, this.projectTreatmentBMPs),
        }).subscribe({
            next: ({ delineations, treatmentBMPs }) => {
                window.scroll(0, 0);
                this.isLoadingSubmitSubject.next(false);
                this.projectWorkflowProgressService.updateProgress(this.projectID);
                this.projectService.listDelineationsByProjectIDProject(this.projectID).subscribe((delineations) => {
                    this.delineations = delineations;
                    this.delineationsSubject.next(this.delineations ?? []);
                    this.originalDelineations = JSON.stringify(this.mapDelineationsToGeoJson(this.delineations));
                    this.resetDelineationFeatureGroups();
                    this.selectFeatureImpl(this.selectedTreatmentBMP.TreatmentBMPID);

                    if (continueToNextStep) {
                        this.router.navigateByUrl(`/planning/projects/edit/${this.projectID}/stormwater-treatments/modeled-performance-and-metrics`).then((x) => {
                            this.alertService.pushAlert(new Alert("Your Delineation changes have been saved.", AlertContext.Success, true));
                        });
                        return;
                    }

                    this.alertService.pushAlert(new Alert("Your Delineation changes have been saved.", AlertContext.Success, true));
                });
            },
            error: (error) => {
                this.isLoadingSubmitSubject.next(false);
                window.scroll(0, 0);
            },
        });
    }

    // 6. Utility & State
    // --- Helper Functions ---
    private setLayerEditState(layer: any, style: any, editable: boolean) {
        layer.setStyle(style);
        if (layer.pm) {
            editable ? layer.pm.enable() : layer.pm.disable();
        }
    }

    private calculateAcreage(geoJson: any): number {
        return +(turf.area(geoJson) / this.squareMetersToAcreDivisor).toFixed(2);
    }

    private mapToFeatureCollection(items: any[], geometryMapper: (item: any) => any, propertiesMapper: (item: any) => any) {
        return {
            type: "FeatureCollection",
            features: items.map((item) => ({
                type: "Feature",
                geometry: geometryMapper(item),
                properties: propertiesMapper(item),
            })),
        };
    }
    public isPerformingGeomanAction(skipDrawCheck: boolean = false): boolean {
        //MP 10/1/25 - Added skipDrawCheck because the global draw mode remains enabled momentarily after drawing a shape is complete
        return (this.map?.pm?.globalDrawModeEnabled() && !skipDrawCheck) || this.map?.pm?.globalEditModeEnabled() || this.map?.pm?.globalRemovalModeEnabled();
    }

    // 7. Getters
    public treatmentBMPHasDelineationGeometry(treatmentBMPID: number) {
        return this.delineations?.find((x) => x.TreatmentBMPID == treatmentBMPID)?.Geometry;
    }

    public getTreatmentBMPDelineation(treatmentBMPID: number) {
        return this.delineations?.find((x) => x.TreatmentBMPID == treatmentBMPID);
    }

    public getDelineationAreaForTreatmentBMP(treatmentBMPID: number) {
        let delineation = this.delineations?.find((x) => x.TreatmentBMPID == treatmentBMPID);
        if (delineation?.DelineationArea == null) {
            return "Not provided yet";
        }
        return `${delineation.DelineationArea} ac`;
    }

    public treatmentBMPHasDelineation(treatmentBMPID: number) {
        return this.delineations?.find((x) => x.TreatmentBMPID == treatmentBMPID) != null;
    }

    public updateTreatmentBMPsLayer() {
        if (this.treatmentBMPsLayer) {
            this.map.removeLayer(this.treatmentBMPsLayer);
            this.treatmentBMPsLayer = null;
        }
        const treatmentBMPsGeoJson = this.mapTreatmentBMPsToGeoJson(this.projectTreatmentBMPs);
        this.treatmentBMPsLayer = new L.GeoJSON(treatmentBMPsGeoJson as any, {
            pointToLayer: (feature, latlng) => {
                if (feature.properties.TreatmentBMPID == this.selectedTreatmentBMP?.TreatmentBMPID) {
                    return L.marker(latlng, { icon: MarkerHelper.selectedMarker });
                }
                return L.marker(latlng, { icon: MarkerHelper.treatmentBMPMarker });
            },
            onEachFeature: (feature, layer: L.Marker) => {
                if (this.selectedTreatmentBMP != null) {
                    if (layer.feature.properties.TreatmentBMPID != this.selectedTreatmentBMP.TreatmentBMPID) {
                        return;
                    }
                    this.map.flyTo(layer.getLatLng(), 18);
                }
            },
        });
        this.treatmentBMPsLayer.addTo(this.map);
        this.treatmentBMPsLayer.on("click", (event: L.LeafletEvent) => {
            if (this.isEditingLocation) {
                return;
            }
            this.selectFeatureImpl(event.propagatedFrom.feature.properties.TreatmentBMPID);
        });
    }

    public addOrRemoveGeomanControl(turnOn: boolean) {
        if (turnOn) {
            // Show delete only if selected TreatmentBMP has a delineation
            const bmpSelected = !!this.selectedTreatmentBMP;
            const hasDelineation = bmpSelected && !!(this.selectedDelineation && this.selectedDelineation.Geometry);
            this.leafletHelperService.setupGeomanControls(this.map, !hasDelineation, hasDelineation, hasDelineation, "Delineation");
            return;
        }
        this.map.pm.removeControls();
    }

    private clearSelectedItem() {
        if (this.selectedListItem) {
            this.selectedListItem = null;
            this.selectedListItemDetails = {};
            if (this.selectedObjectMarker) {
                this.map.removeLayer(this.selectedObjectMarker);
            }
            this.selectedObjectMarker = null;
        }
    }
}
