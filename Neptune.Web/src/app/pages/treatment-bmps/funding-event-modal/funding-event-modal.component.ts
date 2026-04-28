import { FormControl } from "@angular/forms";
import { Component, inject, OnInit } from "@angular/core";
import { FormGroup, FormArray, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { AsyncPipe } from "@angular/common";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { FundingEventUpsertDtoFormControls } from "src/app/shared/generated/model/funding-event-upsert-dto";
import { FundingEventFundingSourceSimpleDtoFormControls } from "src/app/shared/generated/model/funding-event-funding-source-simple-dto";
import { FundingEventByTreatmentBMPIDService } from "src/app/shared/generated/api/funding-event-by-treatment-bmpid.service";
import { FundingSourceService } from "src/app/shared/generated/api/funding-source.service";
import { map, shareReplay } from "rxjs/operators";
import { FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { DialogRef } from "@ngneat/dialog";
import { Observable } from "rxjs";

import { FundingEventTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/funding-event-type-enum";

@Component({
    selector: "funding-event-modal",
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent, IconComponent, AsyncPipe],
    templateUrl: "./funding-event-modal.component.html",
    styleUrl: "./funding-event-modal.component.scss",
})
export class FundingEventModalComponent implements OnInit {
    get hasDuplicateFundingSources(): boolean {
        const ids = this.getFundingSourceGroups()
            .map((g) => g.get("FundingSourceID")?.value)
            .filter((id) => id !== null && id !== undefined);
        return ids.length !== new Set(ids).size;
    }
    public fundingEventTypeOptions = FundingEventTypesAsSelectDropdownOptions;
    public fundingSourceOptions$: Observable<FormInputOption[]>;

    getFundingSourceIDControl(group: FormGroup): FormControl {
        return group.get("FundingSourceID") as FormControl;
    }
    getFundingSourceAmountControl(group: FormGroup): FormControl {
        return group.get("Amount") as FormControl;
    }
    getFundingSourceGroups(): FormGroup[] {
        return (this.formGroup.controls.FundingEventFundingSources as FormArray).controls as FormGroup[];
    }
    get fundingSources(): FormArray {
        return this.formGroup.controls.FundingEventFundingSources as FormArray;
    }

    addFundingSource(): void {
        // Use negative IDs for new sources to avoid collision with existing positive IDs
        this.fundingSources.push(
            new FormGroup({
                FundingSourceID: FundingEventFundingSourceSimpleDtoFormControls.FundingSourceID(),
                FundingSourceName: FundingEventFundingSourceSimpleDtoFormControls.FundingSourceName(),
                Amount: FundingEventFundingSourceSimpleDtoFormControls.Amount(),
            })
        );
    }

    removeFundingSource(idx: number): void {
        this.fundingSources.removeAt(idx);
    }
    public ref: DialogRef<FundingEventModalContext, boolean> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public formGroup = new FormGroup({
        FundingEventTypeID: FundingEventUpsertDtoFormControls.FundingEventTypeID(),
        Year: FundingEventUpsertDtoFormControls.Year(),
        Description: FundingEventUpsertDtoFormControls.Description(),
        FundingEventFundingSources: new FormArray([]),
    });

    constructor(
        private fundingEventService: FundingEventByTreatmentBMPIDService,
        private alertService: AlertService,
        private fundingSourceService: FundingSourceService
    ) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        if (this.ref.data?.editData) {
            // Patch simple fields
            this.formGroup.controls.FundingEventTypeID.setValue(this.ref.data.editData.FundingEventTypeID);
            this.formGroup.controls.Year.setValue(this.ref.data.editData.Year);
            this.formGroup.controls.Description.setValue(this.ref.data.editData.Description);

            // Patch funding sources as FormGroups
            const sourcesArray = this.formGroup.controls.FundingEventFundingSources as FormArray;
            sourcesArray.clear();
            (this.ref.data.editData.FundingEventFundingSources || []).forEach((src: any) => {
                sourcesArray.push(
                    new FormGroup({
                        FundingSourceID: FundingEventFundingSourceSimpleDtoFormControls.FundingSourceID(src.FundingSourceID),
                        FundingSourceName: FundingEventFundingSourceSimpleDtoFormControls.FundingSourceName(src.FundingSourceName),
                        Amount: FundingEventFundingSourceSimpleDtoFormControls.Amount(src.Amount),
                    })
                );
            });
        }
        this.fundingSourceOptions$ = this.fundingSourceService.listFundingSource().pipe(
            map((sources) =>
                (sources || []).map(
                    (src) =>
                        ({
                            Value: src.FundingSourceID,
                            Label: src.DisplayName,
                            disabled: false,
                        }) as FormInputOption
                )
            ),
            shareReplay(1)
        );
    }

    save(): void {
        const treatmentBMPID = this.ref.data.treatmentBMPID;
        const dto = this.formGroup.value;
        if (this.ref.data?.editData) {
            // Edit
            this.fundingEventService.updateFundingEventByTreatmentBMPID(treatmentBMPID, this.ref.data.editData.FundingEventID, dto).subscribe(() => {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Successfully updated Funding Event.", AlertContext.Success));
                this.ref.close(true);
            });
        } else {
            // Add
            this.fundingEventService.createFundingEventByTreatmentBMPID(treatmentBMPID, dto).subscribe(() => {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Successfully added Funding Event.", AlertContext.Success));
                this.ref.close(true);
            });
        }
    }

    cancel(): void {
        this.ref.close(null);
    }
}

export class FundingEventModalContext {
    treatmentBMPID: number;
    editData?: any; // FundingEventDto for edit
}
