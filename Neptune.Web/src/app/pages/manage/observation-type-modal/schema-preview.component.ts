import { Component, Input } from "@angular/core";
import { CollectionMethodType, DiscreteValueSchema, PassFailSchema, PercentageSchema } from "./schema-types";

@Component({
    selector: "schema-preview",
    standalone: true,
    template: `
        @if (collectionMethod === "PassFail" && passFailSchema) {
            <dl class="grid-12">
                <dt class="g-col-4">Passing Label</dt><dd class="g-col-8">{{ passFailSchema.PassingScoreLabel }}</dd>
                <dt class="g-col-4">Failing Label</dt><dd class="g-col-8">{{ passFailSchema.FailingScoreLabel }}</dd>
                <dt class="g-col-4">Assessment</dt><dd class="g-col-8">{{ passFailSchema.AssessmentDescription }}</dd>
                <dt class="g-col-4">Properties</dt><dd class="g-col-8">{{ passFailSchema.PropertiesToObserve?.join(", ") || "None" }}</dd>
            </dl>
        }
        @if (collectionMethod === "DiscreteValue" && discreteSchema) {
            <dl class="grid-12">
                <dt class="g-col-4">Unit Label</dt><dd class="g-col-8">{{ discreteSchema.MeasurementUnitLabel }}</dd>
                <dt class="g-col-4">Observations</dt><dd class="g-col-8">{{ discreteSchema.MinimumNumberOfObservations }}{{ discreteSchema.MaximumNumberOfObservations ? " - " + discreteSchema.MaximumNumberOfObservations : "+" }}</dd>
                <dt class="g-col-4">Value Range</dt><dd class="g-col-8">{{ discreteSchema.MinimumValueOfObservations }}{{ discreteSchema.MaximumValueOfObservations != null ? " - " + discreteSchema.MaximumValueOfObservations : "+" }}</dd>
                <dt class="g-col-4">Benchmark</dt><dd class="g-col-8">{{ discreteSchema.BenchmarkDescription }}</dd>
                <dt class="g-col-4">Threshold</dt><dd class="g-col-8">{{ discreteSchema.ThresholdDescription }}</dd>
                <dt class="g-col-4">Assessment</dt><dd class="g-col-8">{{ discreteSchema.AssessmentDescription }}</dd>
                <dt class="g-col-4">Properties</dt><dd class="g-col-8">{{ discreteSchema.PropertiesToObserve?.join(", ") || "None" }}</dd>
            </dl>
        }
        @if (collectionMethod === "Percentage" && percentageSchema) {
            <dl class="grid-12">
                <dt class="g-col-4">Unit Label</dt><dd class="g-col-8">{{ percentageSchema.MeasurementUnitLabel }}</dd>
                <dt class="g-col-4">Benchmark</dt><dd class="g-col-8">{{ percentageSchema.BenchmarkDescription }}</dd>
                <dt class="g-col-4">Threshold</dt><dd class="g-col-8">{{ percentageSchema.ThresholdDescription }}</dd>
                <dt class="g-col-4">Assessment</dt><dd class="g-col-8">{{ percentageSchema.AssessmentDescription }}</dd>
                <dt class="g-col-4">Properties</dt><dd class="g-col-8">{{ percentageSchema.PropertiesToObserve?.join(", ") || "None" }}</dd>
            </dl>
        }
    `,
})
export class SchemaPreviewComponent {
    @Input() collectionMethod: CollectionMethodType | null;
    @Input() passFailSchema: PassFailSchema;
    @Input() discreteSchema: DiscreteValueSchema;
    @Input() percentageSchema: PercentageSchema;
}
