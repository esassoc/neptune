import { Component, OnInit, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { HttpClient } from "@angular/common/http";
import { map, Observable } from "rxjs";
import { environment } from "src/environments/environment";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { DelineationTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/delineation-type-enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";

@Component({
    selector: "gdb-download",
    templateUrl: "./gdb-download.component.html",
    styleUrl: "./gdb-download.component.scss",
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
})
export class GdbDownloadComponent implements OnInit {
    public FormFieldType = FormFieldType;
    public jurisdictionOptions$: Observable<FormInputOption[]>;
    public delineationTypeOptions: FormInputOption[] = DelineationTypesAsSelectDropdownOptions.map((x) => ({ Label: x.Label, Value: x.Value, disabled: false }));

    public jurisdictionControl = new FormControl<number | null>(null, { validators: [Validators.required] });
    public delineationTypeControl = new FormControl<number | null>(null, { validators: [Validators.required] });
    public isWorking = signal(false);

    constructor(
        private alertService: AlertService,
        private httpClient: HttpClient,
        private stormwaterJurisdictionService: StormwaterJurisdictionService
    ) {}

    ngOnInit(): void {
        this.jurisdictionOptions$ = this.stormwaterJurisdictionService
            .listStormwaterJurisdiction()
            .pipe(map((js) => js.map((j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as FormInputOption)));
    }

    public download(): void {
        if (this.jurisdictionControl.invalid || this.delineationTypeControl.invalid) return;
        this.isWorking.set(true);

        const body = {
            StormwaterJurisdictionID: this.jurisdictionControl.value!,
            DelineationTypeID: this.delineationTypeControl.value!,
        };

        // The generated DelineationGeometryService client types this endpoint as JSON (Swashbuckle doesn't emit
        // [Produces("application/zip")] into the OpenAPI response), so we call HttpClient directly to get a
        // blob plus access to Content-Disposition for the server-supplied filename.
        this.httpClient
            .post(`${environment.mainAppApiUrl}/delineations/gdb/download`, body, { responseType: "blob", observe: "response" })
            .subscribe({
                next: (response) => {
                    this.isWorking.set(false);
                    const fileName = parseFilenameFromContentDisposition(response.headers.get("content-disposition")) ?? "delineations.zip";
                    const url = window.URL.createObjectURL(response.body!);
                    const a = document.createElement("a");
                    a.href = url;
                    a.download = fileName;
                    a.click();
                    window.URL.revokeObjectURL(url);
                },
                error: () => {
                    this.isWorking.set(false);
                    this.alertService.pushAlert(new Alert("Failed to build delineation export.", AlertContext.Danger, true));
                },
            });
    }
}

function parseFilenameFromContentDisposition(header: string | null): string | null {
    if (!header) return null;
    const utf8Match = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(header);
    if (utf8Match) {
        try {
            return decodeURIComponent(utf8Match[1].trim());
        } catch {
            // Fall through to the plain filename match.
        }
    }
    const plainMatch = /filename\s*=\s*"?([^";]+)"?/i.exec(header);
    return plainMatch ? plainMatch[1].trim() : null;
}
