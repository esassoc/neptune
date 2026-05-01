import { Component, Input, OnInit, OnDestroy } from "@angular/core";
import { Router, RouterModule, RouterOutlet } from "@angular/router";
import { AsyncPipe, DatePipe } from "@angular/common";
import { Observable, Subscription } from "rxjs";

import { WorkflowNavComponent } from "src/app/shared/components/workflow-nav/workflow-nav.component";
import { WorkflowNavItemComponent } from "src/app/shared/components/workflow-nav/workflow-nav-item/workflow-nav-item.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";

import { DialogService } from "@ngneat/dialog";
import { ChangeDateAndTypeModalComponent, ChangeDateAndTypeModalContext } from "./change-date-and-type-modal/change-date-and-type-modal.component";

import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { FieldVisitWorkflowService } from "../services/field-visit-workflow.service";

@Component({
    selector: "field-visit-workflow-outlet",
    standalone: true,
    templateUrl: "./field-visit-workflow-outlet.component.html",
    styleUrl: "./field-visit-workflow-outlet.component.scss",
    imports: [RouterModule, RouterOutlet, AsyncPipe, DatePipe, WorkflowNavComponent, WorkflowNavItemComponent, LoadingDirective, AlertDisplayComponent],
})
export class FieldVisitWorkflowOutletComponent implements OnInit, OnDestroy {
    @Input() fieldVisitID: number | null = null;

    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    public isLoading = true;
    private sub: Subscription | undefined;

    constructor(private workflowService: FieldVisitWorkflowService, private dialogService: DialogService, private router: Router) {}

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
}
