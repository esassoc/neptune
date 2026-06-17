import { Component, EventEmitter, inject, Output, ViewChild } from "@angular/core";
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "../../forms/form-field/form-field.component";
import { DialogRef } from "@ngneat/dialog";

@Component({
    selector: "file-upload-modal",
    imports: [FormsModule, ReactiveFormsModule, FormFieldComponent],
    templateUrl: "./file-upload-modal.component.html",
    styleUrl: "./file-upload-modal.component.scss",
})
export class FileUploadModalComponent {
    public ref: DialogRef<any, IFileResourceUpload> = inject(DialogRef);
    FormFieldType = FormFieldType;

    @ViewChild("fileUploadField") fileUploadField: any;
    @Output() fileChanged = new EventEmitter<File>();

    // Mirror FileResources.ValidateFileUpload's accepted extensions exactly so the client can't
    // pick a file the server will reject. uploadFileAccepts feeds the input's accept attribute;
    // allowedExtensions backs the explicit check in updateFile() (accept alone is only a hint and
    // doesn't block drag-drop or "All files" selection). NPT-1053 rework: previously there was no
    // validation, so an unsupported file (e.g. .gdb) silently closed the modal with no error.
    public uploadFileAccepts = ".pdf,.png,.jpg,.jpeg,.doc,.docx,.xlsx,.txt,.csv";
    private allowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx", ".xlsx", ".txt", ".csv"];

    public file: File;
    public fileName: string;
    public fileExtension: string;
    public fileError: string;

    public formGroup: FormGroup<FileResourceUploadForm> = new FormGroup<FileResourceUploadForm>({
        File: new FormControl<File>(null),
        FileDescription: new FormControl<string>(""),
    });

    onClickFileUpload(event: any): void {
        const fileUploadInput = this.fileUploadField.nativeElement;
        fileUploadInput.click();
    }

    updateFile(event: any): void {
        const selectedFile: File = event.target.files.item(0);
        this.fileError = null;

        if (selectedFile) {
            const name = selectedFile.name;
            const i = name.lastIndexOf(".");
            const extension = i > 0 ? name.slice(i) : "";

            if (!this.allowedExtensions.includes(extension.toLowerCase())) {
                // Reject the file and surface a clear message instead of letting it through to a
                // silent server-side rejection. Clear the input so the user can re-pick.
                this.file = null;
                this.fileName = null;
                this.fileExtension = null;
                event.target.value = "";
                this.fileError = `${extension ? extension.slice(1).toUpperCase() : "This file type"} is not an accepted file type. Accepted extensions: PDF, PNG, JPG, DOCX, DOC, XLSX, CSV, TXT.`;
                this.fileChanged.emit(null);
                return;
            }

            this.file = selectedFile;
            this.fileName = i > 0 ? name.slice(0, i) : name;
            this.fileExtension = extension;
        } else {
            this.file = null;
            this.fileName = null;
            this.fileExtension = null;
        }
        this.fileChanged.emit(this.file);
    }

    submitFileResourceUpload(): void {
        let fileResourceUpload: IFileResourceUpload = {
            File: this.file,
            DocumentDescription: this.formGroup.get("FileDescription").value,
        };

        this.ref.close(fileResourceUpload);
    }

    close(): void {
        this.ref.close(null);
    }
}

export interface IFileResourceUpload {
    File: File;
    DocumentDescription: string;
}

export interface FileResourceUploadForm {
    File: FormControl<File>;
    FileDescription: FormControl<string>;
}
