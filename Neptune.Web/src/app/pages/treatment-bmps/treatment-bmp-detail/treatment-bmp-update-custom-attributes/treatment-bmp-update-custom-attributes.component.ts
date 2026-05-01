import { Component, Input, ViewChild, inject } from "@angular/core";
import { Router, RouterModule } from "@angular/router";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { CustomAttributeTypePurposes } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { IDeactivateComponent } from "src/app/shared/guards/unsaved-changes.guard";
import { TreatmentBmpCustomAttributesFormComponent } from "src/app/shared/components/treatment-bmp-editors/custom-attributes-form/treatment-bmp-custom-attributes-form.component";

/**
 * Routed page wrapper that supplies page-header + back-to-BMP chrome around the
 * embedded TreatmentBmpCustomAttributesFormComponent. The form is also embedded
 * inline in the Field Visit workflow's Inventory step.
 */
@Component({
    selector: "treatment-bmp-update-custom-attributes",
    standalone: true,
    imports: [PageHeaderComponent, RouterModule, AlertDisplayComponent, TreatmentBmpCustomAttributesFormComponent],
    templateUrl: "./treatment-bmp-update-custom-attributes.component.html",
    styleUrl: "./treatment-bmp-update-custom-attributes.component.scss",
})
export class TreatmentBmpUpdateCustomAttributesComponent implements IDeactivateComponent {
    private router = inject(Router);

    @Input() treatmentBMPID?: number;
    @Input() customAttributePurposeID?: number;

    @ViewChild(TreatmentBmpCustomAttributesFormComponent) editor?: TreatmentBmpCustomAttributesFormComponent;

    public get purposeName(): string | undefined {
        return CustomAttributeTypePurposes.find((x) => x.Value == this.customAttributePurposeID)?.DisplayName;
    }

    public canExit(): boolean {
        return this.editor?.canExit() ?? true;
    }

    public onSaved(): void {
        if (this.treatmentBMPID != null) {
            this.router.navigate(["/treatment-bmps", this.treatmentBMPID]);
        }
    }

    public onCancelled(): void {
        if (this.treatmentBMPID != null) {
            this.router.navigate(["/treatment-bmps", this.treatmentBMPID]);
        }
    }
}
