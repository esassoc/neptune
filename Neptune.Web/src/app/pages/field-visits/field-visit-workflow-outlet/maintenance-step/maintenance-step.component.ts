import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { MaintenanceRecordService } from "src/app/shared/generated/api/maintenance-record.service";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

@Component({
    selector: "field-visit-maintenance-step",
    standalone: true,
    imports: [AsyncPipe],
    templateUrl: "./maintenance-step.component.html",
    styleUrl: "./maintenance-step.component.scss",
})
export class FieldVisitMaintenanceStepComponent implements OnInit {
    public workflow$: Observable<FieldVisitWorkflowDto | null>;

    constructor(
        private workflowService: FieldVisitWorkflowService,
        private maintenanceRecordService: MaintenanceRecordService,
        private alertService: AlertService,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
    }

    beginMaintenance(workflow: FieldVisitWorkflowDto): void {
        this.maintenanceRecordService.createForFieldVisitMaintenanceRecord(workflow.FieldVisitID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Maintenance Record started.", AlertContext.Success));
            this.workflowService.refresh().subscribe(() => {
                this.router.navigate(["/field-visits", workflow.FieldVisitID, "maintenance", "edit"]);
            });
        });
    }

    editMaintenance(workflow: FieldVisitWorkflowDto): void {
        this.router.navigate(["/field-visits", workflow.FieldVisitID, "maintenance", "edit"]);
    }

    skipToPostMaintenanceAssessment(workflow: FieldVisitWorkflowDto): void {
        this.router.navigate(["/field-visits", workflow.FieldVisitID, "post-maintenance-assessment"]);
    }
}
