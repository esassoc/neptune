import { Component, EventEmitter, Input, Output, signal } from "@angular/core";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";

export type FieldState = "pending" | "accepted" | "edited" | "rejected";

export interface SourceNavigation {
    evidence: string | null;
    documentSource: string | null;
}

@Component({
    selector: "field-card",
    standalone: true,
    imports: [FormFieldComponent, ReactiveFormsModule],
    templateUrl: "./field-card.component.html",
    styleUrl: "./field-card.component.scss",
})
export class FieldCardComponent {
    @Input() fieldLabel = "";
    @Input() extractedValue: string | null = null;
    @Input() extractionEvidence: string | null = null;
    @Input() documentSource: string | null = null;
    @Input() confidence: "high" | "medium" | "low" | "none" = "none";
    @Input() fieldType: FormFieldType = FormFieldType.Text;
    @Input() selectOptions: FormInputOption[] = [];

    @Output() valueAccepted = new EventEmitter<string | null>();
    @Output() valueEdited = new EventEmitter<string>();
    @Output() valueRejected = new EventEmitter<void>();
    @Output() navigateToSource = new EventEmitter<SourceNavigation>();

    public FormFieldType = FormFieldType;
    public state = signal<FieldState>("pending");
    public isEditing = signal(false);
    public showEvidence = signal(false);
    public editControl = new FormControl("");
    public displayValue = signal<string | null>(null);

    get currentValue(): string | null {
        return this.displayValue() ?? this.extractedValue;
    }

    accept(): void {
        this.state.set("accepted");
        this.isEditing.set(false);
        this.valueAccepted.emit(this.currentValue);
    }

    startEdit(): void {
        this.editControl.setValue(this.currentValue ?? "");
        this.isEditing.set(true);
    }

    saveEdit(): void {
        this.state.set("edited");
        this.isEditing.set(false);
        this.displayValue.set(this.editControl.value);
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

    goToSource(): void {
        if (this.extractionEvidence || this.documentSource) {
            this.navigateToSource.emit({ evidence: this.extractionEvidence, documentSource: this.documentSource });
        }
    }
}
