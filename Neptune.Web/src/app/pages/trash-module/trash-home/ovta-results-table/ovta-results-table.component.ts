import { Component, computed, input, output, signal } from "@angular/core";
import { RouterLink } from "@angular/router";
import { BtnGroupRadioInputComponent, IBtnGroupRadioInputOption } from "src/app/shared/components/inputs/btn-group-radio-input/btn-group-radio-input.component";
import { NoteComponent } from "src/app/shared/components/note/note.component";
import { OVTA_AREA_STYLE_BY_SCORE } from "src/app/shared/constants/map-styles";
import { OVTAResultsAreaDataIssueDto } from "src/app/shared/generated/model/ovta-results-area-data-issue-dto";
import { OVTAResultsLandUseRowDto } from "src/app/shared/generated/model/ovta-results-land-use-row-dto";

// NPT-1126: OVTA-Based Results table, mockup option 6a ("Paired columns, verbose headings").

type Track = 1 | 2;
type ResultsView = "percent" | "acres" | "assessments";
type Category = "plu" | "alu";
type Score = "A" | "B" | "C" | "D";
type AcresField = keyof Omit<OVTAResultsLandUseRowDto, "IsPriorityLandUse" | "LandUseTypeName" | "SortOrder">;

export interface OVTAResultsTableRow {
    isCategory: boolean;
    category: Category;
    name: string;
    acres: Record<AcresField, number>;
}

interface ValueColumn {
    field: AcresField;
    label: string;
    groupStart: boolean;
    muted: boolean;
}

const ACRES_FIELDS: AcresField[] = [
    "TotalMappedAcres",
    "BaselineAcresA",
    "BaselineAcresB",
    "BaselineAcresC",
    "BaselineAcresD",
    "ProgressAcresA",
    "ProgressAcresB",
    "ProgressAcresC",
    "ProgressAcresD",
    "NotYetAssessedAcres",
    "AssessedOnceAcres",
    "AssessedTwiceAcres",
    "AssessedThreeOrMoreAcres",
];

const CATEGORY_NAMES: Record<Category, string> = { plu: "Priority Land Use", alu: "Alternate Land Use" };

const ACRES_FORMAT = new Intl.NumberFormat("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export function formatAcres(acres: number | null | undefined): string {
    if (acres == null || Math.abs(acres) < 0.005) {
        return "0.00";
    }
    return ACRES_FORMAT.format(acres);
}

// Percent of the row's own Total Mapped Land Use, so a category row is never an average of its children.
export function formatPercent(acres: number | null | undefined, totalAcres: number | null | undefined): string {
    if (!acres || !totalAcres || acres <= 0 || totalAcres <= 0) {
        return "0.0%";
    }
    const percent = (acres / totalAcres) * 100;
    return percent < 0.1 ? "< 0.1%" : `${percent.toFixed(1)}%`;
}

@Component({
    selector: "ovta-results-table",
    imports: [BtnGroupRadioInputComponent, NoteComponent, RouterLink],
    templateUrl: "./ovta-results-table.component.html",
    styleUrl: "./ovta-results-table.component.scss",
})
export class OvtaResultsTableComponent {
    readonly rows = input<OVTAResultsLandUseRowDto[]>([]);
    readonly areasWithProgressButNoBaseline = input<OVTAResultsAreaDataIssueDto[]>([]);
    readonly hasLandUseBlocks = input(true);
    readonly hasBaselineScores = input(true);
    readonly scoreDefinitionsClicked = output<void>();

    public readonly scores: Score[] = ["A", "B", "C", "D"];
    public readonly scoreColors: Record<Score, string> = {
        A: OVTA_AREA_STYLE_BY_SCORE["A"].color,
        B: OVTA_AREA_STYLE_BY_SCORE["B"].color,
        C: OVTA_AREA_STYLE_BY_SCORE["C"].color,
        D: OVTA_AREA_STYLE_BY_SCORE["D"].color,
    };

    public readonly trackOptions: IBtnGroupRadioInputOption[] = [
        { label: "Track 1 — Baseline Only", value: 1 },
        { label: "Track 2 — Baseline + Progress", value: 2 },
    ];
    public readonly viewOptions: IBtnGroupRadioInputOption[] = [
        { label: "Percent of Land Use by Score", value: "percent" },
        { label: "Acres by Score", value: "acres" },
        { label: "Acres of Land Use Type by # of Assessments", value: "assessments" },
    ];

    public readonly track = signal<Track>(1);
    public readonly view = signal<ResultsView>("percent");
    public readonly expanded = signal<Record<Category, boolean>>({ plu: true, alu: true });

    public readonly defaultTrackLabel = this.trackOptions[0].label;
    public readonly defaultViewLabel = this.viewOptions[0].label;

    public readonly isScoreView = computed(() => this.view() !== "assessments");
    public readonly showProgress = computed(() => this.track() === 2);

    public readonly scoreSuperHeader = computed(() => (this.view() === "percent" ? "Percent of Land Use by OVTA Score" : "Area by OVTA Score (acres)"));

    public readonly scoreSubHeaders = computed(() =>
        this.view() === "percent"
            ? { baseline: "% of Land Use with Baseline Score", progress: "% of Land Use with Progress Score" }
            : { baseline: "Area with Baseline Score (ac)", progress: "Area with Progress Score (ac)" }
    );

    // The value columns after Land Use and Total Mapped Land Use, in display order. Track 1 has no Progress
    // columns at all (not hidden ones); the assessments view ignores the Track switch.
    public readonly valueColumns = computed<ValueColumn[]>(() => {
        if (!this.isScoreView()) {
            return [
                { field: "NotYetAssessedAcres", label: "Area Not Yet Assessed (ac)", groupStart: true, muted: true },
                { field: "AssessedOnceAcres", label: "Area Assessed Once (ac)", groupStart: false, muted: false },
                { field: "AssessedTwiceAcres", label: "Area Assessed Twice (ac)", groupStart: false, muted: false },
                { field: "AssessedThreeOrMoreAcres", label: "Area Assessed 3 or More Times (ac)", groupStart: false, muted: false },
            ];
        }
        const subHeaders = this.scoreSubHeaders();
        return this.scores.flatMap((score) => {
            const columns: ValueColumn[] = [{ field: `BaselineAcres${score}` as AcresField, label: subHeaders.baseline, groupStart: true, muted: false }];
            if (this.showProgress()) {
                columns.push({ field: `ProgressAcres${score}` as AcresField, label: subHeaders.progress, groupStart: false, muted: false });
            }
            return columns;
        });
    });

    public readonly scoreGroupColspan = computed(() => (this.showProgress() ? 2 : 1));

    public readonly tableRows = computed<OVTAResultsTableRow[]>(() => {
        const rows = [...(this.rows() ?? [])].sort((a, b) => (a.SortOrder ?? 0) - (b.SortOrder ?? 0));
        const expanded = this.expanded();
        return (["plu", "alu"] as Category[]).flatMap((category) => {
            const children: OVTAResultsTableRow[] = rows
                .filter((x) => !!x.IsPriorityLandUse === (category === "plu"))
                .map((x) => ({ isCategory: false, category, name: x.LandUseTypeName ?? "", acres: this.toAcres(x) }));
            const categoryRow: OVTAResultsTableRow = { isCategory: true, category, name: CATEGORY_NAMES[category], acres: this.sumAcres(children) };
            return expanded[category] ? [categoryRow, ...children] : [categoryRow];
        });
    });

    public onTrackChange(value: Track): void {
        this.track.set(value);
    }

    public onViewChange(value: ResultsView): void {
        this.view.set(value);
    }

    public toggleCategory(category: Category): void {
        this.expanded.update((x) => ({ ...x, [category]: !x[category] }));
    }

    public setAllExpanded(isExpanded: boolean): void {
        this.expanded.set({ plu: isExpanded, alu: isExpanded });
    }

    public formatValue(row: OVTAResultsTableRow, field: AcresField): string {
        return this.view() === "percent" ? formatPercent(row.acres[field], row.acres.TotalMappedAcres) : formatAcres(row.acres[field]);
    }

    public formatAcres(acres: number): string {
        return formatAcres(acres);
    }

    private toAcres(row: OVTAResultsLandUseRowDto): Record<AcresField, number> {
        return Object.fromEntries(ACRES_FIELDS.map((field) => [field, row[field] ?? 0])) as Record<AcresField, number>;
    }

    private sumAcres(rows: OVTAResultsTableRow[]): Record<AcresField, number> {
        return Object.fromEntries(ACRES_FIELDS.map((field) => [field, rows.reduce((sum, x) => sum + x.acres[field], 0)])) as Record<AcresField, number>;
    }
}
