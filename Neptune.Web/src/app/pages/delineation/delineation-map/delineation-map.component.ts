import { Component, OnInit, signal } from "@angular/core";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { BehaviorSubject, forkJoin, map, Observable, shareReplay, tap } from "rxjs";
import * as L from "leaflet";
import "@geoman-io/leaflet-geoman-free";
import "leaflet.markercluster";
import { environment } from "src/environments/environment";

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

import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { DelineationsLayerComponent } from "src/app/shared/components/leaflet/layers/delineations-layer/delineations-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { StormwaterNetworkLayerComponent } from "src/app/shared/components/leaflet/layers/stormwater-network-layer/stormwater-network-layer.component";
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

    public bmps: TreatmentBMPDelineationMapDto[] = [];
    public selectedBMP = signal<TreatmentBMPDelineationMapDto | null>(null);
    public selectedDelineation = signal<DelineationDto | null>(null);
    public editMode = signal<EditMode>("idle");

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
        this.bmpsClusterLayer = (L as any).markerClusterGroup({
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
            const marker = L.marker([bmp.Latitude, bmp.Longitude], { icon: MarkerHelper.treatmentBMPMarker });
            marker.on("click", () => {
                if (this.editMode() !== "idle") {
                    return;
                }
                this.selectBMP(bmp);
            });
            this.bmpMarkerByID.set(bmp.TreatmentBMPID, marker);
            this.bmpsClusterLayer.addLayer(marker);
        }
        this.bmpsClusterLayer["legendHtml"] = "<img src='./assets/main/map-icons/marker-icon-violet.png' style='height:17px'>";
        this.layerControl.addOverlay(this.bmpsClusterLayer, "Treatment BMPs");
        this.map.addLayer(this.bmpsClusterLayer);
    }

    public selectBMP(bmp: TreatmentBMPDelineationMapDto): void {
        this.cancelEdit();
        this.selectedBMP.set(bmp);
        this.selectedDelineation.set(null);
        this.clearSelectedDelineationLayer();
        this.refreshMarkerHighlight();

        const requestedID = bmp.TreatmentBMPID;
        this.delineationService.getForTreatmentBMPDelineation(requestedID).subscribe((dto) => {
            if (this.selectedBMP()?.TreatmentBMPID !== requestedID) {
                return;
            }
            this.selectedDelineation.set(dto);
            this.applyDelineationToSelectedBMP(dto);
            this.renderSelectedDelineation();
        });
    }

    private refreshMarkerHighlight(): void {
        const selected = this.selectedBMP();
        for (const [id, marker] of this.bmpMarkerByID) {
            const isSelected = id === selected?.TreatmentBMPID;
            marker.setIcon(isSelected ? MarkerHelper.selectedMarker : MarkerHelper.treatmentBMPMarker);
            marker.setZIndexOffset(isSelected ? 10000 : 1000);
        }
        if (!selected) {
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

    private renderSelectedDelineation(): void {
        this.clearSelectedDelineationLayer();
        const delineation = this.selectedDelineation();
        if (!delineation?.Geometry) {
            return;
        }
        const geom = JSON.parse(delineation.Geometry);
        this.selectedDelineationLayer = L.geoJSON(geom, { style: this.delineationSelectedStyle }).addTo(this.map);
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

    public deleteDelineation(): void {
        const bmp = this.selectedBMP();
        const delineation = this.selectedDelineation();
        if (!bmp || !delineation) {
            return;
        }
        this.confirmService
            .confirm({
                title: "Delete Delineation",
                message: `<p>You are about to delete the delineation for <strong>${bmp.TreatmentBMPName}</strong>.</p><p>This action cannot be undone. Are you sure you wish to proceed?</p>`,
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
                this.leafletHelperService.setupGeomanControls(this.map, false, true, false, "Delineation");
                this.selectedDelineationLayer.eachLayer((l) => (l as any).pm?.enable());
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
        if (!this.selectedBMP()) {
            return;
        }
        this.cancelEdit();
        this.editMode.set("editingLocation");
        this.pendingLocation = null;
        const el = document.querySelector(".leaflet-interactive") as HTMLElement | null;
        if (el) {
            el.style.cursor = "crosshair";
        }
    }

    private attachMapClickForLocation(): void {
        this.map.on("click", (event: L.LeafletMouseEvent) => {
            if (this.editMode() !== "editingLocation") {
                return;
            }
            this.pendingLocation = { lat: event.latlng.lat, lng: event.latlng.lng };
            if (this.locationPreviewMarker) {
                this.map.removeLayer(this.locationPreviewMarker);
            }
            this.locationPreviewMarker = L.marker(event.latlng, { icon: MarkerHelper.selectedMarker, zIndexOffset: 11000 }).addTo(this.map);
        });
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
                    bmp.Latitude = newLat;
                    bmp.Longitude = newLng;
                    this.selectedBMP.set({ ...bmp });
                    this.clearLocationPreview();
                    this.editMode.set("idle");
                    this.renderBMPMarkers();
                    this.refreshMarkerHighlight();
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
                    TreatmentBMPID: bmp.TreatmentBMPID,
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
                        this.renderSelectedDelineation();
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
        this.renderSelectedDelineation();
    }

    private clearLocationPreview(): void {
        if (this.locationPreviewMarker) {
            this.map.removeLayer(this.locationPreviewMarker);
            this.locationPreviewMarker = null;
        }
        this.pendingLocation = null;
        const el = document.querySelector(".leaflet-interactive") as HTMLElement | null;
        if (el) {
            el.style.cursor = "";
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
            this.renderSelectedDelineation();
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
        return `${environment.ocStormwaterToolsBaseUrl}/RegionalSubbasinRevisionRequest/New/${bmp.TreatmentBMPID}`;
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

    public editExistingDelineation(): void {
        const delineation = this.selectedDelineation();
        if (!delineation) {
            return;
        }
        if (delineation.DelineationTypeID === DelineationTypeEnum.Distributed) {
            this.startEditDistributed();
        } else {
            this.startEditCentralized();
        }
    }
}
