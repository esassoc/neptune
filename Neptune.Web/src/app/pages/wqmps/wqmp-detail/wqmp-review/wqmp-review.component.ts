import { Component, computed, inject, Input, OnInit, signal, ViewChild, ViewContainerRef } from "@angular/core";
import { DatePipe } from "@angular/common";
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
import { ParcelService } from "src/app/shared/generated/api/parcel.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { WaterQualityManagementPlanExtractionResultDto } from "src/app/shared/generated/model/water-quality-management-plan-extraction-result-dto";
import { EvidenceBoundingBox, FieldCardComponent, SourceNavigation } from "src/app/pages/wqmps/wqmp-detail/wqmp-review/field-card/field-card.component";
import { IDeactivateComponent } from "src/app/shared/guards/unsaved-changes.guard";
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

interface DraftOverlayEntry {
    state: "accepted" | "edited" | "rejected";
    value?: string | null;
}

@Component({
    selector: "wqmp-review",
    standalone: true,
    imports: [AlertDisplayComponent, FieldCardComponent, PdfJsViewerModule, RouterLink, AsyncPipe, DatePipe],
    templateUrl: "./wqmp-review.component.html",
    styleUrl: "./wqmp-review.component.scss",
})
export class WqmpReviewComponent implements OnInit, IDeactivateComponent {
    @Input() waterQualityManagementPlanID!: number;

    @ViewChild("pdfViewer") pdfViewer: PdfJsViewerComponent;

    private router = inject(Router);
    private http = inject(HttpClient);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private parcelService = inject(ParcelService);
    private jurisdictionService = inject(StormwaterJurisdictionService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private viewContainerRef = inject(ViewContainerRef);

    // Prefix used on ExtractedField.key for parcel rows. Reusing the field-card machinery
    // (accept/edit/reject/navigate) keeps the UX consistent and lets draft-overlay keep
    // working unchanged. Filter by this prefix to distinguish parcels from single-value fields.
    public readonly PARCEL_KEY_PREFIX = "__Parcel__-";
    private userParcelCounter = 0;

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
    public isApplying = false;
    public isSavingDraft = false;
    public hasUnsavedChanges = signal(false);
    public isApproved = computed(() => !!this.currentResult()?.ApprovedDate);
    public isNavigating = signal(false);
    // Tracks whether the PDF.js iframe has fully loaded — until then, evidence clicks can't
    // reach PDFViewerApplication.pdfDocument. If the user clicks early, queue the request here
    // and replay once onDocumentLoad fires.
    private pdfLoaded = false;
    private pendingNavigation: SourceNavigation | null = null;
    // After `navigateToSource` picks a target page, this holds the details of the box we
    // want to draw there. `onPdfPageRendered` fires every time PDF.js finishes rendering a
    // page; we reinspect the DOM at that point and draw. Keying off the render event means
    // we succeed even when the target page is far off-screen and renders async.
    //
    // `box` (from Claude when it read the page as an image), when present, short-circuits
    // to a precise draw. Otherwise `searchPhrase` drives a text-layer span match, and a null
    // searchPhrase falls back to a page-level outline.
    private pendingHighlight: { pageNumber: number; box: EvidenceBoundingBox | null; searchPhrase: string | null } | null = null;


    public steps = [
        { number: 1, title: "Location", desc: "Jurisdiction & parcels" },
        { number: 2, title: "Basics", desc: "Project details & contacts" },
        { number: 3, title: "BMPs", desc: "Treatment controls", disabled: true },
        { number: 4, title: "Review", desc: "Final review & submit", disabled: true },
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

        this.pageData$ = forkJoin([jurisdictions$, hydrologicSubareas$]).pipe(
            tap(([jurisdictions, hydrologicSubareas]) => {
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
                    this.applyDraftOverlay(extractionResult.DraftOverlayJson ?? null);
                } else {
                    this.fields.set([]);
                }
                this.hasUnsavedChanges.set(false);
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
        if (this.currentResult()?.ApprovedDate) return;
        const confirmed = await this.confirmService.confirm({
            title: "Re-run extraction?",
            message: "This will replace the existing extraction result and discard any unsaved draft edits.",
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
        return this.fields().filter((f) => f.step === this.currentStep());
    }

    get confirmedCount(): number {
        return this.fields().filter((f) => f.state === "accepted" || f.state === "edited").length;
    }

    get totalFieldCount(): number {
        return this.fields().length;
    }

    onPdfLoaded(): void {
        this.pdfLoaded = true;
        // Replay any click that arrived before the iframe finished initializing.
        if (this.pendingNavigation) {
            const nav = this.pendingNavigation;
            this.pendingNavigation = null;
            this.navigateToSource(nav);
        }
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
                const page = await pdfDoc.getPage(i);
                const textContent = await page.getTextContent();
                const pageText = this.normalizeText(textContent.items.map((item: any) => item.str).join(" "));
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

    onFieldAccepted(field: ExtractedField, value: string | null): void {
        field.state = "accepted";
        field.acceptedValue = value;
        this.fields.update((f) => [...f]);
        this.hasUnsavedChanges.set(true);
    }

    onFieldEdited(field: ExtractedField, value: string): void {
        field.state = "edited";
        field.acceptedValue = value;
        this.fields.update((f) => [...f]);
        this.hasUnsavedChanges.set(true);
    }

    onFieldRejected(field: ExtractedField): void {
        field.state = "rejected";
        field.acceptedValue = null;
        this.fields.update((f) => [...f]);
        this.hasUnsavedChanges.set(true);
    }

    // Add a blank user-entered parcel row to the Location step. The reviewer edits the APN
    // in the new row's edit form and hits the row's check mark to accept it; the approve-
    // and-apply flow resolves the APN to a ParcelID via POST /parcels/lookup-by-numbers.
    addParcel(): void {
        if (this.isApproved()) return;
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
        this.hasUnsavedChanges.set(true);
    }

    // origin reflects the source of the field's *value*, not any reviewer action.
    // The template checks field state first for "edited"/"rejected" pills, then falls back
    // to origin. When a reviewer "accepts" an AI value, origin stays "ai" — they're
    // confirming the source, not editing the value.
    getFieldOrigin(field: ExtractedField): FieldOrigin {
        if (field.isUserEntered) return "user";
        return field.value ? "ai" : "blank";
    }

    saveDraft(): void {
        if (this.isApproved()) return;
        this.isSavingDraft = true;
        this.alertService.clearAlerts();
        const overlayJson = this.buildDraftOverlayJson();
        this.wqmpService
            .saveExtractionResultDraftWaterQualityManagementPlan(this.waterQualityManagementPlanID, { DraftOverlayJson: overlayJson })
            .subscribe({
                next: () => {
                    this.isSavingDraft = false;
                    this.hasUnsavedChanges.set(false);
                    this.alertService.pushAlert(new Alert("Draft saved.", AlertContext.Success));
                    this.reload$.next();
                },
                error: () => {
                    this.isSavingDraft = false;
                    this.alertService.pushAlert(new Alert("Failed to save draft.", AlertContext.Danger));
                },
            });
    }

    discardDraft(): void {
        if (this.isApproved()) return;
        if (!confirm("Discard all reviewer edits and revert to AI-extracted values?")) return;
        this.alertService.clearAlerts();
        this.wqmpService
            .clearExtractionResultDraftWaterQualityManagementPlan(this.waterQualityManagementPlanID)
            .subscribe({
                next: () => {
                    this.hasUnsavedChanges.set(false);
                    this.alertService.pushAlert(new Alert("Draft discarded.", AlertContext.Success));
                    this.reload$.next();
                },
                error: () => {
                    this.alertService.pushAlert(new Alert("Failed to discard draft.", AlertContext.Danger));
                },
            });
    }

    approveAndApply(): void {
        if (this.isApproved()) return;
        this.isApplying = true;
        this.alertService.clearAlerts();

        // Fetch the existing WQMP first to preserve required fields, then merge accepted text fields
        this.wqmpService.getWaterQualityManagementPlan(this.waterQualityManagementPlanID).pipe(
            switchMap((existing) => {
                const dto: any = {
                    WaterQualityManagementPlanName: existing.WaterQualityManagementPlanName,
                    StormwaterJurisdictionID: existing.StormwaterJurisdictionID,
                    WaterQualityManagementPlanStatusID: existing.WaterQualityManagementPlanStatusID,
                    WaterQualityManagementPlanModelingApproachID: existing.WaterQualityManagementPlanModelingApproachID,
                    TrashCaptureStatusTypeID: existing.TrashCaptureStatusTypeID,
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
                };

                // Overlay accepted/edited text fields
                const textFields: Record<string, string> = {
                    WaterQualityManagementPlanName: "WaterQualityManagementPlanName",
                    RecordNumber: "RecordNumber",
                    MaintenanceContactName: "MaintenanceContactName",
                    MaintenanceContactOrganization: "MaintenanceContactOrganization",
                    MaintenanceContactPhone: "MaintenanceContactPhone",
                    MaintenanceContactAddress1: "MaintenanceContactAddress1",
                    MaintenanceContactAddress2: "MaintenanceContactAddress2",
                    MaintenanceContactCity: "MaintenanceContactCity",
                    MaintenanceContactState: "MaintenanceContactState",
                    MaintenanceContactZip: "MaintenanceContactZip",
                };
                for (const field of this.fields()) {
                    // Parcels are persisted via a separate endpoint after the main approve
                    // succeeds — skip them here.
                    if (field.key.startsWith(this.PARCEL_KEY_PREFIX)) continue;
                    if (field.state !== "accepted" && field.state !== "edited") continue;
                    if (field.acceptedValue == null) continue;

                    // Text fields
                    if (textFields[field.key]) {
                        dto[textFields[field.key]] = field.acceptedValue;
                        continue;
                    }

                    // Lookup fields (value is already an ID from the dropdown)
                    const lookupConfig = this.lookupFieldConfig[field.key];
                    if (lookupConfig) {
                        dto[lookupConfig.dtoField] = Number(field.acceptedValue);
                        continue;
                    }
                }

                // Handle numeric fields
                const acresField = this.fields().find((f) => f.key === "RecordedWQMPAreaInAcres");
                if (acresField && (acresField.state === "accepted" || acresField.state === "edited") && acresField.acceptedValue) {
                    const parsed = parseFloat(acresField.acceptedValue);
                    if (!isNaN(parsed)) dto.RecordedWQMPAreaInAcres = parsed;
                }

                // Handle date fields
                for (const dateKey of ["ApprovalDate", "DateOfConstruction"]) {
                    const dateField = this.fields().find((f) => f.key === dateKey);
                    if (dateField && (dateField.state === "accepted" || dateField.state === "edited") && dateField.acceptedValue) {
                        dto[dateKey] = dateField.acceptedValue;
                    }
                }

                return this.wqmpService.approveExtractionResultWaterQualityManagementPlan(this.waterQualityManagementPlanID, dto).pipe(
                    switchMap(() => this.syncAcceptedParcels())
                );
            })
        ).subscribe({
            next: () => {
                this.isApplying = false;
                this.hasUnsavedChanges.set(false);
                this.alertService.pushAlert(new Alert("Review approved and applied to WQMP.", AlertContext.Success));
                this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
            },
            error: () => {
                this.isApplying = false;
                this.alertService.pushAlert(new Alert("An error occurred while applying changes.", AlertContext.Danger));
            },
        });
    }

    // After the main approve/apply lands, collect the accepted parcel APN strings, resolve
    // them to ParcelIDs, and PUT the list to the WQMP. Returns an Observable that completes
    // whether or not there are parcels to sync. Missing APNs surface as a warning alert.
    private syncAcceptedParcels(): Observable<unknown> {
        const apns = this.fields()
            .filter((f) => f.key.startsWith(this.PARCEL_KEY_PREFIX))
            .filter((f) => f.state === "accepted" || f.state === "edited")
            .map((f) => (f.acceptedValue ?? f.value ?? "").trim())
            .filter((apn) => apn.length > 0);

        if (apns.length === 0) {
            // User rejected / blanked all parcels — clear them on the WQMP too.
            return this.wqmpService.updateParcelsWaterQualityManagementPlan(this.waterQualityManagementPlanID, []);
        }

        return this.parcelService.lookupByNumbersParcel(apns).pipe(
            switchMap((results) => {
                const ids = results
                    .filter((r) => r.ParcelID != null)
                    .map((r) => r.ParcelID!);
                const missing = results.filter((r) => r.ParcelID == null).map((r) => r.ParcelNumber);
                if (missing.length > 0) {
                    this.alertService.pushAlert(
                        new Alert(
                            `The following APNs were not found in the parcel database and were skipped: ${missing.join(", ")}.`,
                            AlertContext.Warning
                        )
                    );
                }
                return this.wqmpService.updateParcelsWaterQualityManagementPlan(this.waterQualityManagementPlanID, ids);
            })
        );
    }

    cancel(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
    }

    private buildDraftOverlayJson(): string {
        const overlay: Record<string, DraftOverlayEntry> = {};
        for (const field of this.fields()) {
            if (field.state === "pending") continue;
            if (field.state === "rejected") {
                overlay[field.key] = { state: "rejected" };
            } else {
                overlay[field.key] = { state: field.state, value: field.acceptedValue ?? null };
            }
        }
        return JSON.stringify(overlay);
    }

    private applyDraftOverlay(overlayJson: string | null): void {
        if (!overlayJson) return;
        try {
            const overlay = JSON.parse(overlayJson) as Record<string, DraftOverlayEntry>;
            const updated = this.fields().map((field) => {
                const entry = overlay[field.key];
                if (!entry) return field;
                return {
                    ...field,
                    state: entry.state,
                    acceptedValue: entry.state === "rejected" ? null : entry.value ?? null,
                };
            });
            this.fields.set(updated);
        } catch {
            // Ignore malformed overlay — fall back to AI-extracted values
        }
    }

    private parseExtractionResult(json: string): void {
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
