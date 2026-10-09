import { Component, inject } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { Router, RouterLink } from "@angular/router";
import { DialogService } from "@ngneat/dialog";
import { Observable, map } from "rxjs";
import { WqmpModalComponent } from "src/app/pages/wqmps/wqmp-modal/wqmp-modal.component";
import { WqmpUploadModalComponent } from "src/app/pages/wqmps/wqmp-upload-modal/wqmp-upload-modal.component";
import { AuthenticationService } from "src/app/services/authentication.service";
import { PersonDto } from "src/app/shared/generated/model/person-dto";
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
    imports: [RouterLink, AsyncPipe],
})
export class CommonTasksPanelComponent {
    private dialogService = inject(DialogService);
    private alertService = inject(AlertService);
    private authenticationService = inject(AuthenticationService);
    private router = inject(Router);

    public tasks$: Observable<CommonTask[]> = this.authenticationService.getCurrentUser().pipe(map((user) => this.buildTasks(user)));

    private buildTasks(user: PersonDto): CommonTask[] {
        const tasks: CommonTask[] = [
            // Same modal as WQMP list → Actions → Add WQMP
            { label: "Add a WQMP", action: () => this.openAddWqmpModal() },
            // Same modal as WQMP list → Create from PDF (Editor-accessible since NPT-1109)
            { label: "Add a WQMP from PDF (AI)", action: () => this.openCreateWqmpFromPdfModal() },
            { label: "Add a BMP", route: ["/treatment-bmps", "new"] },
            { label: "Add delineations", route: ["/delineation", "delineation-map"] },
            { label: "Add a planning project", route: ["/planning", "projects", "new"] },
        ];
        // /dashboard is ManagerOrAdminOnlyGuard — don't offer JurisdictionEditors a link that bounces them
        if (this.authenticationService.isUserAnAdministrator(user) || this.authenticationService.isUserAJurisdictionManager(user)) {
            tasks.push({ label: "Review provisional records", route: ["/dashboard"] });
        }
        return tasks;
    }

    public openAddWqmpModal(): void {
        const dialogRef = this.dialogService.open(WqmpModalComponent, {
            data: { mode: "add" },
            width: "800px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result && typeof result === "object") {
                this.alertService.clearAlerts();
                this.alertService.pushAlert(new Alert("Water Quality Management Plan created successfully.", AlertContext.Success));
                this.router.navigate(["/water-quality-management-plans", result.WaterQualityManagementPlanID]);
            }
        });
    }

    public openCreateWqmpFromPdfModal(): void {
        const dialogRef = this.dialogService.open(WqmpUploadModalComponent, {
            width: "600px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result?.wqmpID) {
                this.router.navigate(["/water-quality-management-plans", result.wqmpID, "review"]);
            }
        });
    }
}
