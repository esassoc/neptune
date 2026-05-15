import { Component, Input, OnInit, OnDestroy } from "@angular/core";
import { Router, RouterModule, RouterOutlet } from "@angular/router";
import { AsyncPipe, DatePipe } from "@angular/common";
import { Observable, Subscription } from "rxjs";

import { WorkflowNavComponent } from "src/app/shared/components/workflow-nav/workflow-nav.component";
import { WorkflowNavItemComponent } from "src/app/shared/components/workflow-nav/workflow-nav-item/workflow-nav-item.component";
import { WorkflowNavSubItemComponent } from "src/app/shared/components/workflow-nav/workflow-nav-sub-item/workflow-nav-sub-item.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";

import { DialogService } from "@ngneat/dialog";
import { ChangeDateAndTypeModalComponent, ChangeDateAndTypeModalContext } from "./change-date-and-type-modal/change-date-and-type-modal.component";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { FieldVisitWorkflowService } from "../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

@Component({
    selector: "field-visit-workflow-outlet",
    standalone: true,
    templateUrl: "./field-visit-workflow-outlet.component.html",
    styleUrl: "./field-visit-workflow-outlet.component.scss",
    imports: [RouterModule, RouterOutlet, AsyncPipe, DatePipe, WorkflowNavComponent, WorkflowNavItemComponent, WorkflowNavSubItemComponent, LoadingDirective, AlertDisplayComponent],
})
export class FieldVisitWorkflowOutletComponent implements OnInit, OnDestroy {
    @Input() fieldVisitID: number | null = null;

    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    public isLoading = true;
    private sub: Subscription | undefined;

    constructor(
        private workflowService: FieldVisitWorkflowService,
        private fieldVisitService: FieldVisitService,
        private dialogService: DialogService,
        private alertService: AlertService,
        private authenticationService: AuthenticationService,
        private confirmService: ConfirmService,
        private router: Router
    ) {}

    // NPT-984: Delete Field Visit is Manager-only (tightened backend to JurisdictionManageFeature).
    // The frontend gate has to match — Editors performing visits should not see the destructive
    // header-level delete icon.
    public get canManage(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        if (this.fieldVisitID != null) {
            this.sub = this.workflowService.load(this.fieldVisitID).subscribe(() => (this.isLoading = false));
        }
    }

    ngOnDestroy(): void {
        this.sub?.unsubscribe();
        this.workflowService.clear();
    }

    openChangeDateAndTypeModal(workflow: FieldVisitWorkflowDto): void {
        this.dialogService
            .open(ChangeDateAndTypeModalComponent, {
                data: { fieldVisit: workflow } as ChangeDateAndTypeModalContext,
            })
            .afterClosed$.subscribe((updated) => {
                if (updated) {
                    this.workflowService.refresh().subscribe();
                }
            });
    }

    backToBMP(workflow: FieldVisitWorkflowDto): void {
        this.router.navigate(["/treatment-bmps", workflow.TreatmentBMPID]);
    }

    deleteVisit(workflow: FieldVisitWorkflowDto): void {
        this.confirmService
            .confirm({
                title: "Delete Field Visit",
                message: "Are you sure you want to delete this Field Visit? This will remove the visit, all assessments, and the maintenance record.",
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.fieldVisitService.deleteFieldVisit(workflow.FieldVisitID).subscribe(() => {
                    this.alertService.pushAlert(new Alert("Field Visit deleted.", AlertContext.Success));
                    this.router.navigate(["/treatment-bmps", workflow.TreatmentBMPID]);
                });
            });
    }

    wrapUpVisit(workflow: FieldVisitWorkflowDto): void {
        this.confirmService
            .confirm({
                title: "Wrap Up Visit",
                message:
                    "Are you sure you want to wrap up the field visit? Wrapping up will mark the field visit as complete and ready for review by the Jurisdiction Manager. " +
                    "Any unsaved form changes on the current step will be lost.",
                buttonClassYes: "btn btn-primary",
                buttonTextYes: "Continue",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.fieldVisitService.finalizeFieldVisit(workflow.FieldVisitID).subscribe(() => {
                    this.alertService.pushAlert(new Alert("Field Visit marked Complete.", AlertContext.Success));
                    this.router.navigate(["/treatment-bmps", workflow.TreatmentBMPID]);
                });
            });
    }
}
