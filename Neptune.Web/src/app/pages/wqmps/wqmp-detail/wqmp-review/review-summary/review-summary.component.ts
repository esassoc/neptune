import { Component, inject, Input } from "@angular/core";
import { DatePipe, NgTemplateOutlet } from "@angular/common";
import { ExtractedField } from "src/app/pages/wqmps/wqmp-detail/wqmp-review/wqmp-review.component";
import { FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";

/**
 * NPT-1020: read-only summary rendered as Step 4 of the WQMP AI review workflow.
 *
 * Iterates the host page's flat ExtractedField list (Steps 1+2 root fields, parcels,
 * and the per-attribute BMP rows) plus the BMP card-level rejection set, and renders
 * one row per item with a status pill (AI-pending / Accepted / Edited / Rejected).
 * Pure presentational — no API calls, no state mutation. Approve All lives in the
 * sidebar of the host page.
 */
export interface ReviewSummaryGroup {
    title: string;
    rows: ReviewSummaryRow[];
    // NPT-1054: when true, the group renders as a native <details> so the user can toggle
    // visibility. Used for the Source Control BMP categories — there are ~36 rows total
    // and collapsing keeps the summary scannable.
    collapsible?: boolean;
    initiallyOpen?: boolean;
}

export interface ReviewSummaryRow {
    label: string;
    displayValue: string;
    // NPT-1051: live value currently saved on the WQMP record. Drives the third "WQMP Record"
    // column and the "Accepted" status pill for pending fields whose value is already on the
    // record (saved in this or a prior wizard session).
    wqmpRecordValue: string | null;
    state: ExtractedField["state"];
    origin: "ai" | "user" | "blank";
}

@Component({
    selector: "review-summary",
    standalone: true,
    imports: [NgTemplateOutlet],
    providers: [DatePipe],
    templateUrl: "./review-summary.component.html",
    styleUrl: "./review-summary.component.scss",
})
export class ReviewSummaryComponent {
    private datePipe = inject(DatePipe);

    @Input() locationFields: ExtractedField[] = [];
    @Input() basicsFields: ExtractedField[] = [];
    @Input() parcelFields: ExtractedField[] = [];
    @Input() bmpGroups: { bmpIndex: number; displayName: string | null; fields: ExtractedField[]; isRejected: boolean }[] = [];
    // NPT-1054: flat list of Source Control BMP rows. origin mirrors Step 4's row classifier
    // (getSourceControlRowOrigin) so the AI pill only shows when the displayed value actually
    // came from the AI — not when the user accepted a value that was already on the record.
    @Input() sourceControlRows: { categoryName: string; attributeName: string; isPresent: boolean | null; note: string; origin: "ai" | "record" | "edited" | "blank"; wqmpRecordValue: string | null }[] = [];
    @Input() lookupOptionsByKey: Record<string, SelectDropdownOption[]> = {};
    // NPT-1051: parent supplies a key→liveWqmpValue resolver so the summary can render the
    // third "WQMP Record" column without duplicating the key→DTO field mapping in the child.
    @Input() getWqmpFieldValue: (field: ExtractedField) => string | null = () => null;

    get groups(): ReviewSummaryGroup[] {
        const groups: ReviewSummaryGroup[] = [];

        // NPT-984: all groups render as collapsible <details open> panels so the user can
        // toggle individual sections without losing position on the page. Default-open keeps
        // the prior all-visible-on-load behavior; sections can be collapsed individually to
        // reduce scroll, which is especially useful once Source Control's ~36 rows show up.
        if (this.locationFields.length || this.parcelFields.length) {
            const rows: ReviewSummaryRow[] = [
                ...this.locationFields.map((f) => this.toRow(f)),
                ...this.parcelFields.map((f) => this.toRow(f)),
            ];
            groups.push({ title: "Location", rows, collapsible: true, initiallyOpen: true });
        }

        if (this.basicsFields.length) {
            groups.push({ title: "Basics", rows: this.basicsFields.map((f) => this.toRow(f)), collapsible: true, initiallyOpen: true });
        }

        if (this.sourceControlRows.length) {
            // NPT-1054: group by category and emit one collapsible section per category so
            // the summary stays scannable even with ~36 total SC rows. Show ALL attributes,
            // including unset ones (rendered as "(not set)" with a blank-source pill).
            const byCategory = new Map<string, ReviewSummaryRow[]>();
            for (const r of this.sourceControlRows) {
                if (!byCategory.has(r.categoryName)) byCategory.set(r.categoryName, []);
                const isUnset = r.isPresent == null && !r.note;
                const presentDisplay = r.isPresent === true ? "Yes" : r.isPresent === false ? "No" : "—";
                const display = isUnset
                    ? "(not set)"
                    : r.note ? `${presentDisplay} — ${r.note}` : presentDisplay;
                // Map Step 4's four-state origin into the summary's three-state origin so the
                // AI pill (which only fires on origin === "ai") matches Step 4's pill: "record"
                // and "edited" both fall back to "user" (no AI pill); "ai" stays; "blank"
                // collapses to "blank" only when the row is genuinely empty.
                const summaryOrigin: ReviewSummaryRow["origin"] = isUnset
                    ? "blank"
                    : r.origin === "ai"
                        ? "ai"
                        : "user";
                byCategory.get(r.categoryName)!.push({
                    label: r.attributeName,
                    displayValue: display,
                    wqmpRecordValue: r.wqmpRecordValue,
                    state: isUnset ? "pending" : "accepted",
                    origin: summaryOrigin,
                });
            }
            for (const [category, rows] of byCategory.entries()) {
                // NPT-984: SC categories were initially closed by default to keep the long
                // checklist scannable. Per Kathleen's feedback, default-open all groups so
                // the user lands on a fully-visible review; collapsing is opt-in per group.
                groups.push({ title: category, rows, collapsible: true, initiallyOpen: true });
            }
        }

        if (this.bmpGroups.length) {
            // Each BMP card collapses to one summary row. State = "rejected" if the card
            // was reject-toggled; otherwise inherits from the QuickBMPName field's state
            // (the most-meaningful per-BMP signal).
            const rows: ReviewSummaryRow[] = this.bmpGroups.map((g) => {
                const nameField = g.fields.find((f) => f.key.endsWith("-QuickBMPName"));
                const display = (nameField?.acceptedValue ?? nameField?.value) || `BMP #${g.bmpIndex + 1}`;
                const state: ExtractedField["state"] = g.isRejected
                    ? "rejected"
                    : (nameField?.state ?? "pending");
                return {
                    label: `BMP #${g.bmpIndex + 1}`,
                    displayValue: g.isRejected ? "(rejected)" : display,
                    // BMP rows don't map to a single WQMP-record column — leave blank.
                    wqmpRecordValue: null,
                    state,
                    origin: "ai",
                };
            });
            groups.push({ title: "BMPs", rows, collapsible: true, initiallyOpen: true });
        }

        return groups;
    }

    private toRow(f: ExtractedField): ReviewSummaryRow {
        return {
            label: f.label,
            displayValue: this.displayFor(f),
            wqmpRecordValue: this.getWqmpFieldValue(f),
            state: f.state,
            origin: f.isUserEntered ? "user" : f.value ? "ai" : "blank",
        };
    }

    private displayFor(f: ExtractedField): string {
        if (f.state === "rejected") return "(rejected)";
        const raw = f.acceptedValue ?? f.value;
        if (raw == null || raw === "") return "(not set)";
        // Date fields render in MM/dd/yyyy to match the WQMP Record column (which is formatted
        // through the same pipe in getWqmpFieldValueForRow); without this the Workflow column
        // showed raw ISO yyyy-mm-dd while the Record column showed MM/dd/yyyy on the same row.
        if (f.fieldType === FormFieldType.Date) {
            return this.datePipe.transform(raw, "MM/dd/yyyy", "+0000") ?? String(raw);
        }
        // Lookup fields hold an ID — show the label for readability.
        if (f.fieldType === FormFieldType.Select) {
            const options = f.selectOptions ?? this.lookupOptionsByKey[f.key] ?? [];
            const match = options.find((o) => String(o.Value) === String(raw));
            if (match) return match.Label;
        }
        return String(raw);
    }

    /**
     * NPT-1054 diff-dashboard Status. Status carries only the workflow-vs-record diff state;
     * provenance ("AI", "User", etc.) is conveyed elsewhere (AI pill in the Workflow Value
     * cell, the WQMP Record column itself). Three states:
     *  - "saved":   Workflow value matches WQMP record (both non-empty, equal strings).
     *  - "empty":   Both workflow and record are empty — nothing to save, nothing on record.
     *  - "pending": Workflow differs from record — the user has unsaved changes (including
     *               additions, edits, and rejections that haven't been persisted yet).
     */
    public getSaveStatus(row: ReviewSummaryRow): "saved" | "pending" | "empty" {
        const workflowEmpty = !row.displayValue || row.displayValue === "(not set)" || row.displayValue === "(rejected)";
        const recordEmpty = !row.wqmpRecordValue;
        if (workflowEmpty && recordEmpty) return "empty";
        if (!workflowEmpty && !recordEmpty && row.displayValue === row.wqmpRecordValue) return "saved";
        return "pending";
    }

    public isWorkflowRejected(row: ReviewSummaryRow): boolean {
        return row.displayValue === "(rejected)";
    }
}
