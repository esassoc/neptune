import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { TreatmentBmpLocationEditorComponent } from "src/app/shared/components/treatment-bmp-editors/location-editor/treatment-bmp-location-editor.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";

@Component({
    selector: "field-visit-inventory-location-step",
    standalone: true,
    imports: [AsyncPipe, PageHeaderComponent, TreatmentBmpLocationEditorComponent],
    templateUrl: "./inventory-location-step.component.html",
})
export class FieldVisitInventoryLocationStepComponent implements OnInit {
    public workflow$: Observable<FieldVisitWorkflowDto | null>;

    constructor(private workflowService: FieldVisitWorkflowService, private router: Router) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
    }

    onSaved(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.markInventoryUpdatedAndRefresh(workflow.FieldVisitID).subscribe(() => {
            this.router.navigate(["/field-visits", workflow.FieldVisitID, "inventory", "photos"]);
        });
    }

    onCancelled(workflow: FieldVisitWorkflowDto): void {
        this.router.navigate(["/field-visits", workflow.FieldVisitID, "inventory"]);
    }
}
