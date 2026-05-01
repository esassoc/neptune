import { Component, EventEmitter, Input, Output, signal } from "@angular/core";
import { FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import {
    FieldCardComponent,
    SourceNavigation,
} from "src/app/pages/wqmps/wqmp-detail/wqmp-review/field-card/field-card.component";
import { ExtractedField } from "src/app/pages/wqmps/wqmp-detail/wqmp-review/wqmp-review.component";

/**
 * Per-BMP wrapper for Step 3 of the WQMP AI review workflow (NPT-1047).
 *
 * Renders one card per extracted Simple BMP. The body is a stack of FieldCardComponents
 * (Name, Type, # of Individual BMPs, % Site Treated, % Captured, % Retained, Note),
 * so per-field accept/edit/reject keeps working unchanged. The card adds a wrapper
 * "Reject this BMP" toggle: when active, the card grays out and the BMP is dropped
 * from the approval payload regardless of the inner field states.
 */
@Component({
    selector: "bmp-review-card",
    standalone: true,
    imports: [FieldCardComponent],
    templateUrl: "./bmp-review-card.component.html",
    styleUrl: "./bmp-review-card.component.scss",
})
export class BmpReviewCardComponent {
    @Input() bmpIndex!: number;
    /** Header label — typically the extracted BMP name, falling back to "BMP #n". */
    @Input() displayName: string | null = null;
    @Input() fields: ExtractedField[] = [];
    @Input() readOnly = false;
    @Input() initialIsRejected = false;
    @Input() selectedFieldKey: string | null = null;

    /** Reference exposed for template default-binding (FormFieldType.Text fallback). */
    public FormFieldType = FormFieldType;

    @Output() bmpRejected = new EventEmitter<number>();
    @Output() bmpRestored = new EventEmitter<number>();
    @Output() fieldAccepted = new EventEmitter<{ key: string; value: string | null }>();
    @Output() fieldEdited = new EventEmitter<{ key: string; value: string }>();
    @Output() fieldRejected = new EventEmitter<{ key: string }>();
    @Output() navigateToSource = new EventEmitter<{ key: string; nav: SourceNavigation }>();

    public isRejected = signal(false);

    ngOnChanges(): void {
        // Reflect the persisted draft state on (re)hydration. The signal is the source
        // of truth once the user starts interacting; this only seeds it.
        this.isRejected.set(this.initialIsRejected);
    }

    rejectBmp(): void {
        if (this.readOnly) return;
        this.isRejected.set(true);
        this.bmpRejected.emit(this.bmpIndex);
    }

    restoreBmp(): void {
        if (this.readOnly) return;
        this.isRejected.set(false);
        this.bmpRestored.emit(this.bmpIndex);
    }

    onFieldAccepted(key: string, value: string | null): void {
        this.fieldAccepted.emit({ key, value });
    }

    onFieldEdited(key: string, value: string): void {
        this.fieldEdited.emit({ key, value });
    }

    onFieldRejected(key: string): void {
        this.fieldRejected.emit({ key });
    }

    onNavigateToSource(key: string, nav: SourceNavigation): void {
        this.navigateToSource.emit({ key, nav });
    }

    get headerLabel(): string {
        return this.displayName?.trim() || `BMP #${this.bmpIndex + 1}`;
    }
}
