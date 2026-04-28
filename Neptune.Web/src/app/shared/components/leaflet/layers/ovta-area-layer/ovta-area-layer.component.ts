import { Component, Input, OnChanges } from "@angular/core";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";

@Component({
    selector: "ovta-area-layer",
    imports: [],
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

    ngAfterViewInit(): void {
        // Subscribe imperatively rather than via `featureCollection$ | async` in the template.
        // In zoneless production builds the late assignment after ngAfterViewInit doesn't trigger
        // a second template check, so the async-pipe subscription never wires up and the request
        // never fires until something else (e.g. a user click) forces change detection.
        const request$ = this.ovtaID
            ? this.onlandVisualTrashAssessmentService.getAreaAsFeatureCollectionOnlandVisualTrashAssessment(this.ovtaID)
            : this.ovtaAreaID
              ? this.onlandVisualTrashAssessmentAreaService.getAreaAsFeatureCollectionOnlandVisualTrashAssessmentArea(this.ovtaAreaID)
              : null;
        if (!request$) return;
        this.trackLayerRequest$(request$).subscribe((featureCollection) => {
            this.layer = new L.GeoJSON(featureCollection as any, {
                style: this.ovtaAreaStyle,
            });
            this.initLayer();
        });
    }
}
