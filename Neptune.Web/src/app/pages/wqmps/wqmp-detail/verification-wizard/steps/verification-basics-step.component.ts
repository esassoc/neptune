import { Component, Input } from "@angular/core";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";

@Component({
    selector: "verification-basics-step",
    standalone: true,
    imports: [FormFieldComponent, ReactiveFormsModule],
    template: `
        <h3>Verification Basics</h3>
        <div class="grid-12">
            <form-field class="g-col-6" [formControl]="form.controls.WaterQualityManagementPlanVerifyTypeID"
                fieldLabel="Verification Type" [type]="FormFieldType.Select" [required]="true"
                placeholder="Select Type" [formInputOptions]="verifyTypeOptions">
            </form-field>
            <form-field class="g-col-6" [formControl]="form.controls.WaterQualityManagementPlanVisitStatusID"
                fieldLabel="Visit Status" [type]="FormFieldType.Select" [required]="true"
                placeholder="Select Visit Status" [formInputOptions]="visitStatusOptions">
            </form-field>
            <form-field class="g-col-6" [formControl]="form.controls.VerificationDate"
                fieldLabel="Verification Date" [type]="FormFieldType.Date" [required]="true">
            </form-field>
            <form-field class="g-col-6" [formControl]="form.controls.WaterQualityManagementPlanVerifyStatusID"
                fieldLabel="Verify Status" [type]="FormFieldType.Select"
                placeholder="Select Status" [formInputOptions]="verifyStatusOptions">
            </form-field>
            <form-field class="g-col-12" [formControl]="form.controls.EnforcementOrFollowupActions"
                fieldLabel="Enforcement or Follow-up Actions" [type]="FormFieldType.Textarea"
                placeholder="Enter follow-up actions if applicable">
            </form-field>
            <form-field class="g-col-12" [formControl]="form.controls.SourceControlCondition"
                fieldLabel="Source Control Condition Notes" [type]="FormFieldType.Textarea"
                placeholder="Enter source control condition notes if applicable">
            </form-field>
        </div>
    `,
    styles: [`
        @use "/src/scss/abstracts" as *;
        :host { display: block; }
        form-field { display: block; }
    `],
})
export class VerificationBasicsStepComponent {
    @Input() form: FormGroup;
    @Input() verifyTypeOptions: SelectDropdownOption[];
    @Input() visitStatusOptions: SelectDropdownOption[];
    @Input() verifyStatusOptions: SelectDropdownOption[];
    @Input() readonly = false;
    public FormFieldType = FormFieldType;
}
