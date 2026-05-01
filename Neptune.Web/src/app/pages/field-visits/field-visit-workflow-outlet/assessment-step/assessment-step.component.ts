import { Component, Input, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe, DecimalPipe } from "@angular/common";
import { Observable } from "rxjs";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { TreatmentBMPAssessmentByFieldVisitService } from "src/app/shared/generated/api/treatment-bmp-assessment-by-field-visit.service";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/**
 * AssessmentStepComponent renders both the Initial and Post-Maintenance assessment
 * landing pages — the layout is identical, only the assessment type and the
 * "skip to" target differ. Pass the type via the route data: { assessmentTypeID: 1 | 2 }.
 * It's a decision page (Begin / Skip / continue to existing); the dynamic
 * observations form lives in ObservationsStepComponent.
 */
@Component({
    selector: "field-visit-assessment-step",
    standalone: true,
    imports: [AsyncPipe, DecimalPipe],
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

    public assessmentScore(workflow: FieldVisitWorkflowDto): number | null {
        return (this.isPostMaintenance ? workflow.PostMaintenanceAssessmentScore : workflow.InitialAssessmentScore) ?? null;
    }

    public assessmentComplete(workflow: FieldVisitWorkflowDto): boolean {
        return this.isPostMaintenance ? workflow.PostMaintenanceAssessmentComplete : workflow.InitialAssessmentComplete;
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

    goToPhotos(workflow: FieldVisitWorkflowDto): void {
        const branch = this.isPostMaintenance ? "post-maintenance-assessment" : "assessment";
        this.router.navigate(["/field-visits", workflow.FieldVisitID, branch, "photos"]);
    }

    skip(workflow: FieldVisitWorkflowDto): void {
        const next = this.isPostMaintenance ? "summary" : "maintenance";
        this.router.navigate(["/field-visits", workflow.FieldVisitID, next]);
    }
}
