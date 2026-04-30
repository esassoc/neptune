import { Component, Input, ViewChild, inject } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { TreatmentBMPDto } from "src/app/shared/generated/model/treatment-bmp-dto";
import { IDeactivateComponent } from "src/app/shared/guards/unsaved-changes.guard";
import { TreatmentBmpLocationEditorComponent } from "src/app/shared/components/treatment-bmp-editors/location-editor/treatment-bmp-location-editor.component";

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

    @Input() treatmentBMPID?: number;

    @ViewChild(TreatmentBmpLocationEditorComponent) editor?: TreatmentBmpLocationEditorComponent;

    public canExit(): boolean {
        return this.editor?.canExit() ?? true;
    }

    public onSaved(bmp: TreatmentBMPDto): void {
        this.router.navigate(["/treatment-bmps", bmp.TreatmentBMPID]);
    }

    public onCancelled(): void {
        if (this.treatmentBMPID) {
            this.router.navigate(["/treatment-bmps", this.treatmentBMPID]);
        }
    }
}
