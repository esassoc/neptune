import { AsyncPipe } from "@angular/common";
import { Component, OnInit, signal } from "@angular/core";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router, RouterLink } from "@angular/router";
import { map, Observable } from "rxjs";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "ovta-area-upload",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
    templateUrl: "./ovta-area-upload.component.html",
})
export class OvtaAreaUploadComponent implements OnInit {
    public FormFieldType = FormFieldType;
    public jurisdictionOptions$: Observable<FormInputOption[]>;

    public fileControl = new FormControl<File | null>(null, { validators: [Validators.required] });
    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });
    public areaNameFieldControl = new FormControl<string>("OVTAAreaName", { nonNullable: true, validators: [Validators.required] });

    public isUploading = signal(false);
    public errors = signal<string[]>([]);

    constructor(
        private router: Router,
        private alertService: AlertService,
        private ovtaAreaService: OnlandVisualTrashAssessmentAreaService,
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
        if (this.fileControl.invalid || this.jurisdictionControl.invalid || this.areaNameFieldControl.invalid) return;
        this.errors.set([]);
        this.isUploading.set(true);

        this.ovtaAreaService
            .gdbUploadOnlandVisualTrashAssessmentArea(this.fileControl.value!, this.jurisdictionControl.value!, this.areaNameFieldControl.value.trim())
            .subscribe({
                next: (report) => {
                    this.isUploading.set(false);
                    if (report.Errors && report.Errors.length > 0) {
                        this.errors.set(report.Errors);
                        return;
                    }
                    this.router.navigate(["/data-hub", "ovta-area-approve"]);
                },
                error: () => {
                    this.isUploading.set(false);
                    this.alertService.pushAlert(new Alert("Upload failed. Check the file format and try again.", AlertContext.Danger, true));
                },
            });
    }
}
