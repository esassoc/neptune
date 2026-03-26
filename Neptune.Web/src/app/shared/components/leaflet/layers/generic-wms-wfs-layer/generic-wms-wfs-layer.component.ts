import { Component, Input, OnChanges, AfterViewInit, Output, EventEmitter } from "@angular/core";
import { environment } from "src/environments/environment";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
import { WfsService } from "src/app/shared/services/wfs.service";
import { GroupByPipe } from "src/app/shared/pipes/group-by.pipe";

@Component({
    selector: "generic-wms-wfs-layer",
    imports: [],
    templateUrl: "./generic-wms-wfs-layer.component.html",
    styleUrls: ["./generic-wms-wfs-layer.component.scss"],
})
export class GenericWmsWfsLayerComponent extends MapLayerBase implements OnChanges, AfterViewInit {
    private mapClickHandlerWired = false;
    private overlayAddedToControl = false;
    @Input() selectedID?: number;
    @Input() layerControl: L.Control.Layers;
    @Input() map: L.Map;
    @Input() interactive: boolean = false;
    @Input() displayOnLoad: boolean = false;
    @Input() wmsLayerName: string;
    @Input() wmsStyle: string;
    @Input() wfsFeatureType: string;
    @Input() identifierProperty: string;
    @Input() overlayLabel: string;
    @Input() selectedStyle: L.PathOptions = {
        color: "#fcfc12",
        weight: 2,
        opacity: 0.65,
        fillOpacity: 0.1,
    };
    @Input() cqlFilter: string = "1=1";
    @Input() addToLayerControl: boolean = true;
    @Output() selected = new EventEmitter<number>();
    public wfsLayer: L.FeatureGroup;
    public layer: L.Layer;

    constructor(
        private wfsService: WfsService,
        private groupByPipe: GroupByPipe
    ) {
        super();
    }

    private createWmsLayerIfNeeded() {
        // Only create WMS if wmsLayerName is provided
        if (!this.wmsLayerName) {
            this.layer = undefined;
            return;
        }
        if (!this.layer) {
            const wmsOptions: L.WMSOptions = {
                layers: this.wmsLayerName,
                transparent: true,
                format: "image/png",
                tiled: true,
                styles: this.wmsStyle,
                cql_filter: this.cqlFilter,
                maxZoom: 22,
            } as any;
            this.layer = L.tileLayer.wms(environment.geoserverMapServiceUrl + "/wms?", wmsOptions);
        }
        // Add to layerControl only once, after both layer and layerControl are available
        if (this.layer && this.layerControl && !this.overlayAddedToControl && this.addToLayerControl) {
            try {
                this.layerControl.addOverlay(this.layer, this.overlayLabel);
                this.overlayAddedToControl = true;
            } catch (err) {
                console.error(`[GenericWmsWfsLayer] Error adding overlay:`, err, this.layer);
            }
        }
    }

    private createWfsHighlightIfNeeded() {
        // Only create WFS highlight if selectedID is set
        if (this.selectedID == null || this.selectedID === undefined) {
            // Remove previous vector overlay if present
            if (this.wfsLayer && this.map) {
                this.map.removeLayer(this.wfsLayer);
                this.wfsLayer = undefined;
            }
            return;
        }
        this.addSelectedVector(this.selectedID);
    }

    ngAfterViewInit(): void {
        // Initialization is now handled in ngOnChanges
    }

    ngOnChanges(changes: any): void {
        this.createWmsLayerIfNeeded();
        // Only add to map if displayOnLoad is true and WMS layer exists
        if (this.layer && this.map && this.displayOnLoad && !this.map.hasLayer(this.layer)) {
            this.layer.addTo(this.map);
        }
        this.createWfsHighlightIfNeeded();
        this.wireMapClickHandler();
        if (changes.selectedID && !changes.selectedID.firstChange) {
            this.createWfsHighlightIfNeeded();
        }
    }

    private wireMapClickHandler() {
        if (this.interactive && this.map && !this.mapClickHandlerWired) {
            this.map.on("click", this.onMapClick.bind(this));
            this.mapClickHandlerWired = true;
        }
    }

    private addSelectedVector(id: number) {
        // Remove previous vector overlay
        if (this.wfsLayer && this.map) {
            this.map.removeLayer(this.wfsLayer);
        }
        this.wfsLayer = L.featureGroup();
        const cql_filter = `${this.identifierProperty} = ${id}`;
        const request$ = this.wfsService.getGeoserverWFSLayerWithCQLFilter(this.wfsFeatureType, cql_filter, this.identifierProperty);
        this.trackLayerRequest$(request$).subscribe((response) => {
            if (response.length == 0) return;
            const featuresGrouped = this.groupByPipe.transform(response, `properties.${this.identifierProperty}`);
            Object.keys(featuresGrouped).forEach((groupId) => {
                const geoJson = L.geoJSON(featuresGrouped[groupId], {
                    style: this.selectedStyle,
                });
                geoJson.on("click", () => {
                    this.selected.emit(Number(groupId));
                });
                geoJson.addTo(this.wfsLayer);
                // Zoom to bounds
                if ("getBounds" in geoJson && typeof geoJson.getBounds === "function" && this.map) {
                    this.map.fitBounds(geoJson.getBounds());
                }
            });
            if (this.map) {
                this.wfsLayer.addTo(this.map);
            }
        });
    }

    private async onMapClick(e: L.LeafletMouseEvent) {
        // Build WMS GetFeatureInfo request
        const wmsUrl = environment.geoserverMapServiceUrl + "/wms";
        const params = {
            service: "WMS",
            version: "1.1.1",
            request: "GetFeatureInfo",
            layers: this.wmsLayerName,
            query_layers: this.wmsLayerName,
            styles: this.wmsStyle,
            bbox: this.map.getBounds().toBBoxString(),
            width: this.map.getSize().x,
            height: this.map.getSize().y,
            srs: "EPSG:4326",
            format: "image/png",
            info_format: "application/json",
            x: Math.round(this.map.layerPointToContainerPoint(e.layerPoint).x),
            y: Math.round(this.map.layerPointToContainerPoint(e.layerPoint).y),
        };
        const urlParams = new URLSearchParams(params as any).toString();
        const url = `${wmsUrl}?${urlParams}`;
        try {
            const response = await fetch(url);
            const data = await response.json();
            if (data.features && data.features.length > 0) {
                const id = data.features[0].properties[this.identifierProperty];
                if (id) {
                    this.selected.emit(id);
                }
            }
        } catch (err) {
            console.warn("WMS GetFeatureInfo failed:", err);
        }
    }
}

/**
 * Usage Patterns for GenericWmsWfsLayerComponent:
 *
 * 1. Show a single feature (WFS only, not in layer control):
 *    - [selectedID] = feature ID
 *    - [addToLayerControl] = false
 *    - [displayOnLoad] = true
 *    - [interactive] = false (optional)
 *    - WMS can be skipped by not providing wmsLayerName or by conditional logic
 *
 * 2. Show all features via WMS (reference only, no interactivity):
 *    - [displayOnLoad] = true
 *    - [interactive] = false
 *    - [addToLayerControl] = true (default)
 *    - Do not set [selectedID]
 *    - WMS displays all features, no selection/highlighting
 *
 * 3. Show all features via WMS, with interactivity and selection highlighting (WFS for highlight):
 *    - [displayOnLoad] = true
 *    - [interactive] = true
 *    - [addToLayerControl] = true
 *    - [selectedID] = currently selected feature
 *    - WMS displays all features, WFS overlays highlight the selected feature
 *
 * The component will only create the layers needed for the current use case based on the provided inputs.
 */
