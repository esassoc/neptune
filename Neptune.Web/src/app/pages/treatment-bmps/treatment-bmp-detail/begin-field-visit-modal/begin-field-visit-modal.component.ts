import { Component, inject, OnInit } from "@angular/core";
import { FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";

import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";

import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { FieldVisitCreateDto, FieldVisitCreateDtoForm, FieldVisitCreateDtoFormControls } from "src/app/shared/generated/model/field-visit-create-dto";
import { FieldVisitDto } from "src/app/shared/generated/model/field-visit-dto";
import { FieldVisitTypesAsSelectDropdownOptions, FieldVisitTypeEnum } from "src/app/shared/generated/enum/field-visit-type-enum";

import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

export interface BeginFieldVisitModalContext {
    treatmentBMPID: number;
    inProgressFieldVisit: FieldVisitDto | null;
}

@Component({
    selector: "begin-field-visit-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent],
    templateUrl: "./begin-field-visit-modal.component.html",
    styleUrl: "./begin-field-visit-modal.component.scss",
})
export class BeginFieldVisitModalComponent implements OnInit {
    public ref: DialogRef<BeginFieldVisitModalContext, FieldVisitDto | null> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public fieldVisitTypeOptions = FieldVisitTypesAsSelectDropdownOptions;

    public continueOptions = [
        { Label: "Continue the in-progress visit", Value: true, disabled: false },
        { Label: "Start a new visit (the in-progress visit will be marked Unresolved)", Value: false, disabled: false },
    ];

    public formGroup = new FormGroup<FieldVisitCreateDtoForm>({
        VisitDate: FieldVisitCreateDtoFormControls.VisitDate(this.todayDateString(), { validators: [Validators.required] }),
        FieldVisitTypeID: FieldVisitCreateDtoFormControls.FieldVisitTypeID(FieldVisitTypeEnum.DryWeather, { validators: [Validators.required] }),
        ContinueExistingInProgress: FieldVisitCreateDtoFormControls.ContinueExistingInProgress(),
    });

    public hasInProgressVisit = false;
    public isSaving = false;

    constructor(private fieldVisitService: FieldVisitService, private alertService: AlertService) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        const ctx = this.ref.data;
        this.hasInProgressVisit = !!ctx?.inProgressFieldVisit;
        if (this.hasInProgressVisit) {
            // Default to continuing the existing visit; pre-fill date and type from the in-progress
            // visit so the user can see what they'd be continuing.
            this.formGroup.controls.ContinueExistingInProgress.setValue(true);
            this.formGroup.controls.VisitDate.setValue(this.formatDateInputValue(ctx.inProgressFieldVisit!.VisitDate));
            this.formGroup.controls.FieldVisitTypeID.setValue(ctx.inProgressFieldVisit!.FieldVisitTypeID);

            // When the user flips to "Start a new visit", reset Date + Type back to fresh defaults so
            // they don't accidentally clone the in-progress visit's metadata. Flip the other way and
            // we restore the in-progress values.
            this.formGroup.controls.ContinueExistingInProgress.valueChanges.subscribe((value) => {
                if (value === false) {
                    this.formGroup.controls.VisitDate.setValue(this.todayDateString());
                    this.formGroup.controls.FieldVisitTypeID.setValue(FieldVisitTypeEnum.DryWeather);
                } else if (value === true) {
                    this.formGroup.controls.VisitDate.setValue(this.formatDateInputValue(ctx.inProgressFieldVisit!.VisitDate));
                    this.formGroup.controls.FieldVisitTypeID.setValue(ctx.inProgressFieldVisit!.FieldVisitTypeID);
                }
            });
        }
    }

    save(): void {
        // Surface the invalid state so required-field errors light up (Kathleen's "click does
        // nothing" report). Per NPT-1029, modals rely on field-level highlights rather than
        // pushing a global danger alert, so just markAllAsTouched and return.
        if (this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            return;
        }

        this.alertService.clearAlerts();
        this.isSaving = true;

        // Strict boolean: the radio form control should hold true/false, but defend against string
        // coercion from older ng versions or odd ValueAccessor wiring by normalizing here.
        const continueRaw = this.formGroup.controls.ContinueExistingInProgress.value;
        const continueExistingInProgress = this.hasInProgressVisit ? continueRaw === true || (continueRaw as unknown) === "true" : null;

        const dto = new FieldVisitCreateDto({
            VisitDate: this.formGroup.controls.VisitDate.value,
            FieldVisitTypeID: this.formGroup.controls.FieldVisitTypeID.value,
            ContinueExistingInProgress: continueExistingInProgress,
        });

        this.fieldVisitService.createFieldVisit(this.ref.data.treatmentBMPID, dto).subscribe({
            next: (result) => {
                this.isSaving = false;
                this.alertService.pushAlert(new Alert("Field Visit started.", AlertContext.Success));
                this.ref.close(result);
            },
            error: (err) => {
                this.isSaving = false;
                const message = typeof err?.error === "string" ? err.error : "Failed to start the Field Visit. Please try again.";
                this.alertService.pushAlert(new Alert(message, AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.ref.close(null);
    }

    private todayDateString(): string {
        // Local-date string (yyyy-MM-dd) so the date picker shows today's wall-clock date regardless
        // of timezone — `new Date().toISOString()` returns UTC, which can show tomorrow's date for users
        // east of UTC late in the day. This avoids the same UTC-shift bug the visit sidebar surfaces.
        const d = new Date();
        const yyyy = d.getFullYear();
        const mm = String(d.getMonth() + 1).padStart(2, "0");
        const dd = String(d.getDate()).padStart(2, "0");
        return `${yyyy}-${mm}-${dd}`;
    }

    private formatDateInputValue(value: string | Date): string {
        if (!value) return this.todayDateString();
        const d = typeof value === "string" ? new Date(value) : value;
        if (Number.isNaN(d.getTime())) return this.todayDateString();
        return d.toISOString().slice(0, 10);
    }
}
