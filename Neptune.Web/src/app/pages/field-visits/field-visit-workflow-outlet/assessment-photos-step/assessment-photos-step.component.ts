import { Component, Input, OnInit } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormGroup } from "@angular/forms";
import { forkJoin, Observable, of, switchMap, take } from "rxjs";

import { ImageEditorComponent, ImageEditorItem } from "src/app/shared/components/image-editor/image-editor.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";

import { TreatmentBMPAssessmentByFieldVisitService } from "src/app/shared/generated/api/treatment-bmp-assessment-by-field-visit.service";
import { TreatmentBMPAssessmentPhotoService } from "src/app/shared/generated/api/treatment-bmp-assessment-photo.service";
import { TreatmentBMPAssessmentDetailDto } from "src/app/shared/generated/model/treatment-bmp-assessment-detail-dto";
import { TreatmentBMPAssessmentPhotoDto } from "src/app/shared/generated/model/treatment-bmp-assessment-photo-dto";
import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

interface AssessmentPhotoEditorItem extends ImageEditorItem {
    /** Assessment-photo primary key — used for delete + caption updates. */
    TreatmentBMPAssessmentPhotoID?: number;
}

@Component({
    selector: "field-visit-assessment-photos-step",
    standalone: true,
    imports: [AsyncPipe, ImageEditorComponent, LoadingDirective],
    templateUrl: "./assessment-photos-step.component.html",
    styleUrl: "./assessment-photos-step.component.scss",
})
export class FieldVisitAssessmentPhotosStepComponent implements OnInit {
    /** 1 = Initial, 2 = PostMaintenance — passed via route data. */
    @Input() assessmentTypeID: number = 1;

    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    public assessment: TreatmentBMPAssessmentDetailDto | null = null;
    public photos: AssessmentPhotoEditorItem[] = [];
    public captionControlForm = new FormGroup({});
    public isLoading = true;

    constructor(
        private workflowService: FieldVisitWorkflowService,
        private assessmentByFieldVisitService: TreatmentBMPAssessmentByFieldVisitService,
        private photoService: TreatmentBMPAssessmentPhotoService,
        private alertService: AlertService,
        private router: Router
    ) {}

    public get isPostMaintenance(): boolean {
        return this.assessmentTypeID === 2;
    }

    public get headerLabel(): string {
        return this.isPostMaintenance ? "Post-Maintenance Assessment Photos" : "Initial Assessment Photos";
    }

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.workflow$
            .pipe(
                take(1),
                switchMap((workflow) => {
                    if (!workflow) return of(null);
                    return this.assessmentByFieldVisitService.getByTypeTreatmentBMPAssessmentByFieldVisit(workflow.FieldVisitID, this.assessmentTypeID);
                })
            )
            .subscribe((assessment) => {
                this.assessment = assessment;
                if (assessment) {
                    this.refreshPhotos();
                } else {
                    this.isLoading = false;
                }
            });
    }

    private refreshPhotos(): void {
        if (!this.assessment) return;
        this.isLoading = true;
        this.photoService.listTreatmentBMPAssessmentPhoto(this.assessment.TreatmentBMPAssessmentID).subscribe((photos) => {
            this.photos = photos.map((p) => this.toEditorItem(p));
            this.isLoading = false;
        });
    }

    private toEditorItem(photo: TreatmentBMPAssessmentPhotoDto): AssessmentPhotoEditorItem {
        return {
            PrimaryKey: photo.TreatmentBMPAssessmentPhotoID,
            FileResourceGUID: photo.FileResourceGUID,
            Caption: photo.Caption,
            TreatmentBMPAssessmentPhotoID: photo.TreatmentBMPAssessmentPhotoID,
        };
    }

    onPhotoUpload(event: { file: File; caption: string }): void {
        if (!this.assessment) return;
        this.photoService.createTreatmentBMPAssessmentPhoto(this.assessment.TreatmentBMPAssessmentID, event.file, event.caption).subscribe(() => {
            this.alertService.pushAlert(new Alert("Photo uploaded.", AlertContext.Success));
            this.refreshPhotos();
        });
    }

    onPhotoDelete(item: AssessmentPhotoEditorItem): void {
        if (!this.assessment || !item.TreatmentBMPAssessmentPhotoID) return;
        this.photoService.deleteTreatmentBMPAssessmentPhoto(this.assessment.TreatmentBMPAssessmentID, item.TreatmentBMPAssessmentPhotoID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Photo deleted.", AlertContext.Success));
            this.refreshPhotos();
        });
    }

    onSaveCaptions(updated: AssessmentPhotoEditorItem[]): void {
        if (!this.assessment) return;
        if (updated.length === 0) {
            this.alertService.pushAlert(new Alert("No caption changes to save.", AlertContext.Info));
            return;
        }

        const requests = updated
            .filter((item) => item.TreatmentBMPAssessmentPhotoID != null)
            .map((item) =>
                this.photoService.updateCaptionTreatmentBMPAssessmentPhoto(this.assessment!.TreatmentBMPAssessmentID, item.TreatmentBMPAssessmentPhotoID!, {
                    TreatmentBMPAssessmentPhotoID: item.TreatmentBMPAssessmentPhotoID!,
                    Caption: item.Caption ?? null,
                })
            );

        forkJoin(requests).subscribe({
            next: () => {
                this.alertService.pushAlert(new Alert("Captions saved.", AlertContext.Success));
                this.refreshPhotos();
            },
            error: () => {
                this.alertService.pushAlert(new Alert("An error occurred saving captions.", AlertContext.Danger));
            },
        });
    }

    cancel(workflow: FieldVisitWorkflowDto): void {
        const branch = this.isPostMaintenance ? "post-maintenance-assessment" : "assessment";
        this.router.navigate(["/field-visits", workflow.FieldVisitID, branch]);
    }
}
