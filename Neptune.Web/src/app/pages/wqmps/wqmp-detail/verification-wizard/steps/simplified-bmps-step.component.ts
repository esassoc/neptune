import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { BMPChecklistRow } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/structural-bmps-step.component";

@Component({
    selector: "simplified-bmps-step",
    standalone: true,
    imports: [FormsModule],
    template: `
        <h3>Simplified BMP Assessment</h3>
        @if (rows.length) {
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
                    @for (row of rows; track row.id; let i = $index) {
                        <tr>
                            <td>{{ row.name }}</td>
                            <td>{{ row.type }}</td>
                            <td>
                                <div class="radio-group">
                                    <label><input type="radio" [name]="'qbmp-' + row.id" [value]="true" [(ngModel)]="row.isAdequate" [disabled]="readonly" (ngModelChange)="emitChange()"> Yes</label>
                                    <label><input type="radio" [name]="'qbmp-' + row.id" [value]="false" [(ngModel)]="row.isAdequate" [disabled]="readonly" (ngModelChange)="emitChange()"> No</label>
                                    <label><input type="radio" [name]="'qbmp-' + row.id" [value]="null" [(ngModel)]="row.isAdequate" [disabled]="readonly" (ngModelChange)="emitChange()"> N/A</label>
                                </div>
                            </td>
                            <td>
                                <input type="text" class="form-control form-control-sm" [(ngModel)]="row.note" [disabled]="readonly" maxlength="500" (ngModelChange)="emitChange()">
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        } @else {
            <p class="system-text">No simplified BMPs are associated with this WQMP.</p>
        }
    `,
    styles: [`
        .radio-group { display: flex; gap: 0.75rem; }
        .radio-group label { display: flex; align-items: center; gap: 0.25rem; font-weight: normal; cursor: pointer; }
    `],
})
export class SimplifiedBmpsStepComponent {
    @Input() rows: BMPChecklistRow[] = [];
    @Input() readonly = false;
    @Output() rowsChange = new EventEmitter<BMPChecklistRow[]>();

    emitChange(): void {
        this.rowsChange.emit([...this.rows]);
    }
}
