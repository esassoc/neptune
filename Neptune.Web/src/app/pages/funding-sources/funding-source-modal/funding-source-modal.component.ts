import { Component, inject, OnInit } from "@angular/core";
import { FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";
import { map } from "rxjs/operators";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { FundingSourceService } from "src/app/shared/generated/api/funding-source.service";
import { OrganizationService } from "src/app/shared/generated/api/organization.service";
import { FundingSourceUpsertDto, FundingSourceUpsertDtoForm, FundingSourceUpsertDtoFormControls } from "src/app/shared/generated/model/funding-source-upsert-dto";
import { DialogRef } from "@ngneat/dialog";

@Component({
    selector: "funding-source-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent, AsyncPipe],
    templateUrl: "./funding-source-modal.component.html",
    styleUrl: "./funding-source-modal.component.scss",
})
export class FundingSourceModalComponent implements OnInit {
    public ref: DialogRef<{ mode: "add" | "edit"; fundingSource: any }, boolean> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public formGroup = new FormGroup<FundingSourceUpsertDtoForm>({
        OrganizationID: FundingSourceUpsertDtoFormControls.OrganizationID(undefined, { validators: [Validators.required] }),
        FundingSourceName: FundingSourceUpsertDtoFormControls.FundingSourceName(undefined, { validators: [Validators.required] }),
        IsActive: FundingSourceUpsertDtoFormControls.IsActive(false),
        FundingSourceDescription: FundingSourceUpsertDtoFormControls.FundingSourceDescription(),
    });
    public mode: "add" | "edit";
    public organizationOptions$: Observable<any[]>;

    constructor(private alertService: AlertService, private fundingSourceService: FundingSourceService, private organizationService: OrganizationService) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.mode = this.ref.data.mode;
        if (this.mode === "edit" && this.ref.data.fundingSource) {
            this.formGroup.patchValue({
                OrganizationID: this.ref.data.fundingSource.OrganizationID,
                FundingSourceName: this.ref.data.fundingSource.FundingSourceName,
                IsActive: this.ref.data.fundingSource.IsActive,
                FundingSourceDescription: this.ref.data.fundingSource.FundingSourceDescription,
            });
        }
        this.organizationOptions$ = this.organizationService.listOrganization().pipe(
            map((orgs) => orgs.map((org) => ({ Label: org.OrganizationName, Value: org.OrganizationID })))
        );
    }

    save(): void {
        if (this.formGroup.invalid) return;
        const dto = new FundingSourceUpsertDto(this.formGroup.value);
        if (this.mode === "add") {
            this.fundingSourceService.createFundingSource(dto).subscribe(() => {
                this.ref.close(true);
            });
        } else {
            const fundingSourceID = this.ref.data.fundingSource?.FundingSourceID;
            this.fundingSourceService.updateFundingSource(fundingSourceID, dto).subscribe(() => {
                this.ref.close(true);
            });
        }
    }

    cancel(): void {
        this.ref.close(null);
    }
}
