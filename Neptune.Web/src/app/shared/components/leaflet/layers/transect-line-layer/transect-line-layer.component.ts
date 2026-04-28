import { Component, Input, OnChanges } from "@angular/core";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
import { Observable, tap } from "rxjs";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { IFeature } from "src/app/shared/generated/model/i-feature";
import { AsyncPipe } from "@angular/common";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";

@Component({
    selector: "transect-line-layer",
    imports: [AsyncPipe],
    templateUrl: "./transect-line-layer.component.html",
    styleUrl: "./transect-line-layer.component.scss",
})
export class TransectLineLayerComponent extends MapLayerBase implements OnChanges {
    constructor(
        private onlandVisualTrashAssessmentService: OnlandVisualTrashAssessmentService,
        private onlandVisualTrashAssessmentAreaService: OnlandVisualTrashAssessmentAreaService
    ) {
        super();
    }

    @Input() ovtaID: number;
    @Input() ovtaAreaID: number;
    @Input() interactive: boolean = true;
    public layer;

    private get transectLineStyle() {
        return {
            color: "#ff42ff",
            weight: 2,
            interactive: this.interactive,
        };
    }

    public featureCollection$: Observable<IFeature[]>;

    // Assigned in ngOnInit (not ngAfterViewInit) so the template's `@if (featureCollection$ | async)`
    // sees the observable on the first template check and the async pipe actually subscribes.
    // ViewChild template refs used by initLayer() are still safely available by the time the
    // HTTP response arrives and the tap fires.
    ngOnInit(): void {
        this.loadLayer();
    }

    /**
     * Drop the current layer from the map and re-fetch the transect line. Parent components
     * call this after a save that can change observation geometry (e.g. record-observations
     * save) so the map reflects the new line without requiring a full page reload.
     */
    public refresh(): void {
        if (this.layer) {
            this.layer.remove();
            this.layer = undefined;
        }
        this.loadLayer();
    }

    private loadLayer(): void {
        if (this.ovtaID) {
            const request$ = this.onlandVisualTrashAssessmentService.getTransectLineAsFeatureCollectionOnlandVisualTrashAssessment(this.ovtaID);
            this.featureCollection$ = this.trackLayerRequest$(request$).pipe(
                tap((featureCollection) => {
                    this.layer = new L.GeoJSON(featureCollection as any, {
                        style: this.transectLineStyle,
                    });
                    this.initLayer();
                })
            );
        } else if (this.ovtaAreaID) {
            const request$ = this.onlandVisualTrashAssessmentAreaService.getTransectLineAsFeatureCollectionOnlandVisualTrashAssessmentArea(this.ovtaAreaID);
            this.featureCollection$ = this.trackLayerRequest$(request$).pipe(
                tap((featureCollection) => {
                    this.layer = new L.GeoJSON(featureCollection as any, {
                        style: this.transectLineStyle,
                    });
                    this.initLayer();
                })
            );
        }
    }
}
