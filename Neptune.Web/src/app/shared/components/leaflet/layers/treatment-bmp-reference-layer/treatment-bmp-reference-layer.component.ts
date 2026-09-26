import { AfterViewInit, Component, Input } from "@angular/core";
import * as L from "leaflet";
import { MapLayerBase } from "../map-layer-base.component";
import { MarkerHelper } from "src/app/shared/helpers/marker-helper";
import { TreatmentBMPMinimalDto } from "src/app/shared/generated/model/treatment-bmp-minimal-dto";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

/**
 * Read-only reference markers built from an in-memory list of BMPs (no API call), e.g. the BMPs
 * already associated with a WQMP while placing a new one. BMPs without coordinates are skipped.
 */
@Component({
    selector: "treatment-bmp-reference-layer",
    templateUrl: "./treatment-bmp-reference-layer.component.html",
})
export class TreatmentBMPReferenceLayerComponent extends MapLayerBase implements AfterViewInit {
    @Input() treatmentBMPs: TreatmentBMPMinimalDto[] = [];
    @Input() layerLabel: string = "Existing BMPs";

    public layer: L.FeatureGroup = L.featureGroup();

    ngAfterViewInit(): void {
        (this.treatmentBMPs ?? [])
            .filter((bmp) => bmp.Latitude != null && bmp.Longitude != null)
            .forEach((bmp) => {
                // Leaflet popups are raw HTML; escape the user-editable name/type to prevent stored XSS.
                const name = escapeHtml(bmp.TreatmentBMPName ?? "");
                const type = escapeHtml(bmp.TreatmentBMPTypeName ?? "");
                L.marker([bmp.Latitude, bmp.Longitude], { icon: MarkerHelper.inventoriedTreatmentBMPMarker })
                    .bindPopup(`<b>Name:</b> <a target="_blank" rel="noopener noreferrer" href="/treatment-bmps/${bmp.TreatmentBMPID}">${name}</a><br>` + `<b>Type:</b> ${type}`)
                    .addTo(this.layer);
            });
        this.initLayer();
    }
}
