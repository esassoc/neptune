import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { TreatmentBmpCustomAttributesFormComponent } from "src/app/shared/components/treatment-bmp-editors/custom-attributes-form/treatment-bmp-custom-attributes-form.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { CustomAttributeTypePurposeEnum } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";

@Component({
    selector: "field-visit-inventory-attributes-step",
    standalone: true,
    imports: [AsyncPipe, PageHeaderComponent, TreatmentBmpCustomAttributesFormComponent],
    templateUrl: "./inventory-attributes-step.component.html",
})
export class FieldVisitInventoryAttributesStepComponent implements OnInit {
    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    public OtherDesignAttributesPurposeID = CustomAttributeTypePurposeEnum.OtherDesignAttributes;

    constructor(private workflowService: FieldVisitWorkflowService, private router: Router) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
    }

    onSaved(workflow: FieldVisitWorkflowDto): void {
        this.workflowService.markInventoryUpdatedAndRefresh(workflow.FieldVisitID).subscribe(() => {
            this.router.navigate(["/field-visits", workflow.FieldVisitID, "inventory"]);
        });
    }

    onCancelled(workflow: FieldVisitWorkflowDto): void {
        this.router.navigate(["/field-visits", workflow.FieldVisitID, "inventory"]);
    }
}
