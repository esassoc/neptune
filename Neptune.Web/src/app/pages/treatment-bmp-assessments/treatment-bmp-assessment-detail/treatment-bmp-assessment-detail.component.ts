import { Component, inject, Input } from "@angular/core";
import { AsyncPipe, DatePipe, DecimalPipe } from "@angular/common";
import { Router, RouterLink } from "@angular/router";
import { BehaviorSubject, catchError, forkJoin, Observable, of, switchMap } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { ImageCarouselComponent, ImageCarouselItem } from "src/app/shared/components/image-carousel/image-carousel.component";

import { TreatmentBMPAssessmentService } from "src/app/shared/generated/api/treatment-bmp-assessment.service";
import { TreatmentBMPAssessmentPhotoService } from "src/app/shared/generated/api/treatment-bmp-assessment-photo.service";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { TreatmentBMPAssessmentDetailDto } from "src/app/shared/generated/model/treatment-bmp-assessment-detail-dto";
import { TreatmentBMPAssessmentObservationTypeForFormDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-for-form-dto";
import { TreatmentBMPAssessmentScoreDetailDto } from "src/app/shared/generated/model/treatment-bmp-assessment-score-detail-dto";
import { TreatmentBMPObservationDto } from "src/app/shared/generated/model/treatment-bmp-observation-dto";
import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";

import { AuthenticationService } from "src/app/services/authentication.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/** One row per "property observed" inside a single observation-type card. Read-only sibling of
 * the editor's `ObservationPanelProperty` — value/notes are pre-formatted display strings. */
export interface ObservationDetailRow {
    propertyObserved: string;
    displayValue: string;
    notes: string;
}

/** Read-only view-model for a single observation type. Mirrors the editor's `ObservationTypePanel`
 * but holds display strings (one row per property) instead of FormControls. Built from the
 * assessment DTO's `ObservationTypes` + `Observations` + parsed schema JSON. */
export interface ObservationDetailPanel {
    observationTypeID: number;
    name: string;
    collectionMethod: "DiscreteValue" | "PassFail" | "Percentage" | string;
    measurementUnitLabel: string;
    valueColumnLabel: string;
    observationScore: string | null;
    rows: ObservationDetailRow[];
}

interface AssessmentDetailVm {
    assessment: TreatmentBMPAssessmentDetailDto;
    workflow: FieldVisitWorkflowDto;
    photos: ImageCarouselItem[];
    isInitial: boolean;
    panels: ObservationDetailPanel[];
    scoreDetail: TreatmentBMPAssessmentScoreDetailDto;
    canEdit: boolean;
}

/**
 * NPT-1056: SPA port of the legacy MVC `TreatmentBMPAssessment/Detail.cshtml`. Surfaces a
 * single assessment (initial or post-maintenance) — header, photos, full per-observation
 * scoring breakdown (Threshold / Observed / Benchmark / Weight / Score), per-observation-
 * type cards (2-up grid mirroring legacy), and (manager-gated) Edit + Delete actions.
 *
 * Score-detail data comes from a dedicated `/score-detail` endpoint that materializes the
 * benchmark/threshold graph server-side; the detail DTO alone doesn't carry those fields.
 */
@Component({
    selector: "treatment-bmp-assessment-detail",
    standalone: true,
    imports: [AsyncPipe, DatePipe, DecimalPipe, RouterLink, PageHeaderComponent, AlertDisplayComponent, LoadingDirective, ImageCarouselComponent],
    templateUrl: "./treatment-bmp-assessment-detail.component.html",
    styleUrl: "./treatment-bmp-assessment-detail.component.scss",
})
export class TreatmentBmpAssessmentDetailComponent {
    private assessmentService = inject(TreatmentBMPAssessmentService);
    private assessmentPhotoService = inject(TreatmentBMPAssessmentPhotoService);
    private fieldVisitService = inject(FieldVisitService);
    private authenticationService = inject(AuthenticationService);
    private confirmService = inject(ConfirmService);
    private alertService = inject(AlertService);
    private router = inject(Router);

    @Input() treatmentBMPAssessmentID!: number;

    private reload$ = new BehaviorSubject<void>(undefined);

    public vm$: Observable<AssessmentDetailVm | null> = this.reload$.pipe(
        switchMap(() =>
            this.assessmentService.getByIDTreatmentBMPAssessment(this.treatmentBMPAssessmentID).pipe(
                switchMap((assessment) => {
                    const fieldVisitID = assessment.FieldVisitID;
                    if (fieldVisitID == null) return of(null as AssessmentDetailVm | null);
                    return forkJoin({
                        workflow: this.fieldVisitService.getByIDFieldVisit(fieldVisitID),
                        photos: this.assessmentPhotoService.listTreatmentBMPAssessmentPhoto(assessment.TreatmentBMPAssessmentID!),
                        scoreDetail: this.assessmentService.getScoreDetailTreatmentBMPAssessment(assessment.TreatmentBMPAssessmentID!),
                    }).pipe(
                        switchMap(({ workflow, photos, scoreDetail }) => {
                            this.isLoading = false;
                            const isInitial = workflow.InitialAssessmentID === assessment.TreatmentBMPAssessmentID;
                            const carouselItems: ImageCarouselItem[] = (photos ?? []).map((p) => ({
                                FileResourceGUID: p.FileResourceGUID ?? undefined,
                                Caption: p.Caption ?? null,
                            }));
                            const panels = this.buildPanels(assessment);
                            // Edit button gate mirrors the legacy MVC `DetailViewData.CanEdit` —
                            // editor-level permission plus the assessment must not be complete
                            // yet (a complete assessment is locked until someone toggles it back).
                            const canEdit = this.canEditOrDelete && !assessment.IsAssessmentComplete;
                            return of({ assessment, workflow, photos: carouselItems, isInitial, panels, scoreDetail, canEdit } satisfies AssessmentDetailVm);
                        }),
                    );
                }),
                catchError(() => {
                    this.isLoading = false;
                    return of(null as AssessmentDetailVm | null);
                }),
            ),
        ),
    );

    public isLoading = true;

    public get canManage(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    /** Edit + Delete gate. Mirrors the API's `[JurisdictionEditFeature]` on the Delete endpoint
     * (and the legacy MVC `[TreatmentBMPAssessmentManageFeature]`), which allows Editor in
     * addition to Manager. The frontend doesn't enforce the BMP-jurisdiction match here — the
     * API does — so a user with the right role in any jurisdiction will see the button and get
     * a 403 if they don't actually manage this BMP's jurisdiction. */
    public get canEditOrDelete(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    }

    public editAssessmentRoute(vm: AssessmentDetailVm): (string | number)[] {
        // Edit routes back into the existing workflow observations step rather than duplicating
        // the form here. The workflow distinguishes initial vs post-maintenance by route segment,
        // not query param, so pick based on which slot the assessment fills.
        const segment = vm.isInitial ? "assessment" : "post-maintenance-assessment";
        return ["/field-visits", vm.workflow.FieldVisitID!, segment, "observations"];
    }

    /** Builds the per-type display panels. One panel per observation type the BMP type
     * declares (regardless of whether observation data has been recorded), so that an empty
     * type still renders an "Observation data not entered" placeholder per the legacy view. */
    private buildPanels(assessment: TreatmentBMPAssessmentDetailDto): ObservationDetailPanel[] {
        const types = assessment.ObservationTypes ?? [];
        const observations = assessment.Observations ?? [];
        return types.map((t) => this.buildPanel(t, observations));
    }

    private buildPanel(typeForForm: TreatmentBMPAssessmentObservationTypeForFormDto, observations: TreatmentBMPObservationDto[]): ObservationDetailPanel {
        const schema = this.parseSchema(typeForForm.TreatmentBMPAssessmentObservationTypeSchema);
        const properties: string[] = Array.isArray(schema?.PropertiesToObserve) ? schema.PropertiesToObserve : [];
        const collectionMethod = typeForForm.ObservationTypeCollectionMethodName ?? "";
        const measurementUnitLabel: string = (schema?.MeasurementUnitLabel ?? "").toString().trim();
        const passingLabel: string = (schema?.PassingScoreLabel ?? "Pass").toString();
        const failingLabel: string = (schema?.FailingScoreLabel ?? "Fail").toString();

        const observation = observations.find((o) => o.TreatmentBMPAssessmentObservationTypeID === typeForForm.TreatmentBMPAssessmentObservationTypeID);
        const singleValues = this.parseObservationData(observation?.ObservationData);

        // Build one row per property declared in the schema — keeps the row order stable even
        // when an assessor didn't record a value for one of the properties.
        const rows: ObservationDetailRow[] = properties.map((prop) => {
            const recorded = singleValues.find((v) => v?.PropertyObserved === prop);
            return {
                propertyObserved: prop,
                displayValue: this.formatValue(recorded?.ObservationValue, collectionMethod, measurementUnitLabel, passingLabel, failingLabel),
                notes: (recorded?.Notes ?? "").toString().trim(),
            };
        });

        // Pick a value-column header that matches the legacy view: "Observed Value" for
        // DiscreteValue/PassFail, the per-type unit label for Percentage (e.g., "% Trapped").
        const valueColumnLabel = collectionMethod === "Percentage" && measurementUnitLabel ? measurementUnitLabel : "Observed Value";

        return {
            observationTypeID: typeForForm.TreatmentBMPAssessmentObservationTypeID!,
            name: typeForForm.TreatmentBMPAssessmentObservationTypeName ?? "",
            collectionMethod,
            measurementUnitLabel,
            valueColumnLabel,
            observationScore: observation?.ObservationScore?.toString() ?? null,
            rows,
        };
    }

    private parseSchema(schemaJson: string | null | undefined): any {
        if (!schemaJson) return {};
        try {
            return JSON.parse(schemaJson);
        } catch {
            return {};
        }
    }

    private parseObservationData(observationData: string | null | undefined): Array<{ PropertyObserved: string; ObservationValue: any; Notes?: string }> {
        if (!observationData) return [];
        try {
            const parsed = JSON.parse(observationData);
            return Array.isArray(parsed?.SingleValueObservations) ? parsed.SingleValueObservations : [];
        } catch {
            return [];
        }
    }

    /** Formats a single observation value for the per-property row. Mirrors the legacy
     * rendering: "{value} {unit}" for DiscreteValue, "{value}%" for Percentage, and the
     * configured Pass/Fail labels (which can be custom per-type, e.g. "Working"/"Broken").
     * Returns "—" when no value was recorded. */
    private formatValue(value: unknown, collectionMethod: string, unit: string, passingLabel: string, failingLabel: string): string {
        if (value == null || value === "") return "—";
        if (collectionMethod === "PassFail") {
            if (value === true || value === "true") return passingLabel;
            if (value === false || value === "false") return failingLabel;
            return "—";
        }
        if (collectionMethod === "Percentage") {
            return `${value}%`;
        }
        if (collectionMethod === "DiscreteValue" && unit) {
            return `${value} ${unit}`;
        }
        return String(value);
    }

    public delete(vm: AssessmentDetailVm): void {
        this.confirmService
            .confirm({
                title: "Delete Assessment",
                message:
                    "Delete this Treatment BMP Assessment? Observations and photos attached to this assessment will also be deleted. This cannot be undone.",
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.assessmentService.deleteTreatmentBMPAssessment(vm.assessment.TreatmentBMPAssessmentID!).subscribe({
                    next: () => {
                        this.alertService.pushAlert(new Alert("Assessment deleted.", AlertContext.Success));
                        this.router.navigate(["/treatment-bmps", vm.assessment.TreatmentBMPID]);
                    },
                    error: () => this.alertService.pushAlert(new Alert("Failed to delete Assessment.", AlertContext.Danger)),
                });
            });
    }
}
