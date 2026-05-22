import { Component, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OvtaAreaGdbStagingReportDto } from "src/app/shared/generated/model/ovta-area-gdb-staging-report-dto";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

@Component({
    selector: "ovta-area-approve",
    standalone: true,
    imports: [RouterLink, PageHeaderComponent, AlertDisplayComponent],
    templateUrl: "./ovta-area-approve.component.html",
})
export class OvtaAreaApproveComponent implements OnInit {
    public report = signal<OvtaAreaGdbStagingReportDto | null>(null);
    public isWorking = signal(false);

    constructor(
        private router: Router,
        private alertService: AlertService,
        private ovtaAreaService: OnlandVisualTrashAssessmentAreaService
    ) {}

    ngOnInit(): void {
        this.loadReport();
    }

    private loadReport(): void {
        this.ovtaAreaService.gdbStagingReportOnlandVisualTrashAssessmentArea().subscribe({
            next: (r) => this.report.set(r ?? null),
            error: () => this.alertService.pushAlert(new Alert("Failed to load staging report.", AlertContext.Danger, true)),
        });
    }

    public approve(): void {
        const r = this.report();
        if (!r || (r.Errors && r.Errors.length > 0)) return;
        this.isWorking.set(true);
        this.ovtaAreaService.gdbApproveOnlandVisualTrashAssessmentArea().subscribe({
            next: (count) => {
                this.isWorking.set(false);
                this.alertService.pushAlert(new Alert(`${count} OVTA Area(s) successfully uploaded.`, AlertContext.Success, true));
                this.router.navigate(["/data-hub"], { queryParams: { tab: "trash" } });
            },
            error: () => {
                this.isWorking.set(false);
                this.alertService.pushAlert(new Alert("Approval failed. Refresh to see the latest staging report.", AlertContext.Danger, true));
                this.loadReport();
            },
        });
    }

    public discard(): void {
        this.isWorking.set(true);
        this.ovtaAreaService.gdbDiscardStagingOnlandVisualTrashAssessmentArea().subscribe({
            next: () => {
                this.isWorking.set(false);
                this.alertService.pushAlert(new Alert("Staged OVTA Areas discarded.", AlertContext.Info, true));
                this.router.navigate(["/data-hub"], { queryParams: { tab: "trash" } });
            },
            error: () => {
                this.isWorking.set(false);
                this.alertService.pushAlert(new Alert("Failed to discard staged OVTA Areas.", AlertContext.Danger, true));
            },
        });
    }
}
