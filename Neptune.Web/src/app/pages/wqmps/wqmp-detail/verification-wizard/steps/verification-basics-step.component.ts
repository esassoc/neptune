import { Component, inject } from "@angular/core";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WorkflowBodyComponent } from "src/app/shared/components/workflow-body/workflow-body.component";
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
    imports: [FormFieldComponent, ReactiveFormsModule, PageHeaderComponent, WorkflowBodyComponent],
    template: `
        <page-header pageTitle="Verification Basics"></page-header>
        <workflow-body [helpCustomRichTextTypeID]="service.stepHelpID('basics')">
            <div class="grid-12">
                <form-field class="g-col-6" [formControl]="service.basicsForm.controls.WaterQualityManagementPlanVerifyTypeID"
                    fieldLabel="Verification Type" [type]="FormFieldType.Select" [required]="true"
                    placeholder="Select Type" [formInputOptions]="service.verifyTypeOptions">
                </form-field>
                <form-field class="g-col-6" [formControl]="service.basicsForm.controls.WaterQualityManagementPlanVisitStatusID"
                    fieldLabel="Visit Status" [type]="FormFieldType.Select" [required]="true"
                    placeholder="Select Visit Status" [formInputOptions]="service.visitStatusOptions">
                </form-field>
                <form-field class="g-col-6" [formControl]="service.basicsForm.controls.VerificationDate"
                    fieldLabel="Verification Date" [type]="FormFieldType.Date" [required]="true">
                </form-field>
                <form-field class="g-col-6" [formControl]="service.basicsForm.controls.WaterQualityManagementPlanVerifyStatusID"
                    fieldLabel="Verify Status" [type]="FormFieldType.Select"
                    placeholder="Select Status" [formInputOptions]="service.verifyStatusOptions">
                </form-field>
                <form-field class="g-col-12" [formControl]="service.basicsForm.controls.EnforcementOrFollowupActions"
                    fieldLabel="Enforcement or Follow-up Actions" [type]="FormFieldType.Textarea"
                    placeholder="Enter follow-up actions if applicable">
                </form-field>
                <form-field class="g-col-12" [formControl]="service.basicsForm.controls.SourceControlCondition"
                    fieldLabel="Source Control Condition Notes" [type]="FormFieldType.Textarea"
                    placeholder="Enter source control condition notes if applicable">
                </form-field>
            </div>

            @if (service.mode() !== 'view') {
                <div class="page-footer">
                    <button class="btn btn-primary-outline" (click)="service.save('basics', false)" [disabled]="service.isSaving()">Save</button>
                    <button class="btn btn-primary" (click)="service.save('basics', true)" [disabled]="service.isSaving()">Save and Continue</button>
                </div>
            }
        </workflow-body>
    `,
    styles: [`
        :host { display: block; }
        form-field { display: block; }
    `],
})
export class VerificationBasicsStepComponent {
    public service = inject(WqmpVerificationWorkflowService);
    public FormFieldType = FormFieldType;
}
