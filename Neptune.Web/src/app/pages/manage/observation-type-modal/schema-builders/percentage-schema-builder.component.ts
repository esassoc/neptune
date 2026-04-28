import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { PercentageSchema } from "src/app/pages/manage/observation-type-modal/schema-types";
import { PropertiesToObserveEditorComponent } from "./properties-to-observe-editor.component";

@Component({
    selector: "percentage-schema-builder",
    standalone: true,
    imports: [FormsModule, PropertiesToObserveEditorComponent],
    template: `
        <div class="grid-12">
            <div class="g-col-12">
                <label class="field-label">Measurement Unit Label</label>
                <input type="text" class="form-control" [(ngModel)]="schema.MeasurementUnitLabel" (ngModelChange)="emit()" placeholder="e.g. Percent Cover">
            </div>
            <div class="g-col-12">
                <label class="field-label">Benchmark Description</label>
                <textarea class="form-control" rows="2" [(ngModel)]="schema.BenchmarkDescription" (ngModelChange)="emit()" placeholder="Benchmark instructions"></textarea>
            </div>
            <div class="g-col-12">
                <label class="field-label">Threshold Description</label>
                <textarea class="form-control" rows="2" [(ngModel)]="schema.ThresholdDescription" (ngModelChange)="emit()" placeholder="Threshold instructions"></textarea>
            </div>
            <div class="g-col-12">
                <label class="field-label">Assessment Description</label>
                <textarea class="form-control" rows="2" [(ngModel)]="schema.AssessmentDescription" (ngModelChange)="emit()" placeholder="General assessment instructions"></textarea>
            </div>
            <div class="g-col-12">
                <properties-to-observe-editor [properties]="schema.PropertiesToObserve" (propertiesChange)="schema.PropertiesToObserve = $event; emit()"></properties-to-observe-editor>
            </div>
        </div>
    `,
    styles: [`.field-label { font-weight: 600; font-size: 0.875rem; display: block; margin-bottom: 0.25rem; }`],
})
export class PercentageSchemaBuilderComponent {
    @Input() schema: PercentageSchema;
    @Output() schemaChange = new EventEmitter<PercentageSchema>();
    emit(): void { this.schemaChange.emit({ ...this.schema }); }
}
