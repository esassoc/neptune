import { Component, inject, OnInit } from "@angular/core";
import { FormGroup, FormsModule, FormControl, ReactiveFormsModule, Validators } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "../../forms/form-field/form-field.component";
import { DialogRef } from "@ngneat/dialog";

@Component({
    selector: "file-description-update-modal",
    imports: [FormsModule, ReactiveFormsModule, FormFieldComponent],
    templateUrl: "./file-description-update-modal.component.html",
    styleUrl: "./file-description-update-modal.component.scss",
})
export class FileDescriptionUpdateModalComponent implements OnInit {
    public ref: DialogRef<any, any> = inject(DialogRef);
    FormFieldType = FormFieldType;

    public formGroup: FormGroup<FileDescriptionUpdateForm> = new FormGroup<FileDescriptionUpdateForm>({
        FileDescription: new FormControl<string>("", [Validators.maxLength(200)]),
    });

    ngOnInit(): void {
        // The dialog is opened with data: { FileResource: fileResource }, so the description
        // lives at ref.data.FileResource.DocumentDescription — not ref.data.DocumentDescription.
        // Reading the wrong path yielded undefined, and FormGroup.setValue throws on an undefined
        // value, which aborted ngOnInit and left the modal with no textarea (NPT-1053 rework).
        // Default to "" so the control always gets a defined value.
        this.formGroup.setValue({
            FileDescription: this.ref.data.FileResource?.DocumentDescription ?? "",
        });
    }

    submitFileUpdate(): void {
        this.ref.data.DocumentDescription = this.formGroup.get("FileDescription").value;
        this.ref.close(this.ref.data);
    }

    close(): void {
        this.ref.close(null);
    }
}

export interface FileDescriptionUpdateForm {
    FileDescription?: FormControl<string>;
}
