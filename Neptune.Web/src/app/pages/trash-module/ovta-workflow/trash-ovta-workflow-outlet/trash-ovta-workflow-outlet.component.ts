import { Component, OnInit, Input } from "@angular/core";
import { Router, RouterLink, RouterOutlet } from "@angular/router";
import { Observable, tap } from "rxjs";
import { OvtaWorkflowProgressService } from "src/app/shared/services/ovta-workflow-progress.service";
import { AsyncPipe } from "@angular/common";
import { OnlandVisualTrashAssessmentWorkflowProgressDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-workflow-progress-dto";
import { WorkflowNavComponent } from "../../../../shared/components/workflow-nav/workflow-nav.component";
import { WorkflowNavItemComponent } from "../../../../shared/components/workflow-nav/workflow-nav-item/workflow-nav-item.component";

@Component({
    selector: "trash-ovta-workflow-outlet",
    imports: [RouterOutlet, RouterLink, AsyncPipe, WorkflowNavComponent, WorkflowNavItemComponent],
    templateUrl: "./trash-ovta-workflow-outlet.component.html",
    styleUrl: "./trash-ovta-workflow-outlet.component.scss",
    standalone: true,
})
export class TrashOvtaWorkflowOutletComponent implements OnInit {
    public submitted: boolean = false;
    public progress$: Observable<OnlandVisualTrashAssessmentWorkflowProgressDto>;
    @Input() onlandVisualTrashAssessmentID: number | null = null;

    constructor(
        private router: Router,
        private ovtaProgressService: OvtaWorkflowProgressService
    ) {}

    ngOnInit() {
        this.progress$ = this.ovtaProgressService.progressObservable$.pipe(
            tap(() => {
                this.submitted = false;
            })
        );
        this.ovtaProgressService.getProgress(this.onlandVisualTrashAssessmentID);
    }
}
