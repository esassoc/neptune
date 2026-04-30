import { Component } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { Router, RouterModule } from "@angular/router";
import { ColDef } from "ag-grid-community";
import { Observable } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";

import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { TreatmentBMPAssessmentService } from "src/app/shared/generated/api/treatment-bmp-assessment.service";
import { MaintenanceRecordService } from "src/app/shared/generated/api/maintenance-record.service";
import { FieldVisitDto } from "src/app/shared/generated/model/field-visit-dto";
import { TreatmentBMPAssessmentGridDto } from "src/app/shared/generated/model/treatment-bmp-assessment-grid-dto";
import { MaintenanceRecordGridDto } from "src/app/shared/generated/model/maintenance-record-grid-dto";

import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AuthenticationService } from "src/app/services/authentication.service";

@Component({
    selector: "field-records",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe, RouterModule],
    templateUrl: "./field-records.component.html",
    styleUrl: "./field-records.component.scss",
})
export class FieldRecordsComponent {
    public fieldVisits$: Observable<FieldVisitDto[]>;
    public assessments$: Observable<TreatmentBMPAssessmentGridDto[]>;
    public maintenanceRecords$: Observable<MaintenanceRecordGridDto[]>;

    public fieldVisitColumnDefs: ColDef[];
    public assessmentColumnDefs: ColDef[];
    public maintenanceRecordColumnDefs: ColDef[];

    public canManage = false;

    constructor(
        private fieldVisitService: FieldVisitService,
        private assessmentService: TreatmentBMPAssessmentService,
        private maintenanceRecordService: MaintenanceRecordService,
        private utility: UtilityFunctionsService,
        private confirmService: ConfirmService,
        private alertService: AlertService,
        private authenticationService: AuthenticationService,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.canManage = this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();

        this.fieldVisitColumnDefs = this.buildFieldVisitColumnDefs();
        this.assessmentColumnDefs = this.buildAssessmentColumnDefs();
        this.maintenanceRecordColumnDefs = this.buildMaintenanceRecordColumnDefs();

        this.refresh();
    }

    private refresh(): void {
        this.fieldVisits$ = this.fieldVisitService.listFieldVisit();
        this.assessments$ = this.assessmentService.listTreatmentBMPAssessment();
        this.maintenanceRecords$ = this.maintenanceRecordService.listMaintenanceRecord();
    }

    private buildFieldVisitColumnDefs(): ColDef[] {
        const cols: ColDef[] = [];
        cols.push(
            this.utility.createActionsColumnDef((params: any) => {
                const visit: FieldVisitDto = params.data;
                const inProgress = visit.FieldVisitStatusID === 1; // FieldVisitStatusEnum.InProgress
                const actions: { ActionName: string; ActionIcon?: string; ActionHandler: () => void }[] = [
                    {
                        ActionName: inProgress ? "Continue" : "View",
                        ActionHandler: () => this.router.navigate(["/field-visits", visit.FieldVisitID]),
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
            this.utility.createBasicColumnDef("Jurisdiction", "OrganizationName"),
            this.utility.createBasicColumnDef("WQMP", "WaterQualityManagementPlanName"),
            this.utility.createBasicColumnDef("Performed By", "PerformedByPersonName"),
            this.utility.createBooleanColumnDef("Field Visit Verified", "IsFieldVisitVerified"),
            this.utility.createBasicColumnDef("Status", "FieldVisitStatusDisplayName"),
            this.utility.createBasicColumnDef("Visit Type", "FieldVisitTypeDisplayName"),
            this.utility.createBooleanColumnDef("Inventory Updated?", "InventoryUpdated"),
            this.utility.createBooleanColumnDef("Required Attributes Entered?", "RequiredAttributesEntered"),
            this.utility.createBasicColumnDef("Initial Assessment?", "InitialAssessmentStatus"),
            this.utility.createDecimalColumnDef("Initial Assessment Score", "AssessmentScoreInitial"),
            this.utility.createBasicColumnDef("Maintenance Occurred?", "MaintenanceOccurred"),
            this.utility.createBasicColumnDef("Post-Maintenance Assessment?", "PostMaintenanceAssessmentStatus"),
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
                        ActionName: "View Observations",
                        ActionHandler: () => this.router.navigate(["/field-visits", row.FieldVisitID, branch, "observations"]),
                    },
                ];
            }),
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
            }),
            this.utility.createBasicColumnDef("BMP Type", "TreatmentBMPTypeName"),
            this.utility.createDateColumnDef("Date", "VisitDate", "MM/dd/yyyy"),
            this.utility.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName"),
            this.utility.createBasicColumnDef("WQMP", "WaterQualityManagementPlanName"),
            this.utility.createBasicColumnDef("Performed By", "PerformedByPersonName"),
            this.utility.createBasicColumnDef("Field Visit Type", "FieldVisitTypeDisplayName"),
            this.utility.createBasicColumnDef("Assessment Type", "TreatmentBMPAssessmentTypeDisplayName"),
            this.utility.createBasicColumnDef("Status", "Status"),
            this.utility.createDecimalColumnDef("Score", "AssessmentScore"),
        ];
    }

    private buildMaintenanceRecordColumnDefs(): ColDef[] {
        return [
            this.utility.createActionsColumnDef((params: any) => {
                const row: MaintenanceRecordGridDto = params.data;
                return [
                    {
                        ActionName: "Edit",
                        ActionHandler: () => this.router.navigate(["/field-visits", row.FieldVisitID, "maintenance", "edit"]),
                    },
                ];
            }),
            this.utility.createLinkColumnDef("BMP Name", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
                FieldDefinitionType: "TreatmentBMP",
            }),
            this.utility.createDateColumnDef("Date", "VisitDate", "MM/dd/yyyy"),
            this.utility.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName"),
            this.utility.createBasicColumnDef("WQMP", "WaterQualityManagementPlanName"),
            this.utility.createBasicColumnDef("Performed By", "PerformedByPersonName"),
            this.utility.createBasicColumnDef("Maintenance Type", "MaintenanceRecordTypeDisplayName"),
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
        this.confirmService
            .confirm({
                title: "Delete Field Visit",
                message: `<p>You are about to delete the field visit for <strong>${visit.TreatmentBMPName ?? "this BMP"}</strong> dated ${new Date(visit.VisitDate).toLocaleDateString()}.</p><p>This will also delete the assessment(s) and maintenance record associated with this visit.</p><p>Are you sure you wish to proceed?</p>`,
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
