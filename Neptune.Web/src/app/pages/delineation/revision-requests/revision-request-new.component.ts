import { Component, OnInit, signal } from "@angular/core";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { BehaviorSubject, forkJoin, map, Observable, shareReplay, tap } from "rxjs";
import * as L from "leaflet";
import "@geoman-io/leaflet-geoman-free";
import * as turf from "@turf/turf";

import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { RegionalSubbasinService } from "src/app/shared/generated/api/regional-subbasin.service";
import { RegionalSubbasinRevisionRequestService } from "src/app/shared/generated/api/regional-subbasin-revision-request.service";
import { TreatmentBMPDto } from "src/app/shared/generated/model/treatment-bmp-dto";

import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { LeafletHelperService } from "src/app/shared/services/leaflet-helper.service";

import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { RegionalSubbasinsLayerComponent } from "src/app/shared/components/leaflet/layers/regional-subbasins-layer/regional-subbasins-layer.component";
import { JurisdictionsLayerComponent } from "src/app/shared/components/leaflet/layers/jurisdictions-layer/jurisdictions-layer.component";
import { StormwaterNetworkLayerComponent } from "src/app/shared/components/leaflet/layers/stormwater-network-layer/stormwater-network-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";

@Component({
    selector: "revision-request-new",
    templateUrl: "./revision-request-new.component.html",
    styleUrl: "./revision-request-new.component.scss",
    imports: [
        AsyncPipe,
        FormsModule,
        ReactiveFormsModule,
        RouterLink,
        NeptuneMapComponent,
        RegionalSubbasinsLayerComponent,
        JurisdictionsLayerComponent,
        StormwaterNetworkLayerComponent,
        PageHeaderComponent,
        AlertDisplayComponent,
        FormFieldComponent,
    ],
})
export class RevisionRequestNewComponent implements OnInit {
    public OverlayMode = OverlayMode;
    public FormFieldType = FormFieldType;

    public initData$: Observable<boolean>;
    public mapHeight = "550px";
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public mapIsReady = false;

    public treatmentBMP = signal<TreatmentBMPDto | null>(null);
    public notesControl = new FormControl<string>("", { nonNullable: true });

    private treatmentBMPID: number;
    private readonly editableLayer: L.FeatureGroup = new L.FeatureGroup();

    private readonly savingSubject = new BehaviorSubject<boolean>(false);
    public readonly saving$ = this.savingSubject.asObservable();

    public hasGeometry = false;

    private readonly polygonStyle: L.PathOptions = { color: "yellow", fillOpacity: 0.4, opacity: 1 };

    // turf.simplify tolerance in degrees (~5m). Tunable: larger thins more aggressively but distorts;
    // smaller preserves shape but removes fewer vertices. Tune during browser verification if needed.
    private readonly simplifyTolerance = 0.00005;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private alertService: AlertService,
        private leafletHelperService: LeafletHelperService,
        private treatmentBMPService: TreatmentBMPService,
        private regionalSubbasinService: RegionalSubbasinService,
        private regionalSubbasinRevisionRequestService: RegionalSubbasinRevisionRequestService
    ) {}

    public ngOnInit(): void {
        this.treatmentBMPID = Number(this.route.snapshot.paramMap.get("treatmentBMPID"));
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;

        this.initData$ = forkJoin({
            bmp: this.treatmentBMPService.getByIDTreatmentBMP(this.treatmentBMPID),
            feature: this.regionalSubbasinService.getUpstreamDelineationForBMPRegionalSubbasin(this.treatmentBMPID),
        }).pipe(
            tap(({ bmp, feature }) => {
                this.treatmentBMP.set(bmp);
                this.renderEditableGeometry(feature as any);
            }),
            map(() => true),
            shareReplay(1)
        );
    }

    private renderEditableGeometry(feature: GeoJSON.Feature | null): void {
        this.editableLayer.addTo(this.map);
        this.setControl();

        if (!feature?.geometry) {
            this.alertService.pushAlert(new Alert("No upstream catchment is available for this BMP.", AlertContext.Warning, true));
            this.refreshControls();
            return;
        }

        this.addGeometryToEditableLayer(feature.geometry);
        this.refreshControls();
        this.startVertexEdit();
        try {
            this.map.flyToBounds(this.editableLayer.getBounds(), { padding: [50, 50] });
        } catch {
            // empty bounds
        }
    }

    // Wire Geoman draw/delete events so the user can delete the polygon (trash control) and re-draw a fresh
    // one (polygon control). Mirrors the edit-boundary.component.ts pattern; keeps exactly one feature.
    private setControl(): void {
        this.map
            .on("pm:create", (event: { layer: L.Path }) => {
                this.editableLayer.clearLayers();
                (event.layer as L.Path & { setStyle?: (s: L.PathOptions) => void }).setStyle?.(this.polygonStyle);
                this.editableLayer.addLayer(event.layer);
                this.refreshControls();
                this.startVertexEdit();
            })
            .on("pm:globalremovalmodetoggled", (e: { enabled: boolean }) => {
                if (e.enabled) {
                    this.editableLayer.clearLayers();
                    this.map.pm.toggleGlobalRemovalMode();
                }
                this.refreshControls();
            });
        this.refreshControls();
    }

    // Re-applies the toolbar so the available controls reflect whether a polygon currently exists:
    // no polygon -> draw on; polygon present -> edit + delete on.
    private refreshControls(): void {
        this.map.pm.removeControls();
        this.hasGeometry = this.editableLayer.getLayers().length > 0;
        this.leafletHelperService.setupGeomanControls(this.map, !this.hasGeometry, this.hasGeometry, this.hasGeometry, "Revision Request");
    }

    private startVertexEdit(): void {
        this.editableLayer.eachLayer((l) => (l as L.Path & { pm?: { enable: () => void } }).pm?.enable());
    }

    private addGeometryToEditableLayer(geometry: GeoJSON.Geometry): void {
        L.geoJSON(geometry as any, { style: this.polygonStyle }).eachLayer((l) => this.editableLayer.addLayer(l));
    }

    // Reduces vertex count on the current polygon using turf.simplify, for delineations traced with an
    // excessive number of vertices. Re-renders the thinned geometry for continued editing.
    public thinVertices(): void {
        if (this.editableLayer.getLayers().length === 0) {
            this.alertService.pushAlert(new Alert("There is no geometry to thin.", AlertContext.Warning, true));
            return;
        }
        const fc = this.editableLayer.toGeoJSON() as GeoJSON.FeatureCollection;
        const feature: GeoJSON.Feature = { type: "Feature", geometry: fc.features[0].geometry, properties: {} };
        const before = this.countVertices(feature.geometry);

        const simplified = turf.simplify(feature as any, { tolerance: this.simplifyTolerance, highQuality: true, mutate: false }) as GeoJSON.Feature;
        const after = this.countVertices(simplified.geometry);

        this.editableLayer.clearLayers();
        this.addGeometryToEditableLayer(simplified.geometry);
        this.refreshControls();
        this.startVertexEdit();

        this.alertService.pushAlert(new Alert(`Thinned the polygon from ${before} to ${after} vertices.`, AlertContext.Info, true));
    }

    private countVertices(geometry: GeoJSON.Geometry): number {
        if (geometry.type === "Polygon") {
            return geometry.coordinates.reduce((sum, ring) => sum + ring.length, 0);
        }
        if (geometry.type === "MultiPolygon") {
            return geometry.coordinates.reduce((sum, poly) => sum + poly.reduce((s, ring) => s + ring.length, 0), 0);
        }
        return 0;
    }

    public submit(): void {
        const fc = this.editableLayer.toGeoJSON() as GeoJSON.FeatureCollection;
        if (!fc.features?.length) {
            this.alertService.pushAlert(new Alert("No geometry to submit.", AlertContext.Warning, true));
            return;
        }
        const feature: GeoJSON.Feature = { type: "Feature", geometry: fc.features[0].geometry, properties: {} };

        const kinks = turf.kinks(feature as any);
        if (kinks.features.length > 0) {
            this.alertService.pushAlert(
                new Alert("The drawn polygon has self-intersecting edges. Please fix the geometry before submitting.", AlertContext.Danger, true)
            );
            return;
        }

        this.savingSubject.next(true);
        this.regionalSubbasinRevisionRequestService
            .createRegionalSubbasinRevisionRequest(this.treatmentBMPID, {
                GeoJson: JSON.stringify(feature),
                Notes: this.notesControl.value || null,
            })
            .subscribe({
                next: (dto) => {
                    this.savingSubject.next(false);
                    this.alertService.pushAlert(new Alert("Successfully submitted the Regional Subbasin Revision Request.", AlertContext.Success, true));
                    this.router.navigate(["delineation", "revision-requests", dto.RegionalSubbasinRevisionRequestID]);
                },
                error: () => this.savingSubject.next(false),
            });
    }

    public cancel(): void {
        this.router.navigate(["delineation", "delineation-map"], { queryParams: { treatmentBMPID: this.treatmentBMPID } });
    }
}
