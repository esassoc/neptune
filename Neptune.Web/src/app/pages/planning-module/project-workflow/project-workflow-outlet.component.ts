import { Component, OnInit, Input } from "@angular/core";
import { RouterLink, RouterOutlet } from "@angular/router";
import { ProjectUpsertDto } from "src/app/shared/generated/model/project-upsert-dto";
import { Observable } from "rxjs";
import { tap } from "rxjs/operators";
import { ProjectWorkflowProgressDto } from "src/app/shared/generated/model/models";
import { WorkflowNavComponent } from "src/app/shared/components/workflow-nav/workflow-nav.component";
import { WorkflowNavItemComponent } from "src/app/shared/components/workflow-nav/workflow-nav-item/workflow-nav-item.component";
import { AsyncPipe } from "@angular/common";
import { ProjectWorkflowProgressService } from "src/app/shared/services/project-workflow-progress.service";

@Component({
    selector: "project-workflow-outlet",
    templateUrl: "./project-workflow-outlet.component.html",
    styleUrls: ["./project-workflow-outlet.component.scss"],
    imports: [WorkflowNavComponent, WorkflowNavItemComponent, AsyncPipe, RouterOutlet, RouterLink],
})
export class ProjectWorkflowOutletComponent implements OnInit {
    public projectModel: ProjectUpsertDto;
    public submitted: boolean = false;
    public progress$: Observable<ProjectWorkflowProgressDto>;
    @Input() projectID: number | null = null;

    constructor(private projectProgressService: ProjectWorkflowProgressService) {}

    ngOnInit(): void {
        this.progress$ = this.projectProgressService.progressObservable$.pipe(
            tap((x) => {
                this.submitted = false;
            })
        );
        this.projectProgressService.getProgress(this.projectID);
    }
}
