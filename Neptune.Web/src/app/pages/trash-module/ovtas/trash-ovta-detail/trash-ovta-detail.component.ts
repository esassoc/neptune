import { AsyncPipe, DatePipe } from "@angular/common";
import { Component, Input } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { Observable, switchMap } from "rxjs";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";
import { OnlandVisualTrashAssessmentDetailDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-detail-dto";
import { AlertDisplayComponent } from "../../../../shared/components/alert-display/alert-display.component";
import { PageHeaderComponent } from "../../../../shared/components/page-header/page-header.component";
import { FieldDefinitionComponent } from "../../../../shared/components/field-definition/field-definition.component";
import { ObservationsMapComponent } from "../observations-map/observations-map.component";
import { OnlandVisualTrashAssessmentObservationWithPhotoDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-observation-with-photo-dto";
import { OnlandVisualTrashAssessmentObservationService } from "src/app/shared/generated/api/onland-visual-trash-assessment-observation.service";
import { PreliminarySourceIdentificationCategories } from "src/app/shared/generated/enum/preliminary-source-identification-category-enum";
import { OnlandVisualTrashAssessmentStatusEnum } from "src/app/shared/generated/enum/onland-visual-trash-assessment-status-enum";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";

@Component({
    selector: "trash-ovta-detail",
    imports: [AsyncPipe, AlertDisplayComponent, PageHeaderComponent, FieldDefinitionComponent, DatePipe, ObservationsMapComponent, RouterLink],
    templateUrl: "./trash-ovta-detail.component.html",
    styleUrl: "./trash-ovta-detail.component.scss",
})
export class TrashOvtaDetailComponent {
    public onlandVisualTrashAssessment$: Observable<OnlandVisualTrashAssessmentDetailDto>;
    public onlandVisualTrashAssessmentObservations$: Observable<OnlandVisualTrashAssessmentObservationWithPhotoDto[]>;
    public PreliminarySourceIdentificationCategories = PreliminarySourceIdentificationCategories;
    public OnlandVisualTrashAssessmentStatusEnum = OnlandVisualTrashAssessmentStatusEnum;

    @Input() onlandVisualTrashAssessmentID!: number;
    constructor(
        private onlandVisualTrashAssessmentService: OnlandVisualTrashAssessmentService,
        private onlandVisualTrashAssessmentObservationService: OnlandVisualTrashAssessmentObservationService,
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private router: Router,
        private datePipe: DatePipe
    ) {}

    ngOnInit(): void {
        this.onlandVisualTrashAssessment$ = this.onlandVisualTrashAssessmentService.getOnlandVisualTrashAssessment(this.onlandVisualTrashAssessmentID);
        this.onlandVisualTrashAssessmentObservations$ = this.onlandVisualTrashAssessment$.pipe(
            switchMap((onlandVisualTrashAssessment) => {
                return this.onlandVisualTrashAssessmentObservationService.listByOnlandVisualTrashAssessmentIDOnlandVisualTrashAssessmentObservation(
                    onlandVisualTrashAssessment.OnlandVisualTrashAssessmentID
                );
            })
        );
    }

    public confirmEditOVTA(onlandVisualTrashAssessmentID: number, onlandVisualTrashAssessmentStatusID: number, completedDate: string) {
        if (onlandVisualTrashAssessmentStatusID == OnlandVisualTrashAssessmentStatusEnum.Complete) {
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
        } else {
            this.router.navigateByUrl(`/trash/onland-visual-trash-assessments/edit/${onlandVisualTrashAssessmentID}/record-observations`);
        }
    }

    public deleteOVTA(onlandVisualTrashAssessmentID: number, createdDate: string, statusID: number, completedDate: string) {
        const finalizedWarning =
            statusID === OnlandVisualTrashAssessmentStatusEnum.Complete
                ? `<br/><p>This OVTA was finalized on ${this.datePipe.transform(
                      completedDate,
                      "MM/dd/yyyy"
                  )}. Deleting it will remove its completed score from the OVTA Area.</p>`
                : "";
        const modalContents = `<p>Are you sure you want to delete the assessment from ${this.datePipe.transform(
            createdDate,
            "MM/dd/yyyy"
        )}? This cannot be undone.</p>${finalizedWarning}`;
        this.confirmService
            .confirm({ buttonClassYes: "btn-primary", buttonTextYes: "Delete", buttonTextNo: "Cancel", title: "Delete OVTA", message: modalContents })
            .then((confirmed) => {
                if (confirmed) {
                    this.onlandVisualTrashAssessmentService.deleteOnlandVisualTrashAssessment(onlandVisualTrashAssessmentID).subscribe(() => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("Your OVTA was successfully deleted.", AlertContext.Success));
                        this.router.navigate(["/trash/onland-visual-trash-assessments"]);
                    });
                }
            });
    }
}
