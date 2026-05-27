import { Component, OnInit, QueryList, signal, ViewChildren } from "@angular/core";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { BehaviorSubject, forkJoin, map, Observable, shareReplay, tap } from "rxjs";
import "leaflet.markercluster";
import "@geoman-io/leaflet-geoman-free";
import * as L from "leaflet";

import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { DelineationDto } from "src/app/shared/generated/model/delineation-dto";
import { TreatmentBMPDelineationMapDto } from "src/app/shared/generated/model/treatment-bmp-delineation-map-dto";
import { DelineationTypeEnum } from "src/app/shared/generated/enum/delineation-type-enum";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

import { DelineationService } from "src/app/shared/generated/api/delineation.service";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { RegionalSubbasinService } from "src/app/shared/generated/api/regional-subbasin.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";

import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { LeafletHelperService } from "src/app/shared/services/leaflet-helper.service";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";

import { escapeHtml } from "src/app/shared/helpers/html-escape";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { StormwaterNetworkLayerComponent } from "src/app/shared/components/leaflet/layers/stormwater-network-layer/stormwater-network-layer.component";
import { ParcelsLayerComponent } from "src/app/shared/components/leaflet/layers/parcels-layer/parcels-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";

type EditMode = "idle" | "editingDistributed" | "editingCentralized" | "editingLocation";

@Component({
    selector: "delineation-map",
    templateUrl: "./delineation-map.component.html",
    styleUrls: ["./delineation-map.component.scss"],
    imports: [
        AsyncPipe,
        FormsModule,
        ReactiveFormsModule,
        RouterLink,
        NeptuneMapComponent,
        RegionalSubbasinsLayerComponent,
        DelineationsLayerComponent,
        JurisdictionsLayerComponent,
        WqmpsLayerComponent,
        StormwaterNetworkLayerComponent,
        ParcelsLayerComponent,
        PageHeaderComponent,
        AlertDisplayComponent,
        IconComponent,
        FormFieldComponent,
    ],
})
export class DelineationMapComponent implements OnInit {
    public OverlayMode = OverlayMode;
    public DelineationTypeEnum = DelineationTypeEnum;
    public FormFieldType = FormFieldType;
    public customRichTextTypeID = NeptunePageTypeEnum.DelineationMap;

    public boundingBox$: Observable<BoundingBoxDto>;
    public initData$: Observable<boolean>;

    public mapHeight = window.innerHeight - window.innerHeight * 0.2 + "px";
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public mapIsReady = false;

    @ViewChildren(DelineationsLayerComponent) private delineationLayers!: QueryList<DelineationsLayerComponent>;

    public bmps: TreatmentBMPDelineationMapDto[] = [];
    public selectedBMP = signal<TreatmentBMPDelineationMapDto | null>(null);
    public selectedDelineation = signal<DelineationDto | null>(null);
    public editMode = signal<EditMode>("idle");
    // NPT-981 r3 (KE): "Edit Delineation" on an existing delineation reopens the flow-type
    // chooser (Distributed / Centralized) so the user can re-delineate OR switch the type —
    // mirroring the legacy "Delineate Drainage Area" control (radio select + Delineate button).
    public choosingFlowType = signal<boolean>(false);
    // Bound to the chooser radios via ngModel. Null until the user picks, which keeps the
    // Delineate button disabled (matches the legacy disabled-until-selected behavior).
    public selectedFlowType: DelineationTypeEnum | null = null;

    private bmpsClusterLayer: any = null;
    private bmpMarkerByID: Map<number, L.Marker> = new Map();
    private selectedDelineationLayer: L.GeoJSON | null = null;
    private locationPreviewMarker: L.Marker | null = null;
    private pendingLocation: { lat: number; lng: number } | null = null;

    public pendingDelineationType: DelineationTypeEnum = DelineationTypeEnum.Distributed;

    public verifiedControl = new FormControl<boolean>(false, { nonNullable: true });

    private readonly savingSubject = new BehaviorSubject<boolean>(false);
    public readonly saving$ = this.savingSubject.asObservable();

    private readonly delineationDefaultStyle: L.PathOptions = { color: "#51F6F8", fillOpacity: 0.2, opacity: 1 };
    private readonly delineationSelectedStyle: L.PathOptions = { color: "yellow", fillOpacity: 0.2, opacity: 1 };
    private readonly delineationDraftStyle: L.PathOptions = { color: "#4782ff", fillOpacity: 0.4, opacity: 1 };

    constructor(
        private route: ActivatedRoute,
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private leafletHelperService: LeafletHelperService,
        private treatmentBMPService: TreatmentBMPService,
        private delineationService: DelineationService,
        private regionalSubbasinService: RegionalSubbasinService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService
    ) {}

    public ngOnInit(): void {
        this.boundingBox$ = this.stormwaterJurisdictionService.getBoundingBoxStormwaterJurisdiction();

        this.verifiedControl.valueChanges.subscribe((isVerified) => {
            const delineation = this.selectedDelineation();
            if (!delineation || delineation.IsVerified === isVerified) {
                return;
            }
            this.applyVerifiedChange(isVerified);
        });
    }

    private applyVerifiedChange(isVerified: boolean): void {
        const delineation = this.selectedDelineation();
        if (!delineation) {
            return;
        }
        this.savingSubject.next(true);
        this.delineationService.setVerifiedDelineation(delineation.DelineationID, { IsVerified: isVerified }).subscribe({
            next: () => {
                this.savingSubject.next(false);
                this.alertService.pushAlert(new Alert(`Delineation marked ${isVerified ? "Verified" : "Provisional"}.`, AlertContext.Success, true));
                this.reloadSelectedDelineation();
                // Verified/Provisional are two separate WMS layers with status-based cql_filters;
                // the row moves between them, so refresh both.
                this.refreshDelineationLayers();
            },
            error: () => {
                this.savingSubject.next(false);
                // revert UI to server state
                this.verifiedControl.setValue(delineation.IsVerified, { emitEvent: false });
            },
        });
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;

        this.attachGeomanHandlers();
        this.attachMapClickForLocation();

        this.initData$ = this.treatmentBMPService.listForDelineationMapTreatmentBMP().pipe(
            tap((bmps) => {
                this.bmps = bmps;
                this.renderBMPMarkers();
                this.maybeSelectFromQueryParam();
            }),
            map(() => true),
            shareReplay(1)
        );
    }

    private maybeSelectFromQueryParam(): void {
        const treatmentBMPID = Number(this.route.snapshot.queryParamMap.get("treatmentBMPID"));
        if (!treatmentBMPID) {
            return;
        }
        const bmp = this.bmps.find((x) => x.TreatmentBMPID === treatmentBMPID);
        if (bmp) {
            this.selectBMP(bmp);
        }
    }

    private renderBMPMarkers(): void {
        if (this.bmpsClusterLayer) {
            this.map.removeLayer(this.bmpsClusterLayer);
            this.layerControl.removeLayer(this.bmpsClusterLayer);
        }
        this.bmpMarkerByID.clear();
        this.bmpsClusterLayer = L.markerClusterGroup({
            // NPT-981 r3 (KE id-7): suppress the blue convex-hull coverage polygon that markercluster
            // draws on hover/click — KE found it confusing when clicking a clustered pin.
            showCoverageOnHover: false,
            iconCreateFunction: (cluster: any) =>
                L.divIcon({
                    html: `<div><span>${cluster.getChildCount()}</span></div>`,
                    className: "treatment-bmp-cluster",
                    iconSize: L.point(40, 40),
                }),
        });
        for (const bmp of this.bmps) {
            if (!bmp.Latitude || !bmp.Longitude) {
                continue;
            }
            // NPT-981 r3: BMP pins are click-to-select only. Relocating a BMP is done via a
            // separate preview marker in startEditLocation (KE id-26), so the cluster marker
            // itself is never dragged.
            const marker = L.marker([bmp.Latitude, bmp.Longitude], { icon: MarkerHelper.inventoriedTreatmentBMPMarker });
            marker.on("click", () => {
                if (this.editMode() !== "idle") {
                    return;
                }
                this.selectBMP(bmp);
            });
            this.bmpMarkerByID.set(bmp.TreatmentBMPID, marker);
            this.bmpsClusterLayer.addLayer(marker);
        }
        this.bmpsClusterLayer["legendHtml"] = "<img src='./assets/main/map-icons/marker-icon-orange.png' style='height:17px'>";
        this.layerControl.addOverlay(this.bmpsClusterLayer, "Treatment BMPs");
        this.map.addLayer(this.bmpsClusterLayer);
    }

    /**
     * NPT-981 r2: handler for the delineations-layer (selected) event. The WMS GetFeatureInfo
     * response from Geoserver returns features in draw order, so features[0] is the topmost —
     * generic-wms-wfs-layer already emits that one ID. Match it against the local BMP cache
     * (every TreatmentBMPDelineationMapDto carries its DelineationID); if the BMP is in the
     * cache, select it via the existing selectBMP flow so the side panel populates. BMPs
     * outside the user's jurisdiction won't be in the cache — clicking such a delineation is
     * a no-op (the verified-delineation layer is visible to everyone but write actions are
     * already permission-gated on the API).
     */
    public onDelineationSelected(delineationID: number): void {
        // NPT-981 r3 (KE id-19): ignore delineation-polygon clicks unless we're idle. While
        // drawing/editing/tracing/moving, a stray polygon click must not hijack the selection
        // (KE got yanked into a centralized delineation mid-draw and stuck in a broken state).
        if (this.editMode() !== "idle") {
            return;
        }
        const bmp = this.bmps.find((b) => b.DelineationID === delineationID);
        if (bmp) {
            // Polygon click → populate the panel but do NOT auto-zoom (KE id-19b / id-23: the
            // map flew away erratically). Only pin clicks zoom.
            this.selectBMP(bmp, false);
        }
    }

    /** @param zoomToSelection fly the map to the selection. True for pin clicks + deep-link;
     *  false for delineation-polygon clicks (KE id-19b). */
    public selectBMP(bmp: TreatmentBMPDelineationMapDto, zoomToSelection: boolean = true): void {
        this.cancelEdit();
        this.choosingFlowType.set(false);
        this.selectedFlowType = null;
        this.selectedBMP.set(bmp);
        this.selectedDelineation.set(null);
        this.clearSelectedDelineationLayer();
        this.refreshMarkerHighlight(zoomToSelection);

        const requestedID = bmp.TreatmentBMPID;
        this.delineationService.getForTreatmentBMPDelineation(requestedID).subscribe((dto) => {
            if (this.selectedBMP()?.TreatmentBMPID !== requestedID) {
                return;
            }
            this.selectedDelineation.set(dto);
            this.applyDelineationToSelectedBMP(dto);
            this.renderSelectedDelineation(zoomToSelection);
        });
    }

    private refreshMarkerHighlight(zoomToSelection: boolean = true): void {
        const selected = this.selectedBMP();
        for (const [id, marker] of this.bmpMarkerByID) {
            const isSelected = id === selected?.TreatmentBMPID;
            marker.setIcon(isSelected ? MarkerHelper.selectedMarker : MarkerHelper.inventoriedTreatmentBMPMarker);
            marker.setZIndexOffset(isSelected ? 10000 : 1000);
        }
        if (!selected || !zoomToSelection) {
            return;
        }
        const selectedMarker = this.bmpMarkerByID.get(selected.TreatmentBMPID);
        if (selectedMarker && this.bmpsClusterLayer?.zoomToShowLayer) {
            this.bmpsClusterLayer.zoomToShowLayer(selectedMarker, () => {
                this.map.flyTo([selected.Latitude, selected.Longitude], Math.max(this.map.getZoom(), 18), { duration: 0.6 });
            });
        } else {
            this.map.flyTo([selected.Latitude, selected.Longitude], 18, { duration: 0.6 });
        }
    }

    private renderSelectedDelineation(zoomToSelection: boolean = true): void {
        this.clearSelectedDelineationLayer();
        const delineation = this.selectedDelineation();
        if (!delineation?.Geometry) {
            return;
        }
        const geom = JSON.parse(delineation.Geometry);
        // NPT-981 r3 (KE): non-interactive so it doesn't swallow map clicks. This highlight
        // overlay is purely decorative (re-selection happens via the WMS GetFeatureInfo handler),
        // and an interactive vector layer consumes the click so the map `click` event never fires
        // — which broke click-to-place during Edit BMP Location whenever the BMP sat inside its
        // own delineation polygon.
        this.selectedDelineationLayer = L.geoJSON(geom, { style: this.delineationSelectedStyle, interactive: false }).addTo(this.map);
        if (!zoomToSelection) {
            return;
        }
        try {
            this.map.flyToBounds(this.selectedDelineationLayer.getBounds(), { padding: [50, 50], duration: 0.6 });
        } catch {
            // empty geometry
        }
    }

    private clearSelectedDelineationLayer(): void {
        if (this.selectedDelineationLayer) {
            this.map.removeLayer(this.selectedDelineationLayer);
            this.selectedDelineationLayer = null;
        }
    }

    /** NPT-981 r3: force the Verified + Provisional WMS layers to re-fetch their tiles after a
     *  delineation mutation. The selected-delineation highlight overlay is just decoration on top
     *  of the GeoServer tiles; without busting those tiles the saved/edited/deleted geometry keeps
     *  showing its pre-mutation shape (KE id-21 ghost-after-edit, id-23 deleted-polygon-lingers). */
    private refreshDelineationLayers(): void {
        this.delineationLayers?.forEach((layer) => layer.redraw());
    }

    /** Open the flow-type chooser for an existing delineation (the "Edit Delineation" action),
     *  pre-selecting the delineation's current type so re-delineating the same type is one click
     *  and switching is just picking the other radio. */
    public openFlowTypeChooser(): void {
        this.selectedFlowType = this.selectedDelineation()?.DelineationTypeID ?? null;
        this.choosingFlowType.set(true);
    }

    public closeFlowTypeChooser(): void {
        this.choosingFlowType.set(false);
    }

    /** Hint text under the radios, mirroring the legacy "Choose a delineation option" copy. */
    public flowTypeHint(): string {
        if (this.selectedFlowType === DelineationTypeEnum.Distributed) {
            return "Draw the drainage area on the map.";
        }
        if (this.selectedFlowType === DelineationTypeEnum.Centralized) {
            return "The delineation will be computed by tracing Regional Subbasins upstream.";
        }
        return "";
    }

    /** The legacy "Delineate" button: enter draw (Distributed) or trace (Centralized) mode for
     *  the selected flow type. The selected type becomes the saved DelineationTypeID, so picking
     *  the opposite radio switches the delineation's type on save. */
    public beginDelineate(): void {
        if (this.selectedFlowType === DelineationTypeEnum.Distributed) {
            this.startEditDistributed();
        } else if (this.selectedFlowType === DelineationTypeEnum.Centralized) {
            this.startEditCentralized();
        }
    }

    public deleteDelineation(): void {
        const bmp = this.selectedBMP();
        const delineation = this.selectedDelineation();
        if (!bmp || !delineation) {
            return;
        }
        this.confirmService
            .confirm({
                title: "Delete Delineation",
                message: `<p>You are about to delete the delineation for <strong>${escapeHtml(bmp.TreatmentBMPName)}</strong>.</p><p>This action cannot be undone. Are you sure you wish to proceed?</p>`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) {
                    return;
                }
                this.savingSubject.next(true);
                this.delineationService.deleteForTreatmentBMPDelineation(bmp.TreatmentBMPID).subscribe({
                    next: () => {
                        this.savingSubject.next(false);
                        this.alertService.pushAlert(new Alert("Delineation deleted.", AlertContext.Success, true));
                        this.selectedDelineation.set(null);
                        this.clearSelectedDelineationLayer();
                        this.applyDelineationToSelectedBMP(null);
                        this.refreshDelineationLayers();
                    },
                    error: () => this.savingSubject.next(false),
                });
            });
    }

    public startEditDistributed(): void {
        if (!this.selectedBMP()) {
            return;
        }
        this.clearSelectedDelineationLayer();
        this.editMode.set("editingDistributed");
        this.pendingDelineationType = DelineationTypeEnum.Distributed;

        const delineation = this.selectedDelineation();
        const hasExisting = delineation?.Geometry != null;
        this.leafletHelperService.setupGeomanControls(this.map, !hasExisting, hasExisting, false, "Delineation");

        if (hasExisting) {
            const geom = JSON.parse(delineation!.Geometry);
            this.selectedDelineationLayer = L.geoJSON(geom, { style: this.delineationDraftStyle }).addTo(this.map);
            this.selectedDelineationLayer.eachLayer((l) => (l as any).pm?.enable());
        } else {
            // NPT-981 r3 (KE id-18): drop straight into draw mode instead of making the user
            // hunt for the Geoman "Add Delineation" toolbar button. setupGeomanControls only
            // *adds* the control; enableDraw actually arms the cursor.
            this.map.pm.enableDraw("Polygon", { snappable: true, snapDistance: 20 });
        }
    }

    public startEditCentralized(): void {
        const bmp = this.selectedBMP();
        if (!bmp) {
            return;
        }
        this.clearSelectedDelineationLayer();
        this.editMode.set("editingCentralized");
        this.pendingDelineationType = DelineationTypeEnum.Centralized;

        this.regionalSubbasinService.getUpstreamDelineationForBMPRegionalSubbasin(bmp.TreatmentBMPID).subscribe({
            next: (feature: any) => {
                if (!feature?.geometry) {
                    this.alertService.pushAlert(new Alert("No upstream catchment available for this BMP.", AlertContext.Warning, true));
                    this.cancelEdit();
                    return;
                }
                this.selectedDelineationLayer = L.geoJSON(feature.geometry, { style: this.delineationDraftStyle }).addTo(this.map);
                // NPT-981 r3 (KE id-22): centralized delineations are traced from the RSB network,
                // not hand-edited. Don't add Geoman controls or enable vertex editing — the traced
                // geometry is display-only. The user accepts it with Save or asks for changes via
                // Request Revision (both rendered in the panel during trace mode).
                try {
                    this.map.flyToBounds(this.selectedDelineationLayer.getBounds(), { padding: [50, 50] });
                } catch {
                    // empty geometry
                }
            },
            error: () => {
                this.alertService.pushAlert(new Alert("Failed to load upstream catchment.", AlertContext.Danger, true));
                this.cancelEdit();
            },
        });
    }

    public startEditLocation(): void {
        const bmp = this.selectedBMP();
        if (!bmp) {
            return;
        }
        this.cancelEdit();
        this.editMode.set("editingLocation");

        // NPT-981 r3 (KE id-27): crosshair via a container class (not inline style) so the rule
        // can also cover the .leaflet-interactive SVG overlay — otherwise the cursor reverts to
        // default when hovering on top of the selected delineation polygon.
        this.setLocationEditCursor(true);

        // NPT-981 r3 (KE id-26): drop a distinct, draggable PREVIEW marker at the current
        // location and leave the real BMP pin in place until Save. Moving the real pin on every
        // map click read as an autosave. The preview lives directly on the map (not the cluster)
        // so cluster re-renders can't orphan it; clearLocationPreview removes it on save + cancel.
        this.locationPreviewMarker = L.marker([bmp.Latitude, bmp.Longitude], {
            icon: MarkerHelper.selectedMarker,
            draggable: true,
            zIndexOffset: 20000,
        }).addTo(this.map);
        this.locationPreviewMarker.on("drag", this.onPreviewMarkerMove);
        this.locationPreviewMarker.on("dragend", this.onPreviewMarkerMove);

        // Seed pending location to the current position so Save without further interaction
        // is a no-op rather than an error.
        this.pendingLocation = { lat: bmp.Latitude, lng: bmp.Longitude };
    }

    /** Property-assigned arrow keeps a stable Function identity so off() unbinds cleanly.
     *  Handles both `drag` (live latlng) and `dragend` (target.getLatLng()). */
    private onPreviewMarkerMove = (event: any): void => {
        this.handleLocationChange(event.latlng ?? event.target.getLatLng());
    };

    private attachMapClickForLocation(): void {
        this.map.on("click", (event: L.LeafletMouseEvent) => {
            if (this.editMode() !== "editingLocation") {
                return;
            }
            this.handleLocationChange(event.latlng);
        });
    }

    /** NPT-981 r3: stage the candidate location and move only the PREVIEW marker. The real BMP
     *  pin stays put until the user clicks Save (saveEdit persists `pendingLocation`). */
    private handleLocationChange(latLng: L.LatLng): void {
        this.pendingLocation = { lat: latLng.lat, lng: latLng.lng };
        this.locationPreviewMarker?.setLatLng(latLng);
    }

    private setLocationEditCursor(on: boolean): void {
        this.map?.getContainer().classList.toggle("location-edit-cursor", on);
    }

    private attachGeomanHandlers(): void {
        this.map.on("pm:create", (event: any) => {
            if (this.editMode() !== "editingDistributed") {
                return;
            }
            this.clearSelectedDelineationLayer();
            this.selectedDelineationLayer = L.geoJSON(event.layer.toGeoJSON(), { style: this.delineationDraftStyle }).addTo(this.map);
            event.layer.remove();
            this.selectedDelineationLayer.eachLayer((l) => (l as any).pm?.enable());
        });
    }

    public saveEdit(): void {
        const bmp = this.selectedBMP();
        if (!bmp) {
            return;
        }
        const mode = this.editMode();

        if (mode === "editingLocation") {
            if (!this.pendingLocation) {
                this.alertService.pushAlert(new Alert("Click on the map to choose a new location before saving.", AlertContext.Warning, true));
                return;
            }
            const newLat = this.pendingLocation.lat;
            const newLng = this.pendingLocation.lng;
            this.savingSubject.next(true);
            this.treatmentBMPService.updateLocationTreatmentBMP(bmp.TreatmentBMPID, { Latitude: newLat, Longitude: newLng }).subscribe({
                next: () => {
                    this.savingSubject.next(false);
                    this.alertService.pushAlert(new Alert("Treatment BMP location updated.", AlertContext.Success, true));
                    // The `bmp` local is a spread copy from the selectedBMP signal (set by
                    // applyDelineationToSelectedBMP after select). Mutating it doesn't
                    // propagate to this.bmps[]; we have to find and mutate the cached entry
                    // so renderBMPMarkers below rebuilds at the new position. Without this,
                    // the API persists the change but the SPA reverts on re-render.
                    const cached = this.bmps.find((b) => b.TreatmentBMPID === bmp.TreatmentBMPID);
                    if (cached) {
                        cached.Latitude = newLat;
                        cached.Longitude = newLng;
                    }
                    bmp.Latitude = newLat;
                    bmp.Longitude = newLng;
                    this.selectedBMP.set({ ...bmp });
                    this.clearLocationPreview();
                    this.editMode.set("idle");
                    this.renderBMPMarkers();
                    // NPT-981 r3 (KE): don't fly the map after a save — the user is already looking
                    // at the spot they just edited. Refresh highlight + delineation without zooming.
                    this.refreshMarkerHighlight(false);
                    this.reloadSelectedDelineation();
                },
                error: () => this.savingSubject.next(false),
            });
            return;
        }

        if (mode === "editingDistributed" || mode === "editingCentralized") {
            const geoJson = this.collectDraftGeoJson();
            if (!geoJson) {
                this.alertService.pushAlert(new Alert("Draw or trace a delineation before saving.", AlertContext.Warning, true));
                return;
            }
            this.savingSubject.next(true);
            this.delineationService
                .upsertForTreatmentBMPDelineation(bmp.TreatmentBMPID, {
                    DelineationTypeID: this.pendingDelineationType,
                    GeoJson: JSON.stringify(geoJson),
                })
                .subscribe({
                    next: (dto) => {
                        this.savingSubject.next(false);
                        this.alertService.pushAlert(new Alert("Delineation saved.", AlertContext.Success, true));
                        this.editMode.set("idle");
                        this.map.pm.removeControls();
                        this.selectedDelineation.set(dto);
                        this.applyDelineationToSelectedBMP(dto);
                        this.renderSelectedDelineation(false);
                        this.refreshDelineationLayers();
                    },
                    error: () => this.savingSubject.next(false),
                });
        }
    }

    public cancelEdit(): void {
        if (this.editMode() === "idle") {
            return;
        }
        if (this.map?.pm) {
            this.map.pm.removeControls();
            if (this.map.pm.globalEditModeEnabled()) {
                this.map.pm.toggleGlobalEditMode();
            }
        }
        this.clearLocationPreview();
        this.editMode.set("idle");
        // Return to the default action view rather than dropping back into the flow-type chooser.
        this.choosingFlowType.set(false);
        this.renderSelectedDelineation();
    }

    private clearLocationPreview(): void {
        // NPT-981 r3: tear down the preview marker + cursor. The real BMP pin was never moved
        // (it's a separate preview marker now), so there's nothing to revert on cancel; on save,
        // renderBMPMarkers rebuilds the real pin at its new position.
        this.pendingLocation = null;
        this.setLocationEditCursor(false);
        if (this.locationPreviewMarker) {
            this.locationPreviewMarker.off("drag", this.onPreviewMarkerMove);
            this.locationPreviewMarker.off("dragend", this.onPreviewMarkerMove);
            this.map.removeLayer(this.locationPreviewMarker);
            this.locationPreviewMarker = null;
        }
    }

    private collectDraftGeoJson(): GeoJSON.Feature | null {
        if (!this.selectedDelineationLayer) {
            return null;
        }
        const layers: any[] = [];
        this.selectedDelineationLayer.eachLayer((l) => layers.push(l));
        if (layers.length === 0) {
            return null;
        }
        const fc = this.selectedDelineationLayer.toGeoJSON() as GeoJSON.FeatureCollection;
        if (!fc.features?.length) {
            return null;
        }
        return { type: "Feature", geometry: fc.features[0].geometry, properties: {} };
    }

    private reloadSelectedDelineation(): void {
        const bmp = this.selectedBMP();
        if (!bmp) {
            return;
        }
        this.delineationService.getForTreatmentBMPDelineation(bmp.TreatmentBMPID).subscribe((dto) => {
            this.selectedDelineation.set(dto);
            this.applyDelineationToSelectedBMP(dto);
            // reloadSelectedDelineation only runs after a save/verify-toggle — never zoom (the
            // user is already on the area they edited).
            this.renderSelectedDelineation(false);
        });
    }

    private applyDelineationToSelectedBMP(dto: DelineationDto | null): void {
        const bmp = this.selectedBMP();
        if (!bmp) {
            return;
        }
        bmp.HasDelineation = !!dto;
        bmp.DelineationID = dto?.DelineationID ?? null;
        bmp.DelineationTypeID = dto?.DelineationTypeID ?? null;
        bmp.IsVerified = dto?.IsVerified ?? null;
        this.selectedBMP.set({ ...bmp });
        this.verifiedControl.setValue(dto?.IsVerified ?? false, { emitEvent: false });
    }

    public requestRevisionUrl(): string {
        const bmp = this.selectedBMP();
        if (!bmp) {
            return "#";
        }
        return `/delineation/revision-requests/new/${bmp.TreatmentBMPID}`;
    }

    public bmpDetailUrl(): string {
        const bmp = this.selectedBMP();
        if (!bmp) {
            return "#";
        }
        return `/treatment-bmps/${bmp.TreatmentBMPID}`;
    }

    public delineationStatusLabel(): string {
        const delineation = this.selectedDelineation();
        if (!delineation) {
            return "None";
        }
        return delineation.IsVerified ? "Verified" : "Provisional";
    }

    public delineationTypeName(): string {
        const delineation = this.selectedDelineation();
        if (!delineation) {
            return "No delineation provided";
        }
        return delineation.DelineationTypeName;
    }

    public delineationAreaLabel(): string {
        const delineation = this.selectedDelineation();
        if (!delineation || delineation.DelineationArea == null) {
            return "—";
        }
        return `${delineation.DelineationArea} ac`;
    }

}
