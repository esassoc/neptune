import { Component, Input } from "@angular/core";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";

/** Per-property reactive controls inside an {@link ObservationTypePanel}. Owned by the parent
 * (observations-step ties them to save logic; the preview modal builds throwaways). */
export interface ObservationPanelProperty {
    propertyObserved: string;
    valueControl: FormControl<string | null>;
    notesControl: FormControl<string | null>;
    /** PassFail only — Pass/Fail dropdown/radio options. */
    passFailOptions?: SelectDropdownOption[];
}

export interface ObservationTypePanel {
    observationTypeID: number;
    name: string;
    collectionMethod: "DiscreteValue" | "PassFail" | "Percentage" | string;
    measurementUnitLabel?: string;
    minValue?: number;
    maxValue?: number;
    assessmentDescription?: string;
    benchmarkDescription?: string;
    thresholdDescription?: string;
    passingLabel?: string;
    failingLabel?: string;
    properties: ObservationPanelProperty[];
}

/** Renders one observation type as a card with the per-property form rows used in the Field
 * Visit Workflow assessment step. Extracted so the Observation Type Preview modal and the live
 * field-visit form share a single rendering path — guarantees the preview matches what assessors
 * see. The parent owns the per-property FormControls; this component just binds them. */
@Component({
    selector: "observation-panel",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent],
    templateUrl: "./observation-panel.component.html",
    styleUrl: "./observation-panel.component.scss",
})
export class ObservationPanelComponent {
    @Input({ required: true }) panel!: ObservationTypePanel;
    /** Unique suffix for radio-group names — required when multiple panels render on the same
     * page (field-visit form) so radios in different panels don't collide. */
    @Input() namePrefix: string = "obs";
    public FormFieldType = FormFieldType;
}
