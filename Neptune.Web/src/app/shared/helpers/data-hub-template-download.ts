import { HttpClient } from "@angular/common/http";
import { WritableSignal } from "@angular/core";
import { environment } from "src/environments/environment";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";

/**
 * NPT-998: shared helper for the Data Hub upload pages' "Download Template" button.
 * Hits the consolidated GET /data-hub/upload-templates/{templateKey} endpoint, streams
 * the response as a blob, and triggers a browser download using the server-supplied
 * Content-Disposition filename (falling back to `fallbackFileName` when the header is
 * absent). The generated API client types the endpoint as JSON because Swashbuckle
 * doesn't emit the response media type into the OpenAPI spec, so each page calls
 * HttpClient directly via this helper.
 *
 * On error, decodes the blob response body to surface the actual server message
 * (ProblemDetails JSON or plain text). Without this the generic interceptor logs
 * "[object Blob]" and the user just sees a "Failed to download" alert with no clue.
 */
export function downloadDataHubTemplate(
    httpClient: HttpClient,
    alertService: AlertService,
    isDownloading: WritableSignal<boolean>,
    templateKey: string,
    fallbackFileName: string,
    templateLabel: string,
): void {
    isDownloading.set(true);
    httpClient
        .get(`${environment.mainAppApiUrl}/data-hub/upload-templates/${templateKey}`, { responseType: "blob", observe: "response" })
        .subscribe({
            next: (response) => {
                isDownloading.set(false);
                const fileName = parseFilenameFromContentDisposition(response.headers.get("content-disposition")) ?? fallbackFileName;
                const url = window.URL.createObjectURL(response.body!);
                const a = document.createElement("a");
                a.href = url;
                a.download = fileName;
                a.click();
                window.URL.revokeObjectURL(url);
            },
            error: async (err) => {
                isDownloading.set(false);
                let serverMessage = `Failed to download the ${templateLabel} upload template.`;
                try {
                    if (err?.error instanceof Blob) {
                        const text = await err.error.text();
                        try {
                            const parsed = JSON.parse(text);
                            serverMessage = parsed.detail ?? parsed.title ?? parsed.message ?? text;
                        } catch {
                            serverMessage = text || serverMessage;
                        }
                    }
                } catch {
                    // Fall back to the generic message.
                }
                alertService.pushAlert(new Alert(serverMessage, AlertContext.Danger, true));
            },
        });
}

function parseFilenameFromContentDisposition(header: string | null): string | null {
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
