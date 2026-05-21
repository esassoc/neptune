import { Component, computed, inject, Input, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogService } from "@ngneat/dialog";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { TreatmentBMPAssessmentObservationTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-detail-dto";
import { TreatmentBMPAssessmentObservationTypeUpsertDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-upsert-dto";
import { ObservationTypeCollectionMethodEnum, ObservationTypeCollectionMethods } from "src/app/shared/generated/enum/observation-type-collection-method-enum";
import { ObservationTargetTypeEnum, ObservationTargetTypes } from "src/app/shared/generated/enum/observation-target-type-enum";
import { ObservationThresholdTypeEnum, ObservationThresholdTypes } from "src/app/shared/generated/enum/observation-threshold-type-enum";
import {
    CollectionMethodType, collectionMethodIDToName, specToTriple, tripleToSpec,
    DiscreteValueSchema, PassFailSchema, PercentageSchema,
    emptyDiscreteValueSchema, emptyPassFailSchema, emptyPercentageSchema,
} from "src/app/shared/observation-types/schema-types";
import {
    ObservationTypePreviewModalComponent,
    ObservationTypePreviewModalData,
} from "src/app/shared/observation-types/observation-type-preview-modal.component";
import { PassFailSchemaBuilderComponent, PassFailSchemaFormGroup, buildPassFailSchemaFormGroup } from "./schema-builders/pass-fail-schema-builder.component";
import { DiscreteSchemaBuilderComponent, DiscreteSchemaFormGroup, buildDiscreteSchemaFormGroup } from "./schema-builders/discrete-schema-builder.component";
import { PercentageSchemaBuilderComponent, PercentageSchemaFormGroup, buildPercentageSchemaFormGroup } from "./schema-builders/percentage-schema-builder.component";

/**
 * Routed full-page editor for a TreatmentBMPAssessmentObservationType, mirroring the legacy MVC
 * Edit.cshtml layout (three card sections: Name & Collection, Data Collection Instructions, Labels
 * and Units). Backs both /manage/observation-types/new (create) and
 * /manage/observation-types/:observationTypeID/edit.
 *
 * Each collection method owns its own reactive FormGroup for the schema fields — those groups are
 * built by the schema-builder modules and passed back into them via [formGroup]. The active group
 * (selected by the current Collection Method) is what gets serialized on Save.
 */
@Component({
    selector: "observation-type-edit",
    standalone: true,
    imports: [
        RouterLink, ReactiveFormsModule, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent,
        PassFailSchemaBuilderComponent, DiscreteSchemaBuilderComponent, PercentageSchemaBuilderComponent,
    ],
    templateUrl: "./observation-type-edit.component.html",
    styleUrl: "./observation-type-edit.component.scss",
})
export class ObservationTypeEditComponent implements OnInit {
    @Input() observationTypeID?: number;

    private observationTypeService = inject(TreatmentBMPAssessmentObservationTypeService);
    private alertService = inject(AlertService);
    private router = inject(Router);
    private dialogService = inject(DialogService);

    public FormFieldType = FormFieldType;
    public isEdit = false;
    public isLoading = signal(false);
    public isSaving = signal(false);

    public formGroup = new FormGroup({
        TreatmentBMPAssessmentObservationTypeName: new FormControl<string>("", { validators: [Validators.required, Validators.maxLength(100)], nonNullable: true }),
        CollectionMethodID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        TargetTypeID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        ThresholdTypeID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
    });

    public collectionMethodOptions: SelectDropdownOption[] = ObservationTypeCollectionMethods.map((x) => ({ Value: x.Value, Label: x.DisplayName, disabled: false }));

    // Filter the PassFail/None pseudo-values out of the Target / Threshold dropdowns — those only
    // apply to PassFail collection method, which we handle by collapsing the dropdowns.
    public targetTypeOptions: SelectDropdownOption[] = ObservationTargetTypes
        .filter((x) => x.Value !== ObservationTargetTypeEnum.PassFail)
        .map((x) => ({ Value: x.Value, Label: x.DisplayName, disabled: false }));
    public thresholdTypeOptions: SelectDropdownOption[] = ObservationThresholdTypes
        .filter((x) => x.Value !== ObservationThresholdTypeEnum.None)
        .map((x) => ({ Value: x.Value, Label: x.DisplayName, disabled: false }));

    public collectionMethodID = signal<number | null>(null);
    public collectionMethod = computed<CollectionMethodType | null>(() => collectionMethodIDToName(this.collectionMethodID()));
    public isPassFail = computed(() => this.collectionMethodID() === ObservationTypeCollectionMethodEnum.PassFail);

    // Per-collection-method schema FormGroups; the schema-builder children bind their <form-field>
    // [formControl]s into these groups so PropertiesToObserve and instruction text are reactive
    // alongside the top-level Name / Collection Method form controls.
    public passFailSchemaGroup: PassFailSchemaFormGroup = buildPassFailSchemaFormGroup(emptyPassFailSchema());
    public discreteSchemaGroup: DiscreteSchemaFormGroup = buildDiscreteSchemaFormGroup(emptyDiscreteValueSchema());
    public percentageSchemaGroup: PercentageSchemaFormGroup = buildPercentageSchemaFormGroup(emptyPercentageSchema());

    // Kept as a method (not a computed signal) on purpose — it reads formGroup.controls[*].value,
    // which are plain getters, not signals. A computed would cache its initial null forever and
    // never re-evaluate on form changes. Templates call methods on every CD cycle anyway.
    public derivedSpecID(): number | null {
        return tripleToSpec({
            CollectionMethodID: this.formGroup.controls.CollectionMethodID.value,
            TargetTypeID: this.formGroup.controls.TargetTypeID.value,
            ThresholdTypeID: this.formGroup.controls.ThresholdTypeID.value,
        });
    }

    ngOnInit(): void {
        this.alertService.clearAlerts();
        // Coerce the route-string param to a real number before any equality / Falsy checks —
        // withComponentInputBinding passes route params as strings, and the "/new" route binds no
        // observationTypeID at all (was previously becoming NaN via numberAttribute and falsely
        // tripping isEdit + a doomed GET).
        const id = this.observationTypeID != null ? Number(this.observationTypeID) : undefined;
        this.observationTypeID = Number.isFinite(id) ? id : undefined;
        this.isEdit = this.observationTypeID != null;

        // Track collection method changes — when switching, reset schemas and snap Target/Threshold
        // to the only valid pair for PassFail.
        this.formGroup.controls.CollectionMethodID.valueChanges.subscribe((cmID) => {
            const previous = this.collectionMethodID();
            this.collectionMethodID.set(cmID ?? null);
            if (previous !== cmID) {
                this.passFailSchemaGroup.reset(emptyPassFailSchema());
                this.discreteSchemaGroup.reset(emptyDiscreteValueSchema());
                this.percentageSchemaGroup.reset(emptyPercentageSchema());
            }
            if (cmID === ObservationTypeCollectionMethodEnum.PassFail) {
                this.formGroup.controls.TargetTypeID.setValue(ObservationTargetTypeEnum.PassFail, { emitEvent: false });
                this.formGroup.controls.ThresholdTypeID.setValue(ObservationThresholdTypeEnum.None, { emitEvent: false });
            } else if (
                this.formGroup.controls.TargetTypeID.value === ObservationTargetTypeEnum.PassFail ||
                this.formGroup.controls.ThresholdTypeID.value === ObservationThresholdTypeEnum.None
            ) {
                // Coming back from PassFail — clear so admin picks a real Target/Threshold.
                this.formGroup.controls.TargetTypeID.setValue(undefined, { emitEvent: false });
                this.formGroup.controls.ThresholdTypeID.setValue(undefined, { emitEvent: false });
            }
        });

        if (this.isEdit) {
            this.isLoading.set(true);
            this.observationTypeService.getTreatmentBMPAssessmentObservationType(this.observationTypeID).subscribe({
                next: (detail: TreatmentBMPAssessmentObservationTypeDetailDto) => {
                    const triple = specToTriple(detail.ObservationTypeSpecificationID);
                    this.formGroup.patchValue({
                        TreatmentBMPAssessmentObservationTypeName: detail.TreatmentBMPAssessmentObservationTypeName,
                        CollectionMethodID: triple?.CollectionMethodID,
                        TargetTypeID: triple?.TargetTypeID,
                        ThresholdTypeID: triple?.ThresholdTypeID,
                    });
                    this.collectionMethodID.set(triple?.CollectionMethodID ?? null);

                    const cm = collectionMethodIDToName(triple?.CollectionMethodID);
                    if (detail.TreatmentBMPAssessmentObservationTypeSchema) {
                        try {
                            const parsed = JSON.parse(detail.TreatmentBMPAssessmentObservationTypeSchema);
                            if (cm === "PassFail") this.passFailSchemaGroup.reset(parsed);
                            else if (cm === "DiscreteValue") this.discreteSchemaGroup.reset(parsed);
                            else if (cm === "Percentage") this.percentageSchemaGroup.reset(parsed);
                        } catch { /* invalid JSON — keep defaults */ }
                    }
                    this.isLoading.set(false);
                },
                error: () => {
                    this.isLoading.set(false);
                    this.alertService.pushAlert(new Alert("Failed to load observation type.", AlertContext.Danger));
                },
            });
        }
    }

    private serializeSchema(): string | undefined {
        const cm = this.collectionMethod();
        if (cm === "PassFail") return JSON.stringify(this.passFailSchemaGroup.getRawValue() as PassFailSchema);
        if (cm === "DiscreteValue") return JSON.stringify(this.discreteSchemaGroup.getRawValue() as DiscreteValueSchema);
        if (cm === "Percentage") return JSON.stringify(this.percentageSchemaGroup.getRawValue() as PercentageSchema);
        return undefined;
    }

    /** Currently-active schema form group, used by the template for `[disabled]` validity gating
     * on the Save button so the user can't submit with an invalid schema half-filled in. */
    public activeSchemaGroup(): FormGroup | null {
        const cm = this.collectionMethod();
        if (cm === "PassFail") return this.passFailSchemaGroup;
        if (cm === "DiscreteValue") return this.discreteSchemaGroup;
        if (cm === "Percentage") return this.percentageSchemaGroup;
        return null;
    }

    save(): void {
        const active = this.activeSchemaGroup();
        if (this.formGroup.invalid || (active && active.invalid)) {
            this.formGroup.markAllAsTouched();
            active?.markAllAsTouched();
            this.alertService.pushAlert(new Alert("Please complete the highlighted required fields before saving.", AlertContext.Danger));
            return;
        }
        const raw = this.formGroup.getRawValue();
        const specID = tripleToSpec({
            CollectionMethodID: raw.CollectionMethodID,
            TargetTypeID: raw.TargetTypeID,
            ThresholdTypeID: raw.ThresholdTypeID,
        });
        if (!specID) {
            this.alertService.pushAlert(new Alert("Pick a Collection Method, Target Type, and Threshold Type before saving.", AlertContext.Danger));
            return;
        }
        const schema = this.serializeSchema();

        const dto: TreatmentBMPAssessmentObservationTypeUpsertDto = {
            TreatmentBMPAssessmentObservationTypeName: raw.TreatmentBMPAssessmentObservationTypeName,
            ObservationTypeSpecificationID: specID,
            TreatmentBMPAssessmentObservationTypeSchema: schema,
        };

        this.isSaving.set(true);
        this.alertService.clearAlerts();
        const save$ = this.isEdit
            ? this.observationTypeService.updateTreatmentBMPAssessmentObservationType(this.observationTypeID, dto)
            : this.observationTypeService.createTreatmentBMPAssessmentObservationType(dto);

        const successMessage = `Observation type ${this.isEdit ? "updated" : "created"}.`;
        save$.subscribe({
            next: () => {
                this.isSaving.set(false);
                // Push the alert AFTER navigation completes so it survives the editor's
                // <app-alert-display>.ngOnDestroy (which clears alerts on unmount by default).
                this.router.navigate(["/manage/observation-types"]).then(() => {
                    this.alertService.pushAlert(new Alert(successMessage, AlertContext.Success));
                });
            },
            error: () => {
                this.isSaving.set(false);
                this.alertService.pushAlert(new Alert(`Failed to ${this.isEdit ? "update" : "create"} observation type.`, AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.router.navigate(["/manage/observation-types"]);
    }

    /** Open the shared preview modal against a snapshot of the live schema-builder values, so admins
     * see exactly what assessors will see in the Field Visit Workflow. Mirrors the detail page's
     * openPreview so both entry points share one rendering path. */
    openPreview(): void {
        const cm = this.collectionMethod();
        const data: ObservationTypePreviewModalData = {
            observationTypeName: this.formGroup.controls.TreatmentBMPAssessmentObservationTypeName.value ?? "",
            collectionMethod: cm,
            passFailSchema: cm === "PassFail" ? this.passFailSchemaGroup.getRawValue() as PassFailSchema : null,
            discreteSchema: cm === "DiscreteValue" ? this.discreteSchemaGroup.getRawValue() as DiscreteValueSchema : null,
            percentageSchema: cm === "Percentage" ? this.percentageSchemaGroup.getRawValue() as PercentageSchema : null,
        };
        this.dialogService.open(ObservationTypePreviewModalComponent, { data, width: "700px" });
    }
}
