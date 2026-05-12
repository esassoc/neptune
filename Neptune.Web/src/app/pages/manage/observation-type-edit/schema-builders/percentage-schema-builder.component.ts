import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { PercentageSchema } from "src/app/shared/observation-types/schema-types";
import { PropertiesToObserveEditorComponent } from "./properties-to-observe-editor.component";
import { SchemaBuilderSection } from "./pass-fail-schema-builder.component";

@Component({
    selector: "percentage-schema-builder",
    standalone: true,
    imports: [FormsModule, FieldDefinitionComponent, PropertiesToObserveEditorComponent],
    template: `
        @switch (section) {
            @case ("instructions") {
                <div class="grid-12">
                    <div class="g-col-12">
                        <label class="field-label">Benchmark Instruction</label>
                        <textarea class="form-control" rows="2" [(ngModel)]="schema.BenchmarkDescription" (ngModelChange)="emit()" placeholder="Benchmark instructions"></textarea>
                    </div>
                    <div class="g-col-12">
                        <label class="field-label">Threshold Instruction</label>
                        <textarea class="form-control" rows="2" [(ngModel)]="schema.ThresholdDescription" (ngModelChange)="emit()" placeholder="Threshold instructions"></textarea>
                    </div>
                    <div class="g-col-12">
                        <label class="field-label">Assessment Instruction</label>
                        <textarea class="form-control" rows="2" [(ngModel)]="schema.AssessmentDescription" (ngModelChange)="emit()" placeholder="General assessment instructions"></textarea>
                    </div>
                </div>
            }
            @case ("labelsUnits") {
                <div class="grid-12">
                    <div class="g-col-12">
                        <field-definition fieldDefinitionType="MeasurementUnitLabel" labelOverride="Measurement Unit Label" [inline]="true"></field-definition>
                        <input type="text" class="form-control" [(ngModel)]="schema.MeasurementUnitLabel" (ngModelChange)="emit()" placeholder="e.g. Percent Cover">
                    </div>
                    <div class="g-col-12">
                        <properties-to-observe-editor [properties]="schema.PropertiesToObserve" (propertiesChange)="schema.PropertiesToObserve = $event; emit()"></properties-to-observe-editor>
                    </div>
                </div>
            }
        }
    `,
    styles: [`.field-label { font-weight: 600; font-size: 0.875rem; display: block; margin-bottom: 0.25rem; }`],
})
export class PercentageSchemaBuilderComponent {
    @Input() schema: PercentageSchema;
    @Input() section: SchemaBuilderSection = "instructions";
    @Output() schemaChange = new EventEmitter<PercentageSchema>();
    emit(): void { this.schemaChange.emit({ ...this.schema }); }
}
