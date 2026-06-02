import { AsyncPipe } from "@angular/common";
import { Component, OnInit, signal } from "@angular/core";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { map, Observable } from "rxjs";
import { LandUseBlockService } from "src/app/shared/generated/api/land-use-block.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "land-use-block-upload",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
    templateUrl: "./land-use-block-upload.component.html",
    styleUrl: "./land-use-block-upload.component.scss",
})
export class LandUseBlockUploadComponent implements OnInit {
    public FormFieldType = FormFieldType;
    public jurisdictionOptions$: Observable<FormInputOption[]>;

    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });
    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });

    public isUploading = signal(false);
    public errors = signal<string[]>([]);
    public successMessage = signal<string | null>(null);

    constructor(
        private alertService: AlertService,
        private landUseBlockService: LandUseBlockService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService,
        private router: Router
    ) {}

    ngOnInit(): void {
        this.jurisdictionOptions$ = this.stormwaterJurisdictionService
            .listStormwaterJurisdiction()
            .pipe(map((js) => js.map((j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as FormInputOption)));
    }

    public onFileChange(file: File | null): void {
        if (!file) return;
        if (!file.name.toLowerCase().endsWith(".zip")) {
            this.alertService.pushAlert(new Alert("Only zipped File Geodatabases (.zip) are accepted.", AlertContext.Danger, true));
            this.fileControl.setValue(null);
        }
    }

    public submit(): void {
        if (this.fileControl.invalid || this.jurisdictionControl.invalid) return;
        // NPT-1077: clear stale banners + signals so this submit starts clean.
        this.alertService.clearAlerts();
        this.errors.set([]);
        this.successMessage.set(null);
        this.isUploading.set(true);

        this.landUseBlockService.uploadGdbLandUseBlock(this.fileControl.value!, this.jurisdictionControl.value!).subscribe({
            next: (result) => {
                this.isUploading.set(false);
                if (result.Errors && result.Errors.length > 0) {
                    this.errors.set(result.Errors);
                    return;
                }
                // NPT-1077: the upload endpoint no longer enqueues the background job. Hand the
                // user to the approve page, which fetches the validation report and (if clean)
                // enqueues the job via POST /land-use-blocks/staging/approve.
                this.router.navigate(["/data-hub/land-use-block-upload/approve"]);
            },
            error: () => {
                this.isUploading.set(false);
                this.alertService.pushAlert(new Alert("Upload failed. Check the file format and try again.", AlertContext.Danger, true));
            },
        });
    }
}
