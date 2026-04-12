import { Component, inject, Input, OnInit, signal, ViewChild } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { HttpClient } from "@angular/common/http";
import { EMPTY, forkJoin, map, Observable, switchMap, tap, shareReplay, catchError, of } from "rxjs";
import { PdfJsViewerModule, PdfJsViewerComponent } from "ng2-pdfjs-viewer";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { WaterQualityManagementPlanExtractionResultDto } from "src/app/shared/generated/model/water-quality-management-plan-extraction-result-dto";
import { FieldCardComponent, SourceNavigation } from "src/app/pages/wqmps/wqmp-detail/wqmp-review/field-card/field-card.component";
import { WaterQualityManagementPlanPrioritiesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-priority-enum";
import { WaterQualityManagementPlanDevelopmentTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-development-type-enum";
import { WaterQualityManagementPlanLandUsesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-land-use-enum";
import { WaterQualityManagementPlanPermitTermsAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-permit-term-enum";
import { HydromodificationAppliesTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/hydromodification-applies-type-enum";
import { TrashCaptureStatusTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/trash-capture-status-type-enum";
import { environment } from "src/environments/environment";

export type ConfidenceLevel = "high" | "medium" | "low" | "none";

export interface ExtractedField {
    key: string;
    label: string;
    value: string | null;
    evidence: string | null;
    source: string | null;
    confidence: ConfidenceLevel;
    step: number;
    acceptedValue?: string | null;
    state: "pending" | "accepted" | "edited" | "rejected";
    fieldType?: FormFieldType;
    selectOptions?: SelectDropdownOption[];
}

@Component({
    selector: "wqmp-review",
    standalone: true,
    imports: [AlertDisplayComponent, FieldCardComponent, PdfJsViewerModule, RouterLink, AsyncPipe],
    templateUrl: "./wqmp-review.component.html",
    styleUrl: "./wqmp-review.component.scss",
})
export class WqmpReviewComponent implements OnInit {
    @Input() waterQualityManagementPlanID!: number;

    @ViewChild("pdfViewer") pdfViewer: PdfJsViewerComponent;

    private router = inject(Router);
    private http = inject(HttpClient);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private jurisdictionService = inject(StormwaterJurisdictionService);
    private alertService = inject(AlertService);

    public FormFieldType = FormFieldType;
    public extractionResult$: Observable<WaterQualityManagementPlanExtractionResultDto>;

    // Lookup field configuration: maps extraction key → dropdown options + DTO field name
    private lookupFieldConfig: Record<string, { options: SelectDropdownOption[]; dtoField: string }> = {};
    public currentStep = signal(1);
    public fields = signal<ExtractedField[]>([]);
    public pdfBlob: Blob | null = null;
    public isApplying = false;
    public isNavigating = signal(false);

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

        this.extractionResult$ = forkJoin([jurisdictions$, hydrologicSubareas$]).pipe(
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
            switchMap(() => this.wqmpService.getExtractionResultWaterQualityManagementPlan(this.waterQualityManagementPlanID)),
            tap((result) => {
                this.parseExtractionResult(result.ExtractionResultJson);
            }),
            switchMap((result) =>
                this.http.get(`${environment.mainAppApiUrl}/file-resources/${result.FileResourceGuid}`, { responseType: "blob" }).pipe(
                    tap((blob) => {
                        this.pdfBlob = blob;
                    }),
                    switchMap(() => [result])
                )
            ),
            catchError(() => {
                this.alertService.pushAlert(new Alert("Failed to load extraction results or PDF document.", AlertContext.Danger));
                return EMPTY;
            }),
            shareReplay(1)
        );
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

    async navigateToSource(nav: SourceNavigation): Promise<void> {
        if (!this.pdfViewer) return;
        this.isNavigating.set(true);

        try {
            this.clearHighlights();

            // Try text search first using the evidence snippet
            if (nav.evidence) {
                const foundPage = await this.searchPdfForText(nav.evidence, nav.documentSource);
                if (foundPage > 0) {
                    this.pdfViewer.page = foundPage;
                    // Wait for page render then highlight
                    setTimeout(() => this.highlightTextOnPage(foundPage, nav.evidence), 500);
                    return;
                }
            }

            // Fall back to the page number from DocumentSource
            if (nav.documentSource) {
                const match = nav.documentSource.match(/page\s*(\d+)/i);
                if (match) {
                    this.pdfViewer.page = parseInt(match[1], 10);
                }
            }
        } finally {
            this.isNavigating.set(false);
        }
    }

    private async searchPdfForText(evidence: string, documentSource: string | null): Promise<number> {
        try {
            const iframe = this.pdfViewer.iframe?.nativeElement as HTMLIFrameElement;
            const pdfApp = (iframe?.contentWindow as any)?.PDFViewerApplication;
            if (!pdfApp?.pdfDocument) return 0;

            const pdfDoc = pdfApp.pdfDocument;
            const totalPages = pdfDoc.numPages;
            const normalizedEvidence = this.normalizeText(evidence);

            // Extract a meaningful search phrase from the evidence (use the middle portion for best specificity)
            const searchPhrase = this.extractSearchPhrase(normalizedEvidence);
            if (!searchPhrase) return 0;

            const matches: number[] = [];
            for (let i = 1; i <= totalPages; i++) {
                const page = await pdfDoc.getPage(i);
                const textContent = await page.getTextContent();
                const pageText = this.normalizeText(textContent.items.map((item: any) => item.str).join(" "));
                if (pageText.includes(searchPhrase)) {
                    matches.push(i);
                }
            }

            if (matches.length === 0) return 0;
            if (matches.length === 1) return matches[0];

            // Multiple matches — pick the one closest to the DocumentSource page hint
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

    private normalizeText(text: string): string {
        return text.toLowerCase().replace(/\s+/g, " ").trim();
    }

    private clearHighlights(): void {
        try {
            const iframe = this.pdfViewer?.iframe?.nativeElement as HTMLIFrameElement;
            const iframeDoc = iframe?.contentDocument || iframe?.contentWindow?.document;
            if (!iframeDoc) return;
            iframeDoc.querySelectorAll(".wqmp-highlight").forEach((el) => el.remove());
        } catch { /* ignore */ }
    }

    private highlightTextOnPage(pageNum: number, evidence: string): void {
        try {
            const iframe = this.pdfViewer?.iframe?.nativeElement as HTMLIFrameElement;
            const iframeDoc = iframe?.contentDocument || iframe?.contentWindow?.document;
            if (!iframeDoc) return;

            const pageEl = iframeDoc.querySelector(`.page[data-page-number="${pageNum}"]`) as HTMLElement;
            if (!pageEl) return;

            const textLayer = pageEl.querySelector(".textLayer");
            if (!textLayer) return;

            const searchPhrase = this.extractSearchPhrase(this.normalizeText(evidence));
            if (!searchPhrase) return;

            // Find all matching spans and compute a bounding box around them
            const spans = textLayer.querySelectorAll("span");
            const matchingRects: DOMRect[] = [];
            const pageRect = pageEl.getBoundingClientRect();

            for (const span of Array.from(spans)) {
                const spanText = this.normalizeText(span.textContent || "");
                if (spanText && (searchPhrase.includes(spanText) || spanText.includes(searchPhrase))) {
                    matchingRects.push(span.getBoundingClientRect());
                }
            }

            if (matchingRects.length === 0) return;

            // Compute a single bounding box around all matched spans
            const minLeft = Math.min(...matchingRects.map((r) => r.left)) - pageRect.left;
            const minTop = Math.min(...matchingRects.map((r) => r.top)) - pageRect.top;
            const maxRight = Math.max(...matchingRects.map((r) => r.right)) - pageRect.left;
            const maxBottom = Math.max(...matchingRects.map((r) => r.bottom)) - pageRect.top;

            const padding = 4;
            const pageWidth = pageEl.offsetWidth;
            const box = iframeDoc.createElement("div");
            box.className = "wqmp-highlight";
            Object.assign(box.style, {
                position: "absolute",
                left: "0px",
                top: `${minTop - padding}px`,
                width: `${pageWidth}px`,
                height: `${maxBottom - minTop + padding * 2}px`,
                border: "2px solid #f59e0b",
                backgroundColor: "rgba(255, 235, 59, 0.12)",
                pointerEvents: "none",
                zIndex: "10",
                borderRadius: "4px",
            });
            pageEl.appendChild(box);
            box.scrollIntoView({ behavior: "smooth", block: "center" });
        } catch { /* ignore */ }
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

    applyToWqmp(): void {
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

                return this.wqmpService.updateWaterQualityManagementPlan(this.waterQualityManagementPlanID, dto);
            })
        ).subscribe({
            next: () => {
                this.isApplying = false;
                this.alertService.pushAlert(new Alert("WQMP updated with extracted data.", AlertContext.Success));
                this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
            },
            error: () => {
                this.isApplying = false;
                this.alertService.pushAlert(new Alert("An error occurred while applying changes.", AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
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

            this.fields.set(allFields);
        } catch {
            this.fields.set([]);
        }
    }

    private makeField(key: string, extracted: any, step: number): ExtractedField {
        const rawValue = extracted?.Value ?? null;
        const evidence = extracted?.ExtractionEvidence ?? null;
        const source = extracted?.DocumentSource ?? null;
        const lookupConfig = this.lookupFieldConfig[key];

        let value = rawValue;
        let fieldType: FormFieldType | undefined;
        let selectOptions: SelectDropdownOption[] | undefined;

        if (lookupConfig) {
            fieldType = FormFieldType.Select;
            selectOptions = lookupConfig.options;
            // Resolve extracted text name to the matching option's ID
            if (rawValue) {
                const match = lookupConfig.options.find((o) => o.Label.toLowerCase() === rawValue.toLowerCase());
                value = match ? String(match.Value) : null;
            }
        }

        return {
            key,
            label: this.fieldLabels[key] || key,
            value,
            evidence,
            source,
            confidence: this.inferConfidence(rawValue, evidence, source),
            step,
            state: "pending",
            fieldType,
            selectOptions,
        };
    }

    private inferConfidence(value: string | null, evidence: string | null, source: string | null): ConfidenceLevel {
        if (!value) return "none";
        if (evidence && source) return "high";
        if (evidence || source) return "medium";
        return "low";
    }
}
