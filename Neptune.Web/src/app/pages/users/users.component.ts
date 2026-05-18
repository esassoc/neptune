import { Component, computed, inject, OnInit, Signal } from "@angular/core";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { map, Observable } from "rxjs";
import { toSignal } from "@angular/core/rxjs-interop";
import { UserService } from "src/app/shared/generated/api/user.service";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { PersonSimpleDto } from "src/app/shared/generated/model/person-simple-dto";
import { AuthenticationService } from "src/app/services/authentication.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

@Component({
    selector: "users",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    templateUrl: "./users.component.html",
    styleUrl: "./users.component.scss",
})
export class UsersComponent implements OnInit {
    public users$: Observable<PersonSimpleDto[]>;
    public columnDefs: Signal<ColDef[]>;

    private utilityFunctions = inject(UtilityFunctionsService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private userService = inject(UserService);
    private authenticationService = inject(AuthenticationService);

    private currentUser = toSignal(this.authenticationService.currentUserSetObservable.pipe(map((u) => u ?? null)), { initialValue: null });
    public isAdmin: Signal<boolean> = computed(() => {
        const u = this.currentUser();
        return !!u && (u.RoleID === RoleEnum.Admin || u.RoleID === RoleEnum.SitkaAdmin);
    });

    ngOnInit(): void {
        this.columnDefs = computed<ColDef[]>(() => [
            {
                ...this.utilityFunctions.createActionsColumnDef((params: any) => [
                    {
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
                        ActionHandler: () => this.deleteUser(params.data),
                    },
                ]),
                hide: !this.isAdmin(),
            },
            this.utilityFunctions.createLinkColumnDef("First Name", "FirstName", "PersonID", { InRouterLink: "/users/" }),
            this.utilityFunctions.createLinkColumnDef("Last Name", "LastName", "PersonID", { InRouterLink: "/users/" }),
            this.utilityFunctions.createBasicColumnDef("Email", "Email"),
            this.utilityFunctions.createBasicColumnDef("Phone", "Phone"),
            this.utilityFunctions.createBasicColumnDef("Role", "RoleName", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctions.createBasicColumnDef("Organization", "OrganizationName", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctions.createBooleanColumnDef("Active?", "IsActive", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctions.createBooleanColumnDef("Support Emails?", "ReceiveSupportEmails", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctions.createBooleanColumnDef("RSB Revision Emails?", "ReceiveRSBRevisionRequestEmails", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctions.createBooleanColumnDef("OCTA Grant Reviewer?", "IsOCTAGrantReviewer", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctions.createBooleanColumnDef("Assigned Stormwater Jurisdiction?", "HasAssignedStormwaterJurisdiction", {
                UseCustomDropdownFilter: true,
            }),
            this.utilityFunctions.createDateColumnDef("Created", "CreateDate", "short"),
            this.utilityFunctions.createDateColumnDef("Updated", "UpdateDate", "short"),
            this.utilityFunctions.createDateColumnDef("Last Activity", "LastActivityDate", "short"),
        ]);
        this.users$ = this.userService.listUser();
    }

    deleteUser(user: PersonSimpleDto) {
        this.confirmService
            .confirm({
                title: "Delete User",
                message: `Are you sure you want to delete user '<strong>${escapeHtml(`${user.FirstName} ${user.LastName}`)}</strong>'?`,
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
                buttonClassYes: "btn-danger",
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.userService.deleteUser(user.PersonID).subscribe(() => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("User deleted successfully.", AlertContext.Success));
                        this.users$ = this.userService.listUser();
                    });
                }
            });
    }
}
