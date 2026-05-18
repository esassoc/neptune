import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { TreatmentBmpImagesEditorComponent } from "src/app/shared/components/treatment-bmp-editors/images-editor/treatment-bmp-images-editor.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";

@Component({
    selector: "field-visit-inventory-photos-step",
    standalone: true,
    imports: [AsyncPipe, PageHeaderComponent, TreatmentBmpImagesEditorComponent],
    templateUrl: "./inventory-photos-step.component.html",
})
export class FieldVisitInventoryPhotosStepComponent implements OnInit {
    public workflow$: Observable<FieldVisitWorkflowDto | null>;

    constructor(private workflowService: FieldVisitWorkflowService, private router: Router) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.workflowService.clearStepAlerts();
    }

    /**
     * Fired by treatment-bmp-images-editor on every persistence event (upload, delete, captions saved).
     * Each one counts as an inventory change, so we flip InventoryUpdated even if the user never clicks
     * the "Save Captions" button — matches legacy MVC where any photos-page submit set the flag.
     */
    onPhotosChanged(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.markInventoryUpdatedAndRefresh(workflow.FieldVisitID).subscribe();
    }

    /** Photos auto-save on upload/delete/caption-save, so there's no per-page Save here — just a
     * Continue button to advance to Attributes. Wrap Up Visit lets the user exit the workflow at
     * this step without going through Attributes, matching the gateway/edit pattern elsewhere. */
    continueToAttributes(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.markInventoryUpdatedAndRefresh(workflow.FieldVisitID).subscribe(() => {
            this.router.navigate(["/field-visits", workflow.FieldVisitID, "inventory", "attributes"]);
        });
    }

    wrapUpVisit(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.markInventoryUpdatedAndRefresh(workflow.FieldVisitID).subscribe(() => {
            this.workflowService.wrapUpVisit(workflow.FieldVisitID);
        });
    }
}
