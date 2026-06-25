import { Component, ElementRef, EventEmitter, Input, Output, signal, ViewChild } from "@angular/core";
import * as L from "leaflet";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";

import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { OnlandVisualTrashAssessmentObservationWithPhotoDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-observation-with-photo-dto";
import { fileResourceUrl } from "src/app/shared/helpers/file-resource-url";
import { ActivatedRoute, Router } from "@angular/router";
import { LandUseBlockLayerComponent } from "src/app/shared/components/leaflet/layers/land-use-block-layer/land-use-block-layer.component";
import { OvtaAreaLayerComponent } from "../../../../shared/components/leaflet/layers/ovta-area-layer/ovta-area-layer.component";
import { TransectLineLayerComponent } from "src/app/shared/components/leaflet/layers/transect-line-layer/transect-line-layer.component";

//This component could use a fair amount of cleanup. It should likely be sent in the treatment bmps and delineations instead of grabbing them itself
@Component({
    selector: "observations-map",
    templateUrl: "./observations-map.component.html",
    styleUrls: ["./observations-map.component.scss"],
    imports: [NeptuneMapComponent, LandUseBlockLayerComponent, TransectLineLayerComponent, OvtaAreaLayerComponent],
})
export class ObservationsMapComponent {
    @ViewChild("ovtaObservations") ovtaObservations: ElementRef;
    @Input("observations") onlandVisualTrashAssessmentObservations: Array<OnlandVisualTrashAssessmentObservationWithPhotoDto>;
    @Input() onlandVisualTrashAssessmentID: number;
    public boundingBox: BoundingBoxDto;

    // Signal (not plain boolean) so zoneless change detection re-runs the template's @if
    // guards when Leaflet fires onMapLoad. With a plain field, the ready flag would flip but
    // the layer @if gates never re-evaluate and the map sits behind the loading spinner
    // forever. Parallels the fix Jamie landed on trash-ovta-record-observations (commit
    // dc98dec57).
    public mapIsReady = signal(false);
    public mapHeight: string = "600px";

    @Output()
    public afterSetControl: EventEmitter<L.Control.Layers> = new EventEmitter();

    @Output()
    public afterLoadMap: EventEmitter<L.LeafletEvent> = new EventEmitter();

    @Output()
    public onMapMoveEnd: EventEmitter<L.LeafletEvent> = new EventEmitter();

    public map: L.Map;
    public layerControl: L.Control.Layers;
    public selectedObjectMarker: L.Layer;
    public selectedOnlandVisualTrashAssessmentObservation: OnlandVisualTrashAssessmentObservationWithPhotoDto;
    public onlandVisualTrashAssessmentObservationsLayer: L.GeoJSON<any>;
    private ovtaObservationOverlayName = "<img src='./assets/main/map-icons/marker-icon-violet.png' style='height:17px'> Observations";

    public ovtaObservationLayer: L.GeoJSON<any>;

    constructor(private router: Router, private route: ActivatedRoute) {}

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady.set(true);
        this.addObservationPointsLayersToMap();
    }

    public addObservationPointsLayersToMap(): void {
        if (this.ovtaObservationLayer) {
            this.map.removeLayer(this.ovtaObservationLayer);
            this.layerControl.removeLayer(this.ovtaObservationLayer);
        }
        const ovtaObservationGeoJSON = this.mapObservationsToGeoJson(this.onlandVisualTrashAssessmentObservations);
        this.ovtaObservationLayer = new L.GeoJSON(ovtaObservationGeoJSON as any, {
            pointToLayer: (feature, latlng) => {
                return L.marker(latlng, { icon: MarkerHelper.treatmentBMPMarker });
            },
            onEachFeature: (feature, layer) => {
                layer.on("click", (e) => {
                    this.selectOnlandVisualTrashAssessmentObservation(feature.properties.OnlandVisualTrashAssessmentObservationID);
                });
            },
        });
        this.ovtaObservationLayer["sortOrder"] = 100;
        this.map.fitBounds(this.ovtaObservationLayer.getBounds());
        this.ovtaObservationLayer.addTo(this.map);
        this.layerControl.addOverlay(this.ovtaObservationLayer, this.ovtaObservationOverlayName);
    }

    private mapObservationsToGeoJson(observations: OnlandVisualTrashAssessmentObservationWithPhotoDto[]) {
        return {
            type: "FeatureCollection",
            features: observations.map((x) => {
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

    public selectOnlandVisualTrashAssessmentObservation(onlandVisualTrashAssessmentObservationID: number) {
        if (!this.map.hasLayer(this.ovtaObservationLayer)) {
            this.ovtaObservationLayer.addTo(this.map);
        }

        this.selectedOnlandVisualTrashAssessmentObservation = this.onlandVisualTrashAssessmentObservations.find(
            (x) => x.OnlandVisualTrashAssessmentObservationID == onlandVisualTrashAssessmentObservationID
        );
        this.ovtaObservationLayer.eachLayer((layer: L.Marker) => {
            if (layer.feature.properties.OnlandVisualTrashAssessmentObservationID == this.selectedOnlandVisualTrashAssessmentObservation.OnlandVisualTrashAssessmentObservationID) {
                if (!layer.feature.properties.DefaultZIndexOffset) {
                    layer.feature.properties.DefaultZIndexOffset = layer.options.zIndexOffset;
                }
                layer.setZIndexOffset(10000);
                layer.setIcon(MarkerHelper.selectedMarker);
                this.ovtaObservations.nativeElement
                    .querySelector(`#ovtaObservation${layer.feature.properties.OnlandVisualTrashAssessmentObservationID}`)
                    .scrollIntoView({ behavior: "smooth", block: "start" });
            } else {
                layer.setIcon(MarkerHelper.buildDefaultLeafletMarkerFromMarkerPath("/assets/main/map-icons/marker-icon-violet.png"));
            }
        });
    }

    public getFileResourceUrl(fileResourceGUID) {
        return fileResourceUrl(fileResourceGUID);
    }
}
