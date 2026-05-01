import { Component, computed, inject, signal } from "@angular/core";
import { DialogRef } from "@ngneat/dialog";
import { CollectionMethodType, DiscreteValueSchema, PassFailSchema, PercentageSchema } from "src/app/shared/observation-types/schema-types";
import { MeasurementUnitTypes } from "src/app/shared/generated/enum/measurement-unit-type-enum";

export interface ObservationTypePreviewModalData {
    observationTypeName: string;
    collectionMethod: CollectionMethodType | null;
    passFailSchema: PassFailSchema | null;
    discreteSchema: DiscreteValueSchema | null;
    percentageSchema: PercentageSchema | null;
}

interface PreviewRow {
    property: string;
    placeholder: string;
}

@Component({
    selector: "observation-type-preview-modal",
    standalone: true,
    template: `
        <div class="modal">
            <div class="modal-header">
                <h3>Preview Observation Type</h3>
            </div>
            <div class="modal-body">
                <p class="system-text">This is a preview of how <strong>{{ data.observationTypeName }}</strong> will look in a Treatment BMP Assessment form.</p>

                @if (assessmentInstruction()) {
                    <div class="instructions">{{ assessmentInstruction() }}</div>
                }

                @if (data.collectionMethod === "PassFail" && data.passFailSchema) {
                    <table class="preview-table">
                        <thead>
                            <tr>
                                <th>Property</th>
                                <th class="score-col">{{ data.passFailSchema.PassingScoreLabel || "Pass" }}</th>
                                <th class="score-col">{{ data.passFailSchema.FailingScoreLabel || "Fail" }}</th>
                            </tr>
                        </thead>
                        <tbody>
                            @for (row of rows(); track $index) {
                                <tr>
                                    <td>{{ row.property }}</td>
                                    <td class="score-col"><input type="radio" disabled /></td>
                                    <td class="score-col"><input type="radio" disabled /></td>
                                </tr>
                            } @empty {
                                <tr><td colspan="3" class="empty">No properties to observe configured.</td></tr>
                            }
                        </tbody>
                    </table>
                } @else if (data.collectionMethod === "DiscreteValue" && data.discreteSchema) {
                    <table class="preview-table">
                        <thead>
                            <tr>
                                <th>Property</th>
                                <th>{{ data.discreteSchema.MeasurementUnitLabel || "Value" }}{{ unitSuffix() }}</th>
                            </tr>
                        </thead>
                        <tbody>
                            @for (row of rows(); track $index) {
                                <tr>
                                    <td>{{ row.property }}</td>
                                    <td><input type="number" class="form-control form-control-sm" [placeholder]="row.placeholder" disabled /></td>
                                </tr>
                            } @empty {
                                <tr><td colspan="2" class="empty">No properties to observe configured.</td></tr>
                            }
                        </tbody>
                    </table>
                } @else if (data.collectionMethod === "Percentage" && data.percentageSchema) {
                    <table class="preview-table">
                        <thead>
                            <tr>
                                <th>Property</th>
                                <th>{{ data.percentageSchema.MeasurementUnitLabel || "Percent" }} (%)</th>
                            </tr>
                        </thead>
                        <tbody>
                            @for (row of rows(); track $index) {
                                <tr>
                                    <td>{{ row.property }}</td>
                                    <td><input type="number" class="form-control form-control-sm" placeholder="0 – 100" disabled /></td>
                                </tr>
                            } @empty {
                                <tr><td colspan="2" class="empty">No properties to observe configured.</td></tr>
                            }
                        </tbody>
                    </table>
                } @else {
                    <p class="system-text">No preview available for this observation type.</p>
                }
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary-outline" (click)="close()">Close</button>
            </div>
        </div>
    `,
    styles: [`
        .instructions { background: #f7f7f7; border-left: 3px solid #0099ab; padding: 0.5rem 0.75rem; margin: 0.75rem 0; white-space: pre-wrap; }
        .preview-table { width: 100%; border-collapse: collapse; margin-top: 0.5rem; }
        .preview-table th, .preview-table td { border: 1px solid #ddd; padding: 0.5rem; vertical-align: middle; }
        .preview-table thead th { background: #f0f0f0; font-weight: 600; }
        .score-col { text-align: center; width: 5rem; }
        .empty { text-align: center; color: #888; font-style: italic; }
    `],
})
export class ObservationTypePreviewModalComponent {
    public ref: DialogRef<ObservationTypePreviewModalData, void> = inject(DialogRef);
    public data: ObservationTypePreviewModalData = this.ref.data;

    public assessmentInstruction = signal(
        this.data.passFailSchema?.AssessmentDescription
        ?? this.data.discreteSchema?.AssessmentDescription
        ?? this.data.percentageSchema?.AssessmentDescription
        ?? "",
    );

    public rows = computed<PreviewRow[]>(() => {
        const props = this.collectProperties();
        const placeholder = this.discretePlaceholder();
        return props.map((p) => ({ property: p, placeholder }));
    });

    public unitSuffix = computed(() => {
        const id = this.data.discreteSchema?.MeasurementUnitTypeID;
        if (id == null) return "";
        const display = MeasurementUnitTypes.find((u) => u.Value === id)?.DisplayName;
        return display ? ` (${display})` : "";
    });

    private collectProperties(): string[] {
        const props =
            this.data.passFailSchema?.PropertiesToObserve
            ?? this.data.discreteSchema?.PropertiesToObserve
            ?? this.data.percentageSchema?.PropertiesToObserve
            ?? [];
        return [...props].sort((a, b) => a.localeCompare(b));
    }

    private discretePlaceholder(): string {
        const s = this.data.discreteSchema;
        if (!s) return "";
        const min = s.MinimumValueOfObservations;
        const max = s.MaximumValueOfObservations;
        if (min != null && max != null) return `${min} – ${max}`;
        if (min != null) return `≥ ${min}`;
        if (max != null) return `≤ ${max}`;
        return "";
    }

    close(): void {
        this.ref.close();
    }
}
