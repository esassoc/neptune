import { Component, Input, ViewChild, inject } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { TreatmentBMPDto } from "src/app/shared/generated/model/treatment-bmp-dto";
import { IDeactivateComponent } from "src/app/shared/guards/unsaved-changes.guard";
import { TreatmentBmpLocationEditorComponent } from "src/app/shared/components/treatment-bmp-editors/location-editor/treatment-bmp-location-editor.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/**
 * Routed page wrapper that supplies the page-header + back-to-BMP chrome around
 * the embedded TreatmentBmpLocationEditorComponent. The editor is also embedded
 * inline in the Field Visit workflow's Inventory step so crews don't have to
 * round-trip out of the workflow to update a BMP's location.
 */
@Component({
    selector: "treatment-bmp-update-location",
    standalone: true,
    imports: [PageHeaderComponent, RouterModule, AlertDisplayComponent, TreatmentBmpLocationEditorComponent],
    templateUrl: "./treatment-bmp-update-location.component.html",
    styleUrls: ["./treatment-bmp-update-location.component.scss"],
})
export class TreatmentBmpUpdateLocationComponent implements IDeactivateComponent {
    private router = inject(Router);
    private alertService = inject(AlertService);

    @Input() treatmentBMPID?: number;

    @ViewChild(TreatmentBmpLocationEditorComponent) editor?: TreatmentBmpLocationEditorComponent;

    public canExit(): boolean {
        return this.editor?.canExit() ?? true;
    }

    public onSaved(bmp: TreatmentBMPDto): void {
        // Navigate first — this page's <app-alert-display> clears alerts on destroy,
        // so the success alert is pushed only after the BMP detail page mounts.
        this.router.navigate(["/treatment-bmps", bmp.TreatmentBMPID]).then(() => {
            this.alertService.pushAlert(new Alert("Treatment BMP location updated successfully.", AlertContext.Success));
        });
    }

    public onCancelled(): void {
        if (this.treatmentBMPID) {
            this.router.navigate(["/treatment-bmps", this.treatmentBMPID]);
        }
    }
}
