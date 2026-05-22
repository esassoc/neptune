import { Component, computed, inject } from "@angular/core";
import { FormControl } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { CollectionMethodType, DiscreteValueSchema, PassFailSchema, PercentageSchema } from "src/app/shared/observation-types/schema-types";
import { SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { ObservationPanelComponent, ObservationPanelProperty, ObservationTypePanel } from "./observation-panel.component";

export interface ObservationTypePreviewModalData {
    observationTypeName: string;
    collectionMethod: CollectionMethodType | null;
    passFailSchema: PassFailSchema | null;
    discreteSchema: DiscreteValueSchema | null;
    percentageSchema: PercentageSchema | null;
}

/** Renders the observation type using the same per-panel component the Field Visit Workflow uses
 * for live assessments, so admins can confirm exactly what assessors will see. Inputs are bound to
 * disabled throwaway FormControls — the modal is preview-only. */
@Component({
    selector: "observation-type-preview-modal",
    standalone: true,
    imports: [ObservationPanelComponent],
    template: `
        <div class="modal">
            <div class="modal-header">
                <h3>Preview Observation Type</h3>
            </div>
            <div class="modal-body">
                <p class="system-text">This is a preview of how <strong>{{ data.observationTypeName }}</strong> will look in a Treatment BMP Assessment form.</p>

                @if (panel(); as p) {
                    <observation-panel [panel]="p" namePrefix="preview"></observation-panel>
                } @else {
                    <p class="system-text">No preview available for this observation type.</p>
                }
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary-outline" (click)="close()">Close</button>
            </div>
        </div>
    `,
})
export class ObservationTypePreviewModalComponent {
    public ref: DialogRef<ObservationTypePreviewModalData, void> = inject(DialogRef);
    public data: ObservationTypePreviewModalData = this.ref.data;

    public panel = computed<ObservationTypePanel | null>(() => this.buildPanel());

    private buildPanel(): ObservationTypePanel | null {
        const cm = this.data.collectionMethod;
        if (!cm) return null;
        const pf = this.data.passFailSchema;
        const dv = this.data.discreteSchema;
        const pc = this.data.percentageSchema;
        const properties = [...(pf?.PropertiesToObserve ?? dv?.PropertiesToObserve ?? pc?.PropertiesToObserve ?? [])]
            .sort((a, b) => a.localeCompare(b));
        if (cm !== "PassFail" && cm !== "DiscreteValue" && cm !== "Percentage") return null;

        const passFailOptions: SelectDropdownOption[] | undefined =
            cm === "PassFail" && pf
                ? [
                      { Value: "true", Label: pf.PassingScoreLabel || "Pass", disabled: false },
                      { Value: "false", Label: pf.FailingScoreLabel || "Fail", disabled: false },
                  ]
                : undefined;

        const props: ObservationPanelProperty[] = properties.map((propertyObserved) => ({
            propertyObserved,
            // Disabled controls render visually inactive — the preview is read-only and the disabled
            // state on form-field is the cue, without needing template-level @if(readOnly) branches.
            valueControl: new FormControl<string | null>({ value: null, disabled: true }),
            notesControl: new FormControl<string | null>({ value: null, disabled: true }),
            passFailOptions,
        }));

        return {
            // ID only namespaces radio names — no real entity ID exists for an in-progress preview,
            // so 0 is a safe sentinel since the modal is the only consumer of this synthetic panel.
            observationTypeID: 0,
            name: this.data.observationTypeName,
            collectionMethod: cm,
            measurementUnitLabel: dv?.MeasurementUnitLabel ?? pc?.MeasurementUnitLabel ?? undefined,
            assessmentDescription: pf?.AssessmentDescription ?? dv?.AssessmentDescription ?? pc?.AssessmentDescription,
            benchmarkDescription: dv?.BenchmarkDescription ?? pc?.BenchmarkDescription,
            thresholdDescription: dv?.ThresholdDescription ?? pc?.ThresholdDescription,
            passingLabel: pf?.PassingScoreLabel,
            failingLabel: pf?.FailingScoreLabel,
            properties: props,
        };
    }

    close(): void {
        this.ref.close();
    }
}
