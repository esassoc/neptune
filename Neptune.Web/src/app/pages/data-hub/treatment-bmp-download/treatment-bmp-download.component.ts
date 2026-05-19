import { Component, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { RouterLink } from "@angular/router";
import { environment } from "src/environments/environment";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "treatment-bmp-download",
    standalone: true,
    imports: [RouterLink, PageHeaderComponent, AlertDisplayComponent, CustomRichTextComponent],
    templateUrl: "./treatment-bmp-download.component.html",
})
export class TreatmentBMPDownloadComponent {
    public NeptunePageTypeEnum = NeptunePageTypeEnum;
    public isWorking = signal(false);

    constructor(private alertService: AlertService, private httpClient: HttpClient) {}

    public download(): void {
        this.isWorking.set(true);

        // Generated TreatmentBMPService client types this endpoint as JSON (Swashbuckle does not emit
        // [Produces("application/zip")] into the OpenAPI response), so call HttpClient directly to get
        // the binary blob plus access to Content-Disposition for the server-supplied filename.
        this.httpClient
            .get(`${environment.mainAppApiUrl}/treatment-bmps/download-gdb`, { responseType: "blob", observe: "response" })
            .subscribe({
                next: (response) => {
                    this.isWorking.set(false);
                    const fileName = parseFilenameFromContentDisposition(response.headers.get("content-disposition")) ?? "TreatmentBMPs_Export.zip";
                    const url = window.URL.createObjectURL(response.body!);
                    const a = document.createElement("a");
                    a.href = url;
                    a.download = fileName;
                    a.click();
                    window.URL.revokeObjectURL(url);
                },
                error: () => {
                    this.isWorking.set(false);
                    this.alertService.pushAlert(new Alert("Failed to build BMP inventory export.", AlertContext.Danger, true));
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
