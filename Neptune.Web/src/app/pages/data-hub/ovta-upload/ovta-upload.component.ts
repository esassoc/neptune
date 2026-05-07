import { Component, signal } from "@angular/core";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { RouterLink } from "@angular/router";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "ovta-upload",
    standalone: true,
    imports: [FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
    templateUrl: "./ovta-upload.component.html",
})
export class OvtaUploadComponent {
    public FormFieldType = FormFieldType;

    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });

    public isUploading = signal(false);
    public errors = signal<string[]>([]);
    public successMessage = signal<string | null>(null);

    constructor(private alertService: AlertService, private ovtaService: OnlandVisualTrashAssessmentService) {}

    public onFileChange(file: File | null): void {
        if (!file) return;
        if (!file.name.toLowerCase().endsWith(".xlsx")) {
            this.alertService.pushAlert(new Alert("Only XLSX files are accepted.", AlertContext.Danger, true));
            this.fileControl.setValue(null);
        }
    }

    public submit(): void {
        if (this.fileControl.invalid) return;
        this.errors.set([]);
        this.successMessage.set(null);
        this.isUploading.set(true);

        this.ovtaService.bulkUploadOnlandVisualTrashAssessment(this.fileControl.value!).subscribe({
            next: (result) => {
                this.isUploading.set(false);
                if (result.Errors && result.Errors.length > 0) {
                    this.errors.set(result.Errors);
                    return;
                }
                this.successMessage.set(`Successfully bulk uploaded OVTAs from ${result.RowsProcessed} row(s).`);
                this.fileControl.reset();
            },
            error: () => {
                this.isUploading.set(false);
                this.alertService.pushAlert(new Alert("Upload failed. Check the file format and try again.", AlertContext.Danger, true));
            },
        });
    }
}
