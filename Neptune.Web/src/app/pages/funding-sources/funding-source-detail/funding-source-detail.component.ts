import { AsyncPipe, CurrencyPipe } from "@angular/common";
import { Component, inject, OnInit } from "@angular/core";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { DialogService } from "@ngneat/dialog";
import { BehaviorSubject, catchError, forkJoin, Observable, of, shareReplay, switchMap, map } from "rxjs";
import { AuthenticationService } from "src/app/services/authentication.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";
import { NoteComponent } from "src/app/shared/components/note/note.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { FundingSourceService } from "src/app/shared/generated/api/funding-source.service";
import { RoleEnum } from "src/app/shared/generated/enum/role-enum";
import { FundingSourceDto } from "src/app/shared/generated/model/funding-source-dto";
import { FundingSourceTreatmentBMPFundingDto } from "src/app/shared/generated/model/funding-source-treatment-bmp-funding-dto";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { FundingSourceModalComponent } from "../funding-source-modal/funding-source-modal.component";

interface FundingSourceDetailViewModel {
    fundingSource: FundingSourceDto;
    bmpFunding: FundingSourceTreatmentBMPFundingDto[];
    totalAmount: number;
}

/**
 * NPT-999: Funding Source Detail page mirroring the legacy MVC FundingSource/Detail view.
 * Composes basics + treatment-bmp funding from focused endpoints via forkJoin (same pattern
 * as the SPA org-detail page). Edit is a card-header pencil that opens the existing
 * FundingSourceModalComponent and re-fires the forkJoin via reload$ on save.
 */
@Component({
    selector: "funding-source-detail",
    standalone: true,
    imports: [AsyncPipe, CurrencyPipe, RouterLink, PageHeaderComponent, AlertDisplayComponent, IconComponent, NoteComponent],
    templateUrl: "./funding-source-detail.component.html",
    styleUrl: "./funding-source-detail.component.scss",
})
export class FundingSourceDetailComponent implements OnInit {
    private route = inject(ActivatedRoute);
    private router = inject(Router);
    private fundingSourceService = inject(FundingSourceService);
    private alertService = inject(AlertService);
    private authenticationService = inject(AuthenticationService);
    private dialogService = inject(DialogService);

    public fundingSourceID!: number;
    public viewModel$: Observable<FundingSourceDetailViewModel | null>;
    private reload$ = new BehaviorSubject<void>(undefined);

    public get isAdmin(): boolean {
        return this.authenticationService.doesCurrentUserHaveOneOfTheseRoles([RoleEnum.Admin, RoleEnum.SitkaAdmin]);
    }

    ngOnInit(): void {
        this.fundingSourceID = +this.route.snapshot.paramMap.get("fundingSourceID")!;

        this.viewModel$ = this.reload$.pipe(
            switchMap(() => forkJoin({
                fundingSource: this.fundingSourceService.getFundingSource(this.fundingSourceID).pipe(
                    catchError((err) => {
                        if (err?.status === 403) {
                            this.alertService.pushAlert(new Alert("You don't have permission to view that funding source.", AlertContext.Danger, true));
                            this.router.navigate(["/funding-sources"]);
                        }
                        return of(null as FundingSourceDto | null);
                    }),
                ),
                bmpFunding: this.fundingSourceService.listTreatmentBMPFundingFundingSource(this.fundingSourceID).pipe(
                    catchError(() => of([] as FundingSourceTreatmentBMPFundingDto[])),
                ),
            })),
            map((result) => result.fundingSource
                ? {
                      fundingSource: result.fundingSource,
                      bmpFunding: result.bmpFunding,
                      totalAmount: result.bmpFunding.reduce((sum, row) => sum + (row.Amount ?? 0), 0),
                  } as FundingSourceDetailViewModel
                : null,
            ),
            shareReplay(1),
        );
    }

    public openEditModal(fundingSource: FundingSourceDto): void {
        const dialogRef = this.dialogService.open(FundingSourceModalComponent, {
            data: { mode: "edit", fundingSource },
        });
        dialogRef.afterClosed$.subscribe((saved) => {
            if (saved) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Funding source updated successfully.", AlertContext.Success, true, "funding-source-saved"));
                this.reload$.next();
            }
        });
    }
}
