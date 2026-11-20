import { Component, computed, inject, signal } from "@angular/core";
import { DialogRef, DialogService } from "@ngneat/dialog";

import { LoadingDirective } from "src/app/shared/directives/loading.directive";

export interface PhotoLightboxItem {
    /** Full-resolution image URL (e.g. `fileResourceUrl(guid)`), not a scaled-down preview. */
    url: string;
    /** Shown under the photo when present. */
    caption?: string | null;
}

export interface PhotoLightboxContext {
    photos: PhotoLightboxItem[];
    /** Index into `photos` of the photo to open on. */
    startIndex: number;
}

/**
 * Shows one photo of a set as large as the viewport allows, with its caption, an "N of M"
 * indicator and prev/next stepping (buttons and left/right arrow keys). The X button, Esc and a
 * backdrop click close it (ngneat defaults). Open it with `PhotoLightboxComponent.open(...)`.
 */
@Component({
    selector: "photo-lightbox",
    standalone: true,
    imports: [LoadingDirective],
    templateUrl: "./photo-lightbox.component.html",
    styleUrl: "./photo-lightbox.component.scss",
    host: { "(document:keydown)": "onKeydown($event)" },
})
export class PhotoLightboxComponent {
    public ref: DialogRef<PhotoLightboxContext> = inject(DialogRef);

    public photos: PhotoLightboxItem[] = this.ref.data?.photos ?? [];
    public currentIndex = signal(this.clampIndex(this.ref.data?.startIndex ?? 0));
    public current = computed(() => this.photos[this.currentIndex()]);
    public isLoading = signal(true);

    public static open(dialogService: DialogService, context: PhotoLightboxContext) {
        return dialogService.open(PhotoLightboxComponent, {
            data: context,
            width: "auto",
            maxWidth: "100%",
            maxHeight: "100%",
            windowClass: "photo-lightbox-dialog",
        });
    }

    public next(): void {
        this.goTo(this.currentIndex() + 1);
    }

    public previous(): void {
        this.goTo(this.currentIndex() - 1);
    }

    public onKeydown(event: KeyboardEvent): void {
        if (this.photos.length < 2) return;
        if (event.key === "ArrowRight") {
            event.preventDefault();
            this.next();
        } else if (event.key === "ArrowLeft") {
            event.preventDefault();
            this.previous();
        }
    }

    public onImageSettled(): void {
        this.isLoading.set(false);
    }

    private goTo(index: number): void {
        if (this.photos.length === 0) return;
        const wrapped = (index + this.photos.length) % this.photos.length;
        if (wrapped === this.currentIndex()) return;
        this.isLoading.set(true);
        this.currentIndex.set(wrapped);
    }

    private clampIndex(index: number): number {
        if (this.photos.length === 0) return 0;
        return Math.min(Math.max(index, 0), this.photos.length - 1);
    }
}
