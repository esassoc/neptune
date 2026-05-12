import { Component, Input } from "@angular/core";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";

@Component({
    selector: "properties-to-observe-editor",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent],
    template: `
        <div class="grid-12">
            @for (prop of control.value; track $index) {
                <div class="g-col-12 prop-row">
                    <span>{{ prop }}</span>
                    <button type="button" class="btn btn-sm btn-danger-outline" (click)="remove($index)" aria-label="Remove property"><i class="fa fa-trash"></i></button>
                </div>
            }
            <form-field class="g-col-10"
                fieldLabel="Properties to Observe"
                fieldDefinitionName="PropertiesToObserve"
                [type]="FormFieldType.Text"
                [formControl]="newPropControl"
                placeholder="New property..."
                (keydown.enter)="add(); $event.preventDefault()"></form-field>
            <div class="g-col-2 add-btn-cell">
                <button type="button" class="btn btn-sm btn-primary-outline" (click)="add()"><i class="fa fa-plus"></i> Add</button>
            </div>
        </div>
    `,
    styles: [`
        .prop-row { display: flex; gap: 0.5rem; align-items: center; }
        .prop-row span { flex: 1; }
        .add-btn-cell { display: flex; align-items: flex-end; padding-bottom: 0.25rem; }
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
