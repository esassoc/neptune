import { Pipe, PipeTransform } from "@angular/core";
import { DomSanitizer, SafeResourceUrl } from "@angular/platform-browser";

@Pipe({
    name: "safeResourceUrl",
})
export class SafeResourceUrlPipe implements PipeTransform {
    constructor(private sanitizer: DomSanitizer) {}

    transform(value: string | null): SafeResourceUrl {
        return this.sanitizer.bypassSecurityTrustResourceUrl(value || "");
    }
}
