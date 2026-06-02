import { Component, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { LandUseBlockService } from "src/app/shared/generated/api/land-use-block.service";
import { LandUseBlockGdbUploadValidationDto } from "src/app/shared/generated/model/land-use-block-gdb-upload-validation-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";

@Component({
    selector: "land-use-block-upload-approve",
    templateUrl: "./land-use-block-upload-approve.component.html",
    styleUrl: "./land-use-block-upload-approve.component.scss",
    imports: [RouterLink, PageHeaderComponent, AlertDisplayComponent],
})
export class LandUseBlockUploadApproveComponent implements OnInit {
    public report = signal<LandUseBlockGdbUploadValidationDto | null>(null);
    public isWorking = signal(false);

    constructor(
        private router: Router,
        private alertService: AlertService,
        private landUseBlockService: LandUseBlockService
    ) {}

    ngOnInit(): void {
        // NPT-1077: fetch the validation report so the user can review the PriorityLandUseType
        // and PermitType errors (if any) before approving. If the user arrives here without a
        // staging batch (e.g. by direct URL), the report will come back empty and the page just
        // says "No staged Land Use Blocks" — no error, no redirect.
        this.landUseBlockService.stagingReportLandUseBlock().subscribe({
            next: (dto) => this.report.set(dto),
            error: () => {
                this.alertService.pushAlert(new Alert("Could not load staging report.", AlertContext.Danger, true));
                this.router.navigate(["/data-hub/land-use-block-upload"]);
            },
        });
    }

    public hasErrors(): boolean {
        const r = this.report();
        return !!r && r.Errors.length > 0;
    }

    public hasStaging(): boolean {
        const r = this.report();
        return !!r && r.TotalStagedRowCount > 0;
    }

    public approve(): void {
        if (this.hasErrors() || !this.hasStaging()) return;
        this.isWorking.set(true);
        this.landUseBlockService.approveStagingLandUseBlock().subscribe({
            next: (count) => {
                this.isWorking.set(false);
                const message =
                    `${count} Land Use Block${count === 1 ? "" : "s"} queued for processing. ` +
                    "You'll receive an email when the import completes.";
                this.alertService.pushAlert(new Alert(message, AlertContext.Success, true));
                this.router.navigate(["/data-hub"]);
            },
            error: () => {
                this.isWorking.set(false);
                this.alertService.pushAlert(new Alert("Failed to approve the staged Land Use Blocks.", AlertContext.Danger, true));
            },
        });
    }

    public cancel(): void {
        this.isWorking.set(true);
        this.landUseBlockService.discardStagingLandUseBlock().subscribe({
            next: () => {
                this.isWorking.set(false);
                this.router.navigate(["/data-hub/land-use-block-upload"]);
            },
            error: () => {
                this.isWorking.set(false);
                this.router.navigate(["/data-hub/land-use-block-upload"]);
            },
        });
    }
}
