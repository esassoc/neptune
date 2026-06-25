import { Component, inject, OnInit } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { StormwaterJurisdictionGridDto } from "src/app/shared/generated/model/stormwater-jurisdiction-grid-dto";
import { StormwaterJurisdictionUpsertDto } from "src/app/shared/generated/model/stormwater-jurisdiction-upsert-dto";
import { StormwaterJurisdictionPublicBMPVisibilityTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/stormwater-jurisdiction-public-b-m-p-visibility-type-enum";
import { StormwaterJurisdictionPublicWQMPVisibilityTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/stormwater-jurisdiction-public-w-q-m-p-visibility-type-enum";

export interface JurisdictionBasicsModalContext {
    jurisdiction: StormwaterJurisdictionGridDto;
}

@Component({
    selector: "jurisdiction-basics-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent],
    templateUrl: "./jurisdiction-basics-modal.component.html",
})
export class JurisdictionBasicsModalComponent implements OnInit {
    public ref: DialogRef<JurisdictionBasicsModalContext, boolean> = inject(DialogRef);
    private jurisdictionService = inject(StormwaterJurisdictionService);
    private alertService = inject(AlertService);

    public FormFieldType = FormFieldType;
    public bmpVisibilityOptions: SelectDropdownOption[] = StormwaterJurisdictionPublicBMPVisibilityTypesAsSelectDropdownOptions;
    public wqmpVisibilityOptions: SelectDropdownOption[] = StormwaterJurisdictionPublicWQMPVisibilityTypesAsSelectDropdownOptions;
    public isSaving = false;

    public formGroup = new FormGroup({
        StormwaterJurisdictionPublicBMPVisibilityTypeID: new FormControl<number>(null, { validators: [Validators.required] }),
        StormwaterJurisdictionPublicWQMPVisibilityTypeID: new FormControl<number>(null, { validators: [Validators.required] }),
    });

    ngOnInit(): void {
        this.alertService.clearAlerts();
        const jurisdiction = this.ref.data.jurisdiction;
        this.formGroup.patchValue({
            StormwaterJurisdictionPublicBMPVisibilityTypeID: jurisdiction.StormwaterJurisdictionPublicBMPVisibilityTypeID,
            StormwaterJurisdictionPublicWQMPVisibilityTypeID: jurisdiction.StormwaterJurisdictionPublicWQMPVisibilityTypeID,
        });
    }

    public save(): void {
        if (this.formGroup.invalid || this.isSaving) return;
        this.isSaving = true;
        const dto = new StormwaterJurisdictionUpsertDto(this.formGroup.value);
        this.jurisdictionService.updateStormwaterJurisdiction(this.ref.data.jurisdiction.StormwaterJurisdictionID, dto).subscribe({
            next: () => this.ref.close(true),
            error: () => (this.isSaving = false),
        });
    }

    public cancel(): void {
        this.ref.close(null);
    }
}
