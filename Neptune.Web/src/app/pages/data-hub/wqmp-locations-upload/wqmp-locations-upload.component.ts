import { AsyncPipe } from "@angular/common";
import { Component, OnInit, signal } from "@angular/core";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { RouterLink } from "@angular/router";
import { map, Observable } from "rxjs";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "wqmp-locations-upload",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
    templateUrl: "./wqmp-locations-upload.component.html",
})
export class WqmpLocationsUploadComponent implements OnInit {
    public FormFieldType = FormFieldType;
    public jurisdictionOptions$: Observable<FormInputOption[]>;

    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });
    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });

    public isUploading = signal(false);
    public errors = signal<string[]>([]);
    public missingApns = signal<string[]>([]);
    public successMessage = signal<string | null>(null);

    constructor(
        private alertService: AlertService,
        private wqmpService: WaterQualityManagementPlanService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService
    ) {}

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
                this.successMessage.set(`Upload successful: ${added} WQMP boundaries added, ${updated} updated.`);
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
