import { Component, inject } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WorkflowBodyComponent } from "src/app/shared/components/workflow-body/workflow-body.component";
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
    imports: [FormsModule, PageHeaderComponent, WorkflowBodyComponent],
    template: `
        <page-header pageTitle="Source Control"></page-header>
        <workflow-body [helpCustomRichTextTypeID]="service.stepHelpID('source-control')">
            @if (service.sourceControlRows().length) {
                <table class="table table-condensed table-bordered">
                    <thead>
                        <tr>
                            <th>Category</th>
                            <th>Attribute</th>
                            <th style="width: 80px">Present?</th>
                            <th style="width: 300px">Condition</th>
                        </tr>
                    </thead>
                    <tbody>
                        @for (row of service.sourceControlRows(); track row.sourceControlBMPID) {
                            <tr>
                                <td>{{ row.categoryName }}</td>
                                <td>{{ row.attributeName }}</td>
                                <td>{{ row.isPresent === true ? "Yes" : row.isPresent === false ? "No" : "" }}</td>
                                <td>
                                    <input type="text" class="form-control form-control-sm" [(ngModel)]="row.condition" [disabled]="readonly" maxlength="1000" (ngModelChange)="touched()">
                                </td>
                            </tr>
                        }
                    </tbody>
                </table>
            } @else {
                <p class="system-text">No source control BMPs are associated with this WQMP.</p>
            }

            @if (!readonly) {
                <div class="page-footer">
                    <button class="btn btn-primary-outline" (click)="service.save('source-control', false)" [disabled]="service.isSaving()">Save</button>
                    <button class="btn btn-primary" (click)="service.save('source-control', true)" [disabled]="service.isSaving()">Save and Continue</button>
                </div>
            }
        </workflow-body>
    `,
    styles: [`
        :host { display: block; }
    `],
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
