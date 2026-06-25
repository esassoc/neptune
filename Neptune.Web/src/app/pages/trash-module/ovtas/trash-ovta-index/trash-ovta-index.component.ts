import { Component } from "@angular/core";
import { BehaviorSubject, Observable, switchMap, tap } from "rxjs";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { ColDef } from "ag-grid-community";
import { AsyncPipe, DatePipe } from "@angular/common";
import { OnlandVisualTrashAssessmentGridDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-grid-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { IconComponent } from "../../../../shared/components/icon/icon.component";
import { Router, RouterLink } from "@angular/router";
import { OnlandVisualTrashAssessmentStatusEnum } from "src/app/shared/generated/enum/onland-visual-trash-assessment-status-enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AuthenticationService } from "src/app/services/authentication.service";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

@Component({
    selector: "trash-ovta-index",
    imports: [NeptuneGridComponent, PageHeaderComponent, AlertDisplayComponent, AsyncPipe, LoadingDirective, IconComponent, RouterLink],
    templateUrl: "./trash-ovta-index.component.html",
    styleUrl: "./trash-ovta-index.component.scss",
})
export class TrashOvtaIndexComponent {
    public onlandVisualTrashAssessments$: Observable<OnlandVisualTrashAssessmentGridDto[]>;
    public ovtaColumnDefs: ColDef[];
    public customRichTextID = NeptunePageTypeEnum.OVTAIndex;
    public isLoading: boolean = true;

    private refreshGridTrigger$ = new BehaviorSubject<void>(null);

    constructor(
        private onlandVisualTrashAssessmentService: OnlandVisualTrashAssessmentService,
        private utilityFunctionsService: UtilityFunctionsService,
        private router: Router,
        private confirmService: ConfirmService,
        private alertService: AlertService,
        private datePipe: DatePipe,
        private authenticationService: AuthenticationService
    ) {}

    ngOnInit(): void {
        this.ovtaColumnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                return [
                    { ActionName: "View", ActionIcon: "fas fa-file-alt", ActionHandler: () => this.router.navigate(["trash", "onland-visual-trash-assessments", params.data.OnlandVisualTrashAssessmentID]) },
                    {
                        ActionName: params.data.OnlandVisualTrashAssessmentStatusID == OnlandVisualTrashAssessmentStatusEnum.Complete ? "Return to Edit" : "Edit",
                        ActionIcon: "fas fa-edit",
                        ActionHandler: () =>
                            params.data.OnlandVisualTrashAssessmentStatusID == OnlandVisualTrashAssessmentStatusEnum.Complete
                                ? this.confirmEditOVTA(params.data.OnlandVisualTrashAssessmentID, params.data.CompletedDate)
                                : this.router.navigateByUrl(`/trash/onland-visual-trash-assessments/edit/${params.data.OnlandVisualTrashAssessmentID}/record-observations`),
                    },
                    {
                        ActionName: "Delete",
                        ActionIcon: "fas fa-trash text-danger",
                        ActionHandler: () =>
                            this.deleteOVTA(
                                params.data.OnlandVisualTrashAssessmentID,
                                params.data.CreatedDate,
                                params.data.OnlandVisualTrashAssessmentStatusID,
                                params.data.CompletedDate
                            ),
                    },
                ];
            }),
            this.utilityFunctionsService.createLinkColumnDef("Assessment ID", "OnlandVisualTrashAssessmentID", "OnlandVisualTrashAssessmentID", {
                InRouterLink: "../onland-visual-trash-assessments/",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Assessment Area Name", "OnlandVisualTrashAssessmentAreaName", "OnlandVisualTrashAssessmentAreaID", {
                InRouterLink: "../onland-visual-trash-assessment-areas/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Assessment Score", "OnlandVisualTrashAssessmentScoreName", {
                CustomDropdownFilterField: "OnlandVisualTrashAssessmentScoreName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Assessment Type", "IsProgressAssessment", { CustomDropdownFilterField: "IsProgressAssessment" }),
            this.utilityFunctionsService.createDateColumnDef("Assessment Date", "CompletedDate", "shortDate"),
            this.utilityFunctionsService.createBasicColumnDef("Status", "OnlandVisualTrashAssessmentStatusName", {
                CustomDropdownFilterField: "OnlandVisualTrashAssessmentStatusName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName", {
                CustomDropdownFilterField: "StormwaterJurisdictionName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Created By", "CreatedByPersonFullName"),
            this.utilityFunctionsService.createBasicColumnDef("Second Assessor", "SecondAssessorName"),
            this.utilityFunctionsService.createDateColumnDef("Created On", "CreatedDate", "short"),
        ];
        this.onlandVisualTrashAssessments$ = this.refreshGridTrigger$.pipe(
            tap(() => (this.isLoading = true)),
            switchMap(() => this.onlandVisualTrashAssessmentService.listOnlandVisualTrashAssessment()),
            tap(() => (this.isLoading = false))
        );
    }

    public confirmEditOVTA(onlandVisualTrashAssessmentID: number, completedDate: string) {
        const modalContents = `<p>This OVTA was finalized on ${this.datePipe.transform(
            completedDate,
            "MM/dd/yyyy"
        )}. Are you sure you want to revert this OVTA to the \"In Progress\" status?</p>`;
        this.confirmService
            .confirm({ buttonClassYes: "btn-primary", buttonTextYes: "Continue", buttonTextNo: "Cancel", title: "Return OVTA to Edit", message: modalContents })
            .then((confirmed) => {
                if (confirmed) {
                    this.onlandVisualTrashAssessmentService.editStatusToAllowEditOnlandVisualTrashAssessment(onlandVisualTrashAssessmentID).subscribe((response) => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert('The OVTA was successfully returned to the "In Progress" status.', AlertContext.Success));
                        this.router.navigateByUrl(`/trash/onland-visual-trash-assessments/edit/${onlandVisualTrashAssessmentID}/record-observations`);
                    });
                }
            });
    }

    public deleteOVTA(onlandVisualTrashAssessmentID: number, createdDate: string, statusID: number, completedDate: string) {
        const safeCreatedDate = escapeHtml(this.datePipe.transform(createdDate, "MM/dd/yyyy") ?? "");
        const safeCompletedDate = escapeHtml(this.datePipe.transform(completedDate, "MM/dd/yyyy") ?? "");
        const finalizedWarning =
            statusID === OnlandVisualTrashAssessmentStatusEnum.Complete
                ? `<br/><p>This OVTA was finalized on ${safeCompletedDate}. Deleting it will remove its completed score from the OVTA Area.</p>`
                : "";
        const modalContents = `<p>Are you sure you want to delete the assessment from ${safeCreatedDate}? This cannot be undone.</p>${finalizedWarning}`;
        this.confirmService
            .confirm({ buttonClassYes: "btn-primary", buttonTextYes: "Delete", buttonTextNo: "Cancel", title: "Delete OVTA", message: modalContents })
            .then((confirmed) => {
                if (confirmed) {
                    this.onlandVisualTrashAssessmentService.deleteOnlandVisualTrashAssessment(onlandVisualTrashAssessmentID).subscribe(() => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("Your OVTA was successfully deleted.", AlertContext.Success));
                        this.refreshGridTrigger$.next();
                    });
                }
            });
    }

    public currentUserHasJurisdictionManagePermission(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }
}
