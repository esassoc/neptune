import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject } from "@angular/core";
import { AsyncPipe, DatePipe } from "@angular/common";
import { ColDef, ICellRendererParams } from "ag-grid-community";
import { BehaviorSubject, Observable, switchMap, tap } from "rxjs";
import { Router } from "@angular/router";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

import { DelineationService } from "src/app/shared/generated/api/delineation.service";
import { DelineationReconciliationReportContextDto } from "src/app/shared/generated/model/delineation-reconciliation-report-context-dto";
import { DelineationReconciliationDiscrepancyGridDto } from "src/app/shared/generated/model/delineation-reconciliation-discrepancy-grid-dto";
import { DelineationReconciliationOverlapGridDto } from "src/app/shared/generated/model/delineation-reconciliation-overlap-grid-dto";

@Component({
    selector: "delineation-reconciliation-report",
    standalone: true,
    templateUrl: "./delineation-reconciliation-report.component.html",
    styleUrl: "./delineation-reconciliation-report.component.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AsyncPipe, DatePipe, PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent],
})
export class DelineationReconciliationReportComponent implements OnInit {
    public customRichTextTypeID = NeptunePageTypeEnum.DelineationReconciliationReport;
    public context$: Observable<DelineationReconciliationReportContextDto>;
    public discrepancies$: Observable<DelineationReconciliationDiscrepancyGridDto[]>;
    public overlaps$: Observable<DelineationReconciliationOverlapGridDto[]>;

    public discrepancyColumnDefs: ColDef[] = [];
    public overlapColumnDefs: ColDef[] = [];

    public isEnqueueing = false;
    private refreshTrigger$ = new BehaviorSubject<void>(undefined);

    private readonly delineationService = inject(DelineationService);
    private readonly utility = inject(UtilityFunctionsService);
    private readonly confirmService = inject(ConfirmService);
    private readonly alertService = inject(AlertService);
    private readonly cdr = inject(ChangeDetectorRef);
    private readonly router = inject(Router);

    ngOnInit(): void {
        this.discrepancyColumnDefs = this.buildDiscrepancyColumnDefs();
        this.overlapColumnDefs = this.buildOverlapColumnDefs();

        this.context$ = this.delineationService.getReconciliationReportContextDelineation();

        this.discrepancies$ = this.refreshTrigger$.pipe(switchMap(() => this.delineationService.getReconciliationDiscrepanciesDelineation()));
        this.overlaps$ = this.refreshTrigger$.pipe(switchMap(() => this.delineationService.getReconciliationOverlapsDelineation()));
    }

    public onCheckForDiscrepancies(): void {
        this.confirmService
            .confirm({
                title: "Check for Discrepancies between Delineations and Regional Subbasin layers",
                message:
                    `<p>This will queue a background job to refresh the discrepancy flags and overlap rows. ` +
                    `The job may take several minutes; refresh the page when it's done to see updated results.</p>` +
                    `<p>Continue?</p>`,
                buttonClassYes: "btn btn-primary",
                buttonTextYes: "Continue",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.isEnqueueing = true;
                this.cdr.detectChanges();
                this.delineationService.enqueueReconciliationCheckDelineation().subscribe({
                    next: (res: any) => {
                        this.isEnqueueing = false;
                        this.alertService.pushAlert(
                            new Alert(res?.message ?? "The discrepancy check job has been queued.", AlertContext.Success, true)
                        );
                        this.cdr.detectChanges();
                    },
                    error: () => {
                        this.isEnqueueing = false;
                        this.cdr.detectChanges();
                    },
                });
            });
    }

    private buildDiscrepancyColumnDefs(): ColDef[] {
        return [
            this.utility.createActionsColumnDef((params: any) => [
                {
                    ActionName: "View on Map",
                    ActionIcon: "fas fa-map-location-dot",
                    ActionHandler: () =>
                        this.router.navigate(["/delineation/delineation-map"], { queryParams: { treatmentBMPID: params.data.TreatmentBMPID } }),
                },
            ]),
            this.utility.createLinkColumnDef("Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "../../treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
                FieldDefinitionLabelOverride: "Name",
            }),
            this.utility.createBasicColumnDef("Type", "TreatmentBMPTypeName", {
                CustomDropdownFilterField: "TreatmentBMPTypeName",
                FieldDefinitionType: "TreatmentBMPType",
                FieldDefinitionLabelOverride: "Type",
            }),
            this.utility.createBasicColumnDef("Delineation Type", "DelineationTypeName", {
                CustomDropdownFilterField: "DelineationTypeName",
                FieldDefinitionType: "DelineationType",
            }),
            this.utility.createDecimalColumnDef("Area (ac)", "AreaInAcres", {
                DecimalPlacesToDisplay: 2,
                FieldDefinitionType: "Area",
                FieldDefinitionLabelOverride: "Area (ac)",
            }),
            this.utility.createDateColumnDef("Date of Last Delineation Modification", "DateLastModified", "short"),
            this.utility.createDateColumnDef("Date of Last Delineation Verification", "DateLastVerified", "short"),
            this.utility.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "../../jurisdictions/",
                FieldDefinitionType: "Jurisdiction",
            }),
        ];
    }

    private buildOverlapColumnDefs(): ColDef[] {
        return [
            this.utility.createActionsColumnDef((params: any) => [
                {
                    ActionName: "View on Map",
                    ActionIcon: "fas fa-map-location-dot",
                    ActionHandler: () =>
                        this.router.navigate(["/delineation/delineation-map"], { queryParams: { treatmentBMPID: params.data.TreatmentBMPID } }),
                },
            ]),
            this.utility.createLinkColumnDef("Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "../../treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
                FieldDefinitionLabelOverride: "Name",
            }),
            this.utility.createBasicColumnDef("Type", "TreatmentBMPTypeName", {
                CustomDropdownFilterField: "TreatmentBMPTypeName",
                FieldDefinitionType: "TreatmentBMPType",
                FieldDefinitionLabelOverride: "Type",
            }),
            this.utility.createDecimalColumnDef("Area (ac)", "AreaInAcres", {
                DecimalPlacesToDisplay: 2,
                FieldDefinitionType: "Area",
                FieldDefinitionLabelOverride: "Area (ac)",
            }),
            this.utility.createDateColumnDef("Date of Last Delineation Modification", "DateLastModified", "short"),
            this.utility.createDateColumnDef("Date of Last Delineation Verification", "DateLastVerified", "short"),
            this.utility.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "../../jurisdictions/",
                FieldDefinitionType: "Jurisdiction",
            }),
            this.utility.createDecimalColumnDef("Area of Overlap (ac)", "AreaOfOverlapInAcres", {
                DecimalPlacesToDisplay: 2,
                FieldDefinitionType: "Area",
                FieldDefinitionLabelOverride: "Area of Overlap (ac)",
            }),
            {
                headerName: "Overlapping Delineations",
                field: "OverlappingDelineations",
                width: 280,
                filter: false,
                sortable: false,
                cellRenderer: (p: ICellRendererParams) =>
                    ((p.value as { TreatmentBMPID: number; TreatmentBMPName: string }[]) ?? [])
                        .map((x) => `<a href="/treatment-bmps/${x.TreatmentBMPID}">${escapeHtml(x.TreatmentBMPName ?? "")}</a>`)
                        .join(", "),
            },
        ];
    }
}
