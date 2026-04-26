import { AfterViewInit, Component, DestroyRef, EventEmitter, Injector, Input, OnDestroy, OnInit, Output, afterNextRender, inject, runInInjectionContext } from "@angular/core";
import { CommonModule } from "@angular/common";
import { Control, LeafletEvent, Map as LeafletMap, MapOptions, DomUtil, ControlPosition } from "leaflet";
import "src/scripts/leaflet.groupedlayercontrol.js";
import * as L from "leaflet";
import { FullScreen } from "leaflet.fullscreen";
import GestureHandling from "leaflet-gesture-handling";
import { LeafletHelperService } from "src/app/shared/services/leaflet-helper.service";
import { BoundingBoxDto } from "src/app/shared/generated/model/models";
import { IconComponent } from "../../icon/icon.component";
import { NominatimService } from "src/app/shared/services/nominatim.service";
import { BehaviorSubject, Observable, combineLatest, debounce, distinctUntilChanged, map, of, shareReplay, switchMap, tap, timer } from "rxjs";
import { NgSelectModule } from "@ng-select/ng-select";
import { FormControl, FormsModule, NG_VALUE_ACCESSOR, ReactiveFormsModule } from "@angular/forms";
import { LegendItem } from "src/app/shared/models/legend-item";
import { Feature, FeatureCollection } from "geojson";
import { DomSanitizer } from "@angular/platform-browser";
import { RegionalSubbasinTraceFromPointComponent } from "../features/regional-subbasin-trace-from-point/regional-subbasin-trace-from-point.component";
import { GroupedLayers } from "src/scripts/leaflet.groupedlayercontrol";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { MapLayerLoadingService } from "src/app/shared/components/leaflet/map-layer-loading.service";

@Component({
    selector: "neptune-map",
    imports: [CommonModule, IconComponent, NgSelectModule, FormsModule, ReactiveFormsModule, RegionalSubbasinTraceFromPointComponent, LoadingDirective],
    templateUrl: "./neptune-map.component.html",
    styleUrls: ["./neptune-map.component.scss"],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            multi: true,
            useExisting: NeptuneMapComponent,
        },
        MapLayerLoadingService,
    ],
})
export class NeptuneMapComponent implements OnInit, AfterViewInit, OnDestroy {
    public mapID: string = "map_" + Date.now().toString(36) + Math.random().toString(36).substring(13);
    public legendID: string = this.mapID + "Legend";
    public map: LeafletMap;
    public tileLayers: { [key: string]: any } = LeafletHelperService.GetDefaultTileLayers();
    public layerControl: GroupedLayers;
    @Input() boundingBox: BoundingBoxDto;
    @Input() mapHeight: string = "500px";
    @Input() selectedTileLayer: string = "Terrain";
    @Input() showLegend: boolean = true;
    @Input() legendPosition: ControlPosition = "topleft";
    @Output() onMapLoad: EventEmitter<NeptuneMapInitEvent> = new EventEmitter();
    @Output() onOverlayToggle: EventEmitter<L.LayersControlEvent> = new EventEmitter();
    @Output() onLegendControlReady: EventEmitter<Control> = new EventEmitter();

    public legendControl: Control;
    public legendItems: LegendItem[] = [];

    public allSearchResults: Feature[] = [];
    public searchString = new FormControl({ value: null, disabled: false });
    public searchResults$: Observable<FeatureCollection>;
    public isSearching: boolean = false;
    private searchCleared: boolean = false;

    public cursorStyle: string = "grab";

    private readonly leafletLoadingLayerCountSubject = new BehaviorSubject<number>(0);
    public readonly isAnyLeafletLayerLoading$ = this.leafletLoadingLayerCountSubject.pipe(
        map((count) => count > 0),
        distinctUntilChanged(),
        shareReplay({ bufferSize: 1, refCount: true })
    );

    public readonly isAnyLayerLoading$ = combineLatest([this.isAnyLeafletLayerLoading$, inject(MapLayerLoadingService).isLoading$]).pipe(
        map(([isLeafletLoading, isApiLoading]) => isLeafletLoading || isApiLoading),
        distinctUntilChanged(),
        shareReplay({ bufferSize: 1, refCount: true })
    );

    private readonly trackedLayerLoadingState = new Map<any, boolean>();
    private readonly trackedLayerListeners = new Map<
        any,
        {
            onLoading: () => void;
            onLoad: () => void;
        }
    >();

    private hasEmittedMapLoad: boolean = false;

    private readonly destroyRef = inject(DestroyRef);
    private readonly injector = inject(Injector);

    constructor(
        public nominatimService: NominatimService,
        public leafletHelperService: LeafletHelperService,
        private sanitizer: DomSanitizer
    ) {}

    ngAfterViewInit(): void {
        const mapOptions: MapOptions = {
            minZoom: 6,
            maxZoom: 20,
            layers: [this.tileLayers[this.selectedTileLayer]],
            gestureHandling: true,
            //            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        } as MapOptions;

        this.map = L.map(this.mapID, mapOptions);

        // Track loading state for any Leaflet layer that emits 'loading'/'load' events (TileLayer/WMS overlays, etc).
        // This lets us show a spinner over the map whenever any visible layer is still fetching.
        this.map.on("layeradd", (e: any) => {
            this.attachLayerLoadingEvents(e?.layer);
        });
        this.map.on("layerremove", (e: any) => {
            this.detachLayerLoadingEvents(e?.layer);
        });

        // Attach to whatever layers were added as part of initial map creation (base tile layer, etc).
        this.map.eachLayer((layer: any) => this.attachLayerLoadingEvents(layer));

        this.map.addControl(
            new FullScreen({
                position: "topleft",
            })
        );
        L.Map.addInitHook("addHandler", "gestureHandling", GestureHandling);

        this.layerControl = new GroupedLayers(this.tileLayers, LeafletHelperService.GetDefaultOverlayTileLayers(), { collapsed: false }).addTo(this.map);

        this.map.on("load", (event: LeafletEvent) => {
            this.scheduleEmitMapLoadOnceAfterNextRender();
        });

        this.map.on("overlayadd", (event: L.LayersControlEvent) => {
            this.legendItems = this.createLegendItems();
            this.onOverlayToggle.emit(event);
        });

        this.map.on("overlayremove", (event: L.LayersControlEvent) => {
            this.legendItems = this.createLegendItems();
            this.onOverlayToggle.emit(event);
        });

        if (this.boundingBox == null) {
            this.boundingBox = LeafletHelperService.defaultBoundingBox;
        }

        this.map.fitBounds(
            [
                [this.boundingBox.Bottom, this.boundingBox.Left],
                [this.boundingBox.Top, this.boundingBox.Right],
            ],
            null
        );

        // Leaflet's 'load' can be delayed or not fire depending on tile timing.
        // Also, in zoneless mode, Output emissions can fail to schedule a render pass.
        // Emit once after the next Angular render so parents gating map children
        // reliably instantiate overlay components without requiring user interaction.
        this.scheduleEmitMapLoadOnceAfterNextRender();

        if (this.showLegend) {
            const legendControl = Control.extend({
                onAdd: (map: LeafletMap) => {
                    const domElement = DomUtil.get(this.mapID + "Legend");
                    if (domElement != null) {
                        L.DomEvent.disableClickPropagation(domElement);
                        return domElement;
                    }
                },
                moveToBottomOfContainer: () => {
                    const container = document.querySelector(
                        `.leaflet-${this.legendPosition.includes("top") ? "top" : "bottom"}.leaflet-${this.legendPosition.includes("left") ? "left" : "right"}`
                    );
                    const legendElement = document.getElementById(this.legendID); // or legendControl.getContainer()
                    if (container && legendElement) {
                        container.appendChild(legendElement); // Moves legend to the bottom
                    }
                },
                onRemove: (map: LeafletMap) => {},
            });
            this.legendControl = new legendControl({
                position: this.legendPosition,
            }).addTo(this.map);
            this.map["showLegend"] = true;
            this.onLegendControlReady.emit(this.legendControl);
        }

        //this.map.fullscreenControl.getContainer().classList.add("leaflet-custom-controls");

        this.searchResults$ = this.searchString.valueChanges.pipe(
            debounce((x) => {
                // debounce search to 500ms when the user is typing in the search
                this.isSearching = true;
                if (this.searchString.value) {
                    return timer(800);
                } else {
                    // don't debounce when the user has cleared the search
                    return timer(800);
                }
            }),
            switchMap((searchString) => {
                this.isSearching = true;
                if (this.searchCleared && !searchString) {
                    return of({ features: [] });
                }
                this.searchCleared = false;
                return this.nominatimService.makeNominatimRequest(searchString);
            }),
            tap((x: FeatureCollection) => {
                this.isSearching = false;
                this.allSearchResults = x?.features ?? [];
            })
        );
    }

    clearSearch() {
        this.searchString.reset();
        this.searchCleared = true;
    }

    public selectCurrent(selectedFeature): void {
        this.map.fitBounds(
            [
                [selectedFeature.bbox[1], selectedFeature.bbox[0]],
                [selectedFeature.bbox[3], selectedFeature.bbox[2]],
            ],
            null
        );
        this.clearSearch();
    }

    private createLegendItems(): LegendItem[] {
        const legendItems = [];

        this.layerControl.getLayers().forEach((obj) => {
            // Check if it's an overlay and added to the map
            if (obj.overlay && this.map.hasLayer(obj.layer)) {
                const legendItem = new LegendItem();
                // if the layer uses a legend image, it may have the title text already in the image, so allow an empty title
                const showEmptyTitle = (obj.layer as any)?.showEmptyTitle ?? false;
                legendItem.Title = showEmptyTitle ? "" : obj.group && obj.group.name ? obj.group.name : obj.name;
                if (LeafletHelperService.hasLegendHtml(obj.layer)) {
                    const legendHtml = obj.layer.legendHtml;
                    legendItem.LegendHtml = this.sanitizer.bypassSecurityTrustHtml(legendHtml);
                } else if (LeafletHelperService.hasUrl(obj.layer) && LeafletHelperService.hasWMSParams(obj.layer)) {
                    legendItem.WmsUrl = LeafletHelperService.getLayerUrl(obj.layer);
                    const wmsParams = LeafletHelperService.getWMSParams(obj.layer) as any;
                    legendItem.WmsLayerName = wmsParams ? wmsParams.layers : undefined;
                    legendItem.WmsLayerStyle = wmsParams ? wmsParams.styles : undefined;
                }

                if ((showEmptyTitle || legendItem.Title) && (legendItem.LegendHtml || legendItem.WmsUrl) && !legendItems.some((item) => item.Title === legendItem.Title)) {
                    legendItems.push(legendItem);
                }
            }
        });
        return legendItems;
    }

    legendToggle(): void {
        if (this.legendControl.getContainer().classList.contains("leaflet-control-layers-expanded")) {
            this.legendControl.getContainer().className = this.legendControl.getContainer().className.replace(" leaflet-control-layers-expanded", "");
        } else {
            this.legendControl.getContainer().classList.add("leaflet-control-layers-expanded");
        }
    }

    onCursorStyleChange(updatedCursorStyle: string) {
        this.cursorStyle = updatedCursorStyle;
    }

    onLegendItemsChange(updatedLegendItems: LegendItem[]) {
        this.legendItems = updatedLegendItems;
    }

    ngOnDestroy(): void {
        console.warn("destroying map");

        for (const [layer, handlers] of this.trackedLayerListeners) {
            try {
                if (layer && typeof layer.off === "function") {
                    layer.off("loading", handlers.onLoading);
                    layer.off("load", handlers.onLoad);
                    layer.off("tileerror", handlers.onLoad);
                    layer.off("error", handlers.onLoad);
                }
            } catch (e) {}
        }
        this.trackedLayerListeners.clear();
        this.trackedLayerLoadingState.clear();
        this.leafletLoadingLayerCountSubject.next(0);

        if (this.map) {
            this.map.off();
            this.map.remove();
            this.map = null;
        }
    }

    ngOnInit(): void {}

    private attachLayerLoadingEvents(layer: any): void {
        if (!layer || typeof layer.on !== "function" || typeof layer.off !== "function") {
            return;
        }

        // Avoid duplicate handlers on the same layer instance.
        if (this.trackedLayerListeners.has(layer)) {
            return;
        }

        const onLoading = () => this.markLayerLoading(layer, true);

        // We only want to show the Leaflet spinner for the initial load of a layer.
        // Tile layers reload on zoom/pan; detaching after the first load avoids spinner flicker during navigation.
        const onLoad = () => this.detachLayerLoadingEvents(layer);

        // Common Leaflet loading events:
        // - Tile layers: 'loading' then 'load' (or 'tileerror')
        // - Some plugin layers emit 'error'
        layer.on("loading", onLoading);
        layer.on("load", onLoad);
        layer.on("tileerror", onLoad);
        layer.on("error", onLoad);

        this.trackedLayerListeners.set(layer, { onLoading, onLoad });
        this.trackedLayerLoadingState.set(layer, false);
    }

    private detachLayerLoadingEvents(layer: any): void {
        if (!layer) {
            return;
        }

        // If this layer was contributing to the spinner count, remove it.
        this.markLayerLoading(layer, false);

        const handlers = this.trackedLayerListeners.get(layer);
        if (!handlers) {
            return;
        }

        try {
            if (typeof layer.off === "function") {
                layer.off("loading", handlers.onLoading);
                layer.off("load", handlers.onLoad);
                layer.off("tileerror", handlers.onLoad);
                layer.off("error", handlers.onLoad);
            }
        } catch (e) {}

        this.trackedLayerListeners.delete(layer);
        this.trackedLayerLoadingState.delete(layer);
    }

    private markLayerLoading(layer: any, isLoading: boolean): void {
        if (!layer) {
            return;
        }

        const wasLoading = this.trackedLayerLoadingState.get(layer) ?? false;
        if (wasLoading === isLoading) {
            return;
        }

        this.trackedLayerLoadingState.set(layer, isLoading);

        const delta = isLoading ? 1 : -1;
        const nextCount = Math.max(0, (this.leafletLoadingLayerCountSubject.value ?? 0) + delta);
        this.leafletLoadingLayerCountSubject.next(nextCount);
    }

    private scheduleEmitMapLoadOnceAfterNextRender(): void {
        if (this.hasEmittedMapLoad) {
            return;
        }

        // afterNextRender must run within an injection context.
        // Use DestroyRef to avoid emitting after the component is destroyed.
        let destroyed = false;
        this.destroyRef.onDestroy(() => {
            destroyed = true;
        });

        runInInjectionContext(this.injector, () => {
            afterNextRender(() => {
                if (!destroyed) {
                    this.emitMapLoadOnce();
                }
            });
        });
    }

    private emitMapLoadOnce(): void {
        if (this.hasEmittedMapLoad) {
            return;
        }

        if (!this.map || !this.layerControl) {
            return;
        }

        // When <neptune-map> mounts inside a parent `@if (...$ | async)` gate (e.g. the
        // OVTA Review-and-Finalize and Record-Observations pages, which wait on observation
        // data before rendering the map), the container's layout often hasn't settled when
        // L.map() ran in ngAfterViewInit — Leaflet measured 0x0, fitBounds picked the wrong
        // zoom, and the WMS base tile layer never fired `load` because no tiles were
        // actually fetched. The spinner counter then stayed at 1 forever. invalidateSize
        // here (post-Angular-render, so layout is settled) makes Leaflet remeasure and
        // re-fetch tiles for the correct viewport, which lets the `load` event fire and
        // the spinner clear without requiring a stray user click.
        this.map.invalidateSize(false);
        if (this.boundingBox) {
            this.map.fitBounds(
                [
                    [this.boundingBox.Bottom, this.boundingBox.Left],
                    [this.boundingBox.Top, this.boundingBox.Right],
                ],
                null
            );
        }

        this.hasEmittedMapLoad = true;
        this.onMapLoad.emit(new NeptuneMapInitEvent(this.map, this.layerControl));
    }
}

export class NeptuneMapInitEvent {
    public map: LeafletMap;
    public layerControl: any;
    constructor(map: LeafletMap, layerControl: any) {
        this.map = map;
        this.layerControl = layerControl;
    }
}
