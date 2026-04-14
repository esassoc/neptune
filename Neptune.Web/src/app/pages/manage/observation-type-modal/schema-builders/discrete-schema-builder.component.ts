import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { DiscreteValueSchema } from "src/app/pages/manage/observation-type-modal/schema-types";
import { MeasurementUnitTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/measurement-unit-type-enum";
import { PropertiesToObserveEditorComponent } from "./properties-to-observe-editor.component";

@Component({
    selector: "discrete-schema-builder",
    standalone: true,
    imports: [FormsModule, FormFieldComponent, PropertiesToObserveEditorComponent],
    template: `
        <div class="grid-12">
            <div class="g-col-6">
                <label class="field-label">Measurement Unit Label</label>
                <input type="text" class="form-control" [(ngModel)]="schema.MeasurementUnitLabel" (ngModelChange)="emit()" placeholder="e.g. Sediment Depth">
            </div>
            <div class="g-col-6">
                <label class="field-label">Measurement Unit Type</label>
                <form-field [formInputOptions]="unitOptions" [type]="FormFieldType.Select" [(ngModel)]="schema.MeasurementUnitTypeID" (ngModelChange)="emit()" placeholder="Select Unit"></form-field>
            </div>
            <div class="g-col-3">
                <label class="field-label">Min # Observations</label>
                <input type="number" class="form-control" [(ngModel)]="schema.MinimumNumberOfObservations" (ngModelChange)="emit()" min="1">
            </div>
            <div class="g-col-3">
                <label class="field-label">Max # Observations</label>
                <input type="number" class="form-control" [(ngModel)]="schema.MaximumNumberOfObservations" (ngModelChange)="emit()">
            </div>
            <div class="g-col-3">
                <label class="field-label">Min Value</label>
                <input type="number" class="form-control" [(ngModel)]="schema.MinimumValueOfObservations" (ngModelChange)="emit()">
            </div>
            <div class="g-col-3">
                <label class="field-label">Max Value</label>
                <input type="number" class="form-control" [(ngModel)]="schema.MaximumValueOfObservations" (ngModelChange)="emit()">
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
export class DiscreteSchemaBuilderComponent {
    @Input() schema: DiscreteValueSchema;
    @Output() schemaChange = new EventEmitter<DiscreteValueSchema>();
    public FormFieldType = FormFieldType;
    public unitOptions = MeasurementUnitTypesAsSelectDropdownOptions;
    emit(): void { this.schemaChange.emit({ ...this.schema }); }
}
