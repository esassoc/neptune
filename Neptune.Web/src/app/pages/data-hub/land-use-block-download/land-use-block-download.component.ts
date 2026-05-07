import { AsyncPipe } from "@angular/common";
import { Component, OnInit, signal } from "@angular/core";
import { FormControl, FormsModule, ReactiveFormsModule, Validators } from "@angular/forms";
import { HttpClient } from "@angular/common/http";
import { RouterLink } from "@angular/router";
import { map, Observable } from "rxjs";
import { environment } from "src/environments/environment";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, FormInputOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "land-use-block-download",
    standalone: true,
    imports: [AsyncPipe, FormsModule, ReactiveFormsModule, RouterLink, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
    templateUrl: "./land-use-block-download.component.html",
})
export class LandUseBlockDownloadComponent implements OnInit {
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
        this.jurisdictionOptions$ = this.stormwaterJurisdictionService
            .listStormwaterJurisdiction()
            .pipe(map((js) => js.map((j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as FormInputOption)));
    }

    public download(): void {
        if (this.jurisdictionControl.invalid) return;
        this.isWorking.set(true);
        const body = { StormwaterJurisdictionID: this.jurisdictionControl.value! };

        this.httpClient
            .post(`${environment.mainAppApiUrl}/land-use-blocks/download-gdb`, body, { responseType: "blob", observe: "response" })
            .subscribe({
                next: (response) => {
                    this.isWorking.set(false);
                    const fileName = parseFilenameFromContentDisposition(response.headers.get("content-disposition")) ?? "land-use-blocks.zip";
                    const url = window.URL.createObjectURL(response.body!);
                    const a = document.createElement("a");
                    a.href = url;
                    a.download = fileName;
                    a.click();
                    window.URL.revokeObjectURL(url);
                },
                error: () => {
                    this.isWorking.set(false);
                    this.alertService.pushAlert(new Alert("Failed to build Land Use Block export.", AlertContext.Danger, true));
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
            // Fall through to plain filename match.
        }
    }
    const plainMatch = /filename\s*=\s*"?([^";]+)"?/i.exec(header);
    return plainMatch ? plainMatch[1].trim() : null;
}
