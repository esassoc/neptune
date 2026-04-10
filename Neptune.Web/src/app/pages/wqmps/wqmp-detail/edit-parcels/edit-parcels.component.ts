import { Component, inject, Input, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { AsyncPipe } from "@angular/common";
import { Observable, forkJoin, tap, map, shareReplay, debounceTime, distinctUntilChanged, switchMap, of } from "rxjs";
import * as L from "leaflet";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { FormFieldComponent } from "src/app/shared/components/forms/form-field/form-field.component";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { ParcelLayerComponent } from "src/app/shared/components/leaflet/layers/parcel-layer/parcel-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { WfsService } from "src/app/shared/services/wfs.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { ParcelService } from "src/app/shared/generated/api/parcel.service";
import { ParcelDisplayDto } from "src/app/shared/generated/model/parcel-display-dto";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";

@Component({
    selector: "edit-parcels",
    imports: [
        AlertDisplayComponent,
        PageHeaderComponent,
        NeptuneMapComponent,
        ParcelLayerComponent,
        WqmpsLayerComponent,
        FormFieldComponent,
        RouterLink,
        AsyncPipe,
        ReactiveFormsModule,
    ],
    templateUrl: "./edit-parcels.component.html",
    styleUrl: "./edit-parcels.component.scss",
})
export class EditParcelsComponent implements OnInit {
    @Input() waterQualityManagementPlanID!: number;

    private router = inject(Router);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private parcelService = inject(ParcelService);
    private wfsService = inject(WfsService);
    private alertService = inject(AlertService);

    public OverlayMode = OverlayMode;
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public mapIsReady = false;
    public isLoadingSubmit = false;

    public loaded$: Observable<boolean>;
    public selectedParcelIDs: number[] = [];
    public selectedParcelsLayer: L.GeoJSON<any>;
    public boundingBox: BoundingBoxDto;
    private dataLoaded = false;
    private initialLoad = true;

    // Search
    public searchControl = new FormControl("");
    public searchResults: ParcelDisplayDto[] = [];
    public showSearchResults = false;

    public selectedParcelRows = signal<ParcelDisplayDto[]>([]);

    private highlightStyle = {
        color: "#fcfc12",
        weight: 2,
        opacity: 0.65,
        fillOpacity: 0.1,
    };

    ngOnInit(): void {
        this.loaded$ = forkJoin([
            this.wqmpService.getParcelIDsWaterQualityManagementPlan(this.waterQualityManagementPlanID),
            this.wqmpService.getBoundaryWaterQualityManagementPlan(this.waterQualityManagementPlanID),
        ]).pipe(
            tap(([parcelIDs, boundaryData]) => {
                this.selectedParcelIDs = parcelIDs ?? [];
                this.selectedParcelRows.set(boundaryData.Parcels ?? []);
                this.boundingBox = boundaryData.BoundingBox;
                this.dataLoaded = true;
                if (this.mapIsReady) {
                    this.addSelectedParcelsToMap();
                }
            }),
            map(() => true),
            shareReplay(1)
        );

        this.searchControl.valueChanges.pipe(
            debounceTime(300),
            distinctUntilChanged(),
            switchMap((term) => {
                if (!term || term.length < 2) {
                    return of([]);
                }
                return this.parcelService.searchParcel(term);
            })
        ).subscribe((results) => {
            this.searchResults = results;
            this.showSearchResults = results.length > 0;
        });
    }

    public handleMapReady(event: NeptuneMapInitEvent): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;
        this.enableParcelClickEvent();
        if (this.dataLoaded) {
            this.addSelectedParcelsToMap();
        }
    }

    public selectSearchResult(parcel: ParcelDisplayDto): void {
        if (!this.selectedParcelIDs.includes(parcel.ParcelID)) {
            this.selectedParcelIDs.push(parcel.ParcelID);
            this.selectedParcelRows.update((rows) => [...rows, parcel]);
            this.addSelectedParcelsToMap();
        }
        this.searchControl.setValue("", { emitEvent: false });
        this.searchResults = [];
        this.showSearchResults = false;
    }

    public removeParcel(parcelID: number): void {
        this.selectedParcelIDs = this.selectedParcelIDs.filter((id) => id !== parcelID);
        this.selectedParcelRows.update((rows) => rows.filter((p) => p.ParcelID !== parcelID));
        this.addSelectedParcelsToMap();
    }

    public save(): void {
        this.isLoadingSubmit = true;
        this.alertService.clearAlerts();

        this.wqmpService.updateParcelsWaterQualityManagementPlan(this.waterQualityManagementPlanID, this.selectedParcelIDs).subscribe({
            next: () => {
                this.isLoadingSubmit = false;
                this.alertService.pushAlert(new Alert("Successfully updated WQMP parcels.", AlertContext.Success));
                this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
            },
            error: () => {
                this.isLoadingSubmit = false;
                this.alertService.pushAlert(new Alert("An error occurred while saving parcels.", AlertContext.Danger));
            },
        });
    }

    public cancel(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
    }

    public hideSearchResults(): void {
        setTimeout(() => (this.showSearchResults = false), 200);
    }

    private addSelectedParcelsToMap(): void {
        if (this.selectedParcelsLayer) {
            this.map.removeLayer(this.selectedParcelsLayer);
            this.selectedParcelsLayer = null;
        }
        if (this.selectedParcelIDs.length > 0) {
            const expectedIDs = [...this.selectedParcelIDs];
            this.wfsService.getGeoserverWFSLayerWithCQLFilter("OCStormwater:Parcels", `ParcelID in (${expectedIDs.join(",")})`, "ParcelID").subscribe((response) => {
                // Discard stale responses if selection changed while WFS was in flight
                if (JSON.stringify([...expectedIDs].sort()) !== JSON.stringify([...this.selectedParcelIDs].sort())) {
                    return;
                }
                if (this.selectedParcelsLayer) {
                    this.map.removeLayer(this.selectedParcelsLayer);
                }
                this.selectedParcelsLayer = L.geoJSON(response as any, { style: this.highlightStyle });
                this.selectedParcelsLayer.addTo(this.map);
                if (this.initialLoad) {
                    this.map.fitBounds(this.selectedParcelsLayer.getBounds());
                    this.initialLoad = false;
                }
            });
        }
    }

    private enableParcelClickEvent(): void {
        this.map.on("click", (event: L.LeafletMouseEvent): void => {
            this.wfsService.getParcelByCoordinate(event.latlng.lng, event.latlng.lat).subscribe((parcelsFeatureCollection: GeoJSON.FeatureCollection) => {
                parcelsFeatureCollection.features.forEach((feature: GeoJSON.Feature) => {
                    const parcelID = feature.properties.ParcelID;
                    const parcelNumber = feature.properties.ParcelNumber;
                    const parcelAddress = feature.properties.ParcelAddress;

                    if (this.selectedParcelIDs.includes(parcelID)) {
                        this.removeParcel(parcelID);
                    } else {
                        this.selectedParcelIDs.push(parcelID);
                        this.selectedParcelRows.update((rows) => [...rows, { ParcelID: parcelID, ParcelNumber: parcelNumber, ParcelAddress: parcelAddress }]);
                        this.addSelectedParcelsToMap();
                    }
                });
            });
        });
    }
}
