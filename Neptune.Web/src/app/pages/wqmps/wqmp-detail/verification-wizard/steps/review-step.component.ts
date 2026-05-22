import { Component, inject } from "@angular/core";
import { DatePipe } from "@angular/common";
import { SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WqmpVerificationWorkflowService } from "src/app/shared/services/wqmp-verification-workflow.service";

@Component({
    selector: "review-step",
    standalone: true,
    imports: [DatePipe, PageHeaderComponent],
    templateUrl: "./review-step.component.html",
    styleUrl: "./review-step.component.scss",
})
export class ReviewStepComponent {
    public service = inject(WqmpVerificationWorkflowService);

    getOptionLabel(options: SelectDropdownOption[], value: any): string {
        if (value == null) return "";
        return options?.find((o) => o.Value === value)?.Label ?? "";
    }
}
