import { Component, EventEmitter, Input, OnChanges, Output, signal, SimpleChanges } from "@angular/core";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";

export type FieldState = "pending" | "accepted" | "edited" | "rejected";
export type FieldOrigin = "ai" | "blank" | "user";

export interface EvidenceBoundingBox {
    PageNumber: number;
    X: number;
    Y: number;
    Width: number;
    Height: number;
}

export interface SourceNavigation {
    value: string | null;
    evidence: string | null;
    documentSource: string | null;
    boundingBox: EvidenceBoundingBox | null;
}

@Component({
    selector: "field-card",
    standalone: true,
    imports: [FormFieldComponent, ReactiveFormsModule],
    templateUrl: "./field-card.component.html",
    styleUrl: "./field-card.component.scss",
})
export class FieldCardComponent implements OnChanges {
    @Input() fieldLabel = "";
    @Input() extractedValue: string | null = null;
    @Input() extractionEvidence: string | null = null;
    @Input() documentSource: string | null = null;
    @Input() boundingBox: EvidenceBoundingBox | null = null;
    @Input() confidence: "high" | "medium" | "low" | "none" = "none";
    @Input() fieldType: FormFieldType = FormFieldType.Text;
    @Input() selectOptions: FormInputOption[] = [];
    // Optional ngx-mask pattern passed through to the form-field (e.g. "(000) 000-0000" for
    // phone, "00000" / "00000-0000" for ZIP). Ignored for non-text / non-number field types.
    @Input() mask: string | null = null;
    @Input() origin: FieldOrigin = "ai";
    @Input() readOnly = false;
    @Input() initialState: FieldState = "pending";
    @Input() initialValue: string | null = null;

    @Output() valueAccepted = new EventEmitter<string | null>();
    @Output() valueEdited = new EventEmitter<string>();
    @Output() valueRejected = new EventEmitter<void>();
    @Output() navigateToSource = new EventEmitter<SourceNavigation>();

    public FormFieldType = FormFieldType;
    public state = signal<FieldState>("pending");
    public isEditing = signal(false);
    public showEvidence = signal(false);
    public editControl: FormControl = new FormControl("");
    public displayValue = signal<string | null>(null);

    ngOnChanges(changes: SimpleChanges): void {
        if (changes["initialState"]) {
            this.state.set(this.initialState);
        }
        if (changes["initialValue"]) {
            this.displayValue.set(this.initialValue);
        }
    }

    get currentValue(): string | null {
        return this.displayValue() ?? this.extractedValue;
    }

    accept(): void {
        if (this.readOnly) return;
        this.state.set("accepted");
        this.isEditing.set(false);
        this.valueAccepted.emit(this.currentValue);
    }

    startEdit(): void {
        if (this.readOnly) return;
        // ng-select uses strict equality against option.Value (number), so coerce numeric-string
        // Select values back to numbers so the dropdown pre-selects the current value.
        const current = this.currentValue ?? "";
        const editValue: string | number =
            this.fieldType === FormFieldType.Select && current !== "" && !isNaN(Number(current))
                ? Number(current)
                : current;
        this.editControl.setValue(editValue);
        this.isEditing.set(true);
    }

    saveEdit(): void {
        if (this.readOnly) return;
        this.state.set("edited");
        this.isEditing.set(false);
        const raw = this.editControl.value;
        const asString = raw == null || raw === "" ? "" : String(raw);
        this.displayValue.set(asString === "" ? null : asString);
        this.valueEdited.emit(asString);
    }

    cancelEdit(): void {
        this.isEditing.set(false);
    }

    reject(): void {
        if (this.readOnly) return;
        this.state.set("rejected");
        this.isEditing.set(false);
        this.valueRejected.emit();
    }

    toggleEvidence(): void {
        this.showEvidence.update((v) => !v);
    }

    getOptionLabel(value: string): string {
        return this.selectOptions?.find((o) => String(o.Value) === value)?.Label ?? value;
    }

    goToSource(): void {
        if (this.extractionEvidence || this.documentSource || this.boundingBox) {
            this.navigateToSource.emit({
                value: this.extractedValue,
                evidence: this.extractionEvidence,
                documentSource: this.documentSource,
                boundingBox: this.boundingBox,
            });
        }
    }
}
