import { AfterViewInit, Component, EventEmitter, Input, OnChanges, Output, SimpleChange } from "@angular/core";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";

import { WfsService } from "src/app/shared/services/wfs.service";
import { GroupByPipe } from "src/app/shared/pipes/group-by.pipe";
import { OVTA_AREA_STYLE_BY_SCORE, SELECTED_FEATURE_STYLE } from "src/app/shared/constants/map-styles";

@Component({
    selector: "selected-ovta-area-layer",
    imports: [],
    templateUrl: "./selected-ovta-area-layer.component.html",
    styleUrl: "./selected-ovta-area-layer.component.scss",
})
export class SelectedOvtaAreaLayerComponent extends MapLayerBase implements OnChanges, AfterViewInit {
    @Input() selectedOVTAAreaID: number;

    @Output() layerBoundsCalculated = new EventEmitter();
    @Output() ovtaAreaSelected = new EventEmitter<number>();

    public layer: L.FeatureGroup;

    private styleDictionary = OVTA_AREA_STYLE_BY_SCORE;

    private highlightStyle = SELECTED_FEATURE_STYLE;

    constructor(
        private wfsService: WfsService,
        private groupByPipe: GroupByPipe
    ) {
        super();
    }

    ngOnChanges(changes: any): void {
        if (changes.selectedOVTAAreaID) {
            if (changes.selectedOVTAAreaID.previousValue == changes.selectedOVTAAreaID.currentValue) return;
            this.selectedOVTAAreaID = changes.selectedOVTAAreaID.currentValue;
            this.highlightSelectedOVTAArea();
        } else if (Object.values(changes).some((x: SimpleChange) => x.firstChange === false)) {
            this.updateLayer();
        }
    }

    ngAfterViewInit(): void {
        this.setupLayer();
        this.updateLayer();
    }

    private updateLayer() {
        this.layer.clearLayers();
        this.addOVTAAreasToLayer();
        this.layer.addTo(this.map);
    }

    private addOVTAAreasToLayer() {
        const cql_filter = ``;

        this.trackLayerRequest$(
            this.wfsService.getGeoserverWFSLayerWithCQLFilter("OCStormwater:OnlandVisualTrashAssessmentAreas", cql_filter, "OnlandVisualTrashAssessmentAreaID")
        ).subscribe((response) => {
            if (response.length == 0) return;

            const featuresGroupedByOVTAAreaID = this.groupByPipe.transform(response, "properties.OnlandVisualTrashAssessmentAreaID");

            Object.keys(featuresGroupedByOVTAAreaID).forEach((ovtaAreaID) => {
                const geoJson = L.geoJSON(featuresGroupedByOVTAAreaID[ovtaAreaID], {
                    style: this.styleDictionary[featuresGroupedByOVTAAreaID[ovtaAreaID][0].properties.Score],
                });
                geoJson.on("mouseover", (e) => {
                    geoJson.setStyle({ fillOpacity: 0.5 });
                });
                geoJson.on("mouseout", (e) => {
                    geoJson.setStyle({ fillOpacity: 0.1 });
                });

                geoJson.on("click", (e) => {
                    this.onOVTAAreaSelected(Number(ovtaAreaID));
                });

                geoJson.addTo(this.layer);
            });
        });
    }

    private onOVTAAreaSelected(ovtaAreaID: number) {
        this.selectedOVTAAreaID = ovtaAreaID;
        this.highlightSelectedOVTAArea();

        this.ovtaAreaSelected.emit(ovtaAreaID);
    }

    private highlightSelectedOVTAArea() {
        this.layer.eachLayer((layer) => {
            // skip if well layer
            if (layer instanceof L.Marker && layer.options.icon) {
                // layer has an icon
                return;
            }

            if (layer instanceof L.GeoJSON) {
                const geoJsonLayers = layer.getLayers() as (L.Path & { feature?: GeoJSON.Feature })[];
                if (geoJsonLayers[0].feature.properties.OnlandVisualTrashAssessmentAreaID == this.selectedOVTAAreaID) {
                    layer.setStyle(this.highlightStyle);
                    if ("getBounds" in layer && typeof layer.getBounds === "function") {
                        this.map.fitBounds(layer.getBounds());
                    }
                } else {
                    layer.setStyle(this.styleDictionary[geoJsonLayers[0].feature.properties.Score]);
                }
            }
        });
    }

    private setupLayer() {
        this.layer = L.geoJSON();
        this.initLayer();
    }
}
