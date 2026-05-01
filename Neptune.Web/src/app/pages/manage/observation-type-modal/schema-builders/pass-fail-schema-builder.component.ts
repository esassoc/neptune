import { Component, EventEmitter, Input, Output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { PassFailSchema } from "src/app/shared/observation-types/schema-types";
import { PropertiesToObserveEditorComponent } from "./properties-to-observe-editor.component";

export type SchemaBuilderSection = "instructions" | "labelsUnits";

@Component({
    selector: "pass-fail-schema-builder",
    standalone: true,
    imports: [FormsModule, PropertiesToObserveEditorComponent],
    template: `
        @switch (section) {
            @case ("instructions") {
                <div class="grid-12">
                    <div class="g-col-12">
                        <label class="field-label">Assessment Instruction</label>
                        <textarea class="form-control" rows="3" [(ngModel)]="schema.AssessmentDescription" (ngModelChange)="emit()" placeholder="Instructions for the assessor"></textarea>
                    </div>
                </div>
            }
            @case ("labelsUnits") {
                <div class="grid-12">
                    <div class="g-col-6">
                        <label class="field-label">Passing Score Label</label>
                        <input type="text" class="form-control" [(ngModel)]="schema.PassingScoreLabel" (ngModelChange)="emit()" placeholder="e.g. Passes">
                    </div>
                    <div class="g-col-6">
                        <label class="field-label">Failing Score Label</label>
                        <input type="text" class="form-control" [(ngModel)]="schema.FailingScoreLabel" (ngModelChange)="emit()" placeholder="e.g. Fails">
                    </div>
                    <div class="g-col-12">
                        <properties-to-observe-editor [properties]="schema.PropertiesToObserve" (propertiesChange)="schema.PropertiesToObserve = $event; emit()"></properties-to-observe-editor>
                    </div>
                </div>
            }
        }
    `,
    styles: [`.field-label { font-weight: 600; font-size: 0.875rem; display: block; margin-bottom: 0.25rem; }`],
})
export class PassFailSchemaBuilderComponent {
    @Input() schema: PassFailSchema;
    @Input() section: SchemaBuilderSection = "instructions";
    @Output() schemaChange = new EventEmitter<PassFailSchema>();
    emit(): void { this.schemaChange.emit({ ...this.schema }); }
}
