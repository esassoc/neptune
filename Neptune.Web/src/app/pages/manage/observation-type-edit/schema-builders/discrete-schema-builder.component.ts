import { Component, Input } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { DiscreteValueSchema } from "src/app/shared/observation-types/schema-types";
import { MeasurementUnitTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/measurement-unit-type-enum";
import { PropertiesToObserveEditorComponent } from "./properties-to-observe-editor.component";
import { SchemaBuilderSection } from "./pass-fail-schema-builder.component";

export type DiscreteSchemaFormGroup = FormGroup<{
    BenchmarkDescription: FormControl<string>;
    ThresholdDescription: FormControl<string>;
    AssessmentDescription: FormControl<string>;
    MeasurementUnitLabel: FormControl<string>;
    MeasurementUnitTypeID: FormControl<number | null>;
    MinimumNumberOfObservations: FormControl<number>;
    MaximumNumberOfObservations: FormControl<number | null>;
    MinimumValueOfObservations: FormControl<number>;
    MaximumValueOfObservations: FormControl<number | null>;
    PropertiesToObserve: FormControl<string[]>;
}>;

/** Build a reactive FormGroup that matches the DiscreteValueSchema shape. */
export function buildDiscreteSchemaFormGroup(initial: DiscreteValueSchema): DiscreteSchemaFormGroup {
    return new FormGroup({
        BenchmarkDescription: new FormControl<string>(initial.BenchmarkDescription ?? "", { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
        ThresholdDescription: new FormControl<string>(initial.ThresholdDescription ?? "", { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
        AssessmentDescription: new FormControl<string>(initial.AssessmentDescription ?? "", { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
        MeasurementUnitLabel: new FormControl<string>(initial.MeasurementUnitLabel ?? "", { nonNullable: true, validators: [Validators.required] }),
        MeasurementUnitTypeID: new FormControl<number | null>(initial.MeasurementUnitTypeID ?? null, { validators: [Validators.required] }),
        MinimumNumberOfObservations: new FormControl<number>(initial.MinimumNumberOfObservations ?? 1, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
        MaximumNumberOfObservations: new FormControl<number | null>(initial.MaximumNumberOfObservations ?? null),
        MinimumValueOfObservations: new FormControl<number>(initial.MinimumValueOfObservations ?? 0, { nonNullable: true, validators: [Validators.required] }),
        MaximumValueOfObservations: new FormControl<number | null>(initial.MaximumValueOfObservations ?? null),
        PropertiesToObserve: new FormControl<string[]>(initial.PropertiesToObserve ?? [], { nonNullable: true, validators: [Validators.required] }),
    });
}

@Component({
    selector: "discrete-schema-builder",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, PropertiesToObserveEditorComponent],
    template: `
        @switch (section) {
            @case ("instructions") {
                <div class="grid-12">
                    <form-field class="g-col-12"
                        fieldLabel="Benchmark Instruction"
                        [type]="FormFieldType.Textarea"
                        [formControl]="formGroup.controls.BenchmarkDescription"
                        [required]="true"
                        placeholder="Benchmark instructions"></form-field>
                    <form-field class="g-col-12"
                        fieldLabel="Threshold Instruction"
                        [type]="FormFieldType.Textarea"
                        [formControl]="formGroup.controls.ThresholdDescription"
                        [required]="true"
                        placeholder="Threshold instructions"></form-field>
                    <form-field class="g-col-12"
                        fieldLabel="Assessment Instruction"
                        [type]="FormFieldType.Textarea"
                        [formControl]="formGroup.controls.AssessmentDescription"
                        [required]="true"
                        placeholder="General assessment instructions"></form-field>
                </div>
            }
            @case ("labelsUnits") {
                <div class="grid-12">
                    <form-field class="g-col-6"
                        fieldLabel="Measurement Unit Label"
                        fieldDefinitionName="MeasurementUnitLabel"
                        [type]="FormFieldType.Text"
                        [formControl]="formGroup.controls.MeasurementUnitLabel"
                        [required]="true"
                        placeholder="e.g. Sediment Depth"></form-field>
                    <form-field class="g-col-6"
                        fieldLabel="Measurement Unit Type"
                        fieldDefinitionName="MeasurementUnit"
                        [type]="FormFieldType.Select"
                        [formInputOptions]="unitOptions"
                        [formControl]="formGroup.controls.MeasurementUnitTypeID"
                        [required]="true"
                        placeholder="Select Unit"></form-field>
                    <form-field class="g-col-3"
                        fieldLabel="Min # Observations"
                        fieldDefinitionName="MinimumNumberOfObservations"
                        [type]="FormFieldType.Number"
                        [formControl]="formGroup.controls.MinimumNumberOfObservations"
                        [required]="true"></form-field>
                    <form-field class="g-col-3"
                        fieldLabel="Max # Observations"
                        fieldDefinitionName="MaximumNumberOfObservations"
                        [type]="FormFieldType.Number"
                        [formControl]="formGroup.controls.MaximumNumberOfObservations"></form-field>
                    <form-field class="g-col-3"
                        fieldLabel="Min Value"
                        fieldDefinitionName="MinimumValueOfEachObservation"
                        [type]="FormFieldType.Number"
                        [formControl]="formGroup.controls.MinimumValueOfObservations"
                        [required]="true"></form-field>
                    <form-field class="g-col-3"
                        fieldLabel="Max Value"
                        fieldDefinitionName="MaximumValueOfEachObservation"
                        [type]="FormFieldType.Number"
                        [formControl]="formGroup.controls.MaximumValueOfObservations"></form-field>
                    <div class="g-col-12">
                        <properties-to-observe-editor [control]="formGroup.controls.PropertiesToObserve"></properties-to-observe-editor>
                    </div>
                </div>
            }
        }
    `,
})
export class DiscreteSchemaBuilderComponent {
    @Input({ required: true }) formGroup!: DiscreteSchemaFormGroup;
    @Input() section: SchemaBuilderSection = "instructions";
    public FormFieldType = FormFieldType;
    public unitOptions = MeasurementUnitTypesAsSelectDropdownOptions;
}
