import { Component, EventEmitter, Input, Output, ViewChild } from "@angular/core";
import { NgTemplateOutlet } from "@angular/common";
import { FormControl, NG_VALUE_ACCESSOR, FormsModule, ReactiveFormsModule, FormArray } from "@angular/forms";
import { TinyMceConfigPipe } from "src/app/shared/pipes/tiny-mce-config.pipe";
import { RequiredPipe } from "src/app/shared/pipes/required.pipe";
import { InputErrorsComponent } from "src/app/shared/components/inputs/input-errors/input-errors.component";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { PopperDirective } from "src/app/shared/directives/popper.directive";
import { EditorComponent, TINYMCE_SCRIPT_SRC } from "@tinymce/tinymce-angular";

import { NgxMaskDirective, provideNgxMask } from "ngx-mask";
import { NgSelectModule } from "@ng-select/ng-select";

@Component({
    selector: "form-field",
    templateUrl: "./form-field.component.html",
    styleUrls: ["./form-field.component.scss"],
    providers: [
        {
            provide: NG_VALUE_ACCESSOR,
            useExisting: FormFieldComponent,
            multi: true,
        },
        {
            provide: TINYMCE_SCRIPT_SRC,
            useValue: "tinymce/tinymce.min.js",
        },
        provideNgxMask(),
    ],
    imports: [NgTemplateOutlet, NgxMaskDirective, FormsModule, ReactiveFormsModule, EditorComponent, FieldDefinitionComponent, PopperDirective, InputErrorsComponent, RequiredPipe, TinyMceConfigPipe, NgSelectModule],
})
export class FormFieldComponent {
    public FormFieldType = FormFieldType;
    @Output() change = new EventEmitter<any>();
    @Input() formControl: FormControl;
    @Input() fieldLabel: string = "";
    @Input() size: "sm" | "md" | "lg" = "lg";
    @Input() placeholder: string = "";
    @Input() type: FormFieldType = FormFieldType.Text;
    @Input() toggleTrue: string = "On";
    @Input() toggleFalse: string = "Off";
    @Input() checkLabel: string;
    @Input() units: string;
    @Input() name: string;
    @Input() fieldDefinitionName: string;
    /** Free-text description that renders as a click-to-open help-icon next to the label,
     * matching the pattern used on the BMP detail page's custom-attributes-display table for
     * attributes that have a description but no backing FieldDefinitionType row. Leave unset to
     * skip the icon entirely. */
    @Input() popoverDescription: string;
    /** Optional popover title; defaults to fieldLabel. */
    @Input() popoverTitle: string;
    @Input() toggleHeight: string = "";
    @Input() mask: string;

    // for select dropdown
    @Input() formInputOptions: FormInputOption[];
    @Input() multiple: boolean = false;

    @Input() readOnly: boolean = false;

    /**
     * comma separated list of file types: https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/file#accept
     */
    @Input() uploadFileAccepts: string;

    @ViewChild("fileUploadField") fileUploadField: any;
    public fileName: string = null;
    public fileExtension: string = null;

    public val: any;
    set value(val) {
        this.val = val;
        this.change.emit(val);

        this.onChange(val);
        this.onTouch(val);
    }

    public isDisabled: boolean = false;

    onChange: any = () => {};
    onTouch: any = () => {};

    writeValue(value: any): void {
        this.val = value;
    }

    registerOnChange(fn: any): void {
        this.onChange = fn;
    }

    registerOnTouched(fn: any): void {
        this.onTouch = fn;
    }

    setDisabledState?(isDisabled: boolean): void {
        this.isDisabled = isDisabled;
    }

    onFileChange(event: any): void {
        let file = event.target.files[0];
        this.value = file;
        if (file) {
            const name = file.name;
            const i = name.lastIndexOf(".");
            this.fileName = i > 0 ? name.slice(0, i) : name;
            this.fileExtension = i > 0 ? name.slice(i) : "";
        } else {
            this.fileName = null;
            this.fileExtension = null;
        }
    }

    onClickFileUpload(event: any): void {
        const fileUploadInput = this.fileUploadField.nativeElement;
        fileUploadInput.click();
    }

    onNgSelectChange(event: any): void {
        this.change.emit(event);
    }
}

export enum FormFieldType {
    Text = "text",
    Textarea = "textarea",
    Check = "check",
    Toggle = "toggle",
    Date = "date",
    Select = "select",
    Number = "number",
    Radio = "radio",
    RTE = "rte",
    File = "file",
}

export interface FormInputOption {
    Value: any;
    Label: string;
    disabled: boolean | null | undefined;
    Group?: string | null | undefined;
}

export interface SelectDropdownOption extends FormInputOption {}
