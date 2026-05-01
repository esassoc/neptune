import { Component, inject } from "@angular/core";
import { DatePipe } from "@angular/common";
import { SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WorkflowBodyComponent } from "src/app/shared/components/workflow-body/workflow-body.component";
import { WqmpVerificationWorkflowService } from "src/app/shared/services/wqmp-verification-workflow.service";

@Component({
    selector: "review-step",
    standalone: true,
    imports: [DatePipe, PageHeaderComponent, WorkflowBodyComponent],
    template: `
        <page-header pageTitle="Review & Finalize"></page-header>
        <workflow-body [helpCustomRichTextTypeID]="service.stepHelpID('review-and-finalize')">
            <div class="review-section">
                <h4>Basics</h4>
                <dl class="grid-12">
                    <dt class="g-col-4">Verification Type</dt>
                    <dd class="g-col-8">{{ getOptionLabel(service.verifyTypeOptions, service.basicsForm.controls.WaterQualityManagementPlanVerifyTypeID.value) }}</dd>
                    <dt class="g-col-4">Visit Status</dt>
                    <dd class="g-col-8">{{ getOptionLabel(service.visitStatusOptions, service.basicsForm.controls.WaterQualityManagementPlanVisitStatusID.value) }}</dd>
                    <dt class="g-col-4">Verification Date</dt>
                    <dd class="g-col-8">{{ service.basicsForm.controls.VerificationDate.value | date: "MM/dd/yyyy" }}</dd>
                    <dt class="g-col-4">Verify Status</dt>
                    <dd class="g-col-8">{{ getOptionLabel(service.verifyStatusOptions, service.basicsForm.controls.WaterQualityManagementPlanVerifyStatusID.value) || "Not set" }}</dd>
                </dl>
            </div>

            @if (service.treatmentBMPRows().length) {
                <div class="review-section">
                    <h4>Structural BMPs ({{ service.treatmentBMPRows().length }})</h4>
                    <table class="table table-condensed">
                        <thead><tr><th>Name</th><th>Adequate?</th><th>Notes</th></tr></thead>
                        <tbody>
                            @for (row of service.treatmentBMPRows(); track row.id) {
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

            @if (service.quickBMPRows().length) {
                <div class="review-section">
                    <h4>Simplified BMPs ({{ service.quickBMPRows().length }})</h4>
                    <table class="table table-condensed">
                        <thead><tr><th>Name</th><th>Adequate?</th><th>Notes</th></tr></thead>
                        <tbody>
                            @for (row of service.quickBMPRows(); track row.id) {
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

            @if (service.sourceControlRows().length) {
                <div class="review-section">
                    <h4>Source Control BMPs</h4>
                    @for (row of service.sourceControlRows(); track row.sourceControlBMPID) {
                        @if (row.condition) {
                            <div class="review-item">
                                <strong>{{ row.attributeName }}</strong>: {{ row.condition }}
                            </div>
                        }
                    }
                </div>
            }

            @if (service.mode() !== 'view') {
                <div class="page-footer">
                    <button class="btn btn-primary-outline" (click)="service.save('review-and-finalize', false)" [disabled]="service.isSaving()">Save</button>
                    <button class="btn btn-primary" (click)="service.finalize()" [disabled]="service.isSaving()">Finalize</button>
                </div>
            }
        </workflow-body>
    `,
    styles: [`
        :host { display: block; }
        .review-section { margin-bottom: 1.5rem; }
        .review-section h4 { margin-bottom: 0.5rem; }
        .review-item { padding: 0.25rem 0; }
    `],
})
export class ReviewStepComponent {
    public service = inject(WqmpVerificationWorkflowService);

    getOptionLabel(options: SelectDropdownOption[], value: any): string {
        if (value == null) return "";
        return options?.find((o) => o.Value === value)?.Label ?? "";
    }
}
