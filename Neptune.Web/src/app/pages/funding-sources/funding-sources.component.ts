import { Component } from "@angular/core";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { AsyncPipe } from "@angular/common";
import { FundingSourceService } from "src/app/shared/generated/api/funding-source.service";
import { FundingSourceDto } from "src/app/shared/generated/model/funding-source-dto";
import { ColDef } from "ag-grid-community";
import { BehaviorSubject, Observable, switchMap } from "rxjs";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { FundingSourceModalComponent } from "./funding-source-modal/funding-source-modal.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { DialogService } from "@ngneat/dialog";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

@Component({
    selector: "funding-sources",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    templateUrl: "./funding-sources.component.html",
})
export class FundingSourcesComponent {
    public fundingSources$: Observable<FundingSourceDto[]>;
    public columnDefs: ColDef[];
    public customRichTextTypeID = NeptunePageTypeEnum.FundingSourcesList;
    private reload$ = new BehaviorSubject<void>(undefined);

    constructor(
        private fundingSourceService: FundingSourceService,
        private utilityFunctions: UtilityFunctionsService,
        private dialogService: DialogService,
        private alertService: AlertService,
        private confirmService: ConfirmService
    ) {}

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctions.createActionsColumnDef((params: any) => [
                {
                    ActionName: "Edit",
                    ActionIcon: "fa fa-edit",
                    ActionHandler: () => this.openEditModal(params.data),
                },
                {
                    ActionName: "Delete",
                    ActionIcon: "fa fa-trash text-danger",
                    ActionHandler: () => this.deleteFundingSource(params.data),
                },
            ]),
            this.utilityFunctions.createLinkColumnDef("Name", "FundingSourceName", "FundingSourceID", {
                InRouterLink: "/funding-sources/",
            }),
            this.utilityFunctions.createLinkColumnDef("Organization", "OrganizationName", "OrganizationID", {
                InRouterLink: "/organizations/",
            }),
            this.utilityFunctions.createBasicColumnDef("Description", "FundingSourceDescription"),
            this.utilityFunctions.createBooleanColumnDef("Active?", "IsActive"),
        ];
        this.fundingSources$ = this.reload$.pipe(switchMap(() => this.fundingSourceService.listFundingSource()));
    }

    openAddModal() {
        const dialogRef = this.dialogService.open(FundingSourceModalComponent, {
            data: {
                mode: "add",
                fundingSource: null,
            },
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Funding source added successfully.", AlertContext.Success));
                this.reload$.next();
            }
        });
    }

    openEditModal(fundingSource: FundingSourceDto) {
        const dialogRef = this.dialogService.open(FundingSourceModalComponent, {
            data: {
                mode: "edit",
                fundingSource,
            },
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Funding source updated successfully.", AlertContext.Success));
                this.reload$.next();
            }
        });
    }

    deleteFundingSource(fundingSource: FundingSourceDto) {
        this.confirmService
            .confirm({
                title: "Delete Funding Source",
                message: `Are you sure you want to delete funding source '<strong>${fundingSource.FundingSourceName}</strong>'?`,
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
                buttonClassYes: "btn-danger",
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.fundingSourceService.deleteFundingSource(fundingSource.FundingSourceID).subscribe(() => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("Funding source deleted successfully.", AlertContext.Success));
                        this.reload$.next();
                    });
                }
            });
    }
}
