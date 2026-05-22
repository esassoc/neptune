import { Component, OnInit } from "@angular/core";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { CommonModule } from "@angular/common";
import { BehaviorSubject, combineLatest, filter, map, Observable, of, shareReplay, switchMap } from "rxjs";
import { ProjectService } from "src/app/shared/generated/api/project.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { DelineationUpsertDto } from "src/app/shared/generated/model/delineation-upsert-dto";
import { ProjectDocumentDto, ProjectDto, ProjectNetworkSolveHistorySimpleDto } from "src/app/shared/generated/model/models";
import { ProjectUpsertDto } from "src/app/shared/generated/model/project-upsert-dto";
import { TreatmentBMPUpsertDto } from "src/app/shared/generated/model/treatment-bmp-upsert-dto";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { GrantScoresComponent } from "src/app/pages/planning-module/projects/grant-scores/grant-scores.component";
import { AttachmentsDisplayComponent } from "src/app/pages/planning-module/projects/attachments-display/attachments-display.component";
import { ProjectModelResultsComponent } from "src/app/pages/planning-module/projects/project-model-results/project-model-results.component";
import { ProjectMapComponent } from "src/app/pages/planning-module/projects/project-map/project-map.component";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { WorkflowBodyComponent } from "src/app/shared/components/workflow-body/workflow-body.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { ProjectWorkflowProgressService } from "src/app/shared/services/project-workflow-progress.service";

@Component({
    selector: "review",
    templateUrl: "./review.component.html",
    styleUrls: ["./review.component.scss"],
    imports: [
        CommonModule,
        RouterLink,
        CustomRichTextComponent,
        ProjectMapComponent,
        ProjectModelResultsComponent,
        AttachmentsDisplayComponent,
        GrantScoresComponent,
        PageHeaderComponent,
        WorkflowBodyComponent,
        AlertDisplayComponent,
    ],
})
export class ReviewComponent {
    private readonly reloadSubject = new BehaviorSubject<void>(undefined);
    private readonly isLoadingSubmitSubject = new BehaviorSubject<boolean>(false);

    public readonly isLoadingSubmit$ = this.isLoadingSubmitSubject.asObservable();

    public projectID$: Observable<number | null> = of(null);
    public project$: Observable<ProjectDto | null> = of(null);
    public treatmentBMPs$: Observable<TreatmentBMPUpsertDto[]> = of([]);
    public delineations$: Observable<DelineationUpsertDto[]> = of([]);
    public projectNetworkSolveHistories$: Observable<ProjectNetworkSolveHistorySimpleDto[]> = of([]);
    public attachments$: Observable<ProjectDocumentDto[]> = of([]);

    public customRichTextTypeID = NeptunePageTypeEnum.HippocampReview;

    constructor(
        private projectService: ProjectService,
        private route: ActivatedRoute,
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private projectWorkflowProgressService: ProjectWorkflowProgressService
    ) {}

    public ngOnInit(): void {
        this.projectID$ = this.route.paramMap.pipe(
            map((params) => parseInt(params.get("projectID") ?? "", 10)),
            filter((projectID) => Number.isFinite(projectID)),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        const refreshTrigger$ = this.reloadSubject.asObservable();

        this.project$ = combineLatest([this.projectID$, refreshTrigger$]).pipe(
            map(([projectID]) => projectID),
            switchMap((projectID) => this.projectService.getProject(projectID)),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.treatmentBMPs$ = combineLatest([this.projectID$, refreshTrigger$]).pipe(
            map(([projectID]) => projectID),
            switchMap((projectID) => this.projectService.listTreatmentBMPsAsUpsertDtosByProjectIDProject(projectID)),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.delineations$ = combineLatest([this.projectID$, refreshTrigger$]).pipe(
            map(([projectID]) => projectID),
            switchMap((projectID) => this.projectService.listDelineationsByProjectIDProject(projectID)),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.projectNetworkSolveHistories$ = combineLatest([this.projectID$, refreshTrigger$]).pipe(
            map(([projectID]) => projectID),
            switchMap((projectID) => this.projectService.listProjectNetworkSolveHistoriesForProjectProject(projectID)),
            shareReplay({ bufferSize: 1, refCount: true })
        );

        this.attachments$ = combineLatest([this.projectID$, refreshTrigger$]).pipe(
            map(([projectID]) => projectID),
            switchMap((projectID) => this.projectService.listAttachmentsByProjectIDProject(projectID)),
            shareReplay({ bufferSize: 1, refCount: true })
        );
    }

    public showModelResultsPanel(project: ProjectDto | null): boolean {
        return !project?.DoesNotIncludeTreatmentBMPs && project.HasModeledResults;
    }

    private mapProjectToUpsertDto(project: ProjectDto): ProjectUpsertDto {
        return new ProjectUpsertDto({
            ProjectName: project.ProjectName,
            OrganizationID: project.OrganizationID,
            StormwaterJurisdictionID: project.StormwaterJurisdictionID,
            PrimaryContactPersonID: project.PrimaryContactPersonID,
            ProjectDescription: project.ProjectDescription,
            AdditionalContactInformation: project.AdditionalContactInformation,
            DoesNotIncludeTreatmentBMPs: project.DoesNotIncludeTreatmentBMPs,
            CalculateOCTAM2Tier2Scores: project.CalculateOCTAM2Tier2Scores,
        });
    }

    public shareOrRevokeOCTAScores(
        projectID: number,
        project: ProjectDto,
        treatmentBMPs: TreatmentBMPUpsertDto[],
        projectNetworkSolveHistories: ProjectNetworkSolveHistorySimpleDto[]
    ) {
        var modalContents =
            "<p>You are about to revoke sharing of this project with the OCTA M2 Tier 2 grant program. This will allow you to edit this project." +
            "<p>Are you sure you wish to proceed?</p>";
        var buttonTextYes = "Revoke";
        var canSubmit = true;

        if (!project.ShareOCTAM2Tier2Scores) {
            buttonTextYes = "Share";

            canSubmit = project.CalculateOCTAM2Tier2Scores && (treatmentBMPs.length > 0 ? projectNetworkSolveHistories.length > 0 : project.DoesNotIncludeTreatmentBMPs);

            modalContents = canSubmit
                ? "<p>I certify that I have inventoried all upstream BMPs of my project within the OC Stormwater Tools Inventory Module and made them ready for modeling.</p>"
                : "<p>You are required to check the box to view OCTA M2 Tier 2 Metrics on the Basics step, and to calculate OCTA Metrics for Treatment BMPs before the project can be shared and submitted to the grant agency.</p>";
        }

        this.confirmService
            .confirm({
                buttonClassYes: "btn-primary",
                //buttonDisabledYes: !canSubmit,
                buttonTextYes: buttonTextYes,
                buttonTextNo: "Cancel",
                title: `${buttonTextYes} Project`,
                message: modalContents,
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.isLoadingSubmitSubject.next(true);

                    var model = this.mapProjectToUpsertDto(project);
                    model.ShareOCTAM2Tier2Scores = !project.ShareOCTAM2Tier2Scores;

                    this.projectService.updateProject(projectID, model).subscribe(
                        () => {
                            this.isLoadingSubmitSubject.next(false);
                            this.projectWorkflowProgressService.updateProgress(projectID);
                            this.reloadSubject.next();
                            this.alertService.pushAlert(new Alert(`Your project was successfully ${buttonTextYes == "Share" ? "shared" : "revoked"}.`, AlertContext.Success));
                            window.scroll(0, 0);
                        },
                        (error) => {
                            this.isLoadingSubmitSubject.next(false);
                            window.scroll(0, 0);
                        }
                    );
                }
            });
    }
}
