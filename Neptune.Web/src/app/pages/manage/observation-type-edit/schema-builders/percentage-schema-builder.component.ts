import { Component, Input } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { PercentageSchema } from "src/app/shared/observation-types/schema-types";
import { PropertiesToObserveEditorComponent } from "./properties-to-observe-editor.component";
import { SchemaBuilderSection } from "./pass-fail-schema-builder.component";

export type PercentageSchemaFormGroup = FormGroup<{
    BenchmarkDescription: FormControl<string>;
    ThresholdDescription: FormControl<string>;
    AssessmentDescription: FormControl<string>;
    MeasurementUnitLabel: FormControl<string>;
    PropertiesToObserve: FormControl<string[]>;
}>;

/** Build a reactive FormGroup that matches the PercentageSchema shape. */
export function buildPercentageSchemaFormGroup(initial: PercentageSchema): PercentageSchemaFormGroup {
    return new FormGroup({
        BenchmarkDescription: new FormControl<string>(initial.BenchmarkDescription ?? "", { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
        ThresholdDescription: new FormControl<string>(initial.ThresholdDescription ?? "", { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
        AssessmentDescription: new FormControl<string>(initial.AssessmentDescription ?? "", { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
        MeasurementUnitLabel: new FormControl<string>(initial.MeasurementUnitLabel ?? "", { nonNullable: true, validators: [Validators.required] }),
        PropertiesToObserve: new FormControl<string[]>(initial.PropertiesToObserve ?? [], { nonNullable: true, validators: [Validators.required] }),
    });
}

@Component({
    selector: "percentage-schema-builder",
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
                    <form-field class="g-col-12"
                        fieldLabel="Measurement Unit Label"
                        fieldDefinitionName="MeasurementUnitLabel"
                        [type]="FormFieldType.Text"
                        [formControl]="formGroup.controls.MeasurementUnitLabel"
                        [required]="true"
                        placeholder="e.g. Percent Cover"></form-field>
                    <div class="g-col-12">
                        <properties-to-observe-editor [control]="formGroup.controls.PropertiesToObserve"></properties-to-observe-editor>
                    </div>
                </div>
            }
        }
    `,
})
export class PercentageSchemaBuilderComponent {
    @Input({ required: true }) formGroup!: PercentageSchemaFormGroup;
    @Input() section: SchemaBuilderSection = "instructions";
    public FormFieldType = FormFieldType;
}
