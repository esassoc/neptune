import { Component, computed, inject, Input, OnInit, signal, ViewChild, ViewContainerRef } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { HttpClient, HttpErrorResponse } from "@angular/common/http";
import { BehaviorSubject, EMPTY, finalize, forkJoin, map, Observable, switchMap, tap, shareReplay, catchError, of } from "rxjs";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { PdfJsViewerModule, PdfJsViewerComponent } from "ng2-pdfjs-viewer";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { ParcelService } from "src/app/shared/generated/api/parcel.service";
import { WaterQualityManagementPlanExtractionResultDto } from "src/app/shared/generated/model/water-quality-management-plan-extraction-result-dto";
import { WaterQualityManagementPlanDto } from "src/app/shared/generated/model/water-quality-management-plan-dto";
import { WaterQualityManagementPlanUpsertDto } from "src/app/shared/generated/model/water-quality-management-plan-upsert-dto";
import { QuickBMPUpsertDto } from "src/app/shared/generated/model/quick-bmp-upsert-dto";
import { QuickBMPMergeSkipDto } from "src/app/shared/generated/model/quick-bmp-merge-skip-dto";
import { WaterQualityManagementPlanSectionSaveResponseDto } from "src/app/shared/generated/model/water-quality-management-plan-section-save-response-dto";
import { EvidenceBoundingBox, FieldCardComponent, SourceNavigation } from "src/app/pages/wqmps/wqmp-detail/wqmp-review/field-card/field-card.component";
import { BmpReviewCardComponent } from "src/app/pages/wqmps/wqmp-detail/wqmp-review/bmp-review-card/bmp-review-card.component";
import { ReviewSummaryComponent } from "src/app/pages/wqmps/wqmp-detail/wqmp-review/review-summary/review-summary.component";
import { IDeactivateComponent } from "src/app/shared/guards/unsaved-changes.guard";
import { escapeHtml } from "src/app/shared/helpers/html-escape";
import { WaterQualityManagementPlanPrioritiesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-priority-enum";
import { WaterQualityManagementPlanDevelopmentTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-development-type-enum";
import { WaterQualityManagementPlanLandUsesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-land-use-enum";
import { WaterQualityManagementPlanPermitTermsAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-permit-term-enum";
import { HydromodificationAppliesTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/hydromodification-applies-type-enum";
import { TrashCaptureStatusTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/trash-capture-status-type-enum";
import { US_STATES } from "src/app/shared/constants/us-states";
import { environment } from "src/environments/environment";

export type ConfidenceLevel = "high" | "medium" | "low" | "none";
export type FieldOrigin = "ai" | "blank" | "user";

export interface ExtractedField {
    key: string;
    label: string;
    value: string | null;
    evidence: string | null;
    source: string | null;
    boundingBox: EvidenceBoundingBox | null;
    confidence: ConfidenceLevel;
    step: number;
    acceptedValue?: string | null;
    state: "pending" | "accepted" | "edited" | "rejected";
    fieldType?: FormFieldType;
    selectOptions?: SelectDropdownOption[];
    // ngx-mask pattern to apply in the form-field (e.g. "(000) 000-0000"). Optional.
    mask?: string;
    // True when the value came from the user (upload modal), not the AI. Drives the
    // "User-entered" origin pill and suppresses AI evidence styling.
    isUserEntered?: boolean;
}

@Component({
    selector: "wqmp-review",
    standalone: true,
    imports: [AlertDisplayComponent, FieldCardComponent, BmpReviewCardComponent, ReviewSummaryComponent, PdfJsViewerModule, RouterLink, AsyncPipe],
    templateUrl: "./wqmp-review.component.html",
    styleUrl: "./wqmp-review.component.scss",
})
export class WqmpReviewComponent implements OnInit, IDeactivateComponent {
    @Input() waterQualityManagementPlanID!: number;

    @ViewChild("pdfViewer") pdfViewer: PdfJsViewerComponent;

    private router = inject(Router);
    private http = inject(HttpClient);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private jurisdictionService = inject(StormwaterJurisdictionService);
    private treatmentBMPTypeService = inject(TreatmentBMPTypeService);
    private parcelService = inject(ParcelService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private viewContainerRef = inject(ViewContainerRef);

    // Prefix used on ExtractedField.key for parcel rows. Reusing the field-card machinery
    // (accept/edit/reject/navigate) keeps the UX consistent and lets draft-overlay keep
    // working unchanged. Filter by this prefix to distinguish parcels from single-value fields.
    public readonly PARCEL_KEY_PREFIX = "__Parcel__-";
    private userParcelCounter = 0;

    // NPT-1047: Step 3 BMP cards. ExtractedField rows for BMPs use a composite key of the
    // form `__BMP__-{i}-{attr}` so each (BMP, attribute) pair has a stable identity for
    // draft persistence. The card-level "reject this BMP" overlay is tracked separately
    // in `rejectedBmpIndices` because it spans multiple fields.
    public readonly BMP_KEY_PREFIX = "__BMP__-";
    public readonly BMP_FIELD_KEYS = [
        "QuickBMPName",
        "TreatmentBMPType",
        "NumberOfIndividualBMPs",
        "PercentOfSiteTreated",
        "PercentCaptured",
        "PercentRetained",
        "QuickBMPNote",
    ];
    private readonly BMP_FIELD_LABELS: Record<string, string> = {
        QuickBMPName: "Name",
        TreatmentBMPType: "Type",
        NumberOfIndividualBMPs: "# of Individual BMPs",
        PercentOfSiteTreated: "% of Site Treated",
        PercentCaptured: "% Captured",
        PercentRetained: "% Retained",
        QuickBMPNote: "Note",
    };
    public rejectedBmpIndices = signal<Set<number>>(new Set());
    public treatmentBMPTypeOptions = signal<SelectDropdownOption[]>([]);

    public FormFieldType = FormFieldType;
    // pageData$ emits on every reload and is the outer gate for the template. It completes
    // even when no extraction result exists yet, so the pre-extraction "Extract with AI"
    // state can render while still showing the uploaded PDF.
    public pageData$: Observable<{ hasResult: boolean }>;
    public currentResult = signal<WaterQualityManagementPlanExtractionResultDto | null>(null);
    public isExtracting = signal(false);
    private reload$ = new BehaviorSubject<void>(undefined);

    // Lookup field configuration: maps extraction key → dropdown options + DTO field name
    private lookupFieldConfig: Record<string, { options: SelectDropdownOption[]; dtoField: string }> = {};
    public currentStep = signal(1);
    public fields = signal<ExtractedField[]>([]);
    public pdfBlob: Blob | null = null;
    // NPT-1051: per-section save in progress. Set while POST /save-{location|basics|bmps} is
    // outstanding; binds the corresponding step's Save button to a spinner + disabled state.
    public savingSection = signal<"location" | "basics" | "bmps" | null>(null);
    // NPT-1051: hasUnsavedChanges is derived from field state — any field accepted/edited/
    // rejected (or any card-level BMP rejection) counts as dirty until the corresponding section
    // Save commits. UnsavedChangesGuard reads canExit() which reads this.
    //
    // User-entered fields (Jurisdiction + WQMP Name from the upload modal) get pre-marked
    // "accepted" at parse time so the field card renders the user's value instead of the AI
    // suggestion. That auto-accept is settled state — not unsaved review work — so it doesn't
    // count toward hasUnsavedChanges. Editing or rejecting a user-entered field still counts.
    public hasUnsavedChanges = computed(() => {
        return this.fields().some((f) => {
            if (f.state === "pending") return false;
            if (f.isUserEntered && f.state === "accepted") return false;
            return true;
        }) || this.rejectedBmpIndices().size > 0;
    });
    public isNavigating = signal(false);
    // NPT-1020 rework: tracks which field card most recently triggered a PDF jump so the
    // template can paint a "selected" outline on it. Reset on extraction reload (see the
    // tap inside the reload pipe in load()).
    public selectedFieldKey = signal<string | null>(null);
    // Tracks whether the PDF.js iframe has fully loaded — until then, evidence clicks can't
    // reach PDFViewerApplication.pdfDocument. If the user clicks early, queue the request here
    // and replay once onDocumentLoad fires.
    private pdfLoaded = false;
    private pendingNavigation: SourceNavigation | null = null;
    // NPT-1020 item 5: pre-extract per-page text once on PDF load so subsequent evidence
    // clicks become O(1) lookups instead of re-iterating every page through getTextContent().
    // pdfBlob is loaded once today (set-once guard in the reload pipe), so the cache stays
    // valid for the component's lifetime. If pdfBlob ever becomes re-fetchable, clear this
    // map AND reset pdfTextCacheBuilding wherever the blob is replaced.
    private pdfTextCache: Map<number, string> = new Map();
    private pdfTextCacheBuilding = false;
    // After `navigateToSource` picks a target page, this holds the details of the box we
    // want to draw there. `onPdfPageRendered` fires every time PDF.js finishes rendering a
    // page; we reinspect the DOM at that point and draw. Keying off the render event means
    // we succeed even when the target page is far off-screen and renders async.
    //
    // `box` (from Claude when it read the page as an image), when present, short-circuits
    // to a precise draw. Otherwise `searchPhrase` drives a text-layer span match, and a null
    // searchPhrase falls back to a page-level outline.
    private pendingHighlight: { pageNumber: number; box: EvidenceBoundingBox | null; searchPhrase: string | null } | null = null;


    public steps: { number: number; title: string; desc: string; disabled?: boolean }[] = [
        { number: 1, title: "Location", desc: "Jurisdiction & parcels" },
        { number: 2, title: "Basics", desc: "Project details & contacts" },
        { number: 3, title: "BMPs", desc: "Treatment controls" },
        { number: 4, title: "Review", desc: "Final review" },
    ];

    // Fields per step
    private locationFields = ["Jurisdiction", "HydrologicSubarea", "RecordedWQMPAreaInAcres"];
    private basicsFields = [
        "WaterQualityManagementPlanName", "WaterQualityManagementPlanPriority", "WaterQualityManagementPlanDevelopmentType",
        "WaterQualityManagementPlanLandUse", "WaterQualityManagementPlanPermitTerm", "ApprovalDate", "DateOfConstruction",
        "HydromodificationAppliesType", "RecordNumber", "TrashCaptureStatusType",
        "MaintenanceContactName", "MaintenanceContactOrganization", "MaintenanceContactPhone",
        "MaintenanceContactAddress1", "MaintenanceContactAddress2", "MaintenanceContactCity", "MaintenanceContactState", "MaintenanceContactZip",
    ];

    private fieldLabels: Record<string, string> = {
        WaterQualityManagementPlanName: "WQMP Name",
        Jurisdiction: "Jurisdiction",
        HydrologicSubarea: "Hydrologic Subarea",
        RecordedWQMPAreaInAcres: "Recorded WQMP Area (Acres)",
        WaterQualityManagementPlanPriority: "Priority",
        WaterQualityManagementPlanDevelopmentType: "Development Type",
        WaterQualityManagementPlanLandUse: "Land Use",
        WaterQualityManagementPlanPermitTerm: "Permit Term",
        ApprovalDate: "Approval Date",
        DateOfConstruction: "Date of Construction",
        HydromodificationAppliesType: "Hydromodification Applies",
        RecordNumber: "Record Number",
        TrashCaptureStatusType: "Trash Capture Status",
        MaintenanceContactName: "Maintenance Contact Name",
        MaintenanceContactOrganization: "Maintenance Contact Organization",
        MaintenanceContactPhone: "Maintenance Contact Phone",
        MaintenanceContactAddress1: "Maintenance Contact Address 1",
        MaintenanceContactAddress2: "Maintenance Contact Address 2",
        MaintenanceContactCity: "Maintenance Contact City",
        MaintenanceContactState: "Maintenance Contact State",
        MaintenanceContactZip: "Maintenance Contact ZIP",
    };

    ngOnInit(): void {
        // Load lookup options first, then parse extraction results
        const jurisdictions$ = this.jurisdictionService.listViewableStormwaterJurisdiction().pipe(
            map((j) => j.map((x) => ({ Label: x.StormwaterJurisdictionName, Value: x.StormwaterJurisdictionID }) as SelectDropdownOption)),
            catchError(() => of([] as SelectDropdownOption[]))
        );
        const hydrologicSubareas$ = this.wqmpService.listHydrologicSubareasWaterQualityManagementPlan().pipe(
            map((s) => s.map((x) => ({ Label: x.HydrologicSubareaName, Value: x.HydrologicSubareaID }) as SelectDropdownOption)),
            catchError(() => of([] as SelectDropdownOption[]))
        );
        // NPT-1047: TreatmentBMPType options drive the type dropdown on each BMP card in
        // Step 3. Loaded once with the other lookups so parseExtractionResult can resolve
        // extracted type strings to their IDs synchronously.
        const treatmentBMPTypes$ = this.treatmentBMPTypeService.listTreatmentBMPType().pipe(
            map((types) => types
                .filter((t) => t.TreatmentBMPTypeID != null && t.TreatmentBMPTypeName)
                .map((t) => ({ Label: t.TreatmentBMPTypeName!, Value: t.TreatmentBMPTypeID! }) as SelectDropdownOption)
                .sort((a, b) => a.Label.localeCompare(b.Label))),
            catchError(() => of([] as SelectDropdownOption[]))
        );

        this.pageData$ = forkJoin([jurisdictions$, hydrologicSubareas$, treatmentBMPTypes$]).pipe(
            tap(([jurisdictions, hydrologicSubareas, treatmentBMPTypes]) => {
                this.treatmentBMPTypeOptions.set(treatmentBMPTypes);
                this.lookupFieldConfig = {
                    Jurisdiction: { options: jurisdictions, dtoField: "StormwaterJurisdictionID" },
                    HydrologicSubarea: { options: hydrologicSubareas, dtoField: "HydrologicSubareaID" },
                    WaterQualityManagementPlanPriority: { options: WaterQualityManagementPlanPrioritiesAsSelectDropdownOptions, dtoField: "WaterQualityManagementPlanPriorityID" },
                    WaterQualityManagementPlanDevelopmentType: { options: WaterQualityManagementPlanDevelopmentTypesAsSelectDropdownOptions, dtoField: "WaterQualityManagementPlanDevelopmentTypeID" },
                    WaterQualityManagementPlanLandUse: { options: WaterQualityManagementPlanLandUsesAsSelectDropdownOptions, dtoField: "WaterQualityManagementPlanLandUseID" },
                    WaterQualityManagementPlanPermitTerm: { options: WaterQualityManagementPlanPermitTermsAsSelectDropdownOptions, dtoField: "WaterQualityManagementPlanPermitTermID" },
                    HydromodificationAppliesType: { options: HydromodificationAppliesTypesAsSelectDropdownOptions, dtoField: "HydromodificationAppliesTypeID" },
                    TrashCaptureStatusType: { options: TrashCaptureStatusTypesAsSelectDropdownOptions, dtoField: "TrashCaptureStatusTypeID" },
                };
            }),
            switchMap(() => this.reload$),
            // Extraction may not exist yet (upload is now a separate step) — endpoint returns
            // null in that case, not a 404, so no exception handling is needed.
            switchMap(() => forkJoin({
                extractionResult: this.wqmpService.getExtractionResultWaterQualityManagementPlan(this.waterQualityManagementPlanID),
                // Always fetch the uploaded document metadata — we need the FileResourceGUID for the
                // PDF blob regardless of extraction state.
                documents: this.wqmpService.listDocumentsWaterQualityManagementPlan(this.waterQualityManagementPlanID),
            })),
            tap(({ extractionResult }) => {
                this.currentResult.set(extractionResult);
                if (extractionResult) {
                    this.parseExtractionResult(extractionResult.ExtractionResultJson);
                } else {
                    this.fields.set([]);
                }
                this.rejectedBmpIndices.set(new Set());
                // Drop any prior selection so a Re-run Extraction (or initial load) doesn't
                // leave a stale "selected" outline on a field card from a previous result.
                this.selectedFieldKey.set(null);
            }),
            switchMap(({ extractionResult, documents }) => {
                // Prefer the extraction result's cached GUID; fall back to the first (primary)
                // document. Either source works — both ultimately point at the same blob.
                const guid = extractionResult?.FileResourceGuid ?? documents[0]?.FileResource?.FileResourceGUID;
                if (!guid || this.pdfBlob) return of({ hasResult: !!extractionResult });
                return this.http.get(`${environment.mainAppApiUrl}/file-resources/${guid}`, { responseType: "blob" }).pipe(
                    tap((blob) => { this.pdfBlob = blob; }),
                    map(() => ({ hasResult: !!extractionResult })),
                    catchError(() => of({ hasResult: !!extractionResult }))
                );
            }),
            catchError(() => {
                this.alertService.pushAlert(new Alert("Failed to load WQMP document.", AlertContext.Danger));
                return EMPTY;
            }),
            shareReplay(1)
        );
    }

    runExtraction(): void {
        // Caller (template) gates this on isAdmin + confirm-dialog when a result already exists.
        this.isExtracting.set(true);
        this.alertService.clearAlerts();
        this.wqmpService.runExtractionWaterQualityManagementPlan(this.waterQualityManagementPlanID)
            .pipe(finalize(() => this.isExtracting.set(false)))
            .subscribe({
                next: () => {
                    this.alertService.pushAlert(new Alert("Extraction completed. Review the extracted fields below.", AlertContext.Success));
                    this.reload$.next();
                },
                error: (err: HttpErrorResponse) => {
                    // 400s carry our friendly { message } payload (PDF too large / Claude 4xx);
                    // anything else falls through to the generic HttpErrorInterceptor alert.
                    const msg = err.status === 400
                        ? (err.error?.message ?? "Extraction failed.")
                        : "Extraction failed. Please try again.";
                    if (err.status === 400) {
                        this.alertService.pushAlert(new Alert(msg, AlertContext.Danger));
                    }
                },
            });
    }

    async confirmReExtract(): Promise<void> {
        // NPT-1051: Re-run is always available — saved sections in the live WQMP survive a
        // re-extract by construction (extraction overwrites the AI suggestion, not the WQMP).
        const confirmed = await this.confirmService.confirm({
            title: "Re-run extraction?",
            message: "This will replace the AI-extracted suggestions with a fresh run. Sections you've already saved are preserved on the WQMP.",
            buttonTextYes: "Re-run",
            buttonTextNo: "Cancel",
            buttonClassYes: "btn-danger",
        }, this.viewContainerRef);
        if (confirmed) this.runExtraction();
    }

    // IDeactivateComponent — returns true when it's safe to leave (no unsaved changes),
    // false to have the UnsavedChangesGuard show the confirm dialog.
    canExit(): boolean {
        return !this.hasUnsavedChanges();
    }

    goToStep(step: number): void {
        if (this.steps[step - 1]?.disabled) return;
        this.currentStep.set(step);
    }

    get fieldsForCurrentStep(): ExtractedField[] {
        // Step 3's BMP fields are grouped under bmp-review-card components rather than
        // rendered flat — exclude them from the top-level field list so the Step 1/2
        // template path doesn't duplicate them.
        return this.fields().filter(
            (f) => f.step === this.currentStep() && !f.key.startsWith(this.BMP_KEY_PREFIX)
        );
    }

    // NPT-1020: helpers for Step 4's review-summary component — slice the flat fields
    // signal back into the per-step buckets the summary renders.
    get summaryLocationFields(): ExtractedField[] {
        return this.fields().filter((f) => f.step === 1 && !f.key.startsWith(this.PARCEL_KEY_PREFIX));
    }
    get summaryBasicsFields(): ExtractedField[] {
        return this.fields().filter((f) => f.step === 2);
    }
    get summaryParcelFields(): ExtractedField[] {
        return this.fields().filter((f) => f.key.startsWith(this.PARCEL_KEY_PREFIX));
    }
    get summaryBmpGroups(): { bmpIndex: number; displayName: string | null; fields: ExtractedField[]; isRejected: boolean }[] {
        // Same grouping logic as bmpGroupsForCurrentStep but without the step==3 gate
        // (the summary view runs on Step 4).
        const groups = new Map<number, ExtractedField[]>();
        for (const field of this.fields()) {
            if (!field.key.startsWith(this.BMP_KEY_PREFIX)) continue;
            const suffix = field.key.slice(this.BMP_KEY_PREFIX.length);
            const dashIdx = suffix.indexOf("-");
            if (dashIdx < 0) continue;
            const bmpIndex = parseInt(suffix.slice(0, dashIdx), 10);
            if (!Number.isFinite(bmpIndex)) continue;
            if (!groups.has(bmpIndex)) groups.set(bmpIndex, []);
            groups.get(bmpIndex)!.push(field);
        }
        const rejected = this.rejectedBmpIndices();
        return Array.from(groups.entries())
            .sort(([a], [b]) => a - b)
            .map(([bmpIndex, fields]) => {
                const nameField = fields.find((f) => f.key.endsWith("-QuickBMPName"));
                const displayName = (nameField?.acceptedValue ?? nameField?.value) ?? null;
                return { bmpIndex, displayName, fields, isRejected: rejected.has(bmpIndex) };
            });
    }
    get summaryLookupOptionsByKey(): Record<string, SelectDropdownOption[]> {
        const map: Record<string, SelectDropdownOption[]> = {};
        for (const [key, cfg] of Object.entries(this.lookupFieldConfig)) {
            map[key] = cfg.options;
        }
        return map;
    }

    /**
     * NPT-1047: groups BMP fields back into per-BMP cards for Step 3 rendering. Returns
     * one entry per `__BMP__-{i}` index with the seven attribute fields in the order
     * declared in `BMP_FIELD_KEYS`. Skipped when not on Step 3.
     */
    get bmpGroupsForCurrentStep(): { bmpIndex: number; displayName: string | null; fields: ExtractedField[]; isRejected: boolean }[] {
        if (this.currentStep() !== 3) return [];
        const groups = new Map<number, ExtractedField[]>();
        for (const field of this.fields()) {
            if (!field.key.startsWith(this.BMP_KEY_PREFIX)) continue;
            const suffix = field.key.slice(this.BMP_KEY_PREFIX.length);
            const dashIdx = suffix.indexOf("-");
            if (dashIdx < 0) continue;
            const bmpIndex = parseInt(suffix.slice(0, dashIdx), 10);
            if (!Number.isFinite(bmpIndex)) continue;
            if (!groups.has(bmpIndex)) groups.set(bmpIndex, []);
            groups.get(bmpIndex)!.push(field);
        }

        const rejected = this.rejectedBmpIndices();
        const orderIndex = (key: string) => {
            const attr = key.slice(key.lastIndexOf("-") + 1);
            const idx = this.BMP_FIELD_KEYS.indexOf(attr);
            return idx < 0 ? this.BMP_FIELD_KEYS.length : idx;
        };

        return Array.from(groups.entries())
            .sort(([a], [b]) => a - b)
            .map(([bmpIndex, fields]) => {
                fields.sort((a, b) => orderIndex(a.key) - orderIndex(b.key));
                const nameField = fields.find((f) => f.key.endsWith("-QuickBMPName"));
                const displayName = (nameField?.acceptedValue ?? nameField?.value) ?? null;
                return { bmpIndex, displayName, fields, isRejected: rejected.has(bmpIndex) };
            });
    }

    get confirmedCount(): number {
        return this.fields().filter((f) => f.state === "accepted" || f.state === "edited").length;
    }

    get totalFieldCount(): number {
        return this.fields().length;
    }

    onPdfLoaded(): void {
        this.pdfLoaded = true;
        // Build the page-text cache eagerly so the very first evidence click is fast.
        // Don't await — the navigateToSource path falls back to a per-page fetch if a
        // click lands before the cache finishes filling.
        void this.buildPdfTextCache();
        // Replay any click that arrived before the iframe finished initializing.
        if (this.pendingNavigation) {
            const nav = this.pendingNavigation;
            this.pendingNavigation = null;
            this.navigateToSource(nav);
        }
    }

    private async buildPdfTextCache(): Promise<void> {
        if (this.pdfTextCacheBuilding) return;
        this.pdfTextCacheBuilding = true;
        try {
            const iframe = this.pdfViewer?.iframe?.nativeElement as HTMLIFrameElement;
            const pdfApp = (iframe?.contentWindow as any)?.PDFViewerApplication;
            if (!pdfApp?.pdfDocument) return;
            const pdfDoc = pdfApp.pdfDocument;
            const totalPages: number = pdfDoc.numPages;
            for (let i = 1; i <= totalPages; i++) {
                if (this.pdfTextCache.has(i)) continue;
                try {
                    const page = await pdfDoc.getPage(i);
                    const textContent = await page.getTextContent();
                    const pageText = this.normalizeText(textContent.items.map((item: any) => item.str).join(" "));
                    this.pdfTextCache.set(i, pageText);
                } catch {
                    // Skip unreadable pages — searchPdfForText will just miss them.
                }
            }
        } finally {
            this.pdfTextCacheBuilding = false;
        }
    }

    onSourceClick(key: string, nav: SourceNavigation): void {
        this.selectedFieldKey.set(key);
        this.navigateToSource(nav);
    }

    async navigateToSource(nav: SourceNavigation): Promise<void> {
        if (!this.pdfViewer) return;
        // Defer until the PDF.js iframe is ready — the user can click an evidence snippet
        // before `onDocumentLoad` fires on large PDFs or cold caches.
        if (!this.pdfLoaded) {
            this.pendingNavigation = nav;
            return;
        }
        this.isNavigating.set(true);

        try {
            this.clearHighlights();

            // Best: text-layer span match (pixel accurate when the PDF has a usable text
            // layer). Runs first because it beats Claude's vision-estimated coords on any
            // PDF where the text layer is readable. Try multiple phrases so short/idiosyncratic
            // evidence snippets still find a hit before we give up to the vision box.
            const searchCandidates: string[] = [];
            if (nav.evidence) {
                const normalizedEvidence = this.normalizeText(nav.evidence);
                const middleSlice = this.extractSearchPhrase(normalizedEvidence);
                if (middleSlice) searchCandidates.push(middleSlice);
                // Full normalized evidence — for cases where the 12-word middle slice falls on a
                // page break or spans text-layer column boundaries but the whole snippet appears once.
                if (normalizedEvidence && !searchCandidates.includes(normalizedEvidence)) {
                    searchCandidates.push(normalizedEvidence);
                }
            }
            if (nav.value) {
                // Raw value ("14.6", "Michael Gagnet") — most distinctive when the phrase fails
                // because the evidence uses a paraphrase or omits the actual value.
                const normalizedValue = this.normalizeText(nav.value);
                if (normalizedValue.length >= 3 && !searchCandidates.includes(normalizedValue)) {
                    searchCandidates.push(normalizedValue);
                }
            }

            for (const phrase of searchCandidates) {
                const foundPage = await this.searchPdfForText(phrase, nav.documentSource);
                if (foundPage > 0) {
                    this.pendingHighlight = { pageNumber: foundPage, box: null, searchPhrase: phrase };
                    this.pdfViewer.page = foundPage;
                    this.tryDrawPendingHighlight();
                    return;
                }
            }

            // Next: Claude's vision-estimated coords. Imprecise (LLM-vision coords are
            // typically off by a line or two), so we draw with a dashed border + padding to
            // visually signal "approximate region" rather than claiming pixel precision.
            if (nav.boundingBox) {
                this.pendingHighlight = { pageNumber: nav.boundingBox.PageNumber, box: nav.boundingBox, searchPhrase: null };
                this.pdfViewer.page = nav.boundingBox.PageNumber;
                this.tryDrawPendingHighlight();
                return;
            }

            // Last: DocumentSource page number with a whole-page outline. Used when Claude
            // didn't emit a box AND the text layer is empty/garbled (pure scanned PDFs).
            if (nav.documentSource) {
                const match = nav.documentSource.match(/page\s*(\d+)/i);
                if (match) {
                    const pageNumber = parseInt(match[1], 10);
                    this.pendingHighlight = { pageNumber, box: null, searchPhrase: null };
                    this.pdfViewer.page = pageNumber;
                    this.tryDrawPendingHighlight();
                }
            }
        } finally {
            this.isNavigating.set(false);
        }
    }

    onPdfPageRendered(): void {
        // PDF.js lazy-renders pages; whenever any page finishes rendering, check whether it's
        // the one we're waiting on for a highlight and draw the box if so.
        this.tryDrawPendingHighlight();
    }

    private tryDrawPendingHighlight(): void {
        const pending = this.pendingHighlight;
        if (!pending) return;
        let drew = false;
        if (pending.box) {
            drew = this.highlightBoundingBox(pending.pageNumber, pending.box);
        } else if (pending.searchPhrase) {
            drew = this.highlightTextOnPage(pending.pageNumber, pending.searchPhrase);
        } else {
            drew = this.highlightWholePage(pending.pageNumber);
        }
        if (drew) this.pendingHighlight = null;
    }

    // Draw an approximate rectangle from Claude's vision-derived coords (normalized 0-1
    // fractions relative to the page). LLM-vision coords are commonly off by a line or two,
    // so we dashed-border the box and pad it generously vertically (where offsets are most
    // common) so the actual target is usually inside the rendered region.
    private highlightBoundingBox(pageNum: number, bbox: EvidenceBoundingBox): boolean {
        try {
            const iframe = this.pdfViewer?.iframe?.nativeElement as HTMLIFrameElement;
            const iframeDoc = iframe?.contentDocument || iframe?.contentWindow?.document;
            if (!iframeDoc) return false;
            const pageEl = iframeDoc.querySelector(`.page[data-page-number="${pageNum}"]`) as HTMLElement | null;
            if (!pageEl) return false;

            const pageWidth = pageEl.offsetWidth;
            const pageHeight = pageEl.offsetHeight;
            // Claude's vision coords for scans are typically off by a line or two — a paragraph
            // at worst. Generous padding (10% vertical, 3% horizontal) grows the rendered box
            // enough to still capture the evidence when the raw coords drift, at the cost of a
            // less precise-looking rectangle. The dashed border already tells the user the box
            // is approximate.
            const verticalPadFraction = 0.10;
            const horizontalPadFraction = 0.03;

            const left = Math.max(0, (bbox.X - horizontalPadFraction) * pageWidth);
            const top = Math.max(0, (bbox.Y - verticalPadFraction) * pageHeight);
            const right = Math.min(1, bbox.X + bbox.Width + horizontalPadFraction) * pageWidth;
            const bottom = Math.min(1, bbox.Y + bbox.Height + verticalPadFraction) * pageHeight;

            const box = iframeDoc.createElement("div");
            box.className = "wqmp-highlight wqmp-highlight--approx";
            Object.assign(box.style, {
                position: "absolute",
                left: `${left}px`,
                top: `${top}px`,
                width: `${right - left}px`,
                height: `${bottom - top}px`,
                border: "2px dashed #f59e0b",
                backgroundColor: "rgba(255, 235, 59, 0.10)",
                pointerEvents: "none",
                zIndex: "10",
                borderRadius: "3px",
                boxSizing: "border-box",
            });
            pageEl.appendChild(box);
            box.scrollIntoView({ behavior: "smooth", block: "center" });
            return true;
        } catch {
            return false;
        }
    }

    // Scanned PDFs (e.g. copier-generated) often have no usable text layer, so we can't
    // pinpoint where the evidence lives. Outline the whole page as a softer visual cue that
    // "the evidence is somewhere here" instead of silently landing the user on the page.
    private highlightWholePage(pageNum: number): boolean {
        try {
            const iframe = this.pdfViewer?.iframe?.nativeElement as HTMLIFrameElement;
            const iframeDoc = iframe?.contentDocument || iframe?.contentWindow?.document;
            if (!iframeDoc) return false;
            const pageEl = iframeDoc.querySelector(`.page[data-page-number="${pageNum}"]`) as HTMLElement | null;
            if (!pageEl) return false;

            const box = iframeDoc.createElement("div");
            box.className = "wqmp-highlight wqmp-highlight--page";
            Object.assign(box.style, {
                position: "absolute",
                left: "0px",
                top: "0px",
                width: `${pageEl.offsetWidth}px`,
                height: `${pageEl.offsetHeight}px`,
                border: "3px dashed #f59e0b",
                backgroundColor: "rgba(255, 235, 59, 0.06)",
                pointerEvents: "none",
                zIndex: "10",
                borderRadius: "4px",
                boxSizing: "border-box",
            });
            pageEl.appendChild(box);
            box.scrollIntoView({ behavior: "smooth", block: "start" });
            return true;
        } catch {
            return false;
        }
    }

    private async searchPdfForText(searchPhrase: string, documentSource: string | null): Promise<number> {
        try {
            const iframe = this.pdfViewer.iframe?.nativeElement as HTMLIFrameElement;
            const pdfApp = (iframe?.contentWindow as any)?.PDFViewerApplication;
            if (!pdfApp?.pdfDocument) return 0;

            const pdfDoc = pdfApp.pdfDocument;
            const totalPages: number = pdfDoc.numPages;
            const matches: number[] = [];
            for (let i = 1; i <= totalPages; i++) {
                let pageText = this.pdfTextCache.get(i);
                if (pageText === undefined) {
                    // Cache miss (cache still building, or build skipped this page) — fetch on
                    // demand and memoize so the next click skips the work.
                    const page = await pdfDoc.getPage(i);
                    const textContent = await page.getTextContent();
                    pageText = this.normalizeText(textContent.items.map((item: any) => item.str).join(" "));
                    this.pdfTextCache.set(i, pageText);
                }
                if (pageText.includes(searchPhrase)) matches.push(i);
            }

            if (matches.length === 0) return 0;
            if (matches.length === 1) return matches[0];

            // Multiple hits — prefer the one nearest to Claude's documentSource hint.
            if (documentSource) {
                const hintMatch = documentSource.match(/page\s*(\d+)/i);
                if (hintMatch) {
                    const hintPage = parseInt(hintMatch[1], 10);
                    matches.sort((a, b) => Math.abs(a - hintPage) - Math.abs(b - hintPage));
                }
            }
            return matches[0];
        } catch {
            return 0;
        }
    }

    private clearHighlights(): void {
        try {
            const iframe = this.pdfViewer?.iframe?.nativeElement as HTMLIFrameElement;
            const iframeDoc = iframe?.contentDocument || iframe?.contentWindow?.document;
            if (!iframeDoc) return;
            iframeDoc.querySelectorAll(".wqmp-highlight").forEach((el) => el.remove());
        } catch { /* ignore */ }
    }

    private highlightTextOnPage(pageNum: number, searchPhrase: string): boolean {
        // Returns true when the highlight was successfully drawn, false when the target page
        // isn't rendered yet (caller leaves `pendingHighlight` set so a later onPageRendered
        // gets another chance).
        try {
            const iframe = this.pdfViewer?.iframe?.nativeElement as HTMLIFrameElement;
            const iframeDoc = iframe?.contentDocument || iframe?.contentWindow?.document;
            if (!iframeDoc) return false;

            const pageEl = iframeDoc.querySelector(`.page[data-page-number="${pageNum}"]`) as HTMLElement | null;
            if (!pageEl) return false;
            const textLayer = pageEl.querySelector(".textLayer");
            if (!textLayer) return false;

            // Build a single concatenated text-layer string and find where our phrase appears,
            // then collect the rects of spans whose text falls within that matched range. This
            // is more robust than a span-by-span walk because PDF.js often splits text into
            // single-character or short-fragment spans that wouldn't individually match a long
            // phrase.
            const spans = Array.from(textLayer.querySelectorAll("span"));
            const entries: { text: string; rect: DOMRect; start: number; end: number }[] = [];
            let cursor = 0;
            let joined = "";
            for (const span of spans) {
                const text = this.normalizeText(span.textContent || "");
                if (!text) continue;
                const separator = joined.length > 0 && !joined.endsWith(" ") ? " " : "";
                joined += separator + text;
                const start = cursor + separator.length;
                const end = start + text.length;
                entries.push({ text, rect: span.getBoundingClientRect(), start, end });
                cursor = end;
            }

            const idx = joined.indexOf(searchPhrase);
            if (idx < 0) return false;
            const endIdx = idx + searchPhrase.length;

            const matchingRects: DOMRect[] = entries
                .filter((e) => e.end > idx && e.start < endIdx)
                .map((e) => e.rect);
            if (matchingRects.length === 0) return false;

            const pageRect = pageEl.getBoundingClientRect();
            const minTop = Math.min(...matchingRects.map((r) => r.top)) - pageRect.top;
            const maxBottom = Math.max(...matchingRects.map((r) => r.bottom)) - pageRect.top;
            const padding = 4;

            const box = iframeDoc.createElement("div");
            box.className = "wqmp-highlight";
            Object.assign(box.style, {
                position: "absolute",
                left: "0px",
                top: `${minTop - padding}px`,
                width: `${pageEl.offsetWidth}px`,
                height: `${maxBottom - minTop + padding * 2}px`,
                border: "2px solid #f59e0b",
                backgroundColor: "rgba(255, 235, 59, 0.18)",
                pointerEvents: "none",
                zIndex: "10",
                borderRadius: "4px",
            });
            pageEl.appendChild(box);
            box.scrollIntoView({ behavior: "smooth", block: "center" });
            return true;
        } catch {
            return false;
        }
    }

    private normalizeText(text: string): string {
        // Claude's ExtractionEvidence doesn't always round-trip cleanly through PDF.js text
        // extraction — curly quotes, non-breaking spaces, and ligatures can produce mismatches.
        // Canonicalize: NFKD decompose → strip combining marks → unify quote/dash variants
        // → collapse whitespace. Used on both sides before comparison.
        return text
            .normalize("NFKD")
            .replace(/[̀-ͯ]/g, "")
            .replace(/[‘’‛′]/g, "'")
            .replace(/[“”‟″]/g, '"')
            .replace(/[–—−]/g, "-")
            .replace(/\s+/g, " ")
            .toLowerCase()
            .trim();
    }

    private extractSearchPhrase(text: string): string {
        // Use a substantial substring (up to 80 chars) from the middle of the evidence for best specificity
        const words = text.split(" ").filter((w) => w.length > 0);
        if (words.length <= 5) return text;
        const start = Math.floor(words.length / 4);
        const end = Math.min(start + 12, words.length);
        return words.slice(start, end).join(" ");
    }

    // NPT-1051: per-field marks are local-only. They stage in the wizard's field-state map
    // and only write through when the reviewer hits the section's Save button. Mirrors the
    // modal CRUD editors' dirty-tracking pattern: the form gets dirty on edit, save commits,
    // navigation away warns via UnsavedChangesGuard.
    onFieldAccepted(field: ExtractedField, value: string | null): void {
        field.state = "accepted";
        field.acceptedValue = value;
        this.fields.update((f) => [...f]);
    }

    onFieldEdited(field: ExtractedField, value: string): void {
        field.state = "edited";
        field.acceptedValue = value;
        this.fields.update((f) => [...f]);
    }

    onFieldRejected(field: ExtractedField): void {
        field.state = "rejected";
        field.acceptedValue = null;
        this.fields.update((f) => [...f]);
    }

    // NPT-1047: BmpReviewCard emits per-field events keyed by string. Bridge them into
    // the same field-array updates that Step 1/2's per-field cards already drive.
    onBmpFieldAccepted(payload: { key: string; value: string | null }): void {
        const field = this.fields().find((f) => f.key === payload.key);
        if (field) this.onFieldAccepted(field, payload.value);
    }

    onBmpFieldEdited(payload: { key: string; value: string }): void {
        const field = this.fields().find((f) => f.key === payload.key);
        if (field) this.onFieldEdited(field, payload.value);
    }

    onBmpFieldRejected(payload: { key: string }): void {
        const field = this.fields().find((f) => f.key === payload.key);
        if (field) this.onFieldRejected(field);
    }

    // Card-level rejection — independent of per-field state. Excluded BMPs are dropped
    // from the BMPs section save payload regardless of whether their fields were accepted.
    onBmpRejected(bmpIndex: number): void {
        this.rejectedBmpIndices.update((s) => {
            const next = new Set(s);
            next.add(bmpIndex);
            return next;
        });
    }

    onBmpRestored(bmpIndex: number): void {
        this.rejectedBmpIndices.update((s) => {
            const next = new Set(s);
            next.delete(bmpIndex);
            return next;
        });
    }

    // Add a blank user-entered parcel row to the Location step. The reviewer edits the APN
    // in the new row's edit form and hits the row's check mark to accept it; on Save Location
    // the resolved parcel IDs are sent to /save-location which updates the parcel set and
    // recomputes the boundary in one call.
    addParcel(): void {
        const existingParcelCount = this.fields().filter((f) => f.key.startsWith(this.PARCEL_KEY_PREFIX)).length;
        const key = `${this.PARCEL_KEY_PREFIX}user-${this.userParcelCounter++}`;
        const newField: ExtractedField = {
            key,
            label: `Parcel ${existingParcelCount + 1} (APN)`,
            value: null,
            evidence: null,
            source: null,
            boundingBox: null,
            confidence: "none",
            step: 1,
            state: "pending",
            acceptedValue: null,
            isUserEntered: true,
        };
        this.fields.update((f) => [...f, newField]);
        // A freshly added empty parcel row stays in state="pending" — hasUnsavedChanges only
        // flips when the reviewer accepts/edits/rejects something. Until then there's nothing
        // for the section save to write.
    }

    // origin reflects the source of the field's *value*, not any reviewer action.
    // The template checks field state first for "edited"/"rejected" pills, then falls back
    // to origin. When a reviewer "accepts" an AI value, origin stays "ai" — they're
    // confirming the source, not editing the value.
    getFieldOrigin(field: ExtractedField): FieldOrigin {
        if (field.isUserEntered) return "user";
        return field.value ? "ai" : "blank";
    }

    // NPT-1051: per-section save replaces the prior auto-save + Approve-All workflow. Each
    // step (Location, Basics, BMPs) has a Save button that writes accepted/edited fields
    // through to the live WQMP — overwrite semantics matching the modal CRUD editors.
    //   pending   → use AI value (auto-accept on save, AC #7)
    //   accepted  → use AI value (or accepted user value)
    //   edited    → use user value
    //   rejected  → not overlaid; live WQMP value is preserved (AC #6)
    // The dto seeded from the live WQMP is the substrate; only non-rejected fields overlay,
    // so rejected fields naturally fall back to the live value.

    saveLocation(): void {
        this.savingSection.set("location");
        this.alertService.clearAlerts();

        this.wqmpService.getWaterQualityManagementPlan(this.waterQualityManagementPlanID).pipe(
            switchMap((existing) => {
                const dto = this.buildSectionUpsertDto(existing, this.locationFields);
                return this.collectAcceptedParcelIDs().pipe(
                    switchMap((parcelIDs) => this.wqmpService.saveLocationWaterQualityManagementPlan(
                        this.waterQualityManagementPlanID,
                        { WaterQualityManagementPlan: dto, ParcelIDs: parcelIDs },
                    )),
                );
            }),
            finalize(() => this.savingSection.set(null)),
        ).subscribe({
            next: () => {
                this.alertService.pushAlert(new Alert("Location saved.", AlertContext.Success));
                this.markSectionFieldsClean(this.locationFields);
                this.markParcelFieldsClean();
            },
            error: (err: HttpErrorResponse) => this.handleSectionSaveError(err, "Location"),
        });
    }

    saveBasics(): void {
        this.savingSection.set("basics");
        this.alertService.clearAlerts();

        this.wqmpService.getWaterQualityManagementPlan(this.waterQualityManagementPlanID).pipe(
            switchMap((existing) => {
                const dto = this.buildSectionUpsertDto(existing, this.basicsFields);
                return this.wqmpService.saveBasicsWaterQualityManagementPlan(this.waterQualityManagementPlanID, dto);
            }),
            finalize(() => this.savingSection.set(null)),
        ).subscribe({
            next: () => {
                this.alertService.pushAlert(new Alert("Basics saved.", AlertContext.Success));
                this.markSectionFieldsClean(this.basicsFields);
            },
            error: (err: HttpErrorResponse) => this.handleSectionSaveError(err, "Basics"),
        });
    }

    saveBmps(): void {
        this.savingSection.set("bmps");
        this.alertService.clearAlerts();

        const quickBMPs = this.buildQuickBMPsForSave();
        this.wqmpService.saveBmpsWaterQualityManagementPlan(this.waterQualityManagementPlanID, quickBMPs).pipe(
            finalize(() => this.savingSection.set(null)),
        ).subscribe({
            next: (response: WaterQualityManagementPlanSectionSaveResponseDto) => {
                this.alertService.pushAlert(new Alert("BMPs saved.", AlertContext.Success));
                this.warnAboutSkippedBMPs(response?.SkippedBMPs);
                this.markBmpFieldsClean();
            },
            error: (err: HttpErrorResponse) => this.handleSectionSaveError(err, "BMPs"),
        });
    }

    // Builds an UpsertDto seeded with every field on the live WQMP, then overlays the
    // accepted/edited/pending values for the keys in `sectionKeys`. Rejected and
    // out-of-section fields fall through to the live value untouched.
    private buildSectionUpsertDto(existing: WaterQualityManagementPlanDto, sectionKeys: string[]): WaterQualityManagementPlanUpsertDto {
        const dto: any = {
            StormwaterJurisdictionID: existing.StormwaterJurisdictionID,
            WaterQualityManagementPlanStatusID: existing.WaterQualityManagementPlanStatusID,
            WaterQualityManagementPlanModelingApproachID: existing.WaterQualityManagementPlanModelingApproachID,
            TrashCaptureStatusTypeID: existing.TrashCaptureStatusTypeID,
            WaterQualityManagementPlanName: existing.WaterQualityManagementPlanName,
            RecordNumber: existing.RecordNumber,
            RecordedWQMPAreaInAcres: existing.RecordedWQMPAreaInAcres,
            MaintenanceContactName: existing.MaintenanceContactName,
            MaintenanceContactOrganization: existing.MaintenanceContactOrganization,
            MaintenanceContactPhone: existing.MaintenanceContactPhone,
            MaintenanceContactAddress1: existing.MaintenanceContactAddress1,
            MaintenanceContactAddress2: existing.MaintenanceContactAddress2,
            MaintenanceContactCity: existing.MaintenanceContactCity,
            MaintenanceContactState: existing.MaintenanceContactState,
            MaintenanceContactZip: existing.MaintenanceContactZip,
            WaterQualityManagementPlanPriorityID: existing.WaterQualityManagementPlanPriorityID,
            WaterQualityManagementPlanDevelopmentTypeID: existing.WaterQualityManagementPlanDevelopmentTypeID,
            WaterQualityManagementPlanLandUseID: existing.WaterQualityManagementPlanLandUseID,
            WaterQualityManagementPlanPermitTermID: existing.WaterQualityManagementPlanPermitTermID,
            HydromodificationAppliesTypeID: existing.HydromodificationAppliesTypeID,
            HydrologicSubareaID: existing.HydrologicSubareaID,
            ApprovalDate: existing.ApprovalDate,
            DateOfConstruction: existing.DateOfConstruction,
            TrashCaptureEffectiveness: existing.TrashCaptureEffectiveness,
            LastNereidLogID: existing.LastNereidLogID,
            WaterQualityManagementPlanBoundaryNotes: existing.WaterQualityManagementPlanBoundaryNotes,
        };

        // String text fields where the wizard key matches the dto field name 1:1.
        const textFieldKeys = new Set([
            "WaterQualityManagementPlanName", "RecordNumber",
            "MaintenanceContactName", "MaintenanceContactOrganization", "MaintenanceContactPhone",
            "MaintenanceContactAddress1", "MaintenanceContactAddress2",
            "MaintenanceContactCity", "MaintenanceContactState", "MaintenanceContactZip",
        ]);

        for (const key of sectionKeys) {
            const field = this.fields().find((f) => f.key === key);
            if (!field) continue;
            // Rejected fields preserve the live value — skip the overlay entirely.
            if (field.state === "rejected") continue;

            // pending → AI value (auto-accept on save); accepted/edited → reviewer's value.
            const raw = (field.state === "edited" || field.state === "accepted")
                ? field.acceptedValue
                : field.value;
            const v = this.normalizeOverlayValue(raw);

            // Tester feedback (Kathleen): don't overwrite user-entered data with nothing. If the
            // AI didn't extract a value and the reviewer hasn't typed one in (state=pending or
            // accepted with no value), leave the seeded live value alone. The reviewer can
            // explicitly clear via Edit (state=edited will still go through the textFieldKeys
            // branch below to write null). Required-FK columns (TrashCaptureStatusTypeID,
            // StormwaterJurisdictionID) are also protected by this skip — they'd 400 the request
            // on null otherwise.
            if (v == null && field.state !== "edited") continue;

            if (textFieldKeys.has(key)) {
                dto[key] = v;
                continue;
            }

            const lookupConfig = this.lookupFieldConfig[key];
            if (lookupConfig) {
                if (v == null) {
                    dto[lookupConfig.dtoField] = null;
                } else {
                    const n = Number(v);
                    if (!isNaN(n)) dto[lookupConfig.dtoField] = n;
                }
                continue;
            }

            if (key === "RecordedWQMPAreaInAcres") {
                if (v == null) {
                    dto.RecordedWQMPAreaInAcres = null;
                } else {
                    const parsed = parseFloat(v);
                    if (!isNaN(parsed)) dto.RecordedWQMPAreaInAcres = parsed;
                }
                continue;
            }

            if (key === "ApprovalDate" || key === "DateOfConstruction") {
                dto[key] = v;
                continue;
            }
        }

        return dto as WaterQualityManagementPlanUpsertDto;
    }

    // Empty / whitespace-only strings collapse to null so a cleared field doesn't write
    // "" into a nullable column or coerce to Number(0) for a lookup FK.
    private normalizeOverlayValue(raw: string | null | undefined): string | null {
        if (raw == null) return null;
        const trimmed = String(raw).trim();
        return trimmed === "" ? null : trimmed;
    }

    // Resolves the accepted/edited APNs from the parcel rows into ParcelIDs via the
    // /parcels/lookup-by-numbers endpoint. Pending rows with non-empty values are auto-
    // accepted (AC #7); rejected rows + blank rows are dropped. Misses (APN with no
    // matching parcel) are surfaced as a non-blocking warning toast.
    private collectAcceptedParcelIDs(): Observable<number[]> {
        const parcelFields = this.fields().filter((f) => f.key.startsWith(this.PARCEL_KEY_PREFIX));
        const apns = parcelFields
            .filter((f) => f.state !== "rejected")
            .map((f) => (f.acceptedValue ?? f.value ?? "").toString().trim())
            .filter((apn) => apn.length > 0);
        if (apns.length === 0) return of([]);
        return this.parcelService.lookupByNumbersParcel(apns).pipe(
            map((results) => {
                const missed = results.filter((r) => r.ParcelID == null).map((r) => r.ParcelNumber);
                if (missed.length > 0) {
                    const escaped = missed.map((s) => escapeHtml(s ?? "")).join(", ");
                    this.alertService.pushAlert(new Alert(
                        `Saved Location, but the following APNs could not be matched to a parcel and were skipped: ${escaped}.`,
                        AlertContext.Warning,
                    ));
                }
                return results.map((r) => r.ParcelID).filter((id): id is number => id != null);
            }),
        );
    }

    // NPT-1020 item 3: emit a non-blocking warning per QuickBMP that couldn't be
    // auto-created (typically because the AI didn't extract a Treatment BMP Type and
    // the reviewer didn't pick one before Save). The reviewer can still create the BMPs
    // manually via the WQMP detail page.
    private warnAboutSkippedBMPs(skipped: Array<QuickBMPMergeSkipDto> | null | undefined): void {
        if (!skipped?.length) return;
        for (const entry of skipped) {
            const name = escapeHtml(entry.ProposedName ?? "(unnamed)");
            const reasons = (entry.Reasons ?? []).map((r) => escapeHtml(r)).join(", ");
            const msg = reasons
                ? `Could not auto-create BMP "${name}" — missing required field(s): ${reasons}. Add it manually on the WQMP page if needed.`
                : `Could not auto-create BMP "${name}". Add it manually on the WQMP page if needed.`;
            this.alertService.pushAlert(new Alert(msg, AlertContext.Warning));
        }
    }

    // After a successful section save, reset the reviewer's per-field marks so the page
    // re-pends those fields against the now-up-to-date live WQMP. The next pass starts
    // clean; hasUnsavedChanges recomputes from there.
    private markSectionFieldsClean(sectionKeys: string[]): void {
        const keyset = new Set(sectionKeys);
        this.fields.update((current) => current.map((f) => keyset.has(f.key)
            ? { ...f, state: "pending" as const, acceptedValue: null }
            : f));
    }

    private markParcelFieldsClean(): void {
        this.fields.update((current) => current.map((f) => f.key.startsWith(this.PARCEL_KEY_PREFIX)
            ? { ...f, state: "pending" as const, acceptedValue: null }
            : f));
    }

    private markBmpFieldsClean(): void {
        this.fields.update((current) => current.map((f) => f.key.startsWith(this.BMP_KEY_PREFIX)
            ? { ...f, state: "pending" as const, acceptedValue: null }
            : f));
        this.rejectedBmpIndices.set(new Set());
    }

    private handleSectionSaveError(err: HttpErrorResponse, section: string): void {
        // The alert component renders message via [innerHTML], so the server's 400 body
        // must be HTML-escaped before display — validation messages may echo user values.
        const raw = (err.status === 400 && typeof err.error === "string")
            ? err.error
            : `Failed to save ${section}. Please try again.`;
        this.alertService.pushAlert(new Alert(escapeHtml(raw), AlertContext.Danger));
    }

    /**
     * Materializes the Step 3 BMP cards into QuickBMPUpsertDto[] for the /save-bmps
     * payload. Drops card-level-rejected BMPs entirely. For each remaining BMP, every
     * field's `acceptedValue` (when accepted/edited) overrides the AI value; pending /
     * rejected fields fall back to the original extracted value (or null). The backend
     * still validates the assembled list and rejects the whole save if it's invalid.
     */
    private buildQuickBMPsForSave(): QuickBMPUpsertDto[] {
        const groups = new Map<number, Map<string, ExtractedField>>();
        for (const field of this.fields()) {
            if (!field.key.startsWith(this.BMP_KEY_PREFIX)) continue;
            const suffix = field.key.slice(this.BMP_KEY_PREFIX.length);
            const dashIdx = suffix.indexOf("-");
            if (dashIdx < 0) continue;
            const bmpIndex = parseInt(suffix.slice(0, dashIdx), 10);
            const attr = suffix.slice(dashIdx + 1);
            if (!Number.isFinite(bmpIndex)) continue;
            if (!groups.has(bmpIndex)) groups.set(bmpIndex, new Map());
            groups.get(bmpIndex)!.set(attr, field);
        }

        const rejected = this.rejectedBmpIndices();
        const result: QuickBMPUpsertDto[] = [];
        // valueOf returns the reviewer-current value as a trimmed non-empty string, or
        // null. Empty/whitespace strings (e.g. when FieldCardComponent.saveEdit() emits
        // "" because the user cleared the field) collapse to null here so downstream
        // numeric parsing can't turn them into a stray 0 (and the backend's [Required]
        // checks fire correctly when the reviewer really did blank a value).
        const valueOf = (f: ExtractedField | undefined): string | null => {
            if (!f) return null;
            // A per-field reject means the reviewer wants this attribute blank, even if
            // the AI extracted a value. Pending falls back to the AI value so cards the
            // reviewer didn't touch still carry the extraction through.
            if (f.state === "rejected") return null;
            const raw = (f.acceptedValue ?? f.value) ?? null;
            if (raw == null) return null;
            const trimmed = String(raw).trim();
            return trimmed === "" ? null : trimmed;
        };
        const numericOf = (s: string | null): number | null => {
            if (s == null) return null;
            const n = Number(s);
            return Number.isFinite(n) ? n : null;
        };

        for (const [bmpIndex, attrMap] of Array.from(groups.entries()).sort(([a], [b]) => a - b)) {
            if (rejected.has(bmpIndex)) continue;

            const name = valueOf(attrMap.get("QuickBMPName"));
            const typeID = numericOf(valueOf(attrMap.get("TreatmentBMPType")));
            // Backend NumberOfIndividualBMPs is int? — truncate decimals so model binding
            // doesn't reject "2.5"-style values. Default 1 mirrors the prompt's default
            // and the manual-entry UI's behavior.
            const countRaw = numericOf(valueOf(attrMap.get("NumberOfIndividualBMPs")));
            const count = countRaw != null ? Math.trunc(countRaw) : 1;

            // Skip phantom rows where the reviewer rejected the name and didn't supply
            // a replacement — there's nothing to merge against on the backend.
            if (!name) continue;

            result.push({
                QuickBMPName: name,
                TreatmentBMPTypeID: typeID,
                NumberOfIndividualBMPs: count,
                PercentOfSiteTreated: numericOf(valueOf(attrMap.get("PercentOfSiteTreated"))),
                PercentCaptured: numericOf(valueOf(attrMap.get("PercentCaptured"))),
                PercentRetained: numericOf(valueOf(attrMap.get("PercentRetained"))),
                QuickBMPNote: valueOf(attrMap.get("QuickBMPNote")),
                DryWeatherFlowOverrideID: null,
            });
        }

        return result;
    }

    private parseExtractionResult(json: string): void {
        // Reset BMP rejection state on every parse so re-extracting doesn't leak stale
        // rejections from the previous extraction.
        this.rejectedBmpIndices.set(new Set());

        try {
            const parsed = JSON.parse(json);
            const wqmp = parsed.WQMP || {};
            const allFields: ExtractedField[] = [];

            for (const key of this.locationFields) {
                allFields.push(this.makeField(key, wqmp[key], 1));
            }
            for (const key of this.basicsFields) {
                allFields.push(this.makeField(key, wqmp[key], 2));
            }

            // Parcels: each entry is { ParcelNumber: { Value, Evidence, ... } }. Render one
            // field card per extracted APN on the Location step. User-added rows get
            // appended at click-time with a different key suffix so they don't collide.
            const parcelArr = parsed.Parcels;
            if (Array.isArray(parcelArr)) {
                parcelArr.forEach((parcel: any, i: number) => {
                    const extracted = parcel?.ParcelNumber;
                    if (!extracted) return;
                    const field = this.makeField(`${this.PARCEL_KEY_PREFIX}${i}`, extracted, 1);
                    field.label = `Parcel ${i + 1} (APN)`;
                    allFields.push(field);
                });
            }

            // NPT-1047 Step 3: each entry in `QuickBMPs` is { QuickBMPName: ExtractedValue,
            // TreatmentBMPType: ExtractedValue, ...7 attributes }. Emit one field per
            // (BMP, attribute) keyed `__BMP__-{i}-{attr}`, step=3. The bmp-review-card
            // component groups them back into per-BMP cards via bmpGroupsForCurrentStep.
            const bmpArr = parsed.QuickBMPs;
            if (Array.isArray(bmpArr)) {
                const bmpTypeOptions = this.treatmentBMPTypeOptions();
                bmpArr.forEach((bmp: any, i: number) => {
                    if (!bmp) return;
                    for (const attr of this.BMP_FIELD_KEYS) {
                        const extracted = bmp[attr];
                        const key = `${this.BMP_KEY_PREFIX}${i}-${attr}`;
                        const field = this.makeField(key, extracted, 3);
                        field.label = this.BMP_FIELD_LABELS[attr] ?? attr;

                        // Type field: Select dropdown of TreatmentBMPType IDs. Pre-resolve
                        // the extracted name string to a TreatmentBMPTypeID via case-insensitive
                        // match; staff confirms or corrects via dropdown if no match.
                        if (attr === "TreatmentBMPType") {
                            field.fieldType = FormFieldType.Select;
                            field.selectOptions = bmpTypeOptions;
                            const rawName = extracted?.Value as string | null | undefined;
                            if (rawName) {
                                const match = bmpTypeOptions.find(
                                    (o) => o.Label.toLowerCase() === rawName.toLowerCase()
                                );
                                field.value = match ? String(match.Value) : null;
                            }
                        } else if (
                            attr === "NumberOfIndividualBMPs" ||
                            attr === "PercentOfSiteTreated" ||
                            attr === "PercentCaptured" ||
                            attr === "PercentRetained"
                        ) {
                            field.fieldType = FormFieldType.Number;
                        }

                        allFields.push(field);
                    }
                });
            }

            // Jurisdiction + WQMP Name are user-entered in the upload modal — they are
            // authoritative and don't need review. Override the AI-extracted values, mark
            // the fields accepted, stamp them as user-origin so the review page renders
            // "User-entered" rather than "AI-extracted" pills and skips the evidence chrome.
            const current = this.currentResult();
            const confirmedJurisdictionID = current?.StormwaterJurisdictionID;
            if (confirmedJurisdictionID) {
                const jurisdictionField = allFields.find((f) => f.key === "Jurisdiction");
                if (jurisdictionField) {
                    const confirmedValue = String(confirmedJurisdictionID);
                    jurisdictionField.value = confirmedValue;
                    jurisdictionField.acceptedValue = confirmedValue;
                    jurisdictionField.state = "accepted";
                    jurisdictionField.confidence = "high";
                    jurisdictionField.isUserEntered = true;
                    jurisdictionField.evidence = null;
                    jurisdictionField.source = null;
                    jurisdictionField.boundingBox = null;
                }
            }
            const confirmedName = current?.WaterQualityManagementPlanName;
            if (confirmedName) {
                const nameField = allFields.find((f) => f.key === "WaterQualityManagementPlanName");
                if (nameField) {
                    nameField.value = confirmedName;
                    nameField.acceptedValue = confirmedName;
                    nameField.state = "accepted";
                    nameField.confidence = "high";
                    nameField.isUserEntered = true;
                    nameField.evidence = null;
                    nameField.source = null;
                    nameField.boundingBox = null;
                }
            }

            this.fields.set(allFields);
        } catch {
            this.fields.set([]);
        }
    }

    private makeField(key: string, extracted: any, step: number): ExtractedField {
        const rawValue = extracted?.Value ?? null;
        const evidence = extracted?.ExtractionEvidence ?? null;
        const source = extracted?.DocumentSource ?? null;
        const boundingBox = this.parseBoundingBox(extracted?.BoundingBox);
        const lookupConfig = this.lookupFieldConfig[key];

        let value = rawValue;
        let fieldType: FormFieldType | undefined;
        let selectOptions: SelectDropdownOption[] | undefined;
        let mask: string | undefined;

        if (lookupConfig) {
            fieldType = FormFieldType.Select;
            selectOptions = lookupConfig.options;
            // Resolve extracted text name to the matching option's ID
            if (rawValue) {
                const match = lookupConfig.options.find((o) => o.Label.toLowerCase() === rawValue.toLowerCase());
                value = match ? String(match.Value) : null;
            }
        } else if (key === "MaintenanceContactState") {
            // Canonical US states picklist — AI may return a full name or abbreviation;
            // coerce to the 2-letter value for storage.
            fieldType = FormFieldType.Select;
            selectOptions = US_STATES;
            if (rawValue) {
                const trimmed = rawValue.trim();
                const match = US_STATES.find(
                    (s) => s.Value === trimmed.toUpperCase() || s.Label.toLowerCase() === trimmed.toLowerCase()
                );
                value = match ? String(match.Value) : null;
            }
        } else if (key === "ApprovalDate" || key === "DateOfConstruction") {
            fieldType = FormFieldType.Date;
            // Normalize extracted dates to yyyy-mm-dd for the date input
            if (rawValue) {
                const parsed = new Date(rawValue);
                if (!isNaN(parsed.getTime())) {
                    value = parsed.toISOString().slice(0, 10);
                }
            }
        } else if (key === "RecordedWQMPAreaInAcres") {
            fieldType = FormFieldType.Number;
        } else if (key === "MaintenanceContactPhone") {
            // Format any raw 10-digit AI capture as (NNN) NNN-NNNN for display; mask enforces
            // it on user edits too.
            mask = "(000) 000-0000";
            if (rawValue) {
                const digits = rawValue.replace(/\D/g, "");
                if (digits.length === 10) {
                    value = `(${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6)}`;
                }
            }
        } else if (key === "MaintenanceContactZip") {
            // Accept either 5-digit or ZIP+4. The separator pattern makes both shapes work.
            mask = "00000||00000-0000";
        }

        return {
            key,
            label: this.fieldLabels[key] || key,
            value,
            evidence,
            source,
            boundingBox,
            confidence: this.inferConfidence(rawValue, evidence, source),
            step,
            state: "pending",
            fieldType,
            selectOptions,
            mask,
        };
    }

    private parseBoundingBox(box: any): EvidenceBoundingBox | null {
        if (box === null || box === undefined || typeof box !== "object") return null;
        const { PageNumber, X, Y, Width, Height } = box;
        // All five fields must be finite numbers, normalized coords within [0, 1], and
        // PageNumber must be a positive integer. Reject anything partial so we don't draw
        // a garbage box from a half-populated result.
        if ([PageNumber, X, Y, Width, Height].some((n) => typeof n !== "number" || !isFinite(n))) return null;
        if (PageNumber < 1 || !Number.isInteger(PageNumber)) return null;
        if (X < 0 || X > 1 || Y < 0 || Y > 1 || Width <= 0 || Width > 1 || Height <= 0 || Height > 1) return null;
        return { PageNumber, X, Y, Width, Height };
    }

    private inferConfidence(value: string | null, evidence: string | null, source: string | null): ConfidenceLevel {
        if (!value) return "none";
        if (evidence && source) return "high";
        if (evidence || source) return "medium";
        return "low";
    }
}
