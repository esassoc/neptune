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
        // Default to Active for add mode (matches the legacy MVC's `IsActive = true` seed
        // on New). Edit mode's patchValue overrides this with the persisted value.
        IsActive: FundingSourceUpsertDtoFormControls.IsActive(true),
        FundingSourceDescription: FundingSourceUpsertDtoFormControls.FundingSourceDescription(),
    });
    public mode: "add" | "edit";
    public organizationOptions$: Observable<any[]>;

    constructor(private alertService: AlertService, private fundingSourceService: FundingSourceService, private organizationService: OrganizationService) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.mode = this.ref.data.mode;
        // Patch in any pre-fill the caller supplied. Edit mode passes the full FundingSourceDto;
        // add mode may pass a partial seed (e.g. { OrganizationID } from the org-detail page's
        // "Add Funding Source" icon — NPT-999 round 2). Build the patch from defined fields only
        // so an undefined property doesn't clobber the form's default value (notably IsActive,
        // which defaults to true above for add mode — Copilot PR #517 review feedback).
        const seed = this.ref.data.fundingSource;
        if (seed) {
            const patch: Partial<{ OrganizationID: number; FundingSourceName: string; IsActive: boolean; FundingSourceDescription: string }> = {};
            if (seed.OrganizationID !== undefined) patch.OrganizationID = seed.OrganizationID;
            if (seed.FundingSourceName !== undefined) patch.FundingSourceName = seed.FundingSourceName;
            if (seed.IsActive !== undefined) patch.IsActive = seed.IsActive;
            if (seed.FundingSourceDescription !== undefined) patch.FundingSourceDescription = seed.FundingSourceDescription;
            this.formGroup.patchValue(patch);
        }
        this.organizationOptions$ = this.organizationService.listOrganization().pipe(
            map((orgs) => orgs.map((org) => ({ Label: org.OrganizationName, Value: org.OrganizationID })))
        );
    }

    save(): void {
        if (this.formGroup.invalid) {
            // Touching every control flips the form-field error-display into showing the
            // missing-required messages — silent return left the modal feeling unresponsive
            // when a required field wasn't filled.
            this.formGroup.markAllAsTouched();
            return;
        }
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
