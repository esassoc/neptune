import { HttpResponse } from "@angular/common/http";

// NPT-943: save an HttpClient blob response (responseType:"blob", observe:"response") as a file,
// using the server-supplied Content-Disposition filename when present. Shared by the GDB/zip
// download buttons (WQMP Index, Data Hub WQMP download, View All BMPs).
export function saveBlobResponse(response: HttpResponse<Blob>, fallbackFileName: string): void {
    const fileName = parseFilenameFromContentDisposition(response.headers.get("content-disposition")) ?? fallbackFileName;
    const url = window.URL.createObjectURL(response.body!);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    a.click();
    window.URL.revokeObjectURL(url);
}

export function parseFilenameFromContentDisposition(header: string | null): string | null {
    if (!header) return null;
    const utf8Match = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(header);
    if (utf8Match) {
        try {
            return decodeURIComponent(utf8Match[1].trim());
        } catch {
            // Fall through to the plain filename match.
        }
    }
    const plainMatch = /filename\s*=\s*"?([^";]+)"?/i.exec(header);
    return plainMatch ? plainMatch[1].trim() : null;
}
