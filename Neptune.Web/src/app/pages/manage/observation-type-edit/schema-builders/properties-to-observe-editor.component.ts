import { Component, EventEmitter, Input, Output, signal } from "@angular/core";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";

@Component({
    selector: "properties-to-observe-editor",
    standalone: true,
    imports: [FieldDefinitionComponent],
    template: `
        <field-definition fieldDefinitionType="PropertiesToObserve" labelOverride="Properties to Observe" [inline]="true"></field-definition>
        @for (prop of properties; track $index) {
            <div class="prop-row">
                <span>{{ prop }}</span>
                <button type="button" class="btn btn-sm btn-danger-outline" (click)="remove($index)"><i class="fa fa-trash"></i></button>
            </div>
        }
        <div class="prop-row">
            <input type="text" class="form-control form-control-sm" placeholder="New property..."
                [value]="newProp()" (input)="newProp.set($any($event.target).value)"
                (keydown.enter)="add(); $event.preventDefault()">
            <button type="button" class="btn btn-sm btn-primary-outline" (click)="add()"><i class="fa fa-plus"></i> Add</button>
        </div>
    `,
    styles: [`
        .prop-row { display: flex; gap: 0.5rem; align-items: center; margin-bottom: 0.25rem; }
        .prop-row span { flex: 1; }
        .prop-row input { flex: 1; }
    `],
})
export class PropertiesToObserveEditorComponent {
    @Input() properties: string[] = [];
    @Output() propertiesChange = new EventEmitter<string[]>();
    public newProp = signal("");

    add(): void {
        const text = this.newProp().trim();
        if (text && !this.properties.includes(text)) {
            this.propertiesChange.emit([...this.properties, text]);
            this.newProp.set("");
        }
    }

    remove(index: number): void {
        this.propertiesChange.emit(this.properties.filter((_, i) => i !== index));
    }
}
