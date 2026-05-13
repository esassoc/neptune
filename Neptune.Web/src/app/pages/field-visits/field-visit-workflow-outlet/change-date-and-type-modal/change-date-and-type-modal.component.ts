import { Component, inject, OnInit } from "@angular/core";
import { FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";

import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { FieldVisitUpsertDto, FieldVisitUpsertDtoForm, FieldVisitUpsertDtoFormControls } from "src/app/shared/generated/model/field-visit-upsert-dto";
import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { FieldVisitTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/field-visit-type-enum";

export interface ChangeDateAndTypeModalContext {
    fieldVisit: FieldVisitWorkflowDto;
}

@Component({
    selector: "change-date-and-type-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent],
    templateUrl: "./change-date-and-type-modal.component.html",
})
export class ChangeDateAndTypeModalComponent implements OnInit {
    public ref: DialogRef<ChangeDateAndTypeModalContext, boolean> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public fieldVisitTypeOptions = FieldVisitTypesAsSelectDropdownOptions;

    public formGroup = new FormGroup<FieldVisitUpsertDtoForm>({
        VisitDate: FieldVisitUpsertDtoFormControls.VisitDate(undefined, { validators: [Validators.required] }),
        FieldVisitTypeID: FieldVisitUpsertDtoFormControls.FieldVisitTypeID(undefined, { validators: [Validators.required] }),
    });

    constructor(private fieldVisitService: FieldVisitService, private alertService: AlertService) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        const visit = this.ref.data.fieldVisit;
        this.formGroup.patchValue({
            VisitDate: this.toDateInputString(visit.VisitDate),
            FieldVisitTypeID: visit.FieldVisitTypeID,
        });
    }

    save(): void {
        if (this.formGroup.invalid) return;
        const dto = new FieldVisitUpsertDto({
            VisitDate: this.formGroup.controls.VisitDate.value,
            FieldVisitTypeID: this.formGroup.controls.FieldVisitTypeID.value,
        });
        this.fieldVisitService.updateDateAndTypeFieldVisit(this.ref.data.fieldVisit.FieldVisitID, dto).subscribe(() => {
            this.alertService.pushAlert(new Alert("Updated visit date and type.", AlertContext.Success));
            this.ref.close(true);
        });
    }

    cancel(): void {
        this.ref.close(false);
    }

    private toDateInputString(value: string | Date): string {
        const d = typeof value === "string" ? new Date(value) : value;
        return Number.isNaN(d.getTime()) ? "" : d.toISOString().slice(0, 10);
    }
}
