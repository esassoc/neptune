import { Component, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WorkflowBodyComponent } from "src/app/shared/components/workflow-body/workflow-body.component";
import { WqmpVerificationWorkflowService } from "src/app/shared/services/wqmp-verification-workflow.service";

@Component({
    selector: "simplified-bmps-step",
    standalone: true,
    imports: [FormsModule, PageHeaderComponent, WorkflowBodyComponent],
    template: `
        <page-header pageTitle="Simplified BMPs"></page-header>
        <workflow-body [helpCustomRichTextTypeID]="service.stepHelpID('simplified-bmps')">
            @if (service.quickBMPRows().length) {
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
                        @for (row of service.quickBMPRows(); track row.id) {
                            <tr>
                                <td>{{ row.name }}</td>
                                <td>{{ row.type }}</td>
                                <td>
                                    <div class="radio-group">
                                        <label><input type="radio" [name]="'qbmp-' + row.id" [value]="true" [(ngModel)]="row.isAdequate" [disabled]="readonly" (ngModelChange)="touched()"> Yes</label>
                                        <label><input type="radio" [name]="'qbmp-' + row.id" [value]="false" [(ngModel)]="row.isAdequate" [disabled]="readonly" (ngModelChange)="touched()"> No</label>
                                        <label><input type="radio" [name]="'qbmp-' + row.id" [value]="null" [(ngModel)]="row.isAdequate" [disabled]="readonly" (ngModelChange)="touched()"> N/A</label>
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
                <p class="system-text">No simplified BMPs are associated with this WQMP.</p>
            }

            @if (!readonly) {
                <div class="page-footer">
                    <button class="btn btn-primary-outline" (click)="service.save('simplified-bmps', false)" [disabled]="service.isSaving()">Save</button>
                    <button class="btn btn-primary" (click)="service.save('simplified-bmps', true)" [disabled]="service.isSaving()">Save and Continue</button>
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
export class SimplifiedBmpsStepComponent {
    public service = inject(WqmpVerificationWorkflowService);

    get readonly(): boolean {
        return this.service.mode() === "view";
    }

    touched(): void {
        this.service.quickBMPRows.update((rows) => rows.slice());
    }
}
