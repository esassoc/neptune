import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";

@Component({
    selector: "field-visit-inventory-step",
    standalone: true,
    imports: [AsyncPipe, PageHeaderComponent],
    templateUrl: "./inventory-step.component.html",
    styleUrl: "./inventory-step.component.scss",
})
export class FieldVisitInventoryStepComponent implements OnInit {
    public workflow$: Observable<FieldVisitWorkflowDto | null>;

    constructor(private workflowService: FieldVisitWorkflowService, private router: Router) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.workflowService.clearStepAlerts();
    }

    reviewInventory(workflow: FieldVisitWorkflowDto): void {
        this.router.navigate(["/field-visits", workflow.FieldVisitID, "inventory", "location"]);
    }

    skipToAssessment(workflow: FieldVisitWorkflowDto): void {
        this.router.navigate(["/field-visits", workflow.FieldVisitID, "assessment"]);
    }

    wrapUpVisit(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.wrapUpVisit(workflow.FieldVisitID);
    }
}
