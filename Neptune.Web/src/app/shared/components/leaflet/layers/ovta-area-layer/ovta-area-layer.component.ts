import { Component, Input, OnChanges } from "@angular/core";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
import { Observable, tap } from "rxjs";
import { IFeature } from "src/app/shared/generated/model/models";
import { AsyncPipe } from "@angular/common";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";

@Component({
    selector: "ovta-area-layer",
    imports: [AsyncPipe],
    templateUrl: "./ovta-area-layer.component.html",
    styleUrl: "./ovta-area-layer.component.scss",
})
export class OvtaAreaLayerComponent extends MapLayerBase implements OnChanges {
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

    private get ovtaAreaStyle() {
        return {
            color: "blue",
            fillOpacity: 0.2,
            opacity: 0,
            interactive: this.interactive,
        };
    }

    public featureCollection$: Observable<IFeature[]>;

    ngAfterViewInit(): void {
        if (this.ovtaID) {
            const request$ = this.onlandVisualTrashAssessmentService.getAreaAsFeatureCollectionOnlandVisualTrashAssessment(this.ovtaID);
            this.featureCollection$ = this.trackLayerRequest$(request$).pipe(
                tap((transectLineFeatureCollection) => {
                    this.layer = new L.GeoJSON(transectLineFeatureCollection as any, {
                        style: this.ovtaAreaStyle,
                    });
                    this.initLayer();
                })
            );
        } else if (this.ovtaAreaID) {
            const request$ = this.onlandVisualTrashAssessmentAreaService.getAreaAsFeatureCollectionOnlandVisualTrashAssessmentArea(this.ovtaAreaID);
            this.featureCollection$ = this.trackLayerRequest$(request$).pipe(
                tap((transectLineFeatureCollection) => {
                    this.layer = new L.GeoJSON(transectLineFeatureCollection as any, {
                        style: this.ovtaAreaStyle,
                    });
                    this.initLayer();
                })
            );
        }
    }
}
