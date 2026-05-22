import { Component, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WqmpVerificationWorkflowService } from "src/app/shared/services/wqmp-verification-workflow.service";

export interface BMPChecklistRow {
    id: number;
    name: string;
    type: string;
    isAdequate: boolean | null;
    note: string;
}

@Component({
    selector: "structural-bmps-step",
    standalone: true,
    imports: [FormsModule, PageHeaderComponent],
    templateUrl: "./structural-bmps-step.component.html",
    styleUrl: "./structural-bmps-step.component.scss",
})
export class StructuralBmpsStepComponent {
    public service = inject(WqmpVerificationWorkflowService);

    get readonly(): boolean {
        return this.service.mode() === "view";
    }

    /** ngModel mutates row objects in place; bump the signal so dependent computeds (progress) re-run. */
    touched(): void {
        this.service.treatmentBMPRows.update((rows) => rows.slice());
    }
}
