import { Component, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WqmpVerificationWorkflowService } from "src/app/shared/services/wqmp-verification-workflow.service";

export interface SourceControlRow {
    sourceControlBMPID: number;
    attributeName: string;
    categoryName: string;
    isPresent: boolean | null;
    condition: string;
}

@Component({
    selector: "source-control-step",
    standalone: true,
    imports: [FormsModule, PageHeaderComponent],
    templateUrl: "./source-control-step.component.html",
    styleUrl: "./source-control-step.component.scss",
})
export class SourceControlStepComponent {
    public service = inject(WqmpVerificationWorkflowService);

    get readonly(): boolean {
        return this.service.mode() === "view";
    }

    touched(): void {
        this.service.sourceControlRows.update((rows) => rows.slice());
    }
}
