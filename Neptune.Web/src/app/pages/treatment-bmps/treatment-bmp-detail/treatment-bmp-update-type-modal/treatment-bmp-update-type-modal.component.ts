import { Component, inject, OnInit } from "@angular/core";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AsyncPipe } from "@angular/common";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { map, Observable } from "rxjs";
import { TreatmentBMPTypeUpdateDto, TreatmentBMPTypeUpdateDtoForm, TreatmentBMPTypeUpdateDtoFormControls } from "src/app/shared/generated/model/treatment-bmp-type-update-dto";
import { DialogRef } from "@ngneat/dialog";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "treatment-bmp-update-type-modal",
    imports: [AlertDisplayComponent, ReactiveFormsModule, FormFieldComponent, AsyncPipe],
    templateUrl: "./treatment-bmp-update-type-modal.component.html",
    styleUrl: "./treatment-bmp-update-type-modal.component.scss",
})
export class TreatmentBmpUpdateTypeModalComponent implements OnInit {
    public ref: DialogRef<TreatmentBmpUpdateTypeModalContext, boolean> = inject(DialogRef);
    private treatmentBMPService = inject(TreatmentBMPService);
    private treatmentBMPTypeService = inject(TreatmentBMPTypeService);
    private alertService = inject(AlertService);

    public FormFieldType = FormFieldType;

    public formGroup: FormGroup<TreatmentBMPTypeUpdateDtoForm> = new FormGroup<TreatmentBMPTypeUpdateDtoForm>({
        TreatmentBMPTypeID: TreatmentBMPTypeUpdateDtoFormControls.TreatmentBMPTypeID(undefined),
    });

    public treatmentBMPTypeOptions$: Observable<SelectDropdownOption[]>;

    ngOnInit(): void {
        // clear any existing alerts
        this.alertService.clearAlerts();

        // load treatment bmp types as an observable for the template async pipe
        this.treatmentBMPTypeOptions$ = this.treatmentBMPTypeService
            .listTreatmentBMPType()
            .pipe(map((types) => types.map((type) => ({ Label: type.TreatmentBMPTypeName, Value: type.TreatmentBMPTypeID, disabled: false }) as SelectDropdownOption)));

        // if editing, set current value
        if (this.ref.data?.currentTreatmentBMPTypeID != null) {
            this.formGroup.controls["TreatmentBMPTypeID"].setValue(this.ref.data.currentTreatmentBMPTypeID);
        }
    }

    public save(): void {
        if (this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            return;
        }

        const treatmentBMPID = this.ref.data?.treatmentBMPID;
        const updateTypeDto = this.formGroup.value as TreatmentBMPTypeUpdateDto;
        this.treatmentBMPService.updateTypeTreatmentBMP(treatmentBMPID, updateTypeDto).subscribe(() => {
            this.ref.close(true);
        });
    }

    public cancel(): void {
        this.ref.close(null);
    }
}

export class TreatmentBmpUpdateTypeModalContext {
    treatmentBMPID: number;
    currentTreatmentBMPTypeID?: number;
}
