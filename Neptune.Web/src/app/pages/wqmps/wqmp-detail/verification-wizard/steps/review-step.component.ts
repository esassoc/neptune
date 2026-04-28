import { Component, Input } from "@angular/core";
import { FormGroup } from "@angular/forms";
import { SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { VerificationBasicsForm } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/verification-basics-step.component";
import { BMPChecklistRow } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/structural-bmps-step.component";
import { SourceControlRow } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/source-control-step.component";

@Component({
    selector: "review-step",
    standalone: true,
    imports: [],
    template: `
        <h3>Review & Finalize</h3>

        <div class="review-section">
            <h4>Basics</h4>
            <dl class="grid-12">
                <dt class="g-col-4">Verification Type</dt>
                <dd class="g-col-8">{{ getOptionLabel(verifyTypeOptions, basicsForm.controls.WaterQualityManagementPlanVerifyTypeID.value) }}</dd>
                <dt class="g-col-4">Visit Status</dt>
                <dd class="g-col-8">{{ getOptionLabel(visitStatusOptions, basicsForm.controls.WaterQualityManagementPlanVisitStatusID.value) }}</dd>
                <dt class="g-col-4">Verification Date</dt>
                <dd class="g-col-8">{{ basicsForm.controls.VerificationDate.value }}</dd>
                <dt class="g-col-4">Verify Status</dt>
                <dd class="g-col-8">{{ getOptionLabel(verifyStatusOptions, basicsForm.controls.WaterQualityManagementPlanVerifyStatusID.value) || "Not set" }}</dd>
            </dl>
        </div>

        @if (treatmentBMPRows.length) {
            <div class="review-section">
                <h4>Structural BMPs ({{ treatmentBMPRows.length }})</h4>
                <table class="table table-condensed">
                    <thead><tr><th>Name</th><th>Adequate?</th><th>Notes</th></tr></thead>
                    <tbody>
                        @for (row of treatmentBMPRows; track row.id) {
                            <tr>
                                <td>{{ row.name }}</td>
                                <td>{{ row.isAdequate === true ? "Yes" : row.isAdequate === false ? "No" : "N/A" }}</td>
                                <td>{{ row.note }}</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        }

        @if (quickBMPRows.length) {
            <div class="review-section">
                <h4>Simplified BMPs ({{ quickBMPRows.length }})</h4>
                <table class="table table-condensed">
                    <thead><tr><th>Name</th><th>Adequate?</th><th>Notes</th></tr></thead>
                    <tbody>
                        @for (row of quickBMPRows; track row.id) {
                            <tr>
                                <td>{{ row.name }}</td>
                                <td>{{ row.isAdequate === true ? "Yes" : row.isAdequate === false ? "No" : "N/A" }}</td>
                                <td>{{ row.note }}</td>
                            </tr>
                        }
                    </tbody>
                </table>
            </div>
        }

        @if (sourceControlRows.length) {
            <div class="review-section">
                <h4>Source Control BMPs</h4>
                @for (row of sourceControlRows; track row.sourceControlBMPID) {
                    @if (row.condition) {
                        <div class="review-item">
                            <strong>{{ row.attributeName }}</strong>: {{ row.condition }}
                        </div>
                    }
                }
            </div>
        }
    `,
    styles: [`
        .review-section { margin-bottom: 1.5rem; }
        .review-section h4 { margin-bottom: 0.5rem; }
        .review-item { padding: 0.25rem 0; }
    `],
})
export class ReviewStepComponent {
    @Input() basicsForm: FormGroup<VerificationBasicsForm>;
    @Input() verifyTypeOptions: SelectDropdownOption[];
    @Input() visitStatusOptions: SelectDropdownOption[];
    @Input() verifyStatusOptions: SelectDropdownOption[];
    @Input() treatmentBMPRows: BMPChecklistRow[] = [];
    @Input() quickBMPRows: BMPChecklistRow[] = [];
    @Input() sourceControlRows: SourceControlRow[] = [];

    getOptionLabel(options: SelectDropdownOption[], value: any): string {
        if (value == null) return "";
        return options?.find((o) => o.Value === value)?.Label ?? "";
    }
}
