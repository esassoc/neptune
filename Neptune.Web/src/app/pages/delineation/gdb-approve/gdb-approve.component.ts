import { DecimalPipe } from "@angular/common";
import { Component, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { Observable } from "rxjs";
import { DelineationGeometryService } from "src/app/shared/generated/api/delineation-geometry.service";
import { DelineationGdbUploadValidationDto } from "src/app/shared/generated/model/delineation-gdb-upload-validation-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";

@Component({
    selector: "gdb-approve",
    templateUrl: "./gdb-approve.component.html",
    styleUrl: "./gdb-approve.component.scss",
    imports: [RouterLink, PageHeaderComponent, AlertDisplayComponent, DecimalPipe],
})
export class GdbApproveComponent implements OnInit {
    public report = signal<DelineationGdbUploadValidationDto | null>(null);
    public isWorking = signal(false);

    constructor(
        private router: Router,
        private alertService: AlertService,
        private delineationGeometryService: DelineationGeometryService
    ) {}

    ngOnInit(): void {
        this.delineationGeometryService.stagingReportDelineationGeometry().subscribe({
            next: (dto) => this.report.set(dto),
            error: () => {
                this.alertService.pushAlert(new Alert("Could not load staging report.", AlertContext.Danger, true));
                this.router.navigate(["delineation", "gdb-upload"]);
            },
        });
    }

    public hasErrors(): boolean {
        const r = this.report();
        return !!r && r.Errors.length > 0;
    }

    public approve(): void {
        if (this.hasErrors()) return;
        this.isWorking.set(true);
        this.delineationGeometryService.approveDelineationGeometry().subscribe({
            next: (count) => {
                this.isWorking.set(false);
                const message = `${count} ${count === 1 ? "delineation was" : "delineations were"} successfully uploaded.`;
                // Push the success alert after navigation resolves; AlertDisplayComponent clears alerts on destroy,
                // so an alert pushed before navigating away never reaches the destination page.
                this.router.navigate(["delineation", "delineation-map"]).then(() => {
                    this.alertService.pushAlert(new Alert(message, AlertContext.Success, true));
                });
            },
            error: () => {
                this.isWorking.set(false);
                this.alertService.pushAlert(new Alert("Failed to commit staged delineations.", AlertContext.Danger, true));
            },
        });
    }

    public cancel(): void {
        this.isWorking.set(true);
        this.delineationGeometryService.discardStagingDelineationGeometry().subscribe({
            next: () => {
                this.isWorking.set(false);
                this.router.navigate(["delineation", "delineation-map"]);
            },
            error: () => {
                this.isWorking.set(false);
                this.router.navigate(["delineation", "delineation-map"]);
            },
        });
    }
}
