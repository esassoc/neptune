import { AsyncPipe } from "@angular/common";
import { Component, inject, OnInit, signal } from "@angular/core";
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { map, Observable } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { OrganizationService } from "src/app/shared/generated/api/organization.service";
import { UserService } from "src/app/shared/generated/api/user.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { PersonDetailDto } from "src/app/shared/generated/model/person-detail-dto";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

interface EditRolesModalContext {
    detail: PersonDetailDto;
}

@Component({
    selector: "edit-roles-modal",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, AlertDisplayComponent, FormFieldComponent],
    templateUrl: "./edit-roles-modal.component.html",
})
export class EditRolesModalComponent implements OnInit {
    public ref: DialogRef<EditRolesModalContext, boolean> = inject(DialogRef);
    private alertService = inject(AlertService);
    private userService = inject(UserService);
    private organizationService = inject(OrganizationService);
    private authenticationService = inject(AuthenticationService);

    public FormFieldType = FormFieldType;
    public isSaving = signal(false);

    public roleOptions: SelectDropdownOption[] = [];
    public organizationOptions$: Observable<SelectDropdownOption[]>;

    public formGroup = new FormGroup({
        RoleID: new FormControl<number | null>(null, { validators: [Validators.required] }),
        OrganizationID: new FormControl<number | null>(null, { validators: [Validators.required] }),
        IsOCTAGrantReviewer: new FormControl<boolean>(false, { nonNullable: true }),
        ReceiveSupportEmails: new FormControl<boolean>(false, { nonNullable: true }),
        ReceiveRSBRevisionRequestEmails: new FormControl<boolean>(false, { nonNullable: true }),
    });

    ngOnInit(): void {
        const detail = this.ref.data.detail;
        this.formGroup.patchValue({
            RoleID: detail.RoleID,
            OrganizationID: detail.Organization?.OrganizationID,
            IsOCTAGrantReviewer: detail.IsOCTAGrantReviewer,
            ReceiveSupportEmails: detail.ReceiveSupportEmails,
            ReceiveRSBRevisionRequestEmails: detail.ReceiveRSBRevisionRequestEmails,
        });

        this.roleOptions = this.buildRoleOptions();

        this.organizationOptions$ = this.organizationService
            .listOrganization()
            .pipe(map((orgs) => orgs.map((o) => ({ Label: o.OrganizationName, Value: o.OrganizationID, disabled: false }) as SelectDropdownOption)));
    }

    private buildRoleOptions(): SelectDropdownOption[] {
        // SitkaAdmin can assign any role; Admin can assign up to JurisdictionManager (no SitkaAdmin).
        const callerIsSitkaAdmin = this.authenticationService.doesCurrentUserHaveOneOfTheseRoles([RoleEnum.SitkaAdmin]);
        const all: { id: RoleEnum; label: string }[] = [
            { id: RoleEnum.SitkaAdmin, label: "Sitka Administrator" },
            { id: RoleEnum.Admin, label: "Administrator" },
            { id: RoleEnum.JurisdictionManager, label: "Jurisdiction Manager" },
            { id: RoleEnum.JurisdictionEditor, label: "Jurisdiction Editor" },
            { id: RoleEnum.Unassigned, label: "Unassigned" },
        ];
        const allowed = callerIsSitkaAdmin ? all : all.filter((r) => r.id !== RoleEnum.SitkaAdmin);
        return allowed.map((r) => ({ Label: r.label, Value: r.id, disabled: false }) as SelectDropdownOption);
    }

    public save(): void {
        if (this.formGroup.invalid) return;
        this.isSaving.set(true);
        const dto = {
            RoleID: this.formGroup.value.RoleID!,
            OrganizationID: this.formGroup.value.OrganizationID!,
            IsOCTAGrantReviewer: this.formGroup.value.IsOCTAGrantReviewer ?? false,
            ReceiveSupportEmails: this.formGroup.value.ReceiveSupportEmails ?? false,
            ReceiveRSBRevisionRequestEmails: this.formGroup.value.ReceiveRSBRevisionRequestEmails ?? false,
        };
        this.userService.updateRoleUser(this.ref.data.detail.PersonID, dto).subscribe({
            next: () => {
                this.isSaving.set(false);
                this.ref.close(true);
            },
            error: (err) => {
                this.isSaving.set(false);
                const message = typeof err?.error === "string" ? err.error : "Failed to update roles.";
                this.alertService.pushAlert(new Alert(message, AlertContext.Danger, true));
            },
        });
    }

    public cancel(): void {
        this.ref.close(false);
    }
}
