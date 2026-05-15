import { environment } from "src/environments/environment";

/**
 * Builds the SPA API URL for downloading a FileResource by GUID. The `/file-resources/{guid}`
 * endpoint is `[AllowAnonymous]`, so a plain `<a [href]>` works and no Bearer token plumbing
 * is needed. Centralized here so callers don't duplicate the path (which historically led to
 * wrong routes — e.g. the legacy MVC `FileResource/DisplayResource/{guid}` returning 401 from
 * the API origin — NPT-995 Round 6).
 */
export function fileResourceUrl(guid: string): string {
    return `${environment.mainAppApiUrl}/file-resources/${guid}`;
}
