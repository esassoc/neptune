import { Component, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { map, Observable } from "rxjs";
import { DelineationGeometryService } from "src/app/shared/generated/api/delineation-geometry.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";

@Component({
    selector: "gdb-upload",
    templateUrl: "./gdb-upload.component.html",
    styleUrl: "./gdb-upload.component.scss",
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
})
export class GdbUploadComponent implements OnInit {
    public FormFieldType = FormFieldType;
    public jurisdictionOptions$: Observable<FormInputOption[]>;

    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });
    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });
    public bmpNameFieldControl = new FormControl<string>("TreatmentBMPName", { nonNullable: true, validators: [Validators.required] });
    public statusFieldControl = new FormControl<string>("DelineationStatus", { nonNullable: true });

    public isUploading = signal(false);
    public errors = signal<string[]>([]);

    constructor(
        private router: Router,
        private alertService: AlertService,
        private delineationGeometryService: DelineationGeometryService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService
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
        if (this.fileControl.invalid || this.jurisdictionControl.invalid || this.bmpNameFieldControl.invalid) return;
        this.errors.set([]);
        this.isUploading.set(true);

        this.delineationGeometryService
            .uploadDelineationGeometry(
                this.fileControl.value!,
                this.jurisdictionControl.value!,
                this.bmpNameFieldControl.value.trim(),
                this.statusFieldControl.value?.trim() || undefined
            )
            .subscribe({
                next: (result) => {
                    this.isUploading.set(false);
                    if (result.Errors.length > 0) {
                        this.errors.set(result.Errors);
                        return;
                    }
                    this.router.navigate(["delineation", "gdb-approve"]);
                },
                error: () => {
                    this.isUploading.set(false);
                    this.alertService.pushAlert(new Alert("Upload failed. Check the file format and try again.", AlertContext.Danger, true));
                },
            });
    }
}
