import { Component, inject, Input, OnInit } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable, tap, shareReplay } from "rxjs";

import * as L from "leaflet";
import "@geoman-io/leaflet-geoman-free";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { NeptuneMapComponent, NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";

import { ParcelLayerComponent } from "src/app/shared/components/leaflet/layers/parcel-layer/parcel-layer.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { LeafletHelperService } from "src/app/shared/services/leaflet-helper.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanBoundaryResponseDto } from "src/app/shared/generated/model/water-quality-management-plan-boundary-response-dto";
import { ParcelDisplayDto } from "src/app/shared/generated/model/parcel-display-dto";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { WaterQualityManagementPlanBoundaryUpsertDto } from "src/app/shared/generated/model/water-quality-management-plan-boundary-upsert-dto";

@Component({
    selector: "edit-boundary",
    imports: [
        AlertDisplayComponent,
        PageHeaderComponent,
        NeptuneMapComponent,
        ParcelLayerComponent,
        WqmpsLayerComponent,
        IconComponent,
        RouterLink,
        AsyncPipe,
    ],
    templateUrl: "./edit-boundary.component.html",
    styleUrl: "./edit-boundary.component.scss",
})
export class EditBoundaryComponent implements OnInit {
    @Input() waterQualityManagementPlanID!: number;

    private router = inject(Router);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private alertService = inject(AlertService);
    private leafletHelperService = inject(LeafletHelperService);

    public OverlayMode = OverlayMode;
    public mapHeight = "650px";
    public map: L.Map;
    public layerControl: L.Control.Layers;
    public mapIsReady = false;
    public isLoadingSubmit = false;

    public layer: L.FeatureGroup = new L.FeatureGroup();
    public isPerformingDrawAction = false;

    public boundaryData$: Observable<WaterQualityManagementPlanBoundaryResponseDto>;
    public parcels: ParcelDisplayDto[] = [];
    public calculatedAcreage: number | null = null;
    public boundingBox: BoundingBoxDto;

    ngOnInit(): void {
        this.boundaryData$ = this.wqmpService.getBoundaryWaterQualityManagementPlan(this.waterQualityManagementPlanID).pipe(
            tap((response) => {
                this.parcels = response.Parcels ?? [];
                this.calculatedAcreage = response.CalculatedWQMPAcreage ?? null;
                this.boundingBox = response.BoundingBox;
            }),
            shareReplay(1)
        );
    }

    public handleMapReady(event: NeptuneMapInitEvent, boundaryData: WaterQualityManagementPlanBoundaryResponseDto): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;

        if (boundaryData.BoundaryAsFeatureCollection) {
            this.addFeatureCollectionToFeatureGroup(boundaryData.BoundaryAsFeatureCollection, this.layer);
        }
        this.layer.addTo(this.map);
        this.setControl();

        if (this.layer.getLayers().length > 0) {
            this.map.fitBounds(this.layer.getBounds());
            this.map.pm.toggleGlobalEditMode();
        }

        this.layer.on("click", () => {
            if (!this.map.pm.globalEditModeEnabled()) {
                this.map.pm.toggleGlobalEditMode();
            }
        });
    }

    public save(): void {
        this.isLoadingSubmit = true;
        this.alertService.clearAlerts();

        const dto: WaterQualityManagementPlanBoundaryUpsertDto = {};
        this.layer.eachLayer((layer: L.Path & { toGeoJSON: () => GeoJSON.Feature }) => {
            dto.GeometryAsGeoJson = JSON.stringify(layer.toGeoJSON());
        });

        this.wqmpService.updateBoundaryWaterQualityManagementPlan(this.waterQualityManagementPlanID, dto).subscribe({
            next: () => {
                this.isLoadingSubmit = false;
                // Navigate first — the edit-boundary's <app-alert-display> clears alerts on destroy
                // (alert-display.component.ts:31), so we push the success alert only after the
                // new detail page's alert-display has mounted.
                this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]).then(() => {
                    this.alertService.pushAlert(new Alert("Successfully updated WQMP boundary.", AlertContext.Success));
                });
            },
            error: () => {
                this.isLoadingSubmit = false;
                this.alertService.pushAlert(new Alert("An error occurred while saving the boundary.", AlertContext.Danger));
            },
        });
    }

    public cancel(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
    }

    public setControl(): void {
        this.map
            .on("pm:create", (event: { shape: string; layer: L.Path & { toGeoJSON: () => GeoJSON.Feature } }) => {
                this.isPerformingDrawAction = false;
                const layer = event.layer;
                this.layer.clearLayers();
                this.layer.addLayer(layer);
                this.selectFeatureImpl();
            })
            .on("pm:globaleditmodetoggled", (e: any) => {
                if (e.enabled) {
                    this.map.eachLayer((layer: any) => {
                        if (layer.pm && (this.layer != layer || !this.layer.hasLayer(layer))) {
                            layer.pm.disable();
                        }
                    });
                    this.layer.eachLayer((layer: L.Path) => {
                        layer.pm.enable();
                    });
                    return;
                }
                this.selectFeatureImpl();
            })
            .on("pm:globalremovalmodetoggled", (e: any) => {
                if (e.enabled) {
                    this.layer.clearLayers();
                    this.map.pm.toggleGlobalRemovalMode();
                    return;
                }
                this.selectFeatureImpl();
            });
        this.addOrRemoveGeomanControl(true);
    }

    public addOrRemoveGeomanControl(turnOn: boolean): void {
        if (turnOn) {
            const hasPolygon = this.layer.getLayers().length > 0;
            this.leafletHelperService.setupGeomanControls(this.map, !hasPolygon, hasPolygon, hasPolygon, "WQMP Boundary");
            return;
        }
        this.map.pm.removeControls();
    }

    public selectFeatureImpl(): void {
        if (this.isPerformingDrawAction) {
            return;
        }
        this.map.pm.removeControls();
        this.addOrRemoveGeomanControl(true);
    }

    public addFeatureCollectionToFeatureGroup(featureCollection: any, featureGroup: L.FeatureGroup): void {
        if (featureCollection.features && featureCollection.features.length > 0) {
            L.geoJson(featureCollection, {
                onEachFeature: function (feature, layer) {
                    featureGroup.addLayer(layer);
                },
            });
        }
    }
}
