import { CommonModule } from "@angular/common";
import { Component, Input, OnChanges } from "@angular/core";
import "leaflet.markercluster";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { Observable, tap } from "rxjs";
import { IFeature } from "src/app/shared/generated/model/i-feature";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

@Component({
    selector: "inventoried-bmps-layer",
    imports: [CommonModule],
    templateUrl: "./inventoried-bmps-layer.component.html",
    styleUrls: ["./inventoried-bmps-layer.component.scss"],
})
export class InventoriedBMPsLayerComponent extends MapLayerBase implements OnChanges {
    // NPT-1092: when set, scope the layer to a single WQMP's linked BMPs (the boundary editors);
    // otherwise it shows all inventory-verified BMPs the caller can view (the default use).
    @Input() waterQualityManagementPlanID?: number;
    // Only applies in WQMP-scoped mode (the all-inventory endpoint is verified-only by definition).
    @Input() verifiedOnly: boolean = true;
    @Input() layerLabel: string = "Inventoried BMP Locations";

    public layer: L.MarkerClusterGroup = new L.MarkerClusterGroup({
        iconCreateFunction: function (cluster) {
            var childCount = cluster.getChildCount();

            return new L.DivIcon({
                html: "<div><span>" + childCount + "</span></div>",
                className: "treatment-bmp-cluster",
                iconSize: new L.Point(40, 40),
            });
        },
    });

    public treatmentBMPs$: Observable<IFeature[]>;

    constructor(
        private treatmentBMPService: TreatmentBMPService,
        private waterQualityManagementPlanService: WaterQualityManagementPlanService
    ) {
        super();
    }

    // Assigned in ngOnInit (not ngAfterViewInit) so the template's `@if (treatmentBMPs$ | async)`
    // sees the observable on the first template check and the async pipe actually subscribes.
    // ViewChild template refs used by initLayer() are still safely available by the time the
    // HTTP response arrives and the tap fires.
    ngOnInit(): void {
        const request$ = this.waterQualityManagementPlanID
            ? this.waterQualityManagementPlanService.listTreatmentBMPsAsFeatureCollectionWaterQualityManagementPlan(this.waterQualityManagementPlanID, this.verifiedOnly)
            : this.treatmentBMPService.listInventoryVerifiedTreatmentBMPsAsFeatureCollectionTreatmentBMP();
        this.treatmentBMPs$ = this.trackLayerRequest$(request$).pipe(
            tap((treatmentBMPs) => {
                const inventoriedTreatmentBMPsLayer = new L.GeoJSON(treatmentBMPs as any, {
                    pointToLayer: (feature, latlng) => {
                        return L.marker(latlng, { icon: MarkerHelper.inventoriedTreatmentBMPMarker });
                    },
                    onEachFeature: (feature, layer) => {
                        // SPA detail route. Leaflet popups are raw HTML so we can't use
                        // [routerLink]; root-relative path + target="_blank" still opens
                        // the SPA in a fresh tab. Escape server-provided strings (BMP name/type
                        // are user-editable) to prevent stored XSS, and rel="noopener" the link.
                        const name = escapeHtml(feature.properties.TreatmentBMPName ?? "");
                        const type = escapeHtml(feature.properties.TreatmentBMPTypeName ?? "");
                        layer.bindPopup(
                            `<b>Name:</b> <a target="_blank" rel="noopener noreferrer" href="/treatment-bmps/${feature.properties.TreatmentBMPID}">${name}</a><br>` +
                                `<b>Type:</b> ${type}`
                        );
                    },
                });
                this.layer.addLayer(inventoriedTreatmentBMPsLayer);
                this.initLayer();
            })
        );
    }
}
