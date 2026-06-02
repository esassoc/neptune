import { AsyncPipe } from "@angular/common";
import { HttpClient } from "@angular/common/http";
import { Component, OnInit, signal } from "@angular/core";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { RouterLink } from "@angular/router";
import { map, Observable } from "rxjs";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { downloadDataHubTemplate } from "src/app/shared/helpers/data-hub-template-download";

@Component({
    selector: "simplified-bmp-upload",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, CustomRichTextComponent, FormFieldComponent],
    templateUrl: "./simplified-bmp-upload.component.html",
})
export class SimplifiedBmpUploadComponent implements OnInit {
    public NeptunePageTypeEnum = NeptunePageTypeEnum;
    public FormFieldType = FormFieldType;
    public jurisdictionOptions$: Observable<FormInputOption[]>;

    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });
    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });

    public isUploading = signal(false);
    public isDownloadingTemplate = signal(false);
    public errors = signal<string[]>([]);
    public successMessage = signal<string | null>(null);

    constructor(
        private alertService: AlertService,
        private wqmpService: WaterQualityManagementPlanService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private httpClient: HttpClient
    ) {}

    public downloadTemplate(): void {
        downloadDataHubTemplate(this.httpClient, this.alertService, this.isDownloadingTemplate, "simplified-bmp", "SimplifiedBMPBulkUploadTemplate.xlsx", "Simplified BMP");
    }

    ngOnInit(): void {
        this.jurisdictionOptions$ = this.stormwaterJurisdictionService
            .listStormwaterJurisdiction()
            .pipe(map((js) => js.map((j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as FormInputOption)));
    }

    public onFileChange(file: File | null): void {
        if (!file) return;
        if (!file.name.toLowerCase().endsWith(".xlsx")) {
            this.alertService.pushAlert(new Alert("Only XLSX files are accepted.", AlertContext.Danger, true));
            this.fileControl.setValue(null);
        }
    }

    public submit(): void {
        if (this.fileControl.invalid || this.jurisdictionControl.invalid) return;
        // NPT-1073: clear stale banners from prior attempts so the "Upload failed" / file-rejection
        // alerts don't stack across submits. Same fix as the WQMP uploader (NPT-1072).
        this.alertService.clearAlerts();
        this.errors.set([]);
        this.successMessage.set(null);
        this.isUploading.set(true);

        this.wqmpService.uploadSimplifiedBMPsWaterQualityManagementPlan(this.fileControl.value!, this.jurisdictionControl.value!).subscribe({
            next: (result) => {
                this.isUploading.set(false);
                if (result.Errors && result.Errors.length > 0) {
                    this.errors.set(result.Errors);
                    return;
                }
                this.alertService.clearAlerts();
                this.successMessage.set(`Upload successful: ${result.AddedCount} Simplified BMPs added, ${result.UpdatedCount} Simplified BMPs updated.`);
                this.fileControl.reset();
            },
            error: () => {
                this.isUploading.set(false);
                this.alertService.pushAlert(new Alert("Upload failed. Check the file format and try again.", AlertContext.Danger, true));
            },
        });
    }
}
