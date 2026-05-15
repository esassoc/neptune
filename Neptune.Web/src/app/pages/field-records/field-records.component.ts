import { Component, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { ActivatedRoute, Router, RouterModule } from "@angular/router";
import { ColDef } from "ag-grid-community";
import { BehaviorSubject, Observable } from "rxjs";
import { switchMap } from "rxjs/operators";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import {
    BtnGroupRadioInputComponent,
    IBtnGroupRadioInputOption,
} from "src/app/shared/components/inputs/btn-group-radio-input/btn-group-radio-input.component";

import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { TreatmentBMPAssessmentService } from "src/app/shared/generated/api/treatment-bmp-assessment.service";
import { MaintenanceRecordService } from "src/app/shared/generated/api/maintenance-record.service";
import { FieldVisitDto } from "src/app/shared/generated/model/field-visit-dto";
import { TreatmentBMPAssessmentGridDto } from "src/app/shared/generated/model/treatment-bmp-assessment-grid-dto";
import { MaintenanceRecordGridDto } from "src/app/shared/generated/model/maintenance-record-grid-dto";

import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { escapeHtml } from "src/app/shared/helpers/html-escape";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AuthenticationService } from "src/app/services/authentication.service";

type ActiveTab = "field-visits" | "assessments" | "maintenance-records";

@Component({
    selector: "field-records",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe, RouterModule, BtnGroupRadioInputComponent],
    templateUrl: "./field-records.component.html",
    styleUrl: "./field-records.component.scss",
})
export class FieldRecordsComponent implements OnInit {
    // NPT-984: wire the three grids' observables through a class-level reload trigger so the
    // `| async` pipes have a real Observable to subscribe to at template-init time. Previously
    // the fields were declared without a value and assigned inside refresh() during ngOnInit;
    // on lazy-loaded routes in zoneless mode that race led to the page rendering empty until
    // the user clicked somewhere to force a CD pass (Kathleen's "page does not load until I
    // click it myself" report). The reload$ trigger replaces the prior reassignment pattern
    // while still letting refresh() re-fetch after deletes.
    private reload$ = new BehaviorSubject<void>(undefined);
    public fieldVisits$: Observable<FieldVisitDto[]>;
    public assessments$: Observable<TreatmentBMPAssessmentGridDto[]>;
    public maintenanceRecords$: Observable<MaintenanceRecordGridDto[]>;

    public fieldVisitColumnDefs: ColDef[];
    public assessmentColumnDefs: ColDef[];
    public maintenanceRecordColumnDefs: ColDef[];

    public canManage = false;

    /** Tabs are sync'd to a `?tab=` query param so refresh and back-button preserve the user's view. */
    public activeTab: ActiveTab = "field-visits";
    public tabOptions: IBtnGroupRadioInputOption[] = [
        { label: "Field Visits", value: "field-visits" },
        { label: "Assessment Records", value: "assessments" },
        { label: "Maintenance Records", value: "maintenance-records" },
    ];

    constructor(
        private fieldVisitService: FieldVisitService,
        private assessmentService: TreatmentBMPAssessmentService,
        private maintenanceRecordService: MaintenanceRecordService,
        private utility: UtilityFunctionsService,
        private confirmService: ConfirmService,
        private alertService: AlertService,
        private authenticationService: AuthenticationService,
        private router: Router,
        private route: ActivatedRoute
    ) {}

    ngOnInit(): void {
        this.canManage = this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();

        const initialTab = this.route.snapshot.queryParamMap.get("tab") as ActiveTab | null;
        if (initialTab && this.tabOptions.some((o) => o.value === initialTab)) {
            this.activeTab = initialTab;
        }

        this.fieldVisitColumnDefs = this.buildFieldVisitColumnDefs();
        this.assessmentColumnDefs = this.buildAssessmentColumnDefs();
        this.maintenanceRecordColumnDefs = this.buildMaintenanceRecordColumnDefs();

        // Pipe each grid feed off the shared reload trigger so `refresh()` re-fetches all three
        // and the async pipes never see an undefined observable.
        this.fieldVisits$ = this.reload$.pipe(switchMap(() => this.fieldVisitService.listFieldVisit()));
        this.assessments$ = this.reload$.pipe(switchMap(() => this.assessmentService.listTreatmentBMPAssessment()));
        this.maintenanceRecords$ = this.reload$.pipe(switchMap(() => this.maintenanceRecordService.listMaintenanceRecord()));
    }

    public onTabChange(value: string): void {
        const next = value as ActiveTab;
        if (this.activeTab === next) return;
        this.activeTab = next;
        this.router.navigate([], {
            relativeTo: this.route,
            queryParams: { tab: next },
            queryParamsHandling: "merge",
            replaceUrl: true,
        });
    }

    private refresh(): void {
        this.reload$.next();
    }

    private buildFieldVisitColumnDefs(): ColDef[] {
        const cols: ColDef[] = [];
        cols.push(
            this.utility.createActionsColumnDef((params: any) => {
                const visit: FieldVisitDto = params.data;
                const inProgress = visit.FieldVisitStatusID === 1; // FieldVisitStatusEnum.InProgress
                // NPT-984: route in-progress visits to the editable workflow outlet and
                // wrapped-up / unresolved / returned visits to the new read-only detail page.
                // The read-only page provides a fully locked-down summary view with
                // MVC-style observation tables, photos, and Manager-only Mark Provisional
                // action that flips back to the editable workflow.
                const actions: { ActionName: string; ActionIcon?: string; ActionHandler: () => void }[] = [
                    {
                        ActionName: inProgress ? "Continue" : "View",
                        ActionHandler: () => this.router.navigate(
                            inProgress
                                ? ["/field-visits", visit.FieldVisitID]
                                : ["/field-visits", visit.FieldVisitID, "view"],
                        ),
                    },
                ];
                if (this.canManage) {
                    actions.push({
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
                        ActionHandler: () => this.deleteFieldVisit(params),
                    });
                }
                return actions;
            })
        );

        cols.push(
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
            }),
            this.utility.createDateColumnDef("Visit Date", "VisitDate", "MM/dd/yyyy"),
            this.utility.createBasicColumnDef("Jurisdiction", "OrganizationName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("WQMP", "WaterQualityManagementPlanName"),
            this.utility.createBasicColumnDef("Performed By", "PerformedByPersonName"),
            this.utility.createBooleanColumnDef("Field Visit Verified", "IsFieldVisitVerified", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Status", "FieldVisitStatusDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Visit Type", "FieldVisitTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBooleanColumnDef("Inventory Updated?", "InventoryUpdated", { UseCustomDropdownFilter: true }),
            this.utility.createBooleanColumnDef("Required Attributes Entered?", "RequiredAttributesEntered", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Initial Assessment?", "InitialAssessmentStatus", { UseCustomDropdownFilter: true }),
            this.utility.createDecimalColumnDef("Initial Assessment Score", "AssessmentScoreInitial"),
            this.utility.createBasicColumnDef("Maintenance Occurred?", "MaintenanceOccurred", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Post-Maintenance Assessment?", "PostMaintenanceAssessmentStatus", { UseCustomDropdownFilter: true }),
            this.utility.createDecimalColumnDef("Post-Maintenance Assessment Score", "AssessmentScorePM")
        );
        return cols;
    }

    private buildAssessmentColumnDefs(): ColDef[] {
        return [
            this.utility.createActionsColumnDef((params: any) => {
                const row: TreatmentBMPAssessmentGridDto = params.data;
                // TreatmentBMPAssessmentTypeEnum: 1 = Initial, 2 = PostMaintenance
                const branch = row.TreatmentBMPAssessmentTypeDisplayName?.toLowerCase().includes("post") ? "post-maintenance-assessment" : "assessment";
                return [
                    {
                        ActionName: row.IsFieldVisitVerified ? "View Observations" : "Edit Observations",
                        ActionHandler: () => this.router.navigate(["/field-visits", row.FieldVisitID, branch, "observations"]),
                    },
                ];
            }),
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
            }),
            this.utility.createBasicColumnDef("BMP Type", "TreatmentBMPTypeName", { UseCustomDropdownFilter: true }),
            this.utility.createDateColumnDef("Date", "VisitDate", "MM/dd/yyyy"),
            this.utility.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("WQMP", "WaterQualityManagementPlanName"),
            this.utility.createBasicColumnDef("Performed By", "PerformedByPersonName"),
            this.utility.createBasicColumnDef("Field Visit Type", "FieldVisitTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Assessment Type", "TreatmentBMPAssessmentTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Status", "Status", { UseCustomDropdownFilter: true }),
            this.utility.createDecimalColumnDef("Score", "AssessmentScore"),
        ];
    }

    private buildMaintenanceRecordColumnDefs(): ColDef[] {
        return [
            this.utility.createActionsColumnDef((params: any) => {
                const row: MaintenanceRecordGridDto = params.data;
                return [
                    {
                        ActionName: row.IsFieldVisitVerified ? "View" : "Edit",
                        ActionHandler: () => this.router.navigate(["/field-visits", row.FieldVisitID, "maintenance", "edit"]),
                    },
                ];
            }),
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
            }),
            this.utility.createDateColumnDef("Date", "VisitDate", "MM/dd/yyyy"),
            this.utility.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("WQMP", "WaterQualityManagementPlanName"),
            this.utility.createBasicColumnDef("Performed By", "PerformedByPersonName"),
            this.utility.createBasicColumnDef("Maintenance Type", "MaintenanceRecordTypeDisplayName", { UseCustomDropdownFilter: true }),
            this.utility.createBasicColumnDef("Description", "MaintenanceRecordDescription"),
            this.utility.createBasicColumnDef("Structural Repair", "StructuralRepairConducted"),
            this.utility.createBasicColumnDef("Mechanical Repair", "MechanicalRepairConducted"),
            this.utility.createBasicColumnDef("Filtration Surface Restored", "FiltrationSurfaceRestored"),
            this.utility.createBasicColumnDef("Infiltration Surface Restored", "InfiltrationSurfaceRestored"),
            this.utility.createBasicColumnDef("Media Replaced", "MediaReplaced"),
            this.utility.createBasicColumnDef("Mulch Added", "MulchAdded"),
            this.utility.createBasicColumnDef("% Trash", "PercentTrash"),
            this.utility.createBasicColumnDef("% Green Waste", "PercentGreenWaste"),
            this.utility.createBasicColumnDef("% Sediment", "PercentSediment"),
            this.utility.createBasicColumnDef("Area Reseeded", "AreaReseeded"),
            this.utility.createBasicColumnDef("Vegetation Planted", "VegetationPlanted"),
            this.utility.createBasicColumnDef("Surface Erosion Repaired", "SurfaceAndBankErosionRepaired"),
            this.utility.createBasicColumnDef("Material Removed (cu-ft)", "TotalMaterialVolumeRemovedCubicFeet"),
            this.utility.createBasicColumnDef("Material Removed (gal)", "TotalMaterialVolumeRemovedGallons"),
        ];
    }

    private deleteFieldVisit(params: any): void {
        const visit: FieldVisitDto = params.data;
        const bmpName = escapeHtml(visit.TreatmentBMPName ?? "this BMP");
        const visitDate = new Date(visit.VisitDate).toLocaleDateString();
        this.confirmService
            .confirm({
                title: "Delete Field Visit",
                message: `<p>You are about to delete the field visit for <strong>${bmpName}</strong> dated ${visitDate}.</p><p>This will also delete the assessment(s) and maintenance record associated with this visit.</p><p>Are you sure you wish to proceed?</p>`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.fieldVisitService.deleteFieldVisit(visit.FieldVisitID).subscribe(() => {
                        this.alertService.pushAlert(new Alert("Successfully deleted Field Visit.", AlertContext.Success));
                        params.api.applyTransaction({ remove: [visit] });
                        this.refresh();
                    });
                }
            });
    }
}
