import { Component, Input, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { TreatmentBMPAssessmentByFieldVisitService } from "src/app/shared/generated/api/treatment-bmp-assessment-by-field-visit.service";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { IconComponent } from "src/app/shared/components/icon/icon.component";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/**
 * AssessmentStepComponent renders both the Initial and Post-Maintenance assessment
 * landing pages — the layout is identical (workflow description, navigation hint,
 * icon legend explaining sub-step completion icons, and Begin/Continue buttons),
 * only the assessment type and the skip target differ. Pass the type via route data:
 * `{ assessmentTypeID: 1 | 2 }` — `withComponentInputBinding()` binds it to the
 * `assessmentTypeID` @Input. The actual observations form lives in
 * `ObservationsStepComponent` (the Observations sub-step under each assessment).
 */
@Component({
    selector: "field-visit-assessment-step",
    standalone: true,
    imports: [AsyncPipe, PageHeaderComponent, IconComponent],
    templateUrl: "./assessment-step.component.html",
    styleUrl: "./assessment-step.component.scss",
})
export class FieldVisitAssessmentStepComponent implements OnInit {
    /** 1 = Initial, 2 = PostMaintenance — passed via route data. */
    @Input() assessmentTypeID: number = 1;
    public workflow$: Observable<FieldVisitWorkflowDto | null>;

    constructor(
        private workflowService: FieldVisitWorkflowService,
        private assessmentByFieldVisitService: TreatmentBMPAssessmentByFieldVisitService,
        private alertService: AlertService,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.workflowService.clearStepAlerts();
    }

    public get isPostMaintenance(): boolean {
        return this.assessmentTypeID === 2;
    }

    public get headerLabel(): string {
        return this.isPostMaintenance ? "Post-Maintenance Assessment" : "Initial Assessment";
    }

    public hasAssessment(workflow: FieldVisitWorkflowDto): boolean {
        return this.isPostMaintenance ? !!workflow.PostMaintenanceAssessmentID : !!workflow.InitialAssessmentID;
    }

    beginAssessment(workflow: FieldVisitWorkflowDto): void {
        this.assessmentByFieldVisitService.createTreatmentBMPAssessmentByFieldVisit(workflow.FieldVisitID, this.assessmentTypeID).subscribe(() => {
            this.alertService.pushAlert(new Alert(`Started ${this.headerLabel}.`, AlertContext.Success));
            this.workflowService.refresh().subscribe(() => this.goToObservations(workflow));
        });
    }

    goToObservations(workflow: FieldVisitWorkflowDto): void {
        const branch = this.isPostMaintenance ? "post-maintenance-assessment" : "assessment";
        this.router.navigate(["/field-visits", workflow.FieldVisitID, branch, "observations"]);
    }

    /**
     * Skip to Maintenance — only surfaced for Initial Assessment. The Post-Maintenance
     * landing page hides the Skip button (matching legacy MVC), so this method is never
     * called when `isPostMaintenance` is true.
     */
    skip(workflow: FieldVisitWorkflowDto): void {
        this.router.navigate(["/field-visits", workflow.FieldVisitID, "maintenance"]);
    }

    wrapUpVisit(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.wrapUpVisit(workflow.FieldVisitID);
    }
}
