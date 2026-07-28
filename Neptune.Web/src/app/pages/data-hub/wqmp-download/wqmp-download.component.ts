import { Component, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { RouterLink } from "@angular/router";
import { environment } from "src/environments/environment";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { saveBlobResponse } from "src/app/shared/helpers/download-file";

// NPT-943: Data Hub WQMP download — all WQMPs (with a boundary) the user can access. Sends an empty
// ID list to the shared WQMP GDB endpoint (⇒ all viewable). Mirrors treatment-bmp-download.
@Component({
    selector: "wqmp-download",
    standalone: true,
    imports: [RouterLink, PageHeaderComponent, AlertDisplayComponent],
    templateUrl: "./wqmp-download.component.html",
})
export class WqmpDownloadComponent {
    public isWorking = signal(false);

    constructor(private alertService: AlertService, private httpClient: HttpClient) {}

    public download(): void {
        this.isWorking.set(true);
        this.httpClient
            .post(`${environment.mainAppApiUrl}/water-quality-management-plans/download-gdb`, { WaterQualityManagementPlanIDs: [] }, { responseType: "blob", observe: "response" })
            .subscribe({
                next: (response) => {
                    this.isWorking.set(false);
                    saveBlobResponse(response, "WaterQualityManagementPlans_Export.zip");
                },
                error: () => {
                    this.isWorking.set(false);
                    this.alertService.pushAlert(new Alert("Failed to build the WQMP geodatabase export.", AlertContext.Danger, true));
                },
            });
    }
}
