import { Component, Input, ViewChild, inject } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { IDeactivateComponent } from "src/app/shared/guards/unsaved-changes.guard";
import { TreatmentBmpImagesEditorComponent } from "src/app/shared/components/treatment-bmp-editors/images-editor/treatment-bmp-images-editor.component";

/**
 * Routed page wrapper that supplies page-header + back-to-BMP chrome around the
 * embedded TreatmentBmpImagesEditorComponent. The editor is also embedded inline
 * in the Field Visit workflow's Inventory step.
 */
@Component({
    selector: "treatment-bmp-update-images",
    standalone: true,
    imports: [PageHeaderComponent, RouterModule, AlertDisplayComponent, TreatmentBmpImagesEditorComponent],
    templateUrl: "./treatment-bmp-update-images.component.html",
    styleUrls: ["./treatment-bmp-update-images.component.scss"],
})
export class TreatmentBmpUpdateImagesComponent implements IDeactivateComponent {
    private router = inject(Router);

    @Input() treatmentBMPID?: number;

    @ViewChild(TreatmentBmpImagesEditorComponent) editor?: TreatmentBmpImagesEditorComponent;

    public canExit(): boolean {
        return this.editor?.canExit() ?? true;
    }

    public onSaved(): void {
        // Saved alert is pushed by the editor; stay on the page so the user can
        // continue tweaking captions or adding more photos.
    }

    public onCancelled(): void {
        if (this.treatmentBMPID != null) {
            this.router.navigate(["/treatment-bmps", this.treatmentBMPID]);
        }
    }
}
