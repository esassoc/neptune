import { Component, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WqmpVerificationWorkflowService } from "src/app/shared/services/wqmp-verification-workflow.service";

@Component({
    selector: "simplified-bmps-step",
    standalone: true,
    imports: [FormsModule, PageHeaderComponent],
    templateUrl: "./simplified-bmps-step.component.html",
    styleUrl: "./simplified-bmps-step.component.scss",
})
export class SimplifiedBmpsStepComponent {
    public service = inject(WqmpVerificationWorkflowService);

    get readonly(): boolean {
        return this.service.mode() === "view";
    }

    touched(): void {
        this.service.quickBMPRows.update((rows) => rows.slice());
    }
}
