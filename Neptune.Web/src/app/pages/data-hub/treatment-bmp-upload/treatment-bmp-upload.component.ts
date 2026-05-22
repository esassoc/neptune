import { AsyncPipe } from "@angular/common";
import { Component, OnInit, signal } from "@angular/core";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { map, Observable } from "rxjs";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "treatment-bmp-upload",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, CustomRichTextComponent, FormFieldComponent],
    templateUrl: "./treatment-bmp-upload.component.html",
})
export class TreatmentBMPUploadComponent implements OnInit {
    public NeptunePageTypeEnum = NeptunePageTypeEnum;
    public FormFieldType = FormFieldType;
    public bmpTypeOptions$: Observable<FormInputOption[]>;

    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });
    public bmpTypeControl = new FormControl<number | null>(null, { validators: [Validators.required] });

    public isUploading = signal(false);
    public errors = signal<string[]>([]);
    public successMessage = signal<string | null>(null);

    constructor(
        private router: Router,
        private alertService: AlertService,
        private treatmentBMPService: TreatmentBMPService,
        private treatmentBMPTypeService: TreatmentBMPTypeService
    ) {}

    ngOnInit(): void {
        this.bmpTypeOptions$ = this.treatmentBMPTypeService
            .listAsDetailDtoTreatmentBMPType()
            .pipe(map((types) => types.map((t) => ({ Label: t.TreatmentBMPTypeName, Value: t.TreatmentBMPTypeID, disabled: false }) as FormInputOption)));
    }

    public onFileChange(file: File | null): void {
        if (!file) return;
        if (!file.name.toLowerCase().endsWith(".csv")) {
            this.alertService.pushAlert(new Alert("Only CSV files are accepted.", AlertContext.Danger, true));
            this.fileControl.setValue(null);
        }
    }

    public submit(): void {
        if (this.fileControl.invalid || this.bmpTypeControl.invalid) return;
        this.errors.set([]);
        this.successMessage.set(null);
        this.isUploading.set(true);

        this.treatmentBMPService.bulkUploadTreatmentBMP(this.fileControl.value!, this.bmpTypeControl.value!).subscribe({
            next: (result) => {
                this.isUploading.set(false);
                if (result.Errors && result.Errors.length > 0) {
                    this.errors.set(result.Errors);
                    return;
                }
                this.successMessage.set(`Upload successful: ${result.AddedCount} records added, ${result.UpdatedCount} records updated.`);
                this.fileControl.reset();
            },
            error: () => {
                this.isUploading.set(false);
                this.alertService.pushAlert(new Alert("Upload failed. Check the file format and try again.", AlertContext.Danger, true));
            },
        });
    }
}
