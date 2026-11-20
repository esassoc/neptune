import { Injectable } from "@angular/core";
import { BoundingBoxDto } from "../generated/model/bounding-box-dto";
import * as L from "leaflet";
import { Layer } from "leaflet";

@Injectable({
    providedIn: "root",
})
export class LeafletHelperService {
    constructor() {}

    public static readonly defaultBoundingBox = new BoundingBoxDto({
        Left: -117.5193786621095,
        Top: 33.844679670212059,
        Right: -118.09341430664051,
        Bottom: 33.46459577300336,
    });

    public readonly blueIcon = L.icon({
        iconUrl: "/assets/main/map-icons/blue-pin.png",
        shadowUrl: "/assets/main/map-icons/shadow-skew.png",
        iconSize: [22, 35],
        iconAnchor: [12, 34],
        shadowAnchor: [4, 26],
        popupAnchor: [1, -34],
        tooltipAnchor: [16, -28],
        shadowSize: [28, 28],
    });

    public readonly blueIconLarge = L.icon({
        iconUrl: "/assets/main/map-icons/blue-pin.png",
        shadowUrl: "/assets/main/map-icons/shadow-skew.png",
        iconSize: [28, 45],
        iconAnchor: [15, 45],
        shadowAnchor: [5, 34],
        popupAnchor: [1, -45],
        tooltipAnchor: [16, -28],
        shadowSize: [35, 35],
    });

    public readonly yellowIcon = L.icon({
        iconUrl: "/assets/main/map-icons/yellow-pin.png",
        shadowUrl: "/assets/main/map-icons/shadow-skew.png",
        iconSize: [22, 35],
        iconAnchor: [12, 34],
        shadowAnchor: [4, 26],
        popupAnchor: [1, -34],
        tooltipAnchor: [16, -28],
        shadowSize: [28, 28],
    });

    public readonly yellowIconLarge = L.icon({
        iconUrl: "/assets/main/map-icons/yellow-pin.png",
        shadowUrl: "/assets/main/map-icons/shadow-skew.png",
        iconSize: [28, 45],
        iconAnchor: [15, 45],
        shadowAnchor: [5, 34],
        popupAnchor: [1, -45],
        tooltipAnchor: [16, -28],
        shadowSize: [35, 35],
    });

    public markerColors: string[] = ["#7F3C8D", "#11A579", "#3969AC", "#F2B701", "#E73F74", "#80BA5A", "#E68310", "#008695", "#CF1C90", "#f97b72", "#4b4b8f", "#A5AA99"];

    public createDivIcon(color: string, dash: boolean = false) {
        return L.divIcon({
            className: "riparis-div-icon",
            html: `<svg width="100%" viewbox="0 0 30 42">
              <path fill="${color}" stroke="#fff" stroke-width="1.5" ${dash ? 'stroke-dasharray="4"' : ""}
                    d="M15 3
                      Q16.5 6.8 25 18
                      A12.8 12.8 0 1 1 5 18
                      Q13.5 6.8 15 3z" />
            </svg>`,
            iconSize: new L.Point(30, 42),
            iconAnchor: [15, 42],
        });
    }

    public fitMapToDefaultBoundingBox(map: L.Map) {
        const defaultBoundingBox = LeafletHelperService.defaultBoundingBox;
        this.fitMapToBoundingBox(map, defaultBoundingBox);
    }

    public fitMapToBoundingBox(map: L.Map, boundingBox: BoundingBoxDto) {
        map.fitBounds([
            [boundingBox.Bottom, boundingBox.Left],
            [boundingBox.Top, boundingBox.Right],
        ]);
    }

    public clusterIconCreateFunction(cluster) {
        const childCount = cluster.getChildCount();

        // currently we aren't using these small medium large sizes, but keeping because it doesn't hurt
        let c = " riparis-cluster-";
        if (childCount < 10) {
            c += "small";
        } else if (childCount < 100) {
            c += "medium";
        } else {
            c += "large";
        }

        return new L.DivIcon({
            html: "<div><span>" + childCount + "</span></div>",
            className: "marker-cluster riparis-cluster" + c,
            iconSize: new L.Point(40, 40),
        });
    }

    public zoomToMarker(map: L.Map, marker: L.Marker, zoomLevel?: number) {
        map.setView(marker.getLatLng(), zoomLevel ?? 18);
    }

    public moveLegendToBottomOfContainer(legendControl: L.Control) {
        if (legendControl && typeof legendControl["moveToBottomOfContainer"] === "function") {
            legendControl["moveToBottomOfContainer"]();
        }
    }

    public setupGeomanControls(map: L.Map, enableDrawPolygon: boolean = false, enableEdit: boolean = false, enableDelete: boolean = false, buttonNoun: string) {
        map.pm.addControls({
            position: "topleft",
            drawMarker: false,
            drawText: false,
            drawCircleMarker: false,
            drawPolyline: false,
            drawRectangle: false,
            drawPolygon: enableDrawPolygon,
            drawCircle: false,
            editMode: enableEdit,
            removalMode: enableDelete,
            cutPolygon: false,
            dragMode: false,
            rotateMode: false,
            snappingOption: true,
            showCancelButton: true,
        });
        map.pm.setGlobalOptions({ allowSelfIntersection: false });
        map.pm.setLang(
            "en",
            {
                buttonTitles: {
                    drawPolyButton: `Add ${buttonNoun}`,
                    editButton: `Edit ${buttonNoun}`,
                    deleteButton: `Delete ${buttonNoun}`,
                },
            },
            "en"
        );
    }

    public static GetDefaultTileLayers(): { [key: string]: any } {
        return {
            Aerial: L.tileLayer("https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}", {
                attribution: "Aerial",
                maxZoom: 22,
                maxNativeZoom: 18,
            }),
            Street: L.tileLayer("https://services.arcgisonline.com/ArcGIS/rest/services/World_Street_Map/MapServer/tile/{z}/{y}/{x}", {
                attribution: "Street",
                maxZoom: 22,
                maxNativeZoom: 18,
            }),
            Terrain: L.tileLayer("https://server.arcgisonline.com/ArcGIS/rest/services/World_Topo_Map/MapServer/tile/{z}/{y}/{x}", {
                attribution: "Terrain",
                maxZoom: 22,
                maxNativeZoom: 18,
            }),
            Hillshade: L.tileLayer("https://wtb.maptiles.arcgis.com/arcgis/rest/services/World_Topo_Base/MapServer/tile/{z}/{y}/{x}'", {
                attribution: "Hillshade",
                maxZoom: 22,
                maxNativeZoom: 18,
            }),
        };
    }

    public static GetDefaultOverlayTileLayers(): { [key: string]: any } {
        return {
            "": {
                "Street Labels": L.tileLayer("https://services.arcgisonline.com/ArcGIS/rest/services/Reference/World_Transportation/MapServer/tile/{z}/{y}/{x}", {
                    attribution: "Street Labels",
                    maxZoom: 22,
                    maxNativeZoom: 18,
                }),
            },
        };
    }

    /**
     * Type guard for layers with a _url property.
     */
    public static hasUrl(layer: Layer): layer is Layer & { _url: string } {
        return typeof (layer as any)._url === "string";
    }

    /**
     * Type-safe getter for _url property.
     */
    public static getLayerUrl(layer: Layer): string | undefined {
        return LeafletHelperService.hasUrl(layer) ? layer._url : undefined;
    }

    /**
     * Type guard for layers with a legendHtml property.
     */
    public static hasLegendHtml(layer: Layer): layer is Layer & { legendHtml: string } {
        return typeof (layer as any).legendHtml === "string";
    }

    /**
     * Type-safe getter for legendHtml property.
     */
    public static getLegendHtml(layer: Layer): string | undefined {
        return LeafletHelperService.hasLegendHtml(layer) ? layer.legendHtml : undefined;
    }
    /**
     * Type guard for layers with a wmsParams property.
     */
    public static hasWMSParams(layer: Layer): layer is Layer & { wmsParams: object } {
        return typeof (layer as any).wmsParams === "object" && (layer as any).wmsParams !== null;
    }

    /**
     * Type-safe getter for wmsParams property.
     */
    public static getWMSParams(layer: Layer): object | undefined {
        return LeafletHelperService.hasWMSParams(layer) ? (layer as any).wmsParams : undefined;
    }
}
