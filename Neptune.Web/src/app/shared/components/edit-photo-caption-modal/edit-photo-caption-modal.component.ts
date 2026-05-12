import { Component, inject, OnInit } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";

import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";

export interface EditPhotoCaptionModalContext {
    /** Current caption text — pre-fills the form. */
    currentCaption: string | null | undefined;
    /** Object URL for the photo preview shown above the caption input. Optional. */
    previewUrl?: string | null;
    /** Header label for the modal (e.g. the photo's filename). Defaults to "Edit Caption". */
    title?: string;
}

/**
 * Modal for editing a single photo's caption. Returns the new caption (or null if cancelled)
 * via DialogRef.close. The caller owns the persistence call — different photo entities live
 * behind different services (TreatmentBMPImage, TreatmentBMPAssessmentPhoto) — so the modal
 * stays a pure form.
 */
@Component({
    selector: "edit-photo-caption-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent],
    templateUrl: "./edit-photo-caption-modal.component.html",
    styleUrl: "./edit-photo-caption-modal.component.scss",
})
export class EditPhotoCaptionModalComponent implements OnInit {
    public ref: DialogRef<EditPhotoCaptionModalContext, string | null> = inject(DialogRef);
    public FormFieldType = FormFieldType;

    public formGroup = new FormGroup({
        Caption: new FormControl<string>("", { nonNullable: true, validators: [Validators.maxLength(2000)] }),
    });

    public previewUrl: string | null = null;
    public title = "Edit Caption";

    ngOnInit(): void {
        const ctx = this.ref.data;
        this.formGroup.controls.Caption.setValue(ctx?.currentCaption ?? "");
        this.previewUrl = ctx?.previewUrl ?? null;
        if (ctx?.title) this.title = ctx.title;
    }

    save(): void {
        if (this.formGroup.invalid) return;
        this.ref.close(this.formGroup.controls.Caption.value ?? "");
    }

    cancel(): void {
        this.ref.close(null);
    }
}
