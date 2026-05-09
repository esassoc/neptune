
import { Component, EventEmitter, Input, OnChanges, OnDestroy, OnInit, Output, SimpleChanges, inject, signal } from "@angular/core";
import { ReactiveFormsModule, FormBuilder, FormGroup, FormArray, FormControl } from "@angular/forms";
import { FileResourceService } from "src/app/shared/generated/api/file-resource.service";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";

@Component({
    selector: "image-editor",
    templateUrl: "./image-editor.component.html",
    styleUrls: ["./image-editor.component.scss"],
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent],
})
export class ImageEditorComponent implements OnInit, OnChanges, OnDestroy {
    private fileResourceService = inject(FileResourceService);
    private confirmService = inject(ConfirmService);

    @Input() images: ImageEditorItem[] = [];
    @Input() isLoadingSubmit: boolean = false;
    @Input() captionControlForm: FormGroup;
    /** When true, captions are read-only inline; clicking the pencil opens a modal owned by the host
     * that persists immediately. The page-level Save/Cancel footer is suppressed in this mode since
     * uploads/deletes already auto-save and captions are no longer batched. */
    @Input() useCaptionModal: boolean = false;

    @Output() newImageAdded = new EventEmitter<{ file: File; caption: string }>();
    @Output() imageDeleted = new EventEmitter<ImageEditorItem>();
    @Output() saveClicked = new EventEmitter<ImageEditorItem[]>();
    @Output() cancelClicked = new EventEmitter<void>();
    /** Emitted when the user clicks the pencil on a photo in modal mode. Host opens its modal
     * and persists via its own service. */
    @Output() captionEditRequested = new EventEmitter<ImageEditorItem>();

    // reactive form: newPhoto and existing photos array
    public newPhotoFormGroup: FormGroup<{ newPhoto: FormControl<File | null>; newPhotoCaption: FormControl<string> }> = new FormGroup<{ newPhoto; newPhotoCaption }>({
        newPhoto: new FormControl<File | null>(null),
        newPhotoCaption: new FormControl<string>(""),
    });

    public FormFieldType = FormFieldType;

    public captionControls: { [guid: string]: FormControl<string> } = {};

    public newPhotoPreviewUrl: string | null = null;
    public imagePreviewUrls = signal<{ [guid: string]: string }>({});

    ngOnInit(): void {
        this.newPhotoFormGroup.get("newPhoto")?.valueChanges.subscribe((file: File | null) => {
            this.handleNewFileChange(file);
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.images && changes.images.currentValue !== changes.images.previousValue) {
            this.imagePreviewUrls.set({});
            this.captionControls = {};
            // Skip wiring the per-photo caption FormControls when the host is using the modal pattern;
            // captions persist via the modal's own save, not via this component's batch Save.
            if (!this.useCaptionModal) {
                this.captionControlForm.controls = {};
            }

            for (const img of this.images) {
                this.loadImagePreview(img);
                if (!this.useCaptionModal) {
                    this.captionControls[img.FileResourceGUID] = new FormControl<string>(img.Caption || "");
                    this.captionControlForm.addControl(img.FileResourceGUID, this.captionControls[img.FileResourceGUID]);
                }
            }
            if (!this.useCaptionModal) {
                this.captionControlForm.markAsPristine();
            }
        }
    }

    public requestEditCaption(image: ImageEditorItem): void {
        this.captionEditRequested.emit(image);
    }

    public addNewImage(): void {
        const file = this.newPhotoFormGroup.get("newPhoto")?.value;
        const caption = this.newPhotoFormGroup.get("newPhotoCaption")?.value || "";
        if (file) {
            this.newImageAdded.emit({ file, caption });
            this.clearNewPreview();
        }
    }

    public deleteImage(image: ImageEditorItem): void {
        this.confirmService
            .confirm({
                title: "Delete Image",
                message: `Are you sure you want to delete this image?`,
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
                buttonClassYes: "btn-danger",
            })
            .then((confirmed) => {
                if (confirmed) {
                    this.imageDeleted.emit(image);
                }
            })
            .catch(() => {
                // ignore
            });
    }

    public save(): void {
        let updatedCaptions: ImageEditorItem[] = [];
        for (const guid in this.captionControls) {
            if (this.captionControls[guid].dirty) {
                const image = this.images.find((img) => img.FileResourceGUID === guid);
                image.Caption = this.captionControls[guid].value;
                updatedCaptions.push(image);
            }
        }

        this.saveClicked.emit(updatedCaptions);
    }

    public cancel(): void {
        this.cancelClicked.emit();
    }

    // invoked when file form control changes
    private handleNewFileChange(file: File | null): void {
        if (!file) {
            return;
        }
        this.newPhotoPreviewUrl = URL.createObjectURL(file);
    }

    public clearNewPreview(): void {
        if (this.newPhotoPreviewUrl) {
            URL.revokeObjectURL(this.newPhotoPreviewUrl);
        }

        this.newPhotoPreviewUrl = null;
        this.newPhotoFormGroup.patchValue({ newPhotoCaption: "" });
    }

    // load/ cache an image preview for an existing image item
    public loadImagePreview(image: ImageEditorItem | undefined): void {
        const guid = image?.FileResourceGUID;
        if (!guid || this.imagePreviewUrls()[guid]) {
            return;
        }

        this.fileResourceService.displayResourceFileResource(guid, "body", false, { httpHeaderAccept: undefined }).subscribe((blob: Blob) => {
            const url = URL.createObjectURL(blob);
            this.imagePreviewUrls.update((urls) => ({ ...urls, [guid]: url }));
        });
    }

    ngOnDestroy(): void {
        // revoke any cached URLs
        if (this.newPhotoPreviewUrl) {
            URL.revokeObjectURL(this.newPhotoPreviewUrl);
        }
        for (const k of Object.keys(this.imagePreviewUrls())) {
            try {
                URL.revokeObjectURL(this.imagePreviewUrls()[k]);
            } catch {
                /* ignore */
            }
        }
    }
}

export interface ImageEditorItem {
    PrimaryKey?: number | null;
    FileResourceGUID?: string | null;
    Caption?: string | null;
}
