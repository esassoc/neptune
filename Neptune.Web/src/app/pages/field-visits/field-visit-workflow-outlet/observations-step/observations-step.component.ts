import { Component, Input, OnInit, signal } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, ReactiveFormsModule, ValidatorFn, Validators } from "@angular/forms";
import { Observable, of, switchMap, take } from "rxjs";

import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";

import { TreatmentBMPAssessmentByFieldVisitService } from "src/app/shared/generated/api/treatment-bmp-assessment-by-field-visit.service";
import { TreatmentBMPAssessmentService } from "src/app/shared/generated/api/treatment-bmp-assessment.service";
import { TreatmentBMPAssessmentDetailDto } from "src/app/shared/generated/model/treatment-bmp-assessment-detail-dto";
import { TreatmentBMPAssessmentObservationTypeForFormDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-for-form-dto";
import { TreatmentBMPObservationDto } from "src/app/shared/generated/model/treatment-bmp-observation-dto";
import { TreatmentBMPAssessmentUpsertDto } from "src/app/shared/generated/model/treatment-bmp-assessment-upsert-dto";
import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";

import { FieldVisitWorkflowService } from "../../services/field-visit-workflow.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/**
 * Observations editor: dynamically renders a form panel per observation type
 * applicable to the BMP type. Each panel contains a row per "property to observe"
 * (from the observation-type's JSON schema). The input rendered for each property
 * varies by collection method:
 *   - DiscreteValue → number input with optional min/max from the schema
 *   - PassFail → Pass/Fail radio
 *   - Percentage → number input clamped 0–100
 *
 * The saved data shape on the wire matches the legacy contract:
 *   { SingleValueObservations: [{ PropertyObserved, ObservationValue, Notes }] }
 * serialized as a JSON string in TreatmentBMPObservation.ObservationData.
 */

interface PropertyControl {
    propertyObserved: string;
    valueControl: FormControl<string | null>;
    notesControl: FormControl<string | null>;
    /** PassFail only — Pass/Fail dropdown options */
    passFailOptions?: SelectDropdownOption[];
}

interface ObservationTypePanel {
    observationTypeID: number;
    name: string;
    collectionMethod: "DiscreteValue" | "PassFail" | "Percentage" | string;
    measurementUnitLabel?: string;
    minValue?: number;
    maxValue?: number;
    assessmentDescription?: string;
    benchmarkDescription?: string;
    thresholdDescription?: string;
    passingLabel?: string;
    failingLabel?: string;
    properties: PropertyControl[];
}

@Component({
    selector: "field-visit-observations-step",
    standalone: true,
    imports: [AsyncPipe, ReactiveFormsModule, LoadingDirective, PageHeaderComponent, FormFieldComponent],
    templateUrl: "./observations-step.component.html",
    styleUrl: "./observations-step.component.scss",
})
export class FieldVisitObservationsStepComponent implements OnInit {
    /** 1 = Initial, 2 = PostMaintenance — passed via route data. */
    @Input() assessmentTypeID: number = 1;

    public workflow$: Observable<FieldVisitWorkflowDto | null>;
    // Signals — plain fields don't reliably trigger CD when mutated inside subscribe callbacks
    // under zoneless behavior, leaving the spinner stuck until a stray click forces a render.
    public assessment = signal<TreatmentBMPAssessmentDetailDto | null>(null);
    public panels = signal<ObservationTypePanel[]>([]);
    public isLoading = signal(true);
    public FormFieldType = FormFieldType;

    constructor(
        private workflowService: FieldVisitWorkflowService,
        private assessmentByFieldVisitService: TreatmentBMPAssessmentByFieldVisitService,
        private assessmentService: TreatmentBMPAssessmentService,
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private router: Router
    ) {}

    public get isPostMaintenance(): boolean {
        return this.assessmentTypeID === 2;
    }

    public get headerLabel(): string {
        // Single-word page title — the sidebar already says which assessment is active,
        // so no need to repeat "Initial / Post-Maintenance Assessment" in the page header.
        return "Observations";
    }

    ngOnInit(): void {
        this.workflow$ = this.workflowService.workflow$;
        this.workflowService.clearStepAlerts();
        this.workflow$
            .pipe(
                take(1),
                switchMap((workflow) => {
                    if (!workflow) return of(null);
                    return this.assessmentByFieldVisitService.getByTypeTreatmentBMPAssessmentByFieldVisit(workflow.FieldVisitID, this.assessmentTypeID);
                })
            )
            .subscribe((assessment) => {
                this.assessment.set(assessment);
                this.panels.set(
                    (assessment?.ObservationTypes ?? [])
                        .map((t) => this.buildPanel(t, assessment?.Observations ?? []))
                        .filter((p): p is ObservationTypePanel => p != null)
                );
                this.isLoading.set(false);
            });
    }

    private buildPanel(typeForForm: TreatmentBMPAssessmentObservationTypeForFormDto, observations: TreatmentBMPObservationDto[]): ObservationTypePanel | null {
        const schemaObj = this.parseSchema(typeForForm.TreatmentBMPAssessmentObservationTypeSchema);
        const properties: string[] = (schemaObj?.PropertiesToObserve as string[]) ?? [];
        if (properties.length === 0) {
            // Without properties to observe, the legacy form has nothing to render either; skip.
            return null;
        }

        const existingObservation = observations.find((o) => o.TreatmentBMPAssessmentObservationTypeID === typeForForm.TreatmentBMPAssessmentObservationTypeID);
        const existingValues = this.parseObservationData(existingObservation?.ObservationData);

        const collectionMethod = typeForForm.ObservationTypeCollectionMethodName ?? "";
        const passingLabel: string | undefined = schemaObj?.PassingScoreLabel;
        const failingLabel: string | undefined = schemaObj?.FailingScoreLabel;

        const passFailOptions: SelectDropdownOption[] | undefined =
            collectionMethod === "PassFail"
                ? [
                      { Value: "true", Label: passingLabel || "Pass", disabled: false },
                      { Value: "false", Label: failingLabel || "Fail", disabled: false },
                  ]
                : undefined;

        const minValue = schemaObj?.MinimumValueOfObservations;
        const maxValue = schemaObj?.MaximumValueOfObservations;
        // Range validators replace the previous DOM-level min/max attrs (lost when we migrated to
        // <form-field>, which doesn't surface min/max on the underlying number input). Validation
        // now lives on the FormControl so formGroup.invalid + Save-button state reflect it.
        const valueValidators = this.buildValueValidators(collectionMethod, minValue, maxValue);

        return {
            observationTypeID: typeForForm.TreatmentBMPAssessmentObservationTypeID,
            name: typeForForm.TreatmentBMPAssessmentObservationTypeName,
            collectionMethod,
            measurementUnitLabel: schemaObj?.MeasurementUnitLabel,
            minValue,
            maxValue,
            assessmentDescription: schemaObj?.AssessmentDescription,
            benchmarkDescription: schemaObj?.BenchmarkDescription,
            thresholdDescription: schemaObj?.ThresholdDescription,
            passingLabel,
            failingLabel,
            properties: properties.map((prop) => {
                const existing = existingValues.find((v) => v.PropertyObserved === prop);
                return {
                    propertyObserved: prop,
                    valueControl: new FormControl<string | null>(this.formatExisting(existing?.ObservationValue), { validators: valueValidators }),
                    notesControl: new FormControl<string | null>(existing?.Notes ?? ""),
                    passFailOptions,
                };
            }),
        };
    }

    private buildValueValidators(collectionMethod: string, minValue: number | undefined, maxValue: number | undefined): ValidatorFn[] {
        if (collectionMethod === "Percentage") {
            return [Validators.min(0), Validators.max(100)];
        }
        if (collectionMethod === "DiscreteValue") {
            const validators: ValidatorFn[] = [];
            if (typeof minValue === "number") validators.push(Validators.min(minValue));
            if (typeof maxValue === "number") validators.push(Validators.max(maxValue));
            return validators;
        }
        return [];
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
            return parsed?.SingleValueObservations ?? [];
        } catch {
            return [];
        }
    }

    private formatExisting(value: any): string {
        if (value === null || value === undefined) return "";
        if (typeof value === "boolean") return value ? "true" : "false";
        return String(value);
    }

    save(workflow: FieldVisitWorkflowDto, nextAction: "stay" | "continue" | "wrap-up"): void {
        const assessment = this.assessment();
        if (!assessment) return;

        this.workflowService.clearStepAlerts();

        // Touch every value control so out-of-range / range-validator errors surface in form-field
        // before we bail out. Notes have no validators, so no need to touch them.
        let hasInvalid = false;
        for (const panel of this.panels()) {
            for (const prop of panel.properties) {
                prop.valueControl.markAsTouched();
                if (prop.valueControl.invalid) hasInvalid = true;
            }
        }
        if (hasInvalid) {
            this.alertService.pushAlert(new Alert("One or more observations are out of range. Please review and correct before saving.", AlertContext.Danger));
            return;
        }

        const observations = this.panels().map((panel) => {
            const observationData = JSON.stringify({
                SingleValueObservations: panel.properties.map((p) => ({
                    PropertyObserved: p.propertyObserved,
                    ObservationValue: this.coerceValue(panel.collectionMethod, p.valueControl.value),
                    Notes: p.notesControl.value ?? null,
                })),
            });
            return {
                TreatmentBMPAssessmentObservationTypeID: panel.observationTypeID,
                ObservationData: observationData,
            };
        });

        const dto = new TreatmentBMPAssessmentUpsertDto({ Observations: observations });

        this.assessmentService.upsertObservationsTreatmentBMPAssessment(assessment.TreatmentBMPAssessmentID, dto).subscribe(() => {
            this.alertService.pushAlert(new Alert("Observations saved.", AlertContext.Success));
            this.workflowService.refresh().subscribe(() => {
                if (nextAction === "continue") {
                    const next = this.isPostMaintenance ? "summary" : "maintenance";
                    this.router.navigate(["/field-visits", workflow.FieldVisitID, next]);
                } else if (nextAction === "wrap-up") {
                    this.workflowService.wrapUpVisit(workflow.FieldVisitID);
                }
            });
        });
    }

    /**
     * For PassFail/DiscreteValue/Percentage, persist the right primitive type
     * inside ObservationData so the C# scoring helpers can parse it. Match the
     * legacy contract: PassFail stores boolean, others store numeric or null.
     */
    private coerceValue(collectionMethod: string, raw: string | null): boolean | number | null {
        if (raw === null || raw === undefined || raw === "") return null;
        if (collectionMethod === "PassFail") {
            return raw === "true";
        }
        const num = Number(raw);
        return Number.isFinite(num) ? num : null;
    }

    copyFromInitial(workflow: FieldVisitWorkflowDto): void {
        const assessment = this.assessment();
        if (!assessment) return;
        this.confirmService
            .confirm({
                title: "Copy data from Initial Assessment?",
                message: "This will overwrite the post-maintenance observations with the values entered for the Initial Assessment. Continue?",
                buttonClassYes: "btn btn-primary",
                buttonTextYes: "Copy",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.assessmentService.copyFromInitialTreatmentBMPAssessment(assessment.TreatmentBMPAssessmentID).subscribe((updated) => {
                    this.assessment.set(updated);
                    this.panels.set(
                        (updated?.ObservationTypes ?? [])
                            .map((t) => this.buildPanel(t, updated?.Observations ?? []))
                            .filter((p): p is ObservationTypePanel => p != null)
                    );
                    this.alertService.pushAlert(new Alert("Copied observations from Initial Assessment.", AlertContext.Success));
                    this.workflowService.refresh().subscribe();
                });
            });
    }
}
