import { Component, EventEmitter, Input, Output, signal } from "@angular/core";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";

export type FieldState = "pending" | "accepted" | "edited" | "rejected";

@Component({
    selector: "field-card",
    standalone: true,
    imports: [FormFieldComponent, IconComponent, ReactiveFormsModule],
    templateUrl: "./field-card.component.html",
    styleUrl: "./field-card.component.scss",
})
export class FieldCardComponent {
    @Input() fieldLabel = "";
    @Input() extractedValue: string | null = null;
    @Input() extractionEvidence: string | null = null;
    @Input() documentSource: string | null = null;
    @Input() fieldType: FormFieldType = FormFieldType.Text;
    @Input() selectOptions: FormInputOption[] = [];

    @Output() valueAccepted = new EventEmitter<string | null>();
    @Output() valueEdited = new EventEmitter<string>();
    @Output() valueRejected = new EventEmitter<void>();

    public FormFieldType = FormFieldType;
    public state = signal<FieldState>("pending");
    public isEditing = signal(false);
    public showEvidence = signal(false);
    public editControl = new FormControl("");

    accept(): void {
        this.state.set("accepted");
        this.isEditing.set(false);
        this.valueAccepted.emit(this.extractedValue);
    }

    startEdit(): void {
        this.editControl.setValue(this.extractedValue ?? "");
        this.isEditing.set(true);
    }

    saveEdit(): void {
        this.state.set("edited");
        this.isEditing.set(false);
        this.extractedValue = this.editControl.value;
        this.valueEdited.emit(this.editControl.value);
    }

    cancelEdit(): void {
        this.isEditing.set(false);
    }

    reject(): void {
        this.state.set("rejected");
        this.isEditing.set(false);
        this.valueRejected.emit();
    }

    toggleEvidence(): void {
        this.showEvidence.update((v) => !v);
    }
}
