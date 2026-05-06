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

    constructor(private fieldVisitService: FieldVisitService, private alertService: AlertService) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        const ctx = this.ref.data;
        this.hasInProgressVisit = !!ctx?.inProgressFieldVisit;
        if (this.hasInProgressVisit) {
            // Default to continuing the existing visit
            this.formGroup.controls.ContinueExistingInProgress.setValue(true);
            this.formGroup.controls.VisitDate.setValue(this.formatDateInputValue(ctx.inProgressFieldVisit!.VisitDate));
            this.formGroup.controls.FieldVisitTypeID.setValue(ctx.inProgressFieldVisit!.FieldVisitTypeID);
        }
    }

    save(): void {
        if (this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            return;
        }
        const dto = new FieldVisitCreateDto({
            VisitDate: this.formGroup.controls.VisitDate.value,
            FieldVisitTypeID: this.formGroup.controls.FieldVisitTypeID.value,
            ContinueExistingInProgress: this.hasInProgressVisit ? this.formGroup.controls.ContinueExistingInProgress.value : null,
        });
        this.fieldVisitService.createFieldVisit(this.ref.data.treatmentBMPID, dto).subscribe((result) => {
            this.alertService.pushAlert(new Alert("Field Visit started.", AlertContext.Success));
            this.ref.close(result);
        });
    }

    cancel(): void {
        this.ref.close(null);
    }

    private todayDateString(): string {
        return new Date().toISOString().slice(0, 10);
    }

    private formatDateInputValue(value: string | Date): string {
        if (!value) return this.todayDateString();
        const d = typeof value === "string" ? new Date(value) : value;
        if (Number.isNaN(d.getTime())) return this.todayDateString();
        return d.toISOString().slice(0, 10);
    }
}
