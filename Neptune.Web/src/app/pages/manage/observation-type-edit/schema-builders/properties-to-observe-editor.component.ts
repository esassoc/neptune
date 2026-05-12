import { Component, Input } from "@angular/core";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";

@Component({
    selector: "properties-to-observe-editor",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent],
    template: `
        @for (prop of control.value; track $index) {
            <div class="prop-row">
                <button type="button" class="btn btn-sm btn-danger-outline" (click)="remove($index)" aria-label="Remove property">
                    <i class="fa fa-trash"></i>
                </button>
                <span>{{ prop }}</span>
            </div>
        }

        <form-field
            fieldLabel="Properties to Observe"
            fieldDefinitionName="PropertiesToObserve"
            [type]="FormFieldType.Text"
            [formControl]="newPropControl"
            placeholder="New property..."
            (keydown.enter)="add(); $event.preventDefault()"></form-field>
        <div class="picker-row">
            <button type="button" class="btn btn-primary-outline btn-sm" (click)="add()">
                <i class="fa fa-plus"></i> Add
            </button>
        </div>
    `,
    styles: [`
        .picker-row {
            display: flex;
            justify-content: flex-end;
            margin-top: 0.5rem;
        }
        .prop-row {
            display: flex;
            gap: 0.5rem;
            align-items: center;
            margin-top: 0.5rem;
        }
        .prop-row span { flex: 1; }
    `],
})
export class PropertiesToObserveEditorComponent {
    @Input({ required: true }) control!: FormControl<string[]>;
    public FormFieldType = FormFieldType;
    public newPropControl = new FormControl<string>("", { nonNullable: true });

    add(): void {
        const text = (this.newPropControl.value ?? "").trim();
        const current = this.control.value ?? [];
        if (text && !current.includes(text)) {
            this.control.setValue([...current, text]);
            this.control.markAsDirty();
            this.newPropControl.setValue("");
        }
    }

    remove(index: number): void {
        const current = this.control.value ?? [];
        this.control.setValue(current.filter((_, i) => i !== index));
        this.control.markAsDirty();
    }
}
