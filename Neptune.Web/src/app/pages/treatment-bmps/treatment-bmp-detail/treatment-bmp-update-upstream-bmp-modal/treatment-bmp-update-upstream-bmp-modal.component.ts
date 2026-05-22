import { Component, inject, OnInit } from "@angular/core";
import { SelectDropdownOption, FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import {
    TreatmentBMPDisplayDto,
    TreatmentBMPUpstreamBMPUpdateDto,
    TreatmentBMPUpstreamBMPUpdateDtoForm,
    TreatmentBMPUpstreamBMPUpdateDtoFormControls,
} from "src/app/shared/generated/model/models";
import { FormGroup, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { map, Observable } from "rxjs";
import { AsyncPipe } from "@angular/common";
import { DialogRef } from "@ngneat/dialog";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "treatment-bmp-update-upstream-bmp-modal",
    imports: [AlertDisplayComponent, FormFieldComponent, ReactiveFormsModule, AsyncPipe],
    templateUrl: "./treatment-bmp-update-upstream-bmp-modal.component.html",
    styleUrl: "./treatment-bmp-update-upstream-bmp-modal.component.scss",
})
export class TreatmentBmpUpdateUpstreamBmpModalComponent implements OnInit {
    public ref: DialogRef<TreatmentBmpUpdateUpstreamBmpModalContext, boolean> = inject(DialogRef);
    private treatmentBMPService = inject(TreatmentBMPService);
    private alertService = inject(AlertService);

    public treatmentBMPOptions$: Observable<SelectDropdownOption[]>;

    public FormFieldType = FormFieldType;

    public formGroup: FormGroup<TreatmentBMPUpstreamBMPUpdateDtoForm> = new FormGroup<TreatmentBMPUpstreamBMPUpdateDtoForm>({
        UpstreamBMPID: TreatmentBMPUpstreamBMPUpdateDtoFormControls.UpstreamBMPID(undefined),
    });

    ngOnInit(): void {
        this.alertService.clearAlerts();

        this.treatmentBMPOptions$ = this.treatmentBMPService
            .listOtherTreatmentBMPsInRegionalSubbasinTreatmentBMP(this.ref.data.treatmentBMPID)
            .pipe(
                map((bmps: TreatmentBMPDisplayDto[]) => bmps.map((bmp) => ({ Label: bmp.TreatmentBMPName, Value: bmp.TreatmentBMPID, disabled: false }) as SelectDropdownOption))
            );

        if (this.ref.data?.currentUpstreamBMPID != null) {
            this.formGroup.controls["UpstreamBMPID"].setValue(this.ref.data.currentUpstreamBMPID);
        }
    }

    public save(): void {
        if (this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            return;
        }

        const treatmentBMPID = this.ref.data?.treatmentBMPID;
        const updateUpstreamDto = this.formGroup.value as TreatmentBMPUpstreamBMPUpdateDto;
        this.treatmentBMPService.updateUpstreamBMPTreatmentBMP(treatmentBMPID, updateUpstreamDto).subscribe(() => {
            this.ref.close(true);
        });
    }

    public cancel(): void {
        this.ref.close(null);
    }
}

export class TreatmentBmpUpdateUpstreamBmpModalContext {
    treatmentBMPID: number;
    currentUpstreamBMPID?: number;
}
