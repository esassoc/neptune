import { Component, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { FieldVisitInventoryUpdatedDto } from "src/app/shared/generated/model/field-visit-inventory-updated-dto";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { TreatmentBmpLocationEditorComponent } from "src/app/shared/components/treatment-bmp-editors/location-editor/treatment-bmp-location-editor.component";
import { TreatmentBmpImagesEditorComponent } from "src/app/shared/components/treatment-bmp-editors/images-editor/treatment-bmp-images-editor.component";
import { TreatmentBmpCustomAttributesFormComponent } from "src/app/shared/components/treatment-bmp-editors/custom-attributes-form/treatment-bmp-custom-attributes-form.component";
import { CustomAttributeTypePurposeEnum } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";

type InventoryPanel = "location" | "photos" | "attributes" | null;

@Component({
    selector: "field-visit-inventory-step",
    standalone: true,
    imports: [AsyncPipe, TreatmentBmpLocationEditorComponent, TreatmentBmpImagesEditorComponent, TreatmentBmpCustomAttributesFormComponent],
    templateUrl: "./inventory-step.component.html",
    styleUrl: "./inventory-step.component.scss",
})
export class FieldVisitInventoryStepComponent implements OnInit {
    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    /** Which inline editor panel is expanded. Null = collapsed list view. */
    public activePanel: InventoryPanel = null;

    public OtherDesignAttributesPurposeID = CustomAttributeTypePurposeEnum.OtherDesignAttributes;

    constructor(
        private workflowService: FieldVisitWorkflowService,
        private fieldVisitService: FieldVisitService,
        private alertService: AlertService,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
    }

    expandPanel(panel: InventoryPanel): void {
        this.activePanel = panel;
    }

    collapsePanel(): void {
        this.activePanel = null;
    }

    onLocationSaved(): void {
        // Stay on the inventory step; collapse panel and refresh workflow header.
        this.activePanel = null;
        this.workflowService.refresh().subscribe();
    }

    onPhotosSaved(): void {
        // Photos editor pushes its own success alert; just refresh and keep panel open
        // so users can continue uploading or tweaking captions.
        this.workflowService.refresh().subscribe();
    }

    onAttributesSaved(): void {
        this.activePanel = null;
        this.workflowService.refresh().subscribe();
    }

    markInventoryUpdatedAndContinue(workflow: FieldVisitWorkflowDto): void {
        const dto = new FieldVisitInventoryUpdatedDto({ InventoryUpdated: true });
        this.fieldVisitService.updateInventoryUpdatedFieldVisit(workflow.FieldVisitID, dto).subscribe(() => {
            this.alertService.pushAlert(new Alert("Inventory updates confirmed.", AlertContext.Success));
            this.workflowService.refresh().subscribe(() => {
                this.router.navigate(["/field-visits", workflow.FieldVisitID, "assessment"]);
            });
        });
    }

    skipInventoryAndContinue(workflow: FieldVisitWorkflowDto): void {
        // Skip is a navigation-only action; do not flip InventoryUpdated.
        this.router.navigate(["/field-visits", workflow.FieldVisitID, "assessment"]);
    }
}
