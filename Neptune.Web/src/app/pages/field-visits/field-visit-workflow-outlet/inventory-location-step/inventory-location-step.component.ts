import { Component, OnInit, ViewChild } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { TreatmentBmpLocationEditorComponent } from "src/app/shared/components/treatment-bmp-editors/location-editor/treatment-bmp-location-editor.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";

type SaveAction = "stay" | "continue" | "wrap-up";

@Component({
    selector: "field-visit-inventory-location-step",
    standalone: true,
    imports: [AsyncPipe, PageHeaderComponent, TreatmentBmpLocationEditorComponent],
    templateUrl: "./inventory-location-step.component.html",
})
export class FieldVisitInventoryLocationStepComponent implements OnInit {
    @ViewChild(TreatmentBmpLocationEditorComponent) editor!: TreatmentBmpLocationEditorComponent;

    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    public isSaving = false;
    private nextAction: SaveAction = "stay";

    constructor(private workflowService: FieldVisitWorkflowService, private router: Router) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.workflowService.clearStepAlerts();
    }

    save(): void {
        this.nextAction = "stay";
        this.triggerSave();
    }

    saveAndContinue(): void {
        this.nextAction = "continue";
        this.triggerSave();
    }

    saveAndWrapUp(): void {
        this.nextAction = "wrap-up";
        this.triggerSave();
    }

    private triggerSave(): void {
        this.workflowService.clearStepAlerts();
        this.isSaving = true;
        this.editor.save();
    }

    onSaved(workflow: FieldVisitWorkflowDto): void {
        this.isSaving = false;
        this.workflowService.markInventoryUpdatedAndRefresh(workflow.FieldVisitID).subscribe(() => {
            const action = this.nextAction;
            this.nextAction = "stay";
            if (action === "continue") {
                this.router.navigate(["/field-visits", workflow.FieldVisitID, "inventory", "photos"]);
            } else if (action === "wrap-up") {
                this.workflowService.wrapUpVisit(workflow.FieldVisitID);
            }
        });
    }
}
