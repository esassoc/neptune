import { Component, inject, Input, OnChanges, SimpleChanges, signal } from "@angular/core";
import { DialogService } from "@ngneat/dialog";
import { FileResourceService } from "src/app/shared/generated/api/file-resource.service";
import { fileResourceUrl } from "src/app/shared/helpers/file-resource-url";
import { PhotoLightboxComponent } from "src/app/shared/components/photo-lightbox/photo-lightbox.component";

import { LoadingDirective } from "../../directives/loading.directive";

@Component({
    selector: "image-carousel",
    templateUrl: "./image-carousel.component.html",
    styleUrls: ["./image-carousel.component.scss"],
    standalone: true,
    imports: [LoadingDirective],
})
export class ImageCarouselComponent implements OnChanges {
    private fileResourceService = inject(FileResourceService);
    private dialogService = inject(DialogService);

    @Input() images: ImageCarouselItem[] = [];
    @Input() noImagesMessage: string = "No images available.";
    public currentImageIndex = 0;
    public imagePreviewUrls = signal<{ [guid: string]: string }>({});

    ngOnChanges(changes: SimpleChanges): void {
        if (changes.images && this.images && this.images.length > 0) {
            this.setImageIndex(0);
        }
    }

    setImageIndex(idx: number): void {
        if (this.images && this.images.length > 0) {
            this.currentImageIndex = (idx + this.images.length) % this.images.length;
            this.loadImagePreview({ FileResourceGUID: this.images[this.currentImageIndex].FileResourceGUID });
        }
    }

    public openLightbox(): void {
        const photos = this.images.filter((image) => image?.FileResourceGUID);
        if (photos.length === 0) return;
        const startIndex = Math.max(photos.indexOf(this.images[this.currentImageIndex]), 0);
        PhotoLightboxComponent.open(this.dialogService, {
            photos: photos.map((image) => ({ url: fileResourceUrl(image.FileResourceGUID), caption: image.Caption })),
            startIndex,
        });
    }

    // Call this to load the image preview for a file resource
    public loadImagePreview(image: ImageCarouselItem): void {
        const guid = image?.FileResourceGUID;
        if (!guid || this.imagePreviewUrls()[guid]) {
            return;
        }

        this.fileResourceService.displayResourceFileResource(guid, "body", false, { httpHeaderAccept: undefined }).subscribe((blob: Blob) => {
            const url = URL.createObjectURL(blob);
            this.imagePreviewUrls.update((urls) => ({ ...urls, [guid]: url }));
        });
    }
}

export interface ImageCarouselItem {
    FileResourceGUID?: string;
    Caption?: string | null;
}
