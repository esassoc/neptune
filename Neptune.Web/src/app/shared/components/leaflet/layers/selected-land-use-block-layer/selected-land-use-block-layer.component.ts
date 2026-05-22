import { AfterViewInit, Component, EventEmitter, Input, OnChanges, Output, SimpleChange } from "@angular/core";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";

import { WfsService } from "src/app/shared/services/wfs.service";
import { GroupByPipe } from "src/app/shared/pipes/group-by.pipe";
import { PriorityLandUseTypeEnum } from "src/app/shared/generated/enum/priority-land-use-type-enum";
import { finalize } from "rxjs";

@Component({
    selector: "selected-land-use-block-layer",
    imports: [],
    templateUrl: "./selected-land-use-block-layer.component.html",
    styleUrl: "./selected-land-use-block-layer.component.scss",
})
export class SelectedLandUseBlockLayerComponent extends MapLayerBase implements OnChanges, AfterViewInit {
    /** IDs to highlight as selected. Pass [] for none, [id] for single-select callers. */
    @Input() selectedLandUseBlockIDs: number[] = [];
    /** When set, restricts the WFS fetch to a single jurisdiction. Omit to render all blocks. */
    @Input() stormwaterJurisdictionID?: number;
    /** Single-select callers (e.g. the LUB index page) want the map to fit-bounds on each
     * external selection change. Multi-select callers (Select Assessment Area) opt out. */
    @Input() zoomToSelectionOnChange: boolean = false;

    @Output() layerBoundsCalculated = new EventEmitter();
    @Output() landUseBlockClicked = new EventEmitter<number>();

    public isLoading: boolean = false;
    public layer: L.FeatureGroup;

    /** Set true between an in-layer click and the parent's input write-back, so the
     * write-back's ngOnChanges doesn't trigger a re-zoom or re-style of work we already did. */
    private clickedWithinLayer: boolean = false;

    private styleDictionary = {
        [PriorityLandUseTypeEnum.Commercial]: {
            color: "#c2fbfc",
            weight: 2,
            opacity: 1,
            fillOpacity: 0.5,
        },
        [PriorityLandUseTypeEnum.HighDensityResidential]: {
            color: "#c0d6fc",
            weight: 2,
            opacity: 1,
            fillOpacity: 0.5,
        },
        [PriorityLandUseTypeEnum.Industrial]: {
            color: "#b4fcb3",
            weight: 2,
            opacity: 1,
            fillOpacity: 0.5,
        },
        [PriorityLandUseTypeEnum.MixedUrban]: {
            color: "#fcb6b9",
            weight: 2,
            opacity: 1,
            fillOpacity: 0.5,
        },
        [PriorityLandUseTypeEnum.CommercialRetail]: {
            color: "#f2cafc",
            weight: 2,
            opacity: 1,
            fillOpacity: 0.5,
        },
        [PriorityLandUseTypeEnum.PublicTransportationStations]: {
            color: "#fcd6b6",
            weight: 2,
            opacity: 1,
            fillOpacity: 0.5,
        },
        [PriorityLandUseTypeEnum.ALU]: {
            color: "#ffffed",
            weight: 2,
            opacity: 1,
            fillOpacity: 0.5,
        },
    };

    private highlightStyle = {
        color: "#fcfc12",
        weight: 2,
        opacity: 1,
        fillOpacity: 0.5,
    };

    constructor(
        private wfsService: WfsService,
        private groupByPipe: GroupByPipe
    ) {
        super();
    }

    ngOnChanges(changes: any): void {
        if (changes.stormwaterJurisdictionID && !changes.stormwaterJurisdictionID.firstChange) {
            // Jurisdiction changed mid-flight — re-fetch the feature set.
            this.updateLayer();
            return;
        }
        if (changes.selectedLandUseBlockIDs) {
            if (this.clickedWithinLayer) {
                this.clickedWithinLayer = false;
                this.applySelectionStyle();
                return;
            }
            if (changes.selectedLandUseBlockIDs.firstChange) {
                // Initial value flows through ngAfterViewInit's updateLayer instead.
                return;
            }
            this.applySelectionStyle();
            if (this.zoomToSelectionOnChange) {
                this.zoomToSelection();
            }
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
        this.addLandUseBlocksToLayer();
        this.layer.addTo(this.map);
    }

    private addLandUseBlocksToLayer() {
        const cql_filter = this.stormwaterJurisdictionID
            ? `StormwaterJurisdictionID = ${this.stormwaterJurisdictionID}`
            : ``;

        const request$ = this.wfsService.getGeoserverWFSLayerWithCQLFilter("OCStormwater:LandUseBlocks", cql_filter, "LandUseBlockID");
        const tracked$ = this.trackLayerRequest$(request$);

        this.isLoading = true;
        tracked$.pipe(finalize(() => (this.isLoading = false))).subscribe((response) => {
            if (response.length == 0) return;

            const featuresGroupedByLandUseBlockID = this.groupByPipe.transform(response, "properties.LandUseBlockID");

            Object.keys(featuresGroupedByLandUseBlockID).forEach((landUseBlockID) => {
                const idAsNumber = Number(landUseBlockID);
                const features = featuresGroupedByLandUseBlockID[landUseBlockID];
                const isSelected = this.selectedLandUseBlockIDs.includes(idAsNumber);
                const baseStyle = isSelected ? this.highlightStyle : this.styleDictionary[features[0].properties.PriorityLandUseTypeID];
                const geoJson = L.geoJSON(features, { style: baseStyle });
                geoJson.on("mouseover", () => {
                    geoJson.setStyle({ fillOpacity: 0.75 });
                });
                geoJson.on("mouseout", () => {
                    geoJson.setStyle({ fillOpacity: 0.5 });
                });

                geoJson.on("click", () => {
                    this.onLandUseBlockClicked(idAsNumber);
                });

                geoJson.addTo(this.layer);
            });

            if (this.zoomToSelectionOnChange) {
                this.zoomToSelection();
            }
        });
    }

    private onLandUseBlockClicked(landUseBlockID: number) {
        this.clickedWithinLayer = true;
        this.landUseBlockClicked.emit(landUseBlockID);
    }

    /** Re-apply category vs highlight style to every rendered feature based on the current
     * selectedLandUseBlockIDs array. Cheap; iterates only what's already on the map. */
    private applySelectionStyle() {
        this.layer.eachLayer((layer) => {
            if (layer instanceof L.Marker && layer.options.icon) {
                return;
            }

            if (layer instanceof L.GeoJSON) {
                const geoJsonLayers = layer.getLayers() as (L.Path & { feature?: GeoJSON.Feature })[];
                const feature = geoJsonLayers[0].feature;
                if (!feature) return;
                const id = feature.properties.LandUseBlockID;
                if (this.selectedLandUseBlockIDs.includes(id)) {
                    layer.setStyle(this.highlightStyle);
                } else {
                    layer.setStyle(this.styleDictionary[feature.properties.PriorityLandUseTypeID]);
                }
            }
        });
    }

    private zoomToSelection() {
        if (!this.selectedLandUseBlockIDs.length) return;
        let combinedBounds: L.LatLngBounds | null = null;
        this.layer.eachLayer((layer) => {
            if (layer instanceof L.GeoJSON) {
                const geoJsonLayers = layer.getLayers() as (L.Path & { feature?: GeoJSON.Feature })[];
                const feature = geoJsonLayers[0].feature;
                if (!feature) return;
                if (this.selectedLandUseBlockIDs.includes(feature.properties.LandUseBlockID)) {
                    const bounds = layer.getBounds();
                    combinedBounds = combinedBounds ? combinedBounds.extend(bounds) : bounds;
                }
            }
        });
        if (combinedBounds) {
            this.map.fitBounds(combinedBounds);
        }
    }

    private setupLayer() {
        this.layer = L.geoJSON();
        this.initLayer();
    }
}
