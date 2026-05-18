import { Component, EventEmitter, inject, Input, OnInit, Output } from "@angular/core";
import { FormGroup } from "@angular/forms";
import { AsyncPipe } from "@angular/common";
import { DialogService } from "@ngneat/dialog";
import { BehaviorSubject, combineLatest, map, Observable, switchMap } from "rxjs";

import { TreatmentBMPImageByTreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp-image-by-treatment-bmp.service";
import { FileResourceService } from "src/app/shared/generated/api/file-resource.service";
import { TreatmentBMPImageDto } from "src/app/shared/generated/model/treatment-bmp-image-dto";
import { ImageEditorComponent, ImageEditorItem } from "src/app/shared/components/image-editor/image-editor.component";
import {
    EditPhotoCaptionModalComponent,
    EditPhotoCaptionModalContext,
} from "src/app/shared/components/edit-photo-caption-modal/edit-photo-caption-modal.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/**
 * Reusable embedded editor for a Treatment BMP's photos. Wraps the generic
 * &lt;image-editor&gt; with the BMP-specific service calls. Used by the BMP detail
 * routed edit page (/treatment-bmps/:id/edit-images) and by the Field Visit
 * Inventory workflow step's Photos panel — both share upload/delete/caption-save
 * logic without round-tripping the user to the detail page.
 */
@Component({
    selector: "treatment-bmp-images-editor",
    standalone: true,
    imports: [AsyncPipe, LoadingDirective, ImageEditorComponent],
    templateUrl: "./treatment-bmp-images-editor.component.html",
})
export class TreatmentBmpImagesEditorComponent implements OnInit {
    private treatmentBMPImageService = inject(TreatmentBMPImageByTreatmentBMPService);
    private fileResourceService = inject(FileResourceService);
    private alertService = inject(AlertService);
    private dialogService = inject(DialogService);

    @Input() treatmentBMPID!: number;
    /** Opt-in modal-based caption editing — replaces the inline caption form + page-level Save with
     * a per-photo pencil button that opens a modal and persists immediately. */
    @Input() useCaptionModal: boolean = false;

    @Output() saved = new EventEmitter<void>();
    @Output() cancelled = new EventEmitter<void>();
    /** Fired after a successful image upload — distinct from `(saved)` (caption save click). */
    @Output() uploaded = new EventEmitter<void>();
    /** Fired after a successful image delete — distinct from `(saved)` (caption save click). */
    @Output() deleted = new EventEmitter<void>();
    /** Fired after a successful caption update via the modal pattern. */
    @Output() captionUpdated = new EventEmitter<void>();

    public treatmentBMPImages$!: Observable<TreatmentBMPImageDto[]>;
    public reloadTrigger$ = new BehaviorSubject<void>(undefined);
    public imageEditorItems$!: Observable<ImageEditorItem[]>;
    public captionControlForm = new FormGroup({});
    public isLoadingSubmit = false;

    ngOnInit(): void {
        this.treatmentBMPImages$ = combineLatest([this.reloadTrigger$]).pipe(
            switchMap(() => this.treatmentBMPImageService.listTreatmentBMPImageByTreatmentBMP(this.treatmentBMPID))
        );

        this.imageEditorItems$ = this.treatmentBMPImages$.pipe(
            map((images) =>
                images.map((bmpImage) => ({
                    PrimaryKey: bmpImage.TreatmentBMPImageID,
                    FileResourceGUID: bmpImage.FileResourceGUID,
                    Caption: bmpImage.Caption,
                }))
            )
        );
    }

    public canExit(): boolean {
        return !this.captionControlForm.dirty;
    }

    public onNewImageAdded(event: { file: File; caption: string }): void {
        this.isLoadingSubmit = true;
        this.treatmentBMPImageService.createTreatmentBMPImageByTreatmentBMP(this.treatmentBMPID, event.file, event.caption).subscribe({
            next: () => {
                this.reloadTrigger$.next();
                this.alertService.pushAlert(new Alert("Image added successfully.", AlertContext.Success));
                this.isLoadingSubmit = false;
                this.uploaded.emit();
            },
            error: () => (this.isLoadingSubmit = false),
        });
    }

    public onImageDeleted(item: ImageEditorItem): void {
        this.isLoadingSubmit = true;
        this.treatmentBMPImageService.deleteTreatmentBMPImageByTreatmentBMP(this.treatmentBMPID, item.PrimaryKey!).subscribe({
            next: () => {
                this.reloadTrigger$.next();
                this.alertService.pushAlert(new Alert("Image deleted successfully.", AlertContext.Success));
                this.isLoadingSubmit = false;
                this.deleted.emit();
            },
            error: () => (this.isLoadingSubmit = false),
        });
    }

    public onSaveClicked(images: ImageEditorItem[]): void {
        this.isLoadingSubmit = true;
        const updates = images.map((img) => ({
            TreatmentBMPImageID: img.PrimaryKey!,
            Caption: img.Caption || "",
        }));
        this.treatmentBMPImageService.updateTreatmentBMPImageByTreatmentBMP(this.treatmentBMPID, updates).subscribe({
            next: () => {
                this.reloadTrigger$.next();
                this.captionControlForm.markAsPristine();
                this.alertService.pushAlert(new Alert("Images updated successfully.", AlertContext.Success));
                this.isLoadingSubmit = false;
                this.saved.emit();
            },
            error: () => (this.isLoadingSubmit = false),
        });
    }

    public onCancelClicked(): void {
        this.cancelled.emit();
    }

    public onCaptionEditRequested(item: ImageEditorItem): void {
        if (!item.PrimaryKey || !item.FileResourceGUID) return;
        // Pre-load the photo blob into an object URL so the modal preview matches what's on the page.
        this.fileResourceService.displayResourceFileResource(item.FileResourceGUID, "body", false, { httpHeaderAccept: undefined }).subscribe({
            next: (blob: Blob) => {
                const previewUrl = URL.createObjectURL(blob);
                this.openCaptionModal(item, previewUrl);
            },
            error: () => {
                // Open the modal anyway without a preview — the user can still edit the caption.
                // Surface a soft warning so the missing thumbnail isn't mysterious.
                this.alertService.pushAlert(new Alert("Could not load the photo preview; caption editor is still available.", AlertContext.Warning));
                this.openCaptionModal(item, null);
            },
        });
    }

    private openCaptionModal(item: ImageEditorItem, previewUrl: string | null): void {
        this.dialogService
            .open(EditPhotoCaptionModalComponent, {
                data: {
                    currentCaption: item.Caption ?? "",
                    previewUrl,
                    title: "Edit Photo Caption",
                } as EditPhotoCaptionModalContext,
            })
            .afterClosed$.subscribe((newCaption: string | null | undefined) => {
                if (previewUrl) URL.revokeObjectURL(previewUrl);
                if (newCaption == null) return;
                if ((newCaption ?? "") === (item.Caption ?? "")) return;
                this.isLoadingSubmit = true;
                this.treatmentBMPImageService
                    .updateTreatmentBMPImageByTreatmentBMP(this.treatmentBMPID, [
                        { TreatmentBMPImageID: item.PrimaryKey!, Caption: newCaption },
                    ])
                    .subscribe({
                        next: () => {
                            this.isLoadingSubmit = false;
                            this.alertService.pushAlert(new Alert("Caption saved.", AlertContext.Success));
                            this.reloadTrigger$.next();
                            this.captionUpdated.emit();
                        },
                        error: () => {
                            this.isLoadingSubmit = false;
                            this.alertService.pushAlert(new Alert("Failed to save caption.", AlertContext.Danger));
                        },
                    });
            });
    }
}
