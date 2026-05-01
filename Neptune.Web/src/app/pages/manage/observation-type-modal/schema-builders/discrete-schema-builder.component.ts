import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { DiscreteValueSchema } from "src/app/shared/observation-types/schema-types";
import { MeasurementUnitTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/measurement-unit-type-enum";
import { PropertiesToObserveEditorComponent } from "./properties-to-observe-editor.component";
import { SchemaBuilderSection } from "./pass-fail-schema-builder.component";

@Component({
    selector: "discrete-schema-builder",
    standalone: true,
    imports: [FormsModule, FormFieldComponent, FieldDefinitionComponent, PropertiesToObserveEditorComponent],
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
                    <div class="g-col-6">
                        <field-definition fieldDefinitionType="MeasurementUnitLabel" labelOverride="Measurement Unit Label"></field-definition>
                        <input type="text" class="form-control" [(ngModel)]="schema.MeasurementUnitLabel" (ngModelChange)="emit()" placeholder="e.g. Sediment Depth">
                    </div>
                    <div class="g-col-6">
                        <form-field [formInputOptions]="unitOptions" [type]="FormFieldType.Select"
                            fieldLabel="Measurement Unit Type" fieldDefinitionName="MeasurementUnit"
                            [(ngModel)]="schema.MeasurementUnitTypeID" (ngModelChange)="emit()" placeholder="Select Unit"></form-field>
                    </div>
                    <div class="g-col-3">
                        <field-definition fieldDefinitionType="MinimumNumberOfObservations" labelOverride="Min # Observations"></field-definition>
                        <input type="number" class="form-control" [(ngModel)]="schema.MinimumNumberOfObservations" (ngModelChange)="emit()" min="1">
                    </div>
                    <div class="g-col-3">
                        <field-definition fieldDefinitionType="MaximumNumberOfObservations" labelOverride="Max # Observations"></field-definition>
                        <input type="number" class="form-control" [(ngModel)]="schema.MaximumNumberOfObservations" (ngModelChange)="emit()">
                    </div>
                    <div class="g-col-3">
                        <field-definition fieldDefinitionType="MinimumValueOfEachObservation" labelOverride="Min Value"></field-definition>
                        <input type="number" class="form-control" [(ngModel)]="schema.MinimumValueOfObservations" (ngModelChange)="emit()">
                    </div>
                    <div class="g-col-3">
                        <field-definition fieldDefinitionType="MaximumValueOfEachObservation" labelOverride="Max Value"></field-definition>
                        <input type="number" class="form-control" [(ngModel)]="schema.MaximumValueOfObservations" (ngModelChange)="emit()">
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
export class DiscreteSchemaBuilderComponent {
    @Input() schema: DiscreteValueSchema;
    @Input() section: SchemaBuilderSection = "instructions";
    @Output() schemaChange = new EventEmitter<DiscreteValueSchema>();
    public FormFieldType = FormFieldType;
    public unitOptions = MeasurementUnitTypesAsSelectDropdownOptions;
    emit(): void { this.schemaChange.emit({ ...this.schema }); }
}
