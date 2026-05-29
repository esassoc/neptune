import { AsyncPipe } from "@angular/common";
import { Component, inject, OnInit } from "@angular/core";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { DialogService } from "@ngneat/dialog";
import { BehaviorSubject, catchError, forkJoin, map, Observable, of, shareReplay, switchMap } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { NoteComponent } from "src/app/shared/components/note/note.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { fileResourceUrl } from "src/app/shared/helpers/file-resource-url";
import { OrganizationService } from "src/app/shared/generated/api/organization.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { FundingSourceSimpleDto } from "src/app/shared/generated/model/funding-source-simple-dto";
import { OrganizationDto } from "src/app/shared/generated/model/organization-dto";
import { PersonSimpleDto } from "src/app/shared/generated/model/person-simple-dto";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { OrganizationModalComponent } from "../organization-modal/organization-modal.component";
import { FundingSourceModalComponent } from "../../funding-sources/funding-source-modal/funding-source-modal.component";

interface OrganizationDetailViewModel {
    organization: OrganizationDto;
    fundingSources: FundingSourceSimpleDto[];
    users: PersonSimpleDto[];
}

/**
 * NPT-999: Organization Detail page mirroring the legacy MVC Organization/Detail view.
 *
 * Composes three focused endpoints (basics, funding sources, users) via forkJoin rather than
 * one super-DTO. The benefit isn't independent panel loading (forkJoin waits for every request
 * to complete before emitting); it's that each endpoint stays narrow and reusable elsewhere,
 * and each can be permission-scoped on its own. Reload$ is a BehaviorSubject so the Edit
 * modal's success path re-pulls every panel without rebuilding the component.
 */
@Component({
    selector: "organization-detail",
    standalone: true,
    imports: [AsyncPipe, RouterLink, PageHeaderComponent, AlertDisplayComponent, IconComponent, NoteComponent],
    templateUrl: "./organization-detail.component.html",
    styleUrl: "./organization-detail.component.scss",
})
export class OrganizationDetailComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private organizationService = inject(OrganizationService);
    private alertService = inject(AlertService);
    private authenticationService = inject(AuthenticationService);
    private dialogService = inject(DialogService);

    public organizationID!: number;
    public viewModel$: Observable<OrganizationDetailViewModel | null>;
    private reload$ = new BehaviorSubject<void>(undefined);
    public fileResourceUrl = fileResourceUrl;

    public get isAdmin(): boolean {
        // Synchronous role check helper — avoids a per-template subscription to currentUser$
        // just to gate the Edit button and the linked user/funding-source items.
        return this.authenticationService.doesCurrentUserHaveOneOfTheseRoles([RoleEnum.Admin, RoleEnum.SitkaAdmin]);
    }

    ngOnInit(): void {
        this.organizationID = +this.route.snapshot.paramMap.get("organizationID")!;

        this.viewModel$ = this.reload$.pipe(
            switchMap(() => forkJoin({
                organization: this.organizationService.getOrganization(this.organizationID).pipe(
                    catchError((err) => {
                        if (err?.status === 403) {
                            this.alertService.pushAlert(new Alert("You don't have permission to view that organization.", AlertContext.Danger, true));
                            this.router.navigate(["/organizations"]);
                        }
                        return of(null as OrganizationDto | null);
                    }),
                ),
                fundingSources: this.organizationService.listFundingSourcesOrganization(this.organizationID).pipe(
                    catchError(() => of([] as FundingSourceSimpleDto[])),
                ),
                users: this.organizationService.listUsersOrganization(this.organizationID).pipe(
                    catchError(() => of([] as PersonSimpleDto[])),
                ),
            })),
            map((result) => result.organization ? { organization: result.organization, fundingSources: result.fundingSources, users: result.users } as OrganizationDetailViewModel : null),
            shareReplay(1),
        );
    }

    public openEditModal(organization: OrganizationDto): void {
        const dialogRef = this.dialogService.open(OrganizationModalComponent, {
            data: { mode: "edit", organization },
        });
        dialogRef.afterClosed$.subscribe((saved) => {
            if (saved) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Organization updated successfully.", AlertContext.Success, true, "organization-saved"));
                this.reload$.next();
            }
        });
    }

    /** NPT-999: legacy MVC detail page surfaced a "+" icon in the Funding Sources panel
     *  header that opened the New Funding Source modal pre-filled with this org. Mirror it. */
    public openAddFundingSourceModal(organization: OrganizationDto): void {
        const dialogRef = this.dialogService.open(FundingSourceModalComponent, {
            data: { mode: "add", fundingSource: { OrganizationID: organization.OrganizationID } },
        });
        dialogRef.afterClosed$.subscribe((saved) => {
            if (saved) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Funding source added successfully.", AlertContext.Success, true, "funding-source-added"));
                this.reload$.next();
            }
        });
    }
}
