import { Component, computed, inject, Input, numberAttribute, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
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
import { SchemaPreviewComponent } from "src/app/shared/observation-types/schema-preview.component";
import { PassFailSchemaBuilderComponent } from "./schema-builders/pass-fail-schema-builder.component";
import { DiscreteSchemaBuilderComponent } from "./schema-builders/discrete-schema-builder.component";
import { PercentageSchemaBuilderComponent } from "./schema-builders/percentage-schema-builder.component";

/**
 * Routed full-page editor for a TreatmentBMPAssessmentObservationType, mirroring the legacy MVC
 * Edit.cshtml layout (three card sections: Name & Collection, Data Collection Instructions, Labels
 * and Units). Backs both /manage/observation-types/new (create) and
 * /manage/observation-types/:observationTypeID/edit. Replaces the prior modal-based editor that
 * compressed all three sections into a 800px-wide dialog.
 */
@Component({
    selector: "observation-type-edit",
    standalone: true,
    imports: [
        RouterLink, ReactiveFormsModule, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent,
        PassFailSchemaBuilderComponent, DiscreteSchemaBuilderComponent, PercentageSchemaBuilderComponent,
        SchemaPreviewComponent,
    ],
    templateUrl: "./observation-type-edit.component.html",
    styleUrl: "./observation-type-edit.component.scss",
})
export class ObservationTypeEditComponent implements OnInit {
    @Input({ transform: numberAttribute }) observationTypeID?: number;

    private observationTypeService = inject(TreatmentBMPAssessmentObservationTypeService);
    private alertService = inject(AlertService);
    private router = inject(Router);

    public FormFieldType = FormFieldType;
    public isEdit = false;
    public isLoading = signal(false);
    public isSaving = signal(false);
    public showPreview = signal(false);

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

    public passFailSchema = signal<PassFailSchema>(emptyPassFailSchema());
    public discreteSchema = signal<DiscreteValueSchema>(emptyDiscreteValueSchema());
    public percentageSchema = signal<PercentageSchema>(emptyPercentageSchema());

    public derivedSpecID = computed(() => tripleToSpec({
        CollectionMethodID: this.formGroup.controls.CollectionMethodID.value,
        TargetTypeID: this.formGroup.controls.TargetTypeID.value,
        ThresholdTypeID: this.formGroup.controls.ThresholdTypeID.value,
    }));

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.isEdit = this.observationTypeID != null;

        // Track collection method changes — when switching, reset schemas and snap Target/Threshold
        // to the only valid pair for PassFail.
        this.formGroup.controls.CollectionMethodID.valueChanges.subscribe((cmID) => {
            const previous = this.collectionMethodID();
            this.collectionMethodID.set(cmID ?? null);
            if (previous !== cmID) {
                this.passFailSchema.set(emptyPassFailSchema());
                this.discreteSchema.set(emptyDiscreteValueSchema());
                this.percentageSchema.set(emptyPercentageSchema());
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
                            if (cm === "PassFail") this.passFailSchema.set(parsed);
                            else if (cm === "DiscreteValue") this.discreteSchema.set(parsed);
                            else if (cm === "Percentage") this.percentageSchema.set(parsed);
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
        if (cm === "PassFail") return JSON.stringify(this.passFailSchema());
        if (cm === "DiscreteValue") return JSON.stringify(this.discreteSchema());
        if (cm === "Percentage") return JSON.stringify(this.percentageSchema());
        return undefined;
    }

    save(): void {
        if (this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            return;
        }
        const raw = this.formGroup.getRawValue();
        const specID = tripleToSpec({
            CollectionMethodID: raw.CollectionMethodID,
            TargetTypeID: raw.TargetTypeID,
            ThresholdTypeID: raw.ThresholdTypeID,
        });
        if (!specID) return;
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

        save$.subscribe({
            next: () => {
                this.isSaving.set(false);
                this.alertService.pushAlert(new Alert(`Observation type ${this.isEdit ? "updated" : "created"}.`, AlertContext.Success));
                this.router.navigate(["/manage/observation-types"]);
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

    togglePreview(): void {
        this.showPreview.set(!this.showPreview());
    }
}
