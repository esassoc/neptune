import { HttpErrorResponse } from "@angular/common/http";

// NPT-1135: Data Hub / Delineation uploads and downloads return 403 for a jurisdiction the caller isn't assigned to.
// The pickers only list the caller's jurisdictions, so this is reached only when an assignment changes mid-session.
export const JURISDICTION_FORBIDDEN_MESSAGE = "You aren't assigned to the selected jurisdiction. Refresh the page to see the jurisdictions you can use.";
export const STAGED_JURISDICTION_FORBIDDEN_MESSAGE = "The staged upload includes a jurisdiction you aren't assigned to. Discard it and upload again.";

export function jurisdictionErrorMessage(error: unknown, fallback: string, forbiddenMessage: string = JURISDICTION_FORBIDDEN_MESSAGE): string {
    return error instanceof HttpErrorResponse && error.status === 403 ? forbiddenMessage : fallback;
}
