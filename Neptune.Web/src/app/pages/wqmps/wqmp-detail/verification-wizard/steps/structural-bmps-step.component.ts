import { Component, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WorkflowBodyComponent } from "src/app/shared/components/workflow-body/workflow-body.component";
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
    imports: [FormsModule, PageHeaderComponent, WorkflowBodyComponent],
    template: `
        <page-header pageTitle="Structural BMPs"></page-header>
        <workflow-body [helpCustomRichTextTypeID]="service.stepHelpID('structural-bmps')">
            @if (service.treatmentBMPRows().length) {
                <table class="table table-condensed table-bordered">
                    <thead>
                        <tr>
                            <th>BMP Name</th>
                            <th>Type</th>
                            <th style="width: 200px">O&M Adequate?</th>
                            <th style="width: 250px">Notes</th>
                        </tr>
                    </thead>
                    <tbody>
                        @for (row of service.treatmentBMPRows(); track row.id) {
                            <tr>
                                <td>{{ row.name }}</td>
                                <td>{{ row.type }}</td>
                                <td>
                                    <div class="radio-group">
                                        <label><input type="radio" [name]="'tbmp-' + row.id" [value]="true" [(ngModel)]="row.isAdequate" [disabled]="readonly" (ngModelChange)="touched()"> Yes</label>
                                        <label><input type="radio" [name]="'tbmp-' + row.id" [value]="false" [(ngModel)]="row.isAdequate" [disabled]="readonly" (ngModelChange)="touched()"> No</label>
                                        <label><input type="radio" [name]="'tbmp-' + row.id" [value]="null" [(ngModel)]="row.isAdequate" [disabled]="readonly" (ngModelChange)="touched()"> N/A</label>
                                    </div>
                                </td>
                                <td>
                                    <input type="text" class="form-control form-control-sm" [(ngModel)]="row.note" [disabled]="readonly" maxlength="500" (ngModelChange)="touched()">
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            } @else {
                <p class="system-text">No structural BMPs are associated with this WQMP.</p>
            }

            @if (!readonly) {
                <div class="page-footer">
                    <button class="btn btn-primary-outline" (click)="service.save('structural-bmps', false)" [disabled]="service.isSaving()">Save</button>
                    <button class="btn btn-primary" (click)="service.save('structural-bmps', true)" [disabled]="service.isSaving()">Save and Continue</button>
                </div>
            }
        </workflow-body>
    `,
    styles: [`
        :host { display: block; }
        .radio-group { display: flex; gap: 0.75rem; }
        .radio-group label { display: flex; align-items: center; gap: 0.25rem; font-weight: normal; cursor: pointer; }
    `],
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
