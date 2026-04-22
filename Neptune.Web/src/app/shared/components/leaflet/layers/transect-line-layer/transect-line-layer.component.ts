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

    ngAfterViewInit(): void {
        if (this.ovtaID) {
            const request$ = this.onlandVisualTrashAssessmentService.getTransectLineAsFeatureCollectionOnlandVisualTrashAssessment(this.ovtaID);
            this.featureCollection$ = this.trackLayerRequest$(request$).pipe(
                tap((transectLineFeatureCollection) => {
                    this.layer = new L.GeoJSON(transectLineFeatureCollection as any, {
                        style: this.transectLineStyle,
                    });
                    this.initLayer();
                })
            );
        } else if (this.ovtaAreaID) {
            const request$ = this.onlandVisualTrashAssessmentAreaService.getTransectLineAsFeatureCollectionOnlandVisualTrashAssessmentArea(this.ovtaAreaID);
            this.featureCollection$ = this.trackLayerRequest$(request$).pipe(
                tap((transectLineFeatureCollection) => {
                    this.layer = new L.GeoJSON(transectLineFeatureCollection as any, {
                        style: this.transectLineStyle,
                    });
                    this.initLayer();
                })
            );
        }
    }
}
