import { Component, inject } from "@angular/core";
import { RouterLink } from "@angular/router";
import { DialogService } from "@ngneat/dialog";
import { WqmpModalComponent } from "src/app/pages/wqmps/wqmp-modal/wqmp-modal.component";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

interface CommonTask {
    label: string;
    route?: string[];
    action?: () => void;
}

// NPT-1123: desk-oriented verb launcher. Every verb lands in an existing flow and keeps that flow's own gate.
@Component({
    selector: "common-tasks-panel",
    templateUrl: "./common-tasks-panel.component.html",
    styleUrls: ["./common-tasks-panel.component.scss"],
    imports: [RouterLink],
})
export class CommonTasksPanelComponent {
    private dialogService = inject(DialogService);
    private alertService = inject(AlertService);

    public tasks: CommonTask[] = [
        // Same modal as WQMP list → Actions → Add WQMP (Editor-accessible), NOT the Manager-only create-from-PDF path
        { label: "Add a WQMP", action: () => this.openAddWqmpModal() },
        { label: "Add a BMP", route: ["/treatment-bmps", "new"] },
        { label: "Add delineations", route: ["/delineation", "delineation-map"] },
        { label: "Add a planning project", route: ["/planning", "projects", "new"] },
        { label: "Review provisional records", route: ["/dashboard"] },
    ];

    public openAddWqmpModal(): void {
        const dialogRef = this.dialogService.open(WqmpModalComponent, {
            data: { mode: "add" },
            width: "800px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Water Quality Management Plan created successfully.", AlertContext.Success));
            }
        });
    }
}
