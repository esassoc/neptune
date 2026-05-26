import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { BehaviorSubject, Observable, shareReplay, switchMap } from "rxjs";
import { ColDef, SelectionChangedEvent } from "ag-grid-community";

import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

import { ManagerDashboardService } from "src/app/shared/generated/api/manager-dashboard.service";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPProvisionalGridDto } from "src/app/shared/generated/model/treatment-bmp-provisional-grid-dto";

@Component({
    selector: "bmp-records-tab",
    standalone: true,
    imports: [AsyncPipe, RouterLink, NeptuneGridComponent, LoadingDirective],
    templateUrl: "./bmp-records-tab.component.html",
    styleUrl: "./bmp-records-tab.component.scss",
})
export class BmpRecordsTabComponent {
    private utility = inject(UtilityFunctionsService);
    private managerDashboardService = inject(ManagerDashboardService);
    private treatmentBMPService = inject(TreatmentBMPService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private router = inject(Router);

    public readonly instructionText =
        "A list of all BMPs with unverified inventory data is shown here. Click the View button to see details of " +
        "an individual BMP and verify the BMP inventory data. You can also select multiple BMP records and verify " +
        "them in bulk through grid.";

    private reload$ = new BehaviorSubject<void>(undefined);
    public rows$: Observable<TreatmentBMPProvisionalGridDto[]> = this.reload$.pipe(
        switchMap(() => this.managerDashboardService.listProvisionalTreatmentBMPsManagerDashboard()),
        shareReplay(1)
    );
    public selectedRowIDs = signal<number[]>([]);
    public columnDefs: ColDef[] = this.buildColumnDefs();

    public onSelectionChanged(event: SelectionChangedEvent): void {
        const selected = event.api.getSelectedRows() as TreatmentBMPProvisionalGridDto[];
        this.selectedRowIDs.set(selected.map((r) => r.TreatmentBMPID));
    }

    public async verifySelected(): Promise<void> {
        const ids = this.selectedRowIDs();
        if (ids.length === 0) return;
        const confirmed = await this.confirmService.confirm({
            title: "Verify BMP Records",
            message: `Verify ${ids.length} selected BMP record${ids.length === 1 ? "" : "s"}?`,
            buttonTextYes: "Verify",
            buttonTextNo: "Cancel",
            buttonClassYes: "btn btn-primary",
        });
        if (!confirmed) return;
        this.managerDashboardService
            .bulkVerifyTreatmentBMPsManagerDashboard({ IDs: ids })
            .subscribe({
                next: ({ VerifiedCount }) => {
                    this.alertService.pushAlert(new Alert(`${VerifiedCount} BMP record${VerifiedCount === 1 ? "" : "s"} verified.`, AlertContext.Success));
                    this.selectedRowIDs.set([]);
                    this.reload$.next();
                },
                error: () => this.alertService.pushAlert(new Alert("Failed to verify BMP records.", AlertContext.Danger)),
            });
    }

    private buildColumnDefs(): ColDef[] {
        return [
            this.utility.createCheckboxSelectionColumnDef(),
            this.utility.createActionsColumnDef((params: any) => {
                const row = params.data as TreatmentBMPProvisionalGridDto;
                if (!row) return [];
                const actions: any[] = [
                    {
                        ActionName: "View",
                        ActionIcon: "fa fa-eye",
                        ActionHandler: () => this.router.navigate(["/treatment-bmps", row.TreatmentBMPID]),
                    },
                ];
                if (row.CanDelete) {
                    actions.push({
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
                        ActionHandler: () => this.delete(row),
                    });
                }
                return actions;
            }),
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", { InRouterLink: "/treatment-bmps/" }),
            this.utility.createBasicColumnDef("BMP Type", "TreatmentBMPTypeName", { UseCustomDropdownFilter: true }),
            this.utility.createDateColumnDef("Date of Last BMP Record Verification", "DateOfLastInventoryVerification", "MM/dd/yyyy"),
            this.utility.createDateColumnDef("Date of Last Inventory Change", "InventoryLastChangedDate", "MM/dd/yyyy"),
            this.utility.createBooleanColumnDef("Has Photos", "HasPhotos", { UseCustomDropdownFilter: true }),
            this.utility.createBooleanColumnDef("Benchmark and Thresholds Set", "BenchmarkAndThresholdsSet", { UseCustomDropdownFilter: true }),
            this.utility.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "/jurisdictions/",
                UseCustomDropdownFilter: true,
            }),
        ];
    }

    private async delete(row: TreatmentBMPProvisionalGridDto): Promise<void> {
        const confirmed = await this.confirmService.confirm({
            title: "Delete BMP",
            message: `Delete <strong>${escapeHtml(row.TreatmentBMPName ?? "")}</strong>? This cannot be undone.`,
            buttonTextYes: "Delete",
            buttonTextNo: "Cancel",
            buttonClassYes: "btn btn-danger",
        });
        if (!confirmed) return;
        this.treatmentBMPService.deleteTreatmentBMP(row.TreatmentBMPID).subscribe({
            next: () => {
                this.alertService.pushAlert(new Alert("BMP deleted.", AlertContext.Success));
                this.reload$.next();
            },
            error: () => this.alertService.pushAlert(new Alert("Failed to delete BMP.", AlertContext.Danger)),
        });
    }
}
