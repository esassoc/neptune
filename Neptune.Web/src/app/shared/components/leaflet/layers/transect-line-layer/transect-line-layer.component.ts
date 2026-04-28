import { Component, Input, OnChanges } from "@angular/core";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";

@Component({
    selector: "transect-line-layer",
    imports: [],
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

    ngAfterViewInit(): void {
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
        // Subscribe imperatively rather than via `featureCollection$ | async` in the template.
        // In zoneless production builds the late assignment after ngAfterViewInit doesn't trigger
        // a second template check, so the async-pipe subscription never wires up and the request
        // never fires until something else (e.g. a user click) forces change detection.
        const request$ = this.ovtaID
            ? this.onlandVisualTrashAssessmentService.getTransectLineAsFeatureCollectionOnlandVisualTrashAssessment(this.ovtaID)
            : this.ovtaAreaID
              ? this.onlandVisualTrashAssessmentAreaService.getTransectLineAsFeatureCollectionOnlandVisualTrashAssessmentArea(this.ovtaAreaID)
              : null;
        if (!request$) return;
        this.trackLayerRequest$(request$).subscribe((featureCollection) => {
            this.layer = new L.GeoJSON(featureCollection as any, {
                style: this.transectLineStyle,
            });
            this.initLayer();
        });
    }
}
