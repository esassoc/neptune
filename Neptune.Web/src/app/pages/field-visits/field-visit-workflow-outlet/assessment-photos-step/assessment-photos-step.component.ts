import { Component, Input, OnInit, signal } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormGroup } from "@angular/forms";
import { DialogService } from "@ngneat/dialog";
import { Observable, of, switchMap, take } from "rxjs";

import { ImageEditorComponent, ImageEditorItem } from "src/app/shared/components/image-editor/image-editor.component";
import { ImageCarouselComponent, ImageCarouselItem } from "src/app/shared/components/image-carousel/image-carousel.component";
import {
    EditPhotoCaptionModalComponent,
    EditPhotoCaptionModalContext,
} from "src/app/shared/components/edit-photo-caption-modal/edit-photo-caption-modal.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

import { TreatmentBMPAssessmentByFieldVisitService } from "src/app/shared/generated/api/treatment-bmp-assessment-by-field-visit.service";
import { TreatmentBMPAssessmentPhotoService } from "src/app/shared/generated/api/treatment-bmp-assessment-photo.service";
import { FileResourceService } from "src/app/shared/generated/api/file-resource.service";
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
    imports: [AsyncPipe, ImageEditorComponent, ImageCarouselComponent, LoadingDirective, PageHeaderComponent],
    templateUrl: "./assessment-photos-step.component.html",
    styleUrl: "./assessment-photos-step.component.scss",
})
export class FieldVisitAssessmentPhotosStepComponent implements OnInit {
    /** 1 = Initial, 2 = PostMaintenance — passed via route data. */
    @Input() assessmentTypeID: number = 1;

    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    // Signals — plain fields don't reliably trigger CD when mutated inside subscribe callbacks
    // under zoneless behavior, leaving the spinner stuck until a stray click forces a render.
    public assessment = signal<TreatmentBMPAssessmentDetailDto | null>(null);
    public photos = signal<AssessmentPhotoEditorItem[]>([]);
    public captionControlForm = new FormGroup({});
    public isLoading = signal(true);
    public isReadOnly = signal(false);

    constructor(
        private workflowService: FieldVisitWorkflowService,
        private assessmentByFieldVisitService: TreatmentBMPAssessmentByFieldVisitService,
        private photoService: TreatmentBMPAssessmentPhotoService,
        private fileResourceService: FileResourceService,
        private alertService: AlertService,
        private dialogService: DialogService,
        private router: Router
    ) {}

    public get isPostMaintenance(): boolean {
        return this.assessmentTypeID === 2;
    }

    public get headerLabel(): string {
        // Single-word page title — the sidebar already says which assessment is active,
        // so no need to repeat "Initial / Post-Maintenance Assessment" in the page header.
        return "Photos";
    }

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.workflowService.clearStepAlerts();
        this.workflow$
            .pipe(
                take(1),
                switchMap((workflow) => {
                    this.isReadOnly.set(this.workflowService.isReadOnly(workflow));
                    if (!workflow) return of(null);
                    return this.assessmentByFieldVisitService.getByTypeTreatmentBMPAssessmentByFieldVisit(workflow.FieldVisitID, this.assessmentTypeID);
                })
            )
            .subscribe((assessment) => {
                this.assessment.set(assessment);
                if (assessment) {
                    this.refreshPhotos();
                } else {
                    this.isLoading.set(false);
                }
            });
    }

    private refreshPhotos(): void {
        const assessment = this.assessment();
        if (!assessment) return;
        this.isLoading.set(true);
        this.photoService.listTreatmentBMPAssessmentPhoto(assessment.TreatmentBMPAssessmentID).subscribe((photos) => {
            this.photos.set(photos.map((p) => this.toEditorItem(p)));
            this.isLoading.set(false);
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
        const assessment = this.assessment();
        if (!assessment) return;
        this.photoService.createTreatmentBMPAssessmentPhoto(assessment.TreatmentBMPAssessmentID, event.file, event.caption).subscribe(() => {
            this.alertService.pushAlert(new Alert("Photo uploaded.", AlertContext.Success));
            this.refreshPhotos();
        });
    }

    onPhotoDelete(item: AssessmentPhotoEditorItem): void {
        const assessment = this.assessment();
        if (!assessment || !item.TreatmentBMPAssessmentPhotoID) return;
        this.photoService.deleteTreatmentBMPAssessmentPhoto(assessment.TreatmentBMPAssessmentID, item.TreatmentBMPAssessmentPhotoID).subscribe(() => {
            this.alertService.pushAlert(new Alert("Photo deleted.", AlertContext.Success));
            this.refreshPhotos();
        });
    }

    onCaptionEditRequested(item: ImageEditorItem): void {
        const assessment = this.assessment();
        if (!assessment) return;
        const photoID = (item as AssessmentPhotoEditorItem).TreatmentBMPAssessmentPhotoID;
        if (!photoID || !item.FileResourceGUID) return;

        // Pre-load the photo blob into an object URL so the modal preview matches the on-page thumbnail.
        this.fileResourceService.displayResourceFileResource(item.FileResourceGUID, "body", false, { httpHeaderAccept: undefined }).subscribe((blob: Blob) => {
            const previewUrl = URL.createObjectURL(blob);
            this.dialogService
                .open(EditPhotoCaptionModalComponent, {
                    data: {
                        currentCaption: item.Caption ?? "",
                        previewUrl,
                        title: "Edit Photo Caption",
                    } as EditPhotoCaptionModalContext,
                })
                .afterClosed$.subscribe((newCaption: string | null | undefined) => {
                    URL.revokeObjectURL(previewUrl);
                    if (newCaption == null) return;
                    if ((newCaption ?? "") === (item.Caption ?? "")) return;
                    this.photoService
                        .updateCaptionTreatmentBMPAssessmentPhoto(assessment.TreatmentBMPAssessmentID, photoID, {
                            TreatmentBMPAssessmentPhotoID: photoID,
                            Caption: newCaption,
                        })
                        .subscribe({
                            next: () => {
                                this.alertService.pushAlert(new Alert("Caption saved.", AlertContext.Success));
                                this.refreshPhotos();
                            },
                            error: () => {
                                this.alertService.pushAlert(new Alert("Failed to save caption.", AlertContext.Danger));
                            },
                        });
                });
        });
    }

    cancel(workflow: FieldVisitWorkflowDto): void {
        const branch = this.isPostMaintenance ? "post-maintenance-assessment" : "assessment";
        this.router.navigate(["/field-visits", workflow.FieldVisitID, branch]);
    }
}
