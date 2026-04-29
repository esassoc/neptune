import { Component, Input } from "@angular/core";
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
}

export interface ReviewSummaryRow {
    label: string;
    displayValue: string;
    state: ExtractedField["state"];
    origin: "ai" | "user" | "blank";
}

@Component({
    selector: "review-summary",
    standalone: true,
    imports: [],
    templateUrl: "./review-summary.component.html",
    styleUrl: "./review-summary.component.scss",
})
export class ReviewSummaryComponent {
    @Input() locationFields: ExtractedField[] = [];
    @Input() basicsFields: ExtractedField[] = [];
    @Input() parcelFields: ExtractedField[] = [];
    @Input() bmpGroups: { bmpIndex: number; displayName: string | null; fields: ExtractedField[]; isRejected: boolean }[] = [];
    @Input() lookupOptionsByKey: Record<string, SelectDropdownOption[]> = {};

    get groups(): ReviewSummaryGroup[] {
        const groups: ReviewSummaryGroup[] = [];

        if (this.locationFields.length || this.parcelFields.length) {
            const rows: ReviewSummaryRow[] = [
                ...this.locationFields.map((f) => this.toRow(f)),
                ...this.parcelFields.map((f) => this.toRow(f)),
            ];
            groups.push({ title: "Location", rows });
        }

        if (this.basicsFields.length) {
            groups.push({ title: "Basics", rows: this.basicsFields.map((f) => this.toRow(f)) });
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
                    state,
                    origin: "ai",
                };
            });
            groups.push({ title: "BMPs", rows });
        }

        return groups;
    }

    private toRow(f: ExtractedField): ReviewSummaryRow {
        return {
            label: f.label,
            displayValue: this.displayFor(f),
            state: f.state,
            origin: f.isUserEntered ? "user" : f.value ? "ai" : "blank",
        };
    }

    private displayFor(f: ExtractedField): string {
        if (f.state === "rejected") return "(rejected)";
        const raw = f.acceptedValue ?? f.value;
        if (raw == null || raw === "") return "(not set)";
        // Lookup fields hold an ID — show the label for readability.
        if (f.fieldType === FormFieldType.Select) {
            const options = f.selectOptions ?? this.lookupOptionsByKey[f.key] ?? [];
            const match = options.find((o) => String(o.Value) === String(raw));
            if (match) return match.Label;
        }
        return String(raw);
    }
}
