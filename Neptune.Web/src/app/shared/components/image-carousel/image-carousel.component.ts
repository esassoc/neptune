import { Component, inject, Input, OnChanges, SimpleChanges, signal } from "@angular/core";
import { FileResourceService } from "src/app/shared/generated/api/file-resource.service";

import { environment } from "src/environments/environment";
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
