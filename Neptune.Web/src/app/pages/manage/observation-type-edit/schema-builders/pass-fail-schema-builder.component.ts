import { Component, Input } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { PassFailSchema } from "src/app/shared/observation-types/schema-types";
import { PropertiesToObserveEditorComponent } from "./properties-to-observe-editor.component";

export type SchemaBuilderSection = "instructions" | "labelsUnits";

export type PassFailSchemaFormGroup = FormGroup<{
    AssessmentDescription: FormControl<string>;
    PassingScoreLabel: FormControl<string>;
    FailingScoreLabel: FormControl<string>;
    PropertiesToObserve: FormControl<string[]>;
}>;

/** Build a reactive FormGroup that matches the PassFailSchema shape. The parent observation-type
 * editor owns the group; the schema-builder template binds each <form-field> to a control in this
 * group. Validators mirror the legacy MVC required-field markers. */
export function buildPassFailSchemaFormGroup(initial: PassFailSchema): PassFailSchemaFormGroup {
    return new FormGroup({
        AssessmentDescription: new FormControl<string>(initial.AssessmentDescription ?? "", { nonNullable: true, validators: [Validators.required, Validators.maxLength(300)] }),
        PassingScoreLabel: new FormControl<string>(initial.PassingScoreLabel ?? "Passes", { nonNullable: true, validators: [Validators.required] }),
        FailingScoreLabel: new FormControl<string>(initial.FailingScoreLabel ?? "Fails", { nonNullable: true, validators: [Validators.required] }),
        PropertiesToObserve: new FormControl<string[]>(initial.PropertiesToObserve ?? [], { nonNullable: true, validators: [Validators.required] }),
    });
}

@Component({
    selector: "pass-fail-schema-builder",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, PropertiesToObserveEditorComponent],
    template: `
        @switch (section) {
            @case ("instructions") {
                <div class="grid-12">
                    <form-field class="g-col-12"
                        fieldLabel="Assessment Instruction"
                        [type]="FormFieldType.Textarea"
                        [rows]="4"
                        [formControl]="formGroup.controls.AssessmentDescription"
                        [required]="true"
                        placeholder="Instructions for the assessor"></form-field>
                </div>
            }
            @case ("labelsUnits") {
                <div class="grid-12">
                    <form-field class="g-col-6"
                        fieldLabel="Passing Score Label"
                        [type]="FormFieldType.Text"
                        [formControl]="formGroup.controls.PassingScoreLabel"
                        [required]="true"
                        placeholder="e.g. Passes"></form-field>
                    <form-field class="g-col-6"
                        fieldLabel="Failing Score Label"
                        [type]="FormFieldType.Text"
                        [formControl]="formGroup.controls.FailingScoreLabel"
                        [required]="true"
                        placeholder="e.g. Fails"></form-field>
                    <div class="g-col-12">
                        <properties-to-observe-editor [control]="formGroup.controls.PropertiesToObserve"></properties-to-observe-editor>
                    </div>
                </div>
            }
        }
    `,
})
export class PassFailSchemaBuilderComponent {
    @Input({ required: true }) formGroup!: PassFailSchemaFormGroup;
    @Input() section: SchemaBuilderSection = "instructions";
    public FormFieldType = FormFieldType;
}
