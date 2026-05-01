import { Component } from "@angular/core";
import { FieldVisitAssessmentStepComponent } from "../assessment-step/assessment-step.component";

/**
 * Thin wrapper around AssessmentStepComponent that pins assessmentTypeID=2 (PostMaintenance).
 * Lets us route this as a separate path so the workflow nav highlights the right item.
 */
@Component({
    selector: "field-visit-post-maintenance-assessment-step",
    standalone: true,
    imports: [FieldVisitAssessmentStepComponent],
    template: '<field-visit-assessment-step [assessmentTypeID]="2"></field-visit-assessment-step>',
})
export class FieldVisitPostMaintenanceAssessmentStepComponent {}
