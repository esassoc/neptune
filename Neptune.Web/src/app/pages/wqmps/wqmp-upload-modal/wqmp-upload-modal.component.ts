import { Component, inject, OnInit, signal, ViewContainerRef } from "@angular/core";
import { FormControl, ReactiveFormsModule, Validators } from "@angular/forms";
import { map, Observable } from "rxjs";
import { AsyncPipe } from "@angular/common";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { DialogRef } from "@ngneat/dialog";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { PDF_EXTRACTION_LIMITS_BULLETS } from "src/app/shared/constants/pdf-extraction-limits";
import { environment } from "src/environments/environment";

@Component({
    selector: "wqmp-upload-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent, AsyncPipe],
    templateUrl: "./wqmp-upload-modal.component.html",
    styleUrl: "./wqmp-upload-modal.component.scss",
})
export class WqmpUploadModalComponent implements OnInit {
    // NPT-1051 rework: the modal owns file selection internally so the user sees
    // upload requirements before committing to a file. No `data: { file }` input.
    public ref: DialogRef<unknown, { wqmpID: number; documentID: number }> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });
    public wqmpNameControl = new FormControl<string>("", {
        nonNullable: true,
        validators: [Validators.required, Validators.maxLength(100)],
    });
    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });
    public jurisdictionOptions$: Observable<FormInputOption[]>;
    public isUploading = signal(false);
    public pdfLimitsBullets = PDF_EXTRACTION_LIMITS_BULLETS;

    constructor(
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private http: HttpClient,
        private viewContainerRef: ViewContainerRef,
        private stormwaterJurisdictionService: StormwaterJurisdictionService
    ) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();

        // NPT-984: use the manageable-jurisdictions endpoint so a JurisdictionManager only
        // sees the jurisdictions they're assigned to. Admin / SitkaAdmin still see all.
        // The backend upload endpoint validates the requested jurisdiction is in the
        // caller's manageable set as defense-in-depth.
        this.jurisdictionOptions$ = this.stormwaterJurisdictionService.listManageableStormwaterJurisdiction().pipe(
            map((jurisdictions) =>
                jurisdictions.map(
                    (j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as FormInputOption
                )
            )
        );
    }

    // form-field's accept=".pdf" only filters the file picker; users can still pick a
    // non-PDF via "All files". Defensively reject and clear the control.
    onFileChange(file: File | null): void {
        if (!file) return;
        if (!file.name.toLowerCase().endsWith(".pdf")) {
            this.alertService.pushAlert(new Alert("Only PDF files are accepted.", AlertContext.Danger));
            this.fileControl.setValue(null);
        }
    }

    upload(overwrite = false): void {
        if (this.fileControl.invalid || this.jurisdictionControl.invalid || this.wqmpNameControl.invalid) {
            this.fileControl.markAsTouched();
            this.jurisdictionControl.markAsTouched();
            this.wqmpNameControl.markAsTouched();
            return;
        }
        this.isUploading.set(true);
        this.alertService.clearAlerts();

        this.postUpload(overwrite).subscribe({
            next: (result) => {
                this.isUploading.set(false);
                this.ref.close({
                    wqmpID: result.WaterQualityManagementPlanID,
                    documentID: result.WaterQualityManagementPlanDocumentID,
                });
            },
            error: (err: HttpErrorResponse) => {
                this.isUploading.set(false);

                if (err.status === 409 && err.error?.CanOverwrite) {
                    this.confirmService.confirm({
                        title: "WQMP Already Exists",
                        message: err.error.Message,
                        buttonTextYes: "Overwrite",
                        buttonTextNo: "Cancel",
                        buttonClassYes: "btn-danger",
                    }, this.viewContainerRef).then((confirmed) => {
                        if (confirmed) {
                            this.upload(true);
                        }
                    });
                    return;
                }

                if (err.status === 409) {
                    this.alertService.pushAlert(new Alert(err.error?.Message ?? "A conflict occurred.", AlertContext.Danger));
                    return;
                }

                if (err.status < 400 || err.status >= 500) {
                    this.alertService.pushAlert(new Alert("An unexpected error occurred during upload.", AlertContext.Danger));
                }
            },
        });
    }

    private postUpload(overwrite: boolean): Observable<any> {
        const formData = new FormData();
        formData.append("file", this.fileControl.value!);
        formData.append("stormwaterJurisdictionID", this.jurisdictionControl.value.toString());
        formData.append("wqmpName", this.wqmpNameControl.value.trim());
        if (overwrite) {
            formData.append("overwrite", "true");
        }
        return this.http.post<any>(`${environment.mainAppApiUrl}/water-quality-management-plans/upload`, formData);
    }

    cancel(): void {
        this.ref.close(null);
    }
}
