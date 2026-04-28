import { CommonModule } from "@angular/common";
import { Component, OnChanges } from "@angular/core";
import { environment } from "src/environments/environment";
import "leaflet.markercluster";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { Observable, tap } from "rxjs";
import { IFeature } from "src/app/shared/generated/model/i-feature";

@Component({
    selector: "inventoried-bmps-layer",
    imports: [CommonModule],
    templateUrl: "./inventoried-bmps-layer.component.html",
    styleUrls: ["./inventoried-bmps-layer.component.scss"],
})
export class InventoriedBMPsLayerComponent extends MapLayerBase implements OnChanges {
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

    constructor(private treatmentBMPService: TreatmentBMPService) {
        super();
    }

    // Assigned in ngOnInit (not ngAfterViewInit) so the template's `@if (treatmentBMPs$ | async)`
    // sees the observable on the first template check and the async pipe actually subscribes.
    // ViewChild template refs used by initLayer() are still safely available by the time the
    // HTTP response arrives and the tap fires.
    ngOnInit(): void {
        const request$ = this.treatmentBMPService.listInventoryVerifiedTreatmentBMPsAsFeatureCollectionTreatmentBMP();
        this.treatmentBMPs$ = this.trackLayerRequest$(request$).pipe(
            tap((treatmentBMPs) => {
                const inventoriedTreatmentBMPsLayer = new L.GeoJSON(treatmentBMPs as any, {
                    pointToLayer: (feature, latlng) => {
                        return L.marker(latlng, { icon: MarkerHelper.inventoriedTreatmentBMPMarker });
                    },
                    onEachFeature: (feature, layer) => {
                        layer.bindPopup(
                            `<b>Name:</b> <a target="_blank" href="${this.ocstBaseUrl()}/TreatmentBMP/Detail/${feature.properties.TreatmentBMPID}">${
                                feature.properties.TreatmentBMPName
                            }</a><br>` + `<b>Type:</b> ${feature.properties.TreatmentBMPTypeName}`
                        );
                    },
                });
                this.layer.addLayer(inventoriedTreatmentBMPsLayer);
                this.initLayer();
            })
        );
    }

    public ocstBaseUrl(): string {
        return environment.ocStormwaterToolsBaseUrl;
    }
}
