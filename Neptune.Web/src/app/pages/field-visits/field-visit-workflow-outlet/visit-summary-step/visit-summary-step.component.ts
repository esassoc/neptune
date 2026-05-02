import { Component, OnInit } from "@angular/core";
import { AsyncPipe, DatePipe, DecimalPipe } from "@angular/common";
import { Observable } from "rxjs";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
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
        private alertService: AlertService,
        private authenticationService: AuthenticationService
    ) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.canManage = this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    finalize(workflow: FieldVisitWorkflowDto): void {
        this.fieldVisitService.finalizeFieldVisit(workflow.FieldVisitID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Field Visit marked Complete.", AlertContext.Success));
            this.workflowService.refresh().subscribe();
        });
    }

    verify(workflow: FieldVisitWorkflowDto): void {
        this.fieldVisitService.verifyFieldVisit(workflow.FieldVisitID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Field Visit verified.", AlertContext.Success));
            this.workflowService.refresh().subscribe();
        });
    }

    markProvisional(workflow: FieldVisitWorkflowDto): void {
        this.fieldVisitService.markProvisionalFieldVisit(workflow.FieldVisitID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Field Visit marked Provisional.", AlertContext.Success));
            this.workflowService.refresh().subscribe();
        });
    }

    returnToEdit(workflow: FieldVisitWorkflowDto): void {
        this.fieldVisitService.returnToEditFieldVisit(workflow.FieldVisitID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Field Visit returned to edit.", AlertContext.Success));
            this.workflowService.refresh().subscribe();
        });
    }

}
