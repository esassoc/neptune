import { Component, OnInit, ViewChild } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { TreatmentBmpCustomAttributesFormComponent } from "src/app/shared/components/treatment-bmp-editors/custom-attributes-form/treatment-bmp-custom-attributes-form.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { CustomAttributeTypePurposeEnum } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";

type SaveAction = "stay" | "continue" | "wrap-up";

@Component({
    selector: "field-visit-inventory-attributes-step",
    standalone: true,
    imports: [AsyncPipe, PageHeaderComponent, TreatmentBmpCustomAttributesFormComponent],
    templateUrl: "./inventory-attributes-step.component.html",
})
export class FieldVisitInventoryAttributesStepComponent implements OnInit {
    @ViewChild(TreatmentBmpCustomAttributesFormComponent) editor!: TreatmentBmpCustomAttributesFormComponent;

    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    public OtherDesignAttributesPurposeID = CustomAttributeTypePurposeEnum.OtherDesignAttributes;
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
        this.editor.saveFromHost();
    }

    onSaved(workflow: FieldVisitWorkflowDto): void {
        this.isSaving = false;
        this.workflowService.markInventoryUpdatedAndRefresh(workflow.FieldVisitID).subscribe(() => {
            const action = this.nextAction;
            this.nextAction = "stay";
            if (action === "continue") {
                this.router.navigate(["/field-visits", workflow.FieldVisitID, "inventory"]);
            } else if (action === "wrap-up") {
                this.workflowService.wrapUpVisit(workflow.FieldVisitID);
            }
        });
    }
}
