import { Component, inject, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { BehaviorSubject, Observable, shareReplay, switchMap } from "rxjs";
import { ColDef, ICellRendererParams, SelectionChangedEvent } from "ag-grid-community";

import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

import { ManagerDashboardService } from "src/app/shared/generated/api/manager-dashboard.service";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { FieldVisitProvisionalGridDto } from "src/app/shared/generated/model/field-visit-provisional-grid-dto";
import { FieldVisitStatusEnum } from "src/app/shared/generated/enum/field-visit-status-enum";

@Component({
    selector: "field-visits-tab",
    standalone: true,
    imports: [AsyncPipe, RouterLink, NeptuneGridComponent, LoadingDirective],
    templateUrl: "./field-visits-tab.component.html",
    styleUrl: "./field-visits-tab.component.scss",
})
export class FieldVisitsTabComponent {
    private utility = inject(UtilityFunctionsService);
    private managerDashboardService = inject(ManagerDashboardService);
    private fieldVisitService = inject(FieldVisitService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private router = inject(Router);

    public readonly instructionText =
        "A list of all Field Visits that are in progress or are complete but not yet verified " +
        "(\"provisional\") are shown here. Click the View button to see details of the Field Visit and verify " +
        "the Field Visit Assessment and Maintenance records. You can also select multiple Field Visit records " +
        "and verify them in bulk through grid.";

    private reload$ = new BehaviorSubject<void>(undefined);
    public rows$: Observable<FieldVisitProvisionalGridDto[]> = this.reload$.pipe(
        switchMap(() => this.managerDashboardService.listProvisionalFieldVisitsManagerDashboard()),
        shareReplay(1)
    );
    public selectedRowIDs = signal<number[]>([]);
    public columnDefs: ColDef[] = this.buildColumnDefs();

    public onSelectionChanged(event: SelectionChangedEvent): void {
        const selected = event.api.getSelectedRows() as FieldVisitProvisionalGridDto[];
        this.selectedRowIDs.set(selected.map((r) => r.FieldVisitID));
    }

    public async verifySelected(): Promise<void> {
        const ids = this.selectedRowIDs();
        if (ids.length === 0) return;
        const confirmed = await this.confirmService.confirm({
            title: "Verify Field Visits",
            message: `Verify ${ids.length} selected Field Visit${ids.length === 1 ? "" : "s"}?`,
            buttonTextYes: "Verify",
            buttonTextNo: "Cancel",
            buttonClassYes: "btn btn-primary",
        });
        if (!confirmed) return;
        this.managerDashboardService
            .bulkVerifyFieldVisitsManagerDashboard({ IDs: ids })
            .subscribe({
                next: ({ VerifiedCount }) => {
                    this.alertService.pushAlert(new Alert(`${VerifiedCount} Field Visit${VerifiedCount === 1 ? "" : "s"} verified.`, AlertContext.Success));
                    this.selectedRowIDs.set([]);
                    this.reload$.next();
                },
                error: () => this.alertService.pushAlert(new Alert("Failed to verify Field Visits.", AlertContext.Danger)),
            });
    }

    private buildColumnDefs(): ColDef[] {
        return [
            this.utility.createCheckboxSelectionColumnDef(),
            this.utility.createActionsColumnDef((params: any) => {
                const row = params.data as FieldVisitProvisionalGridDto;
                if (!row) return [];
                // Continue when the visit is still in progress (the editor route); View otherwise.
                // In-progress visits route into the workflow outlet (/field-visits/:id, which
                // redirects to the inventory step); wrapped-up visits route to the read-only
                // detail surface (/field-visits/:id/view, NPT-984).
                const inProgress = !row.IsFieldVisitVerified && row.FieldVisitStatusID !== FieldVisitStatusEnum.Complete;
                const target = inProgress ? ["/field-visits", row.FieldVisitID] : ["/field-visits", row.FieldVisitID, "view"];
                return [
                    {
                        ActionName: inProgress ? "Continue" : "View",
                        ActionIcon: inProgress ? "fa fa-pencil" : "fa fa-eye",
                        ActionHandler: () => this.router.navigate(target),
                    },
                    {
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
                        ActionHandler: () => this.delete(row),
                    },
                ];
            }),
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", { InRouterLink: "/treatment-bmps/" }),
            this.utility.createDateColumnDef("Visit Date", "VisitDate", "MM/dd/yyyy"),
            this.utility.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "/jurisdictions/",
                UseCustomDropdownFilter: true,
            }),
            this.utility.createLinkColumnDef("Performed By", "PerformedByPersonName", "PerformedByPersonID", {
                InRouterLink: "/users/",
                UseCustomDropdownFilter: true,
            }),
            this.utility.createBasicColumnDef("Field Visit Status", "FieldVisitStatusDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Field Visit Type", "FieldVisitTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.buildAssessmentColumnDef("Initial Assessment", "TreatmentBMPAssessmentIDInitial", "IsAssessmentCompleteInitial"),
            this.utility.createDecimalColumnDef("Initial Assessment Score", "AssessmentScoreInitial", {
                DecimalPlacesToDisplay: 1,
                StringForNullValues: "-",
            }),
            this.buildMaintenanceOccurredColumnDef(),
            this.buildAssessmentColumnDef("Post-Maintenance Assessment", "TreatmentBMPAssessmentIDPM", "IsAssessmentCompletePM"),
            this.utility.createDecimalColumnDef("Post-Maintenance Assessment Score", "AssessmentScorePM", {
                DecimalPlacesToDisplay: 1,
                StringForNullValues: "-",
            }),
        ];
    }

    // Builds a status column that links to the SPA assessment detail page when an assessment
    // ID is present (Complete / In Progress link), or plain "Not Performed" text otherwise.
    // The anchor uses the SPA route directly so Angular's router intercepts the click and
    // keeps navigation in-app (no full page reload).
    private buildAssessmentColumnDef(
        headerName: string,
        idField: keyof FieldVisitProvisionalGridDto,
        completeFlagField: keyof FieldVisitProvisionalGridDto
    ): ColDef {
        return {
            headerName,
            valueGetter: (params) => {
                const row = params.data as FieldVisitProvisionalGridDto;
                if (!row?.[idField]) return "Not Performed";
                return row[completeFlagField] ? "Complete" : "In Progress";
            },
            cellRenderer: (params: ICellRendererParams) => {
                const row = params.data as FieldVisitProvisionalGridDto;
                if (!row?.[idField]) return "Not Performed";
                const text = row[completeFlagField] ? "Complete" : "In Progress";
                const a = document.createElement("a");
                a.href = `/treatment-bmp-assessments/${row[idField]}`;
                a.textContent = text;
                a.addEventListener("click", (e) => {
                    e.preventDefault();
                    this.router.navigate(["/treatment-bmp-assessments", row[idField]]);
                });
                return a;
            },
        };
    }

    // Builds the "Maintenance Occurred" column — hyperlinks "Performed" to the SPA
    // maintenance record detail page when MaintenanceRecordID exists, plain "Not Performed"
    // otherwise.
    private buildMaintenanceOccurredColumnDef(): ColDef {
        return {
            headerName: "Maintenance Occurred",
            valueGetter: (params) => {
                const row = params.data as FieldVisitProvisionalGridDto;
                return row?.MaintenanceRecordID != null ? "Performed" : "Not Performed";
            },
            cellRenderer: (params: ICellRendererParams) => {
                const row = params.data as FieldVisitProvisionalGridDto;
                if (!row?.MaintenanceRecordID) return "Not Performed";
                const a = document.createElement("a");
                a.href = `/maintenance-records/${row.MaintenanceRecordID}`;
                a.textContent = "Performed";
                a.addEventListener("click", (e) => {
                    e.preventDefault();
                    this.router.navigate(["/maintenance-records", row.MaintenanceRecordID]);
                });
                return a;
            },
        };
    }

    private async delete(row: FieldVisitProvisionalGridDto): Promise<void> {
        const confirmed = await this.confirmService.confirm({
            title: "Delete Field Visit",
            message: `Delete the field visit on <strong>${escapeHtml(row.TreatmentBMPName ?? "")}</strong>? This will also delete the assessment(s) and maintenance record for this visit.`,
            buttonTextYes: "Delete",
            buttonTextNo: "Cancel",
            buttonClassYes: "btn btn-danger",
        });
        if (!confirmed) return;
        this.fieldVisitService.deleteFieldVisit(row.FieldVisitID).subscribe({
            next: () => {
                this.alertService.pushAlert(new Alert("Field Visit deleted.", AlertContext.Success));
                this.reload$.next();
            },
            error: () => this.alertService.pushAlert(new Alert("Failed to delete Field Visit.", AlertContext.Danger)),
        });
    }
}
