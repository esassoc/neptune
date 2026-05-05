// Single source of truth for the AI extraction PDF constraints.
// Anthropic's PDF document limits (100 pages, no encryption) plus our
// own size cap from NeptuneConfiguration.MaxExtractablePdfSizeBytes.
// Update here if any of the limits change; all UI surfaces will follow.

export const PDF_EXTRACTION_MAX_PAGES = 100;
export const PDF_EXTRACTION_MAX_SIZE_MB = 200;

/**
 * Verbose bullet list — used in the Create-from-PDF upload modal where
 * the user has the most space to read before committing to upload.
 */
export const PDF_EXTRACTION_LIMITS_BULLETS: string[] = [
    `${PDF_EXTRACTION_MAX_PAGES} pages or fewer. Longer PDFs are rejected by the AI service — split the document and run extraction on each section separately.`,
    `Up to ${PDF_EXTRACTION_MAX_SIZE_MB} MB. Larger files are rejected at upload.`,
    "No password protection or encryption. Re-export an unprotected copy first.",
    "Scanned / image-only PDFs are fine — the AI reads both text and visual content.",
];

/**
 * One-line summary — used in the wizard CTA and the re-run confirm modal,
 * where space is tight but the user is one click away from spending tokens.
 */
export const PDF_EXTRACTION_LIMITS_SUMMARY = `AI extraction supports PDFs ≤ ${PDF_EXTRACTION_MAX_PAGES} pages, ≤ ${PDF_EXTRACTION_MAX_SIZE_MB} MB, no password protection. Scanned PDFs are supported.`;

/**
 * Short hint appended to the alert message when Anthropic returns the
 * generic "Could not process PDF" error. The base error message comes
 * from the API; this fragment supplies the most likely reasons.
 */
export const PDF_EXTRACTION_FAILURE_HINT = `Common reasons: more than ${PDF_EXTRACTION_MAX_PAGES} pages, over ${PDF_EXTRACTION_MAX_SIZE_MB} MB, or password-protected.`;
