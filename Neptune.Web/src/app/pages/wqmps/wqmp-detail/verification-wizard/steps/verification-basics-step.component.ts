import { Component, inject } from "@angular/core";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WqmpVerificationWorkflowService } from "src/app/shared/services/wqmp-verification-workflow.service";

export interface VerificationBasicsForm {
    WaterQualityManagementPlanVerifyTypeID: FormControl<number>;
    WaterQualityManagementPlanVisitStatusID: FormControl<number>;
    VerificationDate: FormControl<string>;
    WaterQualityManagementPlanVerifyStatusID: FormControl<number>;
    EnforcementOrFollowupActions: FormControl<string>;
    SourceControlCondition: FormControl<string>;
}

@Component({
    selector: "verification-basics-step",
    standalone: true,
    imports: [FormFieldComponent, ReactiveFormsModule, PageHeaderComponent],
    templateUrl: "./verification-basics-step.component.html",
    styleUrl: "./verification-basics-step.component.scss",
})
export class VerificationBasicsStepComponent {
    public service = inject(WqmpVerificationWorkflowService);
    public FormFieldType = FormFieldType;
}
