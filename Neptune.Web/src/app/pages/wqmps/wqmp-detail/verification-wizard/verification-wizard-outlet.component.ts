import { Component, Input, OnInit, inject } from "@angular/core";
import { RouterLink, RouterOutlet } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { Observable } from "rxjs";

import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { WorkflowNavComponent } from "src/app/shared/components/workflow-nav/workflow-nav.component";
import { WorkflowNavItemComponent } from "src/app/shared/components/workflow-nav/workflow-nav-item/workflow-nav-item.component";

import { WqmpVerificationWorkflowService } from "src/app/shared/services/wqmp-verification-workflow.service";

@Component({
    selector: "verification-wizard-outlet",
    standalone: true,
    imports: [
        AlertDisplayComponent,
        WorkflowNavComponent, WorkflowNavItemComponent,
        RouterLink, RouterOutlet, AsyncPipe,
    ],
    templateUrl: "./verification-wizard-outlet.component.html",
    styleUrl: "./verification-wizard-outlet.component.scss",
})
export class VerificationWizardOutletComponent implements OnInit {
    @Input() waterQualityManagementPlanID!: number;
    @Input() waterQualityManagementPlanVerifyID?: number;

    public service = inject(WqmpVerificationWorkflowService);

    public loaded$: Observable<boolean>;

    ngOnInit(): void {
        this.loaded$ = this.service.load(
            this.waterQualityManagementPlanID,
            this.waterQualityManagementPlanVerifyID ?? null,
        );
    }
}
