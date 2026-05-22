/**
 * Escape user-controlled or server-echoed text before rendering through
 * Angular's [innerHTML]. The shared AlertDisplayComponent renders alert
 * messages as innerHTML, so any string that may contain a `<`, `>`, `&`,
 * `"`, or `'` from untrusted sources must pass through this helper before
 * being pushed into an Alert.
 */
export function escapeHtml(s: string): string {
    return s
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");
}
