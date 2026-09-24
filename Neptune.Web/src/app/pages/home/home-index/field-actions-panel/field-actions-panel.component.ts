import { Component, DestroyRef, computed, effect, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { DialogService } from "@ngneat/dialog";
import * as L from "leaflet";
import { EMPTY, Observable, Subscription, catchError, forkJoin, map, of, switchMap, tap } from "rxjs";
import {
    BeginFieldVisitModalComponent,
    BeginFieldVisitModalContext,
} from "src/app/pages/treatment-bmps/treatment-bmp-detail/begin-field-visit-modal/begin-field-visit-modal.component";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { NearbyAssetService } from "src/app/shared/generated/api/nearby-asset.service";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { FieldVisitDto } from "src/app/shared/generated/model/field-visit-dto";
import { NearbyAssetDto } from "src/app/shared/generated/model/nearby-asset-dto";
import { NearbyAssetsResultDto } from "src/app/shared/generated/model/nearby-assets-result-dto";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { GeolocationCoordinates, GeolocationFailure, GeolocationService } from "src/app/shared/services/geolocation.service";

type PanelState = "locked" | "locating" | "loading" | "ready" | "error";
type PanelError = GeolocationFailure | "api";

const MAX_ROWS = 10;
const METERS_PER_DEGREE_LATITUDE = 111320;

// NPT-1123: finds the BMPs, WQMPs and OVTA areas the user is standing next to and puts each one's next action on its row.
// Nothing touches geolocation or the API until the user asks — the panel starts locked.
@Component({
    selector: "field-actions-panel",
    templateUrl: "./field-actions-panel.component.html",
    styleUrls: ["./field-actions-panel.component.scss"],
    imports: [NeptuneMapComponent],
})
export class FieldActionsPanelComponent {
    private geolocationService = inject(GeolocationService);
    private nearbyAssetService = inject(NearbyAssetService);
    private fieldVisitService = inject(FieldVisitService);
    private dialogService = inject(DialogService);
    private router = inject(Router);

    // Signals throughout: under zoneless change detection, Leaflet and geolocation callbacks won't re-render plain fields.
    public state = signal<PanelState>("locked");
    public errorKind = signal<PanelError | null>(null);
    public result = signal<NearbyAssetsResultDto | null>(null);
    public userLocation = signal<GeolocationCoordinates | null>(null);
    public hoveredKey = signal<string | null>(null);
    // undefined = still loading; the in-progress endpoint's answer (visit or null) per BMP once known
    public inProgressVisitByBmpID = signal<Map<number, FieldVisitDto | null> | undefined>(undefined);

    public totalCount = computed(() => this.result()?.Assets?.length ?? 0);
    public rows = computed(() => (this.result()?.Assets ?? []).slice(0, MAX_ROWS));
    public radiusLabel = computed(() => `${this.result()?.RadiusMeters ?? 100} m`);
    public boundingBox = computed(() => this.buildBoundingBox());

    private map: L.Map;
    private markersByKey = new Map<string, L.Marker>();
    private unlockSubscription: Subscription;

    constructor() {
        effect(() => this.applyMarkerHighlight(this.hoveredKey()));
        inject(DestroyRef).onDestroy(() => this.unlockSubscription?.unsubscribe());
    }

    public static assetKey(asset: NearbyAssetDto): string {
        return `${asset.AssetType}-${asset.AssetID}`;
    }

    public keyFor(asset: NearbyAssetDto): string {
        return FieldActionsPanelComponent.assetKey(asset);
    }

    public unlock(): void {
        this.unlockSubscription?.unsubscribe();
        this.clearResults();
        this.errorKind.set(null);
        this.state.set("locating");

        this.unlockSubscription = this.geolocationService
            .getCurrentPosition()
            .pipe(
                catchError((failure: GeolocationFailure) => this.fail(failure)),
                tap((coordinates) => {
                    this.userLocation.set(coordinates);
                    this.state.set("loading");
                }),
                switchMap((coordinates) => this.nearbyAssetService.listNearbyAsset(coordinates.latitude, coordinates.longitude).pipe(catchError(() => this.fail("api")))),
                tap((result) => {
                    this.result.set(result);
                    this.state.set("ready");
                }),
                switchMap(() => this.loadInProgressVisits())
            )
            .subscribe((visitsByBmpID) => this.inProgressVisitByBmpID.set(visitsByBmpID));
    }

    public reset(): void {
        this.unlockSubscription?.unsubscribe();
        this.clearResults();
        this.errorKind.set(null);
        this.state.set("locked");
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        const location = this.userLocation();
        const userLatLng = L.latLng(location.latitude, location.longitude);

        L.marker(userLatLng, { icon: MarkerHelper.userLocationMarker, interactive: false, keyboard: false, title: "Your location" }).addTo(this.map);
        const radiusCircle = L.circle(userLatLng, {
            radius: this.result().RadiusMeters,
            color: "#0099ab",
            weight: 1.5,
            dashArray: "4 4",
            fillColor: "#0099ab",
            fillOpacity: 0.06,
            interactive: false,
        }).addTo(this.map);

        this.rows().forEach((asset) => {
            const key = this.keyFor(asset);
            const marker = L.marker([asset.Latitude, asset.Longitude], { icon: MarkerHelper.nearbyAssetMarker, title: asset.AssetName, keyboard: false })
                .on("mouseover", () => this.hoveredKey.set(key))
                .on("mouseout", () => this.hoveredKey.set(null))
                .addTo(this.map);
            this.markersByKey.set(key, marker);
        });

        this.map.fitBounds(radiusCircle.getBounds(), { padding: [12, 12] });
    }

    public onRowEnter(asset: NearbyAssetDto): void {
        this.hoveredKey.set(this.keyFor(asset));
    }

    public onRowLeave(): void {
        this.hoveredKey.set(null);
    }

    public actionLabel(asset: NearbyAssetDto): string {
        switch (asset.AssetType) {
            case "BMP":
                return this.inProgressVisitFor(asset) ? "Continue Visit" : "Start Visit";
            case "WQMP":
                return "Start O&M Verification";
            case "OVTA":
                return "Start OVTA";
            default:
                return "Open";
        }
    }

    // BMP labels depend on the in-progress lookup; hold the button until it answers so we never offer "Start" on a BMP with a visit underway
    public isActionPending(asset: NearbyAssetDto): boolean {
        return asset.AssetType === "BMP" && this.inProgressVisitByBmpID() === undefined;
    }

    public runAction(asset: NearbyAssetDto): void {
        switch (asset.AssetType) {
            case "BMP": {
                const inProgress = this.inProgressVisitFor(asset);
                if (inProgress) {
                    this.router.navigate(["/field-visits", inProgress.FieldVisitID]);
                } else {
                    this.openBeginFieldVisitModal(asset.AssetID);
                }
                break;
            }
            case "WQMP":
                this.router.navigate(["/water-quality-management-plans", asset.AssetID, "verifications", "new"]);
                break;
            case "OVTA":
                // Straight to Initiate (skipping Instructions) so the pre-selection query params survive
                this.router.navigate(["/trash", "onland-visual-trash-assessments", "new", "initiate-ovta"], {
                    queryParams: { ovtaAreaID: asset.AssetID, jurisdictionID: asset.StormwaterJurisdictionID },
                });
                break;
        }
    }

    private inProgressVisitFor(asset: NearbyAssetDto): FieldVisitDto | null {
        return this.inProgressVisitByBmpID()?.get(asset.AssetID) ?? null;
    }

    private openBeginFieldVisitModal(treatmentBMPID: number): void {
        this.dialogService
            .open(BeginFieldVisitModalComponent, {
                data: { treatmentBMPID, inProgressFieldVisit: null } as BeginFieldVisitModalContext,
            })
            .afterClosed$.subscribe((result) => {
                if (result) {
                    this.router.navigate(["/field-visits", result.FieldVisitID]);
                }
            });
    }

    private loadInProgressVisits(): Observable<Map<number, FieldVisitDto | null>> {
        const bmpIDs = this.rows()
            .filter((x) => x.AssetType === "BMP")
            .map((x) => x.AssetID);
        if (bmpIDs.length === 0) {
            return of(new Map());
        }
        return forkJoin(
            bmpIDs.map((id) =>
                this.fieldVisitService.getInProgressForTreatmentBMPFieldVisit(id).pipe(
                    catchError(() => of(null)),
                    map((visit) => [id, visit ?? null] as [number, FieldVisitDto | null])
                )
            )
        ).pipe(map((pairs) => new Map(pairs)));
    }

    private fail(kind: PanelError): Observable<never> {
        this.errorKind.set(kind);
        this.state.set("error");
        return EMPTY;
    }

    private clearResults(): void {
        this.result.set(null);
        this.userLocation.set(null);
        this.hoveredKey.set(null);
        this.inProgressVisitByBmpID.set(undefined);
        // the map itself is torn down by the template's @if; just drop our references
        this.markersByKey.clear();
        this.map = null;
    }

    private applyMarkerHighlight(hoveredKey: string | null): void {
        this.markersByKey.forEach((marker, key) => {
            const isHovered = key === hoveredKey;
            marker.setIcon(isHovered ? MarkerHelper.nearbyAssetSelectedMarker : MarkerHelper.nearbyAssetMarker);
            marker.setZIndexOffset(isHovered ? 10000 : 0);
        });
    }

    private buildBoundingBox(): BoundingBoxDto | null {
        const location = this.userLocation();
        if (!location) {
            return null;
        }
        const radius = (this.result()?.RadiusMeters ?? 100) * 1.25;
        const latDelta = radius / METERS_PER_DEGREE_LATITUDE;
        const lonDelta = radius / (METERS_PER_DEGREE_LATITUDE * Math.cos((location.latitude * Math.PI) / 180));
        return new BoundingBoxDto({
            Bottom: location.latitude - latDelta,
            Top: location.latitude + latDelta,
            Left: location.longitude - lonDelta,
            Right: location.longitude + lonDelta,
        });
    }
}
