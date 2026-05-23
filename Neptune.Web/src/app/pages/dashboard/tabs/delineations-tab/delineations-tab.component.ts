import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { BehaviorSubject, Observable, shareReplay, switchMap } from "rxjs";
import { ColDef, SelectionChangedEvent } from "ag-grid-community";

import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

import { ManagerDashboardService } from "src/app/shared/generated/api/manager-dashboard.service";
import { DelineationService } from "src/app/shared/generated/api/delineation.service";
import { DelineationProvisionalGridDto } from "src/app/shared/generated/model/delineation-provisional-grid-dto";

@Component({
    selector: "delineations-tab",
    standalone: true,
    imports: [AsyncPipe, RouterLink, NeptuneGridComponent],
    templateUrl: "./delineations-tab.component.html",
})
export class DelineationsTabComponent {
    private utility = inject(UtilityFunctionsService);
    private managerDashboardService = inject(ManagerDashboardService);
    private delineationService = inject(DelineationService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private router = inject(Router);

    public readonly instructionText =
        "A list of all BMPs with unverified delineations are shown here. Click the View button to see delineation " +
        "of an individual BMP and verify the record. You can also select multiple BMP records and verify them in " +
        "bulk through grid.";

    private reload$ = new BehaviorSubject<void>(undefined);
    public rows$: Observable<DelineationProvisionalGridDto[]> = this.reload$.pipe(
        switchMap(() => this.managerDashboardService.listProvisionalDelineationsManagerDashboard()),
        shareReplay(1)
    );
    public selectedRowIDs = signal<number[]>([]);
    public columnDefs: ColDef[] = this.buildColumnDefs();

    public onSelectionChanged(event: SelectionChangedEvent): void {
        const selected = event.api.getSelectedRows() as DelineationProvisionalGridDto[];
        this.selectedRowIDs.set(selected.map((r) => r.DelineationID));
    }

    public async verifySelected(): Promise<void> {
        const ids = this.selectedRowIDs();
        if (ids.length === 0) return;
        const confirmed = await this.confirmService.confirm({
            title: "Verify Delineations",
            message: `Verify ${ids.length} selected delineation${ids.length === 1 ? "" : "s"}?`,
            buttonTextYes: "Verify",
            buttonTextNo: "Cancel",
            buttonClassYes: "btn btn-primary",
        });
        if (!confirmed) return;
        this.managerDashboardService
            .bulkVerifyDelineationsManagerDashboard({ IDs: ids })
            .subscribe({
                next: ({ VerifiedCount }) => {
                    this.alertService.pushAlert(new Alert(`${VerifiedCount} delineation${VerifiedCount === 1 ? "" : "s"} verified.`, AlertContext.Success));
                    this.selectedRowIDs.set([]);
                    this.reload$.next();
                },
                error: () => this.alertService.pushAlert(new Alert("Failed to verify delineations.", AlertContext.Danger)),
            });
    }

    private buildColumnDefs(): ColDef[] {
        return [
            this.utility.createCheckboxSelectionColumnDef(),
            this.utility.createActionsColumnDef((params: any) => {
                const row = params.data as DelineationProvisionalGridDto;
                if (!row) return [];
                return [
                    {
                        ActionName: "View on Map",
                        ActionIcon: "fas fa-map-location-dot",
                        ActionHandler: () =>
                            this.router.navigate(["/delineation/delineation-map"], { queryParams: { treatmentBMPID: row.TreatmentBMPID } }),
                    },
                    {
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
                        ActionHandler: () => this.delete(row),
                    },
                ];
            }),
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", { InRouterLink: "/treatment-bmps/" }),
            this.utility.createBasicColumnDef("BMP Type", "TreatmentBMPTypeName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Delineation Type", "DelineationTypeName", { UseCustomDropdownFilter: true }),
            this.utility.createDecimalColumnDef("Delineation Area (ac)", "DelineationAreaInAcres", {
                DecimalPlacesToDisplay: 2,
                FieldDefinitionType: "Area",
                FieldDefinitionLabelOverride: "Delineation Area (ac)",
            }),
            this.utility.createDateColumnDef("Date of Last Delineation Modification", "DateLastModified", "MM/dd/yyyy"),
            this.utility.createDateColumnDef("Date of Last Delineation Verification", "DateLastVerified", "MM/dd/yyyy"),
            this.utility.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "/jurisdictions/",
                UseCustomDropdownFilter: true,
            }),
        ];
    }

    private async delete(row: DelineationProvisionalGridDto): Promise<void> {
        const confirmed = await this.confirmService.confirm({
            title: "Delete Delineation",
            message: `Delete the delineation for <strong>${escapeHtml(row.TreatmentBMPName ?? "")}</strong>? This cannot be undone.`,
            buttonTextYes: "Delete",
            buttonTextNo: "Cancel",
            buttonClassYes: "btn btn-danger",
        });
        if (!confirmed) return;
        this.delineationService.deleteForTreatmentBMPDelineation(row.TreatmentBMPID).subscribe({
            next: () => {
                this.alertService.pushAlert(new Alert("Delineation deleted.", AlertContext.Success));
                this.reload$.next();
            },
            error: () => this.alertService.pushAlert(new Alert("Failed to delete delineation.", AlertContext.Danger)),
        });
    }
}
