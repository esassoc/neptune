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
import { environment } from "src/environments/environment";

@Component({
    selector: "wqmp-upload-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent, AsyncPipe],
    templateUrl: "./wqmp-upload-modal.component.html",
    styleUrl: "./wqmp-upload-modal.component.scss",
})
export class WqmpUploadModalComponent implements OnInit {
    public ref: DialogRef<{ file: File }, { wqmpID: number }> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });
    public jurisdictionOptions$: Observable<FormInputOption[]>;
    public isUploading = signal(false);
    public file: File;

    constructor(
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private http: HttpClient,
        private viewContainerRef: ViewContainerRef,
        private stormwaterJurisdictionService: StormwaterJurisdictionService
    ) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.file = this.ref.data.file;

        this.jurisdictionOptions$ = this.stormwaterJurisdictionService.listStormwaterJurisdiction().pipe(
            map((jurisdictions) =>
                jurisdictions.map(
                    (j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as FormInputOption
                )
            )
        );
    }

    upload(overwrite = false): void {
        if (this.jurisdictionControl.invalid) return;
        this.isUploading.set(true);
        this.alertService.clearAlerts();

        this.postUploadAndExtract(overwrite).subscribe({
            next: (result) => {
                this.isUploading.set(false);
                this.ref.close({ wqmpID: result.WaterQualityManagementPlanID });
            },
            error: (err: HttpErrorResponse) => {
                if (err.status === 409 && err.error?.CanOverwrite) {
                    this.isUploading.set(false);
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
                const message = err.status === 409 ? err.error?.Message : err.error?.message || err.error?.title || "An unexpected error occurred during upload.";
                this.alertService.pushAlert(new Alert(message, AlertContext.Danger));
                this.isUploading.set(false);
            },
        });
    }

    private postUploadAndExtract(overwrite: boolean): Observable<any> {
        const formData = new FormData();
        formData.append("file", this.file);
        formData.append("stormwaterJurisdictionID", this.jurisdictionControl.value.toString());
        if (overwrite) {
            formData.append("overwrite", "true");
        }
        return this.http.post<any>(`${environment.mainAppApiUrl}/water-quality-management-plans/upload-and-extract`, formData);
    }

    cancel(): void {
        this.ref.close(null);
    }
}
