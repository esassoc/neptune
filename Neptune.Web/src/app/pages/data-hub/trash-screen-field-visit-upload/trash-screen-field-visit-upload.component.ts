import { HttpClient } from "@angular/common/http";
import { Component, signal } from "@angular/core";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { RouterLink } from "@angular/router";
import { environment } from "src/environments/environment";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { downloadDataHubTemplate } from "src/app/shared/helpers/data-hub-template-download";

@Component({
    selector: "trash-screen-field-visit-upload",
    standalone: true,
    imports: [FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, CustomRichTextComponent, FormFieldComponent],
    templateUrl: "./trash-screen-field-visit-upload.component.html",
})
export class TrashScreenFieldVisitUploadComponent {
    public NeptunePageTypeEnum = NeptunePageTypeEnum;
    public FormFieldType = FormFieldType;

    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });

    public isUploading = signal(false);
    public isDownloadingTemplate = signal(false);
    public errors = signal<string[]>([]);
    public successMessage = signal<string | null>(null);

    constructor(private alertService: AlertService, private fieldVisitService: FieldVisitService, private httpClient: HttpClient) {}

    public downloadTemplate(): void {
        downloadDataHubTemplate(this.httpClient, this.alertService, this.isDownloadingTemplate, "trash-screen-field-visit", "TrashScreenBulkUploadTemplate.xlsx", "Trash Screen Field Visit");
    }

    public onFileChange(file: File | null): void {
        if (!file) return;
        if (!file.name.toLowerCase().endsWith(".xlsx")) {
            this.alertService.clearAlerts();
            this.alertService.pushAlert(new Alert("Only XLSX files are accepted.", AlertContext.Danger, true));
            this.fileControl.setValue(null);
        }
    }

    public submit(): void {
        if (this.fileControl.invalid) return;
        this.alertService.clearAlerts();
        this.errors.set([]);
        this.successMessage.set(null);
        this.isUploading.set(true);

        this.fieldVisitService.bulkUploadTrashScreenFieldVisit(this.fileControl.value!).subscribe({
            next: (result) => {
                this.isUploading.set(false);
                if (result.Errors && result.Errors.length > 0) {
                    this.errors.set(result.Errors);
                    return;
                }
                this.successMessage.set(`Successfully bulk uploaded ${result.RowsProcessed} Trash Screen Field Visit row(s).`);
                this.fileControl.reset();
            },
            error: () => {
                this.isUploading.set(false);
                this.alertService.pushAlert(new Alert("Upload failed. Check the file format and try again.", AlertContext.Danger, true));
            },
        });
    }
}
