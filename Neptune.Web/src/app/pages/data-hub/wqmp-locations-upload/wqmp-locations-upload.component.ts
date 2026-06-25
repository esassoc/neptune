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
    selector: "wqmp-locations-upload",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, CustomRichTextComponent, FormFieldComponent],
    templateUrl: "./wqmp-locations-upload.component.html",
})
export class WqmpLocationsUploadComponent implements OnInit {
    public NeptunePageTypeEnum = NeptunePageTypeEnum;
    public FormFieldType = FormFieldType;
    public jurisdictionOptions$: Observable<FormInputOption[]>;

    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });
    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });

    public isUploading = signal(false);
    public isDownloadingTemplate = signal(false);
    public errors = signal<string[]>([]);
    public missingApns = signal<string[]>([]);
    public successMessage = signal<string | null>(null);

    constructor(
        private alertService: AlertService,
        private wqmpService: WaterQualityManagementPlanService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private httpClient: HttpClient
    ) {}

    public downloadTemplate(): void {
        downloadDataHubTemplate(this.httpClient, this.alertService, this.isDownloadingTemplate, "wqmp-locations", "UploadWQMPBoundaryTemplate.csv", "WQMP Locations");
    }

    ngOnInit(): void {
        this.jurisdictionOptions$ = this.stormwaterJurisdictionService
            .listStormwaterJurisdiction()
            .pipe(map((js) => js.map((j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as FormInputOption)));
    }

    public onFileChange(file: File | null): void {
        if (!file) return;
        if (!file.name.toLowerCase().endsWith(".csv")) {
            this.alertService.pushAlert(new Alert("Only CSV files are accepted.", AlertContext.Danger, true));
            this.fileControl.setValue(null);
        }
    }

    public submit(): void {
        if (this.fileControl.invalid || this.jurisdictionControl.invalid) return;
        // NPT-1074: clear stale banners from prior attempts so file-rejection / upload-failed
        // alerts don't stack across submits. Same shape as the NPT-1072 + NPT-1073 fixes.
        this.alertService.clearAlerts();
        this.errors.set([]);
        this.missingApns.set([]);
        this.successMessage.set(null);
        this.isUploading.set(true);

        this.wqmpService.uploadBoundaryFromAPNsWaterQualityManagementPlan(this.fileControl.value!, this.jurisdictionControl.value!).subscribe({
            next: (result) => {
                this.isUploading.set(false);
                if (result.Errors && result.Errors.length > 0) {
                    this.errors.set(result.Errors);
                    this.missingApns.set(result.MissingApns ?? []);
                    return;
                }
                const added = result.AddedCount ?? 0;
                const updated = result.UpdatedCount ?? 0;
                this.alertService.clearAlerts();
                // NPT-1074 TC11: server returns no Errors when all APNs are unmatched (it's a
                // soft-miss per the spec, not a row-level error), so we land here with 0/0.
                // Be explicit that nothing was created rather than calling it "successful".
                if (added === 0 && updated === 0) {
                    this.successMessage.set("Upload completed: 0 boundaries created - all APNs unmatched.");
                } else {
                    this.successMessage.set(`Upload successful: ${added} WQMP boundaries added, ${updated} updated.`);
                }
                this.missingApns.set(result.MissingApns ?? []);
                this.fileControl.reset();
            },
            error: () => {
                this.isUploading.set(false);
                this.alertService.pushAlert(new Alert("Upload failed. Check the file format and try again.", AlertContext.Danger, true));
            },
        });
    }
}
