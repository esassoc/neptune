import { AsyncPipe } from "@angular/common";
import { Component, OnInit, signal } from "@angular/core";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { HttpClient } from "@angular/common/http";
import { RouterLink } from "@angular/router";
import { map, Observable } from "rxjs";
import { environment } from "src/environments/environment";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { TrashGeneratingUnitGdbDownloadRequestDto } from "src/app/shared/generated/model/trash-generating-unit-gdb-download-request-dto";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { saveBlobResponse } from "src/app/shared/helpers/download-file";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "trash-analysis-area-download",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
    templateUrl: "./trash-analysis-area-download.component.html",
})
export class TrashAnalysisAreaDownloadComponent implements OnInit {
    public FormFieldType = FormFieldType;
    public jurisdictionOptions$: Observable<FormInputOption[]>;

    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });
    public isWorking = signal(false);

    constructor(
        private alertService: AlertService,
        private httpClient: HttpClient,
        private stormwaterJurisdictionService: StormwaterJurisdictionService
    ) {}

    ngOnInit(): void {
        // NPT-1134: only the jurisdictions the user sees in the Trash Analysis Areas grid (Admins see all).
        this.jurisdictionOptions$ = this.stormwaterJurisdictionService
            .listViewableStormwaterJurisdiction()
            .pipe(map((js) => js.map((j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as FormInputOption)));
    }

    public download(): void {
        if (this.jurisdictionControl.invalid || this.isWorking()) return;
        this.isWorking.set(true);
        const body: TrashGeneratingUnitGdbDownloadRequestDto = { StormwaterJurisdictionID: this.jurisdictionControl.value! };

        // Generated client types this endpoint as JSON since Swashbuckle does not emit
        // [Produces("application/zip")] into the OpenAPI response. Call HttpClient directly to get
        // the binary blob plus access to Content-Disposition for the server-supplied filename.
        this.httpClient.post(`${environment.mainAppApiUrl}/trash-generating-units/download-gdb`, body, { responseType: "blob", observe: "response" }).subscribe({
            next: (response) => {
                this.isWorking.set(false);
                saveBlobResponse(response, "trash-analysis-areas-export.zip");
            },
            error: () => {
                this.isWorking.set(false);
                this.alertService.pushAlert(new Alert("Failed to build the Trash Analysis Area export. Please try again.", AlertContext.Danger, true));
            },
        });
    }
}
