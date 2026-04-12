import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";

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
    imports: [FormsModule],
    template: `
        <h3>Source Control BMP Assessment</h3>
        @if (rows.length) {
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
                    @for (row of rows; track row.sourceControlBMPID) {
                        <tr>
                            <td>{{ row.categoryName }}</td>
                            <td>{{ row.attributeName }}</td>
                            <td>{{ row.isPresent === true ? "Yes" : row.isPresent === false ? "No" : "" }}</td>
                            <td>
                                <input type="text" class="form-control form-control-sm" [(ngModel)]="row.condition" [disabled]="readonly" maxlength="1000" (ngModelChange)="emitChange()">
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        } @else {
            <p class="system-text">No source control BMPs are associated with this WQMP.</p>
        }
    `,
})
export class SourceControlStepComponent {
    @Input() rows: SourceControlRow[] = [];
    @Input() readonly = false;
    @Output() rowsChange = new EventEmitter<SourceControlRow[]>();

    emitChange(): void {
        this.rowsChange.emit([...this.rows]);
    }
}
