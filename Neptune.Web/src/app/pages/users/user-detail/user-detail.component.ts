import { AsyncPipe, DatePipe } from "@angular/common";
import { Component, computed, inject, OnInit, signal, Signal } from "@angular/core";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { toSignal } from "@angular/core/rxjs-interop";
import { ColDef } from "ag-grid-community";
import { DialogService } from "@ngneat/dialog";
import { catchError, map, Observable, of, shareReplay, switchMap } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { UserService } from "src/app/shared/generated/api/user.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { PersonDetailDto } from "src/app/shared/generated/model/person-detail-dto";
import { PersonNotificationDto } from "src/app/shared/generated/model/person-notification-dto";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { escapeHtml } from "src/app/shared/helpers/html-escape";
import { EditRolesModalComponent } from "./edit-roles-modal/edit-roles-modal.component";
import { EditJurisdictionsModalComponent } from "./edit-jurisdictions-modal/edit-jurisdictions-modal.component";

@Component({
    selector: "user-detail",
    standalone: true,
    imports: [AsyncPipe, DatePipe, RouterLink, PageHeaderComponent, AlertDisplayComponent, IconComponent, NeptuneGridComponent],
    templateUrl: "./user-detail.component.html",
    styleUrl: "./user-detail.component.scss",
})
export class UserDetailComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private userService = inject(UserService);
    private alertService = inject(AlertService);
    private authenticationService = inject(AuthenticationService);
    private dialogService = inject(DialogService);
    private confirmService = inject(ConfirmService);
    private utilityFunctions = inject(UtilityFunctionsService);

    public personID!: number;
    public detail$: Observable<PersonDetailDto>;
    public notifications$: Observable<PersonNotificationDto[]>;

    public notificationColumnDefs: ColDef[] = [];
    private currentUser = toSignal(this.authenticationService.currentUserSetObservable.pipe(map((u) => u ?? null)), { initialValue: null });

    public isAdmin: Signal<boolean> = computed(() => {
        const u = this.currentUser();
        return !!u && (u.RoleID === RoleEnum.Admin || u.RoleID === RoleEnum.SitkaAdmin);
    });

    /** KE 5/20/26: the viewed user (not the viewer) — Admin / SitkaAdmin users implicitly
     *  manage every jurisdiction, so the Assigned Jurisdictions panel renders a stub
     *  message and suppresses the Edit pencil for those role IDs. */
    public isDetailUserAdmin(detail: PersonDetailDto): boolean {
        return detail.RoleID === RoleEnum.Admin || detail.RoleID === RoleEnum.SitkaAdmin;
    }

    public isWorking = signal(false);

    ngOnInit(): void {
        this.personID = +this.route.snapshot.paramMap.get("personID")!;

        this.notificationColumnDefs = [
            this.utilityFunctions.createDateColumnDef("Date", "NotificationDate", "short"),
            this.utilityFunctions.createBasicColumnDef("Notification Type", "NotificationTypeDisplayName"),
        ];

        this.detail$ = this.loadDetail();
        this.notifications$ = this.userService.getNotificationsUser(this.personID).pipe(
            catchError(() => of([] as PersonNotificationDto[]))
        );
    }

    private loadDetail(): Observable<PersonDetailDto> {
        return this.userService.getDetailUser(this.personID).pipe(
            catchError((err) => {
                if (err?.status === 403) {
                    this.alertService.pushAlert(new Alert("You don't have permission to view that user's profile.", AlertContext.Danger, true));
                    this.router.navigate(["/users"]);
                }
                return of(null as unknown as PersonDetailDto);
            }),
            shareReplay(1)
        );
    }

    public openEditRolesModal(detail: PersonDetailDto): void {
        const ref = this.dialogService.open(EditRolesModalComponent, {
            data: { detail },
            size: "md",
        });
        ref.afterClosed$.subscribe((saved) => {
            if (saved) {
                this.alertService.clearAlerts();
                this.detail$ = this.loadDetail();
                this.alertService.pushAlert(new Alert("Roles updated.", AlertContext.Success, true, "user-roles-saved"));
            }
        });
    }

    public openEditJurisdictionsModal(detail: PersonDetailDto): void {
        const ref = this.dialogService.open(EditJurisdictionsModalComponent, {
            data: { detail },
            size: "md",
        });
        ref.afterClosed$.subscribe((saved) => {
            if (saved) {
                this.alertService.clearAlerts();
                this.detail$ = this.loadDetail();
                this.alertService.pushAlert(new Alert("Assigned jurisdictions updated.", AlertContext.Success, true, "user-jurisdictions-saved"));
            }
        });
    }

    public toggleActive(detail: PersonDetailDto): void {
        const goingInactive = detail.IsActive;

        // Clear any stale alerts up-front so the user sees only this action's outcome.
        this.alertService.clearAlerts();

        // Pre-check primary-contact organizations client-side. The server validates the same
        // condition, but surfacing it before the confirm dialog opens avoids the awkward
        // "dialog → confirm → error appears" sequence Kathleen flagged in NPT-999 round 2.
        // PrimaryContactOrganizations is already on the loaded DTO, so no extra round-trip.
        if (goingInactive && detail.PrimaryContactOrganizations?.length) {
            const orgNames = detail.PrimaryContactOrganizations.map((o) => o.OrganizationName).join(", ");
            this.alertService.pushAlert(new Alert(
                `Cannot inactivate this user — they are the primary contact for: ${escapeHtml(orgNames)}. Reassign primary contact before inactivating.`,
                AlertContext.Danger,
                true,
                "user-inactivate-blocked",
            ));
            return;
        }

        this.confirmService
            .confirm({
                title: goingInactive ? "Inactivate User" : "Activate User",
                message: `Are you sure you want to ${goingInactive ? "inactivate" : "activate"} <strong>${escapeHtml(`${detail.FirstName} ${detail.LastName}`)}</strong>?`,
                buttonTextYes: goingInactive ? "Inactivate" : "Activate",
                buttonTextNo: "Cancel",
                buttonClassYes: goingInactive ? "btn-danger" : "btn-primary",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.isWorking.set(true);
                this.userService.updateActiveStatusUser(this.personID, { IsActive: !detail.IsActive }).subscribe({
                    next: () => {
                        this.isWorking.set(false);
                        this.detail$ = this.loadDetail();
                        this.alertService.pushAlert(new Alert(`User ${goingInactive ? "inactivated" : "activated"}.`, AlertContext.Success, true, "user-active-toggle"));
                    },
                    error: (err) => {
                        this.isWorking.set(false);
                        // Server returns a plain string in 400 responses — safety net only since
                        // primary-contact is pre-checked above. uniqueCode dedupes if anything
                        // else also pushes the same error.
                        const message = typeof err?.error === "string" ? err.error : "Failed to update active status.";
                        this.alertService.pushAlert(new Alert(escapeHtml(message), AlertContext.Danger, true, "user-active-toggle-error"));
                    },
                });
            });
    }
}
