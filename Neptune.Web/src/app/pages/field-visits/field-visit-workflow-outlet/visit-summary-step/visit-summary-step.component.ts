import { Component, OnInit } from "@angular/core";
import { AsyncPipe, DatePipe, DecimalPipe } from "@angular/common";
import { Observable } from "rxjs";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { TreatmentBMPAssessmentService } from "src/app/shared/generated/api/treatment-bmp-assessment.service";
import { MaintenanceRecordService } from "src/app/shared/generated/api/maintenance-record.service";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AuthenticationService } from "src/app/services/authentication.service";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

@Component({
    selector: "field-visit-visit-summary-step",
    standalone: true,
    imports: [AsyncPipe, DatePipe, DecimalPipe, PageHeaderComponent],
    templateUrl: "./visit-summary-step.component.html",
    styleUrl: "./visit-summary-step.component.scss",
})
export class FieldVisitVisitSummaryStepComponent implements OnInit {
    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    public canManage = false;

    constructor(
        private workflowService: FieldVisitWorkflowService,
        private fieldVisitService: FieldVisitService,
        private assessmentService: TreatmentBMPAssessmentService,
        private maintenanceRecordService: MaintenanceRecordService,
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private authenticationService: AuthenticationService
    ) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.workflowService.clearStepAlerts();
        this.canManage = this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    // NPT-984: the delete buttons on each Visit Summary card need to disappear once the visit
    // is wrapped up (Editor walking through a finished visit shouldn't be able to nuke records,
    // and the Manager review surface shouldn't expose destructive actions either). Centralizing
    // the check here so the template can `@if (isEditable(workflow))` each delete button.
    public isEditable(workflow: FieldVisitWorkflowDto): boolean {
        return !this.workflowService.isReadOnly(workflow);
    }

    verify(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.clearStepAlerts();
        this.fieldVisitService.verifyFieldVisit(workflow.FieldVisitID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Field Visit verified.", AlertContext.Success));
            this.workflowService.refresh().subscribe();
        });
    }

    markProvisional(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.clearStepAlerts();
        this.fieldVisitService.markProvisionalFieldVisit(workflow.FieldVisitID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Field Visit marked Provisional.", AlertContext.Success));
            this.workflowService.refresh().subscribe();
        });
    }

    returnToEdit(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.clearStepAlerts();
        this.fieldVisitService.returnToEditFieldVisit(workflow.FieldVisitID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Field Visit returned to edit.", AlertContext.Success));
            this.workflowService.refresh().subscribe();
        });
    }

    deleteAssessment(workflow: FieldVisitWorkflowDto, kind: "Initial" | "Post-Maintenance"): void {
        const assessmentID = kind === "Initial" ? workflow.InitialAssessmentID : workflow.PostMaintenanceAssessmentID;
        if (!assessmentID) return;
        this.confirmService
            .confirm({
                title: `Delete ${kind} Assessment`,
                message: `Are you sure you want to delete the ${kind} Assessment for this Field Visit? This will remove all entered observations and photos.`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.workflowService.clearStepAlerts();
                this.assessmentService.deleteTreatmentBMPAssessment(assessmentID).subscribe(() => {
                    this.alertService.pushAlert(new Alert(`${kind} Assessment deleted.`, AlertContext.Success));
                    this.workflowService.refresh().subscribe();
                });
            });
    }

    deleteMaintenanceRecord(workflow: FieldVisitWorkflowDto): void {
        const recordID = workflow.MaintenanceRecordID;
        if (!recordID) return;
        this.confirmService
            .confirm({
                title: "Delete Maintenance Record",
                message: "Are you sure you want to delete the Maintenance Record for this Field Visit? This will remove all entered observations.",
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.workflowService.clearStepAlerts();
                this.maintenanceRecordService.deleteMaintenanceRecord(recordID).subscribe(() => {
                    this.alertService.pushAlert(new Alert("Maintenance Record deleted.", AlertContext.Success));
                    this.workflowService.refresh().subscribe();
                });
            });
    }
}
