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
                this.detail$ = this.loadDetail();
                this.alertService.pushAlert(new Alert("Roles updated.", AlertContext.Success, true));
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
                this.detail$ = this.loadDetail();
                this.alertService.pushAlert(new Alert("Assigned jurisdictions updated.", AlertContext.Success, true));
            }
        });
    }

    public toggleActive(detail: PersonDetailDto): void {
        const goingInactive = detail.IsActive;
        this.confirmService
            .confirm({
                title: goingInactive ? "Inactivate User" : "Activate User",
                message: `Are you sure you want to ${goingInactive ? "inactivate" : "activate"} <strong>${detail.FirstName} ${detail.LastName}</strong>?`,
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
                        this.alertService.pushAlert(new Alert(`User ${goingInactive ? "inactivated" : "activated"}.`, AlertContext.Success, true));
                    },
                    error: (err) => {
                        this.isWorking.set(false);
                        // Server returns a plain string in 400 responses (primary-contact violation, etc.).
                        const message = typeof err?.error === "string" ? err.error : "Failed to update active status.";
                        this.alertService.pushAlert(new Alert(message, AlertContext.Danger, true));
                    },
                });
            });
    }
}
