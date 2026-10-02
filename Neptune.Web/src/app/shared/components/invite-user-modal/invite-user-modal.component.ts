import { AsyncPipe } from "@angular/common";
import { HttpContext } from "@angular/common/http";
import { Component, inject, OnInit, signal } from "@angular/core";
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { NgSelectModule } from "@ng-select/ng-select";
import { DialogRef } from "@ngneat/dialog";
import { map, Observable } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { OrganizationService } from "src/app/shared/generated/api/organization.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { UserService } from "src/app/shared/generated/api/user.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
import { StormwaterJurisdictionDisplayDto } from "src/app/shared/generated/model/stormwater-jurisdiction-display-dto";
import { HANDLES_ERRORS_INLINE } from "src/app/shared/interceptors/httpErrorInterceptor";

export interface InviteUserModalContext {
    // Set when opened from a jurisdiction detail page; that jurisdiction is preselected.
    presetJurisdictionID?: number;
    presetJurisdictionName?: string;
}

// NPT-734: invite someone who hasn't signed in yet. The API creates their Person with the chosen role and
// jurisdictions and emails them a sign-up link; their first login links the Auth0 account by email.
// Admins pick any jurisdictions. Jurisdiction Managers pick exactly one of their own and only JE/JM roles.
// The server enforces all of this in PersonInvites.ValidateInviteAsync; the form just avoids offering bad choices.
@Component({
    selector: "invite-user-modal",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, NgSelectModule, FormFieldComponent, CustomRichTextComponent],
    templateUrl: "./invite-user-modal.component.html",
})
export class InviteUserModalComponent implements OnInit {
    public ref: DialogRef<InviteUserModalContext, PersonDto | null> = inject(DialogRef);
    private userService = inject(UserService);
    private organizationService = inject(OrganizationService);
    private jurisdictionService = inject(StormwaterJurisdictionService);
    private authenticationService = inject(AuthenticationService);

    public FormFieldType = FormFieldType;
    public NeptunePageTypeEnum = NeptunePageTypeEnum;
    public isSaving = signal(false);
    public errorMessages = signal<string[]>([]);

    public isAdmin = this.authenticationService.isCurrentUserAnAdministrator();
    public roleOptions: SelectDropdownOption[] = [];
    public organizationOptions$: Observable<SelectDropdownOption[]>;
    public jurisdictions$: Observable<StormwaterJurisdictionDisplayDto[]>;

    public formGroup = new FormGroup({
        FirstName: new FormControl<string>("", { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
        LastName: new FormControl<string>("", { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
        // No Validators.email: it rejects the "Jane Doe <jane@x.com>" form Outlook pastes, which the server
        // accepts and normalizes. The server is the source of truth for what counts as one valid address.
        Email: new FormControl<string>("", { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] }),
        RoleID: new FormControl<number | null>(null, { validators: [Validators.required] }),
        OrganizationID: new FormControl<number | null>(null),
        StormwaterJurisdictionIDs: new FormControl<number[]>([], { nonNullable: true }),
    });

    ngOnInit(): void {
        this.roleOptions = this.buildRoleOptions();
        this.formGroup.controls.RoleID.setValue(RoleEnum.JurisdictionEditor);

        const presetID = this.ref.data?.presetJurisdictionID;
        if (presetID) {
            this.formGroup.controls.StormwaterJurisdictionIDs.setValue([presetID]);
        }
        if (!this.isAdmin) {
            // Managers invite into exactly one of their jurisdictions.
            this.formGroup.controls.StormwaterJurisdictionIDs.addValidators((c) => ((c.value ?? []).length === 1 ? null : { exactlyOne: true }));
            this.formGroup.controls.StormwaterJurisdictionIDs.updateValueAndValidity();
        }

        // "user-viewable" returns every jurisdiction for admins and only assigned ones for a Manager.
        this.jurisdictions$ = this.jurisdictionService
            .listViewableStormwaterJurisdiction()
            .pipe(map((list) => [...list].sort((a, b) => (a.StormwaterJurisdictionName ?? "").localeCompare(b.StormwaterJurisdictionName ?? ""))));

        this.organizationOptions$ = this.organizationService
            .listOrganization()
            .pipe(map((orgs) => orgs.map((o) => ({ Label: o.OrganizationName, Value: o.OrganizationID, disabled: false }) as SelectDropdownOption)));
    }

    private buildRoleOptions(): SelectDropdownOption[] {
        // Mirrors PersonInvites.ListInvitableRoleIDs.
        const all: { id: RoleEnum; label: string }[] = [
            { id: RoleEnum.SitkaAdmin, label: "Sitka Administrator" },
            { id: RoleEnum.Admin, label: "Administrator" },
            { id: RoleEnum.JurisdictionManager, label: "Jurisdiction Manager" },
            { id: RoleEnum.JurisdictionEditor, label: "Jurisdiction Editor" },
            { id: RoleEnum.Unassigned, label: "Unassigned" },
        ];
        let allowed: RoleEnum[];
        if (this.authenticationService.doesCurrentUserHaveOneOfTheseRoles([RoleEnum.SitkaAdmin])) {
            allowed = all.map((r) => r.id);
        } else if (this.isAdmin) {
            allowed = [RoleEnum.Admin, RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor, RoleEnum.Unassigned];
        } else {
            allowed = [RoleEnum.JurisdictionManager, RoleEnum.JurisdictionEditor];
        }
        return all.filter((r) => allowed.includes(r.id)).map((r) => ({ Label: r.label, Value: r.id, disabled: false }) as SelectDropdownOption);
    }

    // A Manager opening this from a jurisdiction page can't change the jurisdiction.
    public get isJurisdictionLocked(): boolean {
        return !this.isAdmin && !!this.ref.data?.presetJurisdictionID;
    }

    public save(): void {
        if (this.formGroup.invalid) return;
        this.isSaving.set(true);
        this.errorMessages.set([]);
        const value = this.formGroup.getRawValue();
        this.userService
            .inviteUser(
                {
                    FirstName: value.FirstName,
                    LastName: value.LastName,
                    Email: value.Email,
                    RoleID: value.RoleID,
                    OrganizationID: value.OrganizationID,
                    StormwaterJurisdictionIDs: value.StormwaterJurisdictionIDs,
                },
                "body",
                false,
                { context: new HttpContext().set(HANDLES_ERRORS_INLINE, true) }
            )
            .subscribe({
                next: (person) => {
                    this.isSaving.set(false);
                    this.ref.close(person);
                },
                error: (err) => {
                    this.isSaving.set(false);
                    this.errorMessages.set(this.readErrors(err));
                },
            });
    }

    // Handles both BadRequest(ModelState) ({ Email: ["..."] }) and ValidationProblemDetails ({ errors: {...} }).
    private readErrors(err: any): string[] {
        const body = err?.error;
        if (typeof body === "string" && body) return [body];
        const dict = body?.errors ?? body;
        if (dict && typeof dict === "object") {
            const messages = Object.values(dict)
                .flatMap((v) => (Array.isArray(v) ? v : [v]))
                .filter((v): v is string => typeof v === "string");
            if (messages.length) return messages;
        }
        return ["Could not send the invitation."];
    }

    public cancel(): void {
        this.ref.close(null);
    }
}
