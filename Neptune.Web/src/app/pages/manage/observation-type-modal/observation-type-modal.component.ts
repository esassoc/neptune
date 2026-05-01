import { Component, computed, inject, OnInit, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
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

@Component({
    selector: "observation-type-modal",
    standalone: true,
    imports: [
        ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent,
        PassFailSchemaBuilderComponent, DiscreteSchemaBuilderComponent,
        PercentageSchemaBuilderComponent, SchemaPreviewComponent,
    ],
    templateUrl: "./observation-type-modal.component.html",
    styles: [`.preview-toggle { display: flex; justify-content: flex-end; margin-bottom: 0.5rem; }`],
})
export class ObservationTypeModalComponent implements OnInit {
    public ref: DialogRef<{ mode: "add" | "edit"; observationTypeID?: number }, boolean> = inject(DialogRef);
    private observationTypeService = inject(TreatmentBMPAssessmentObservationTypeService);
    private alertService = inject(AlertService);

    public FormFieldType = FormFieldType;
    public mode: "add" | "edit";
    public isEdit = false;
    public isLoading = signal(false);
    public showPreview = signal(false);

    public formGroup = new FormGroup({
        TreatmentBMPAssessmentObservationTypeName: new FormControl<string>("", { validators: [Validators.required, Validators.maxLength(100)], nonNullable: true }),
        CollectionMethodID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        TargetTypeID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        ThresholdTypeID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
    });

    // Top-level Collection Method drives whether Target/Threshold are visible and which schema builder renders.
    public collectionMethodOptions: SelectDropdownOption[] = ObservationTypeCollectionMethods.map((x) => ({ Value: x.Value, Label: x.DisplayName, disabled: false }));

    // Target/Threshold options exclude the PassFail/None pseudo-values (those only apply to PassFail collection method,
    // which we handle by collapsing the dropdowns rather than offering them).
    public targetTypeOptions: SelectDropdownOption[] = ObservationTargetTypes
        .filter((x) => x.Value !== ObservationTargetTypeEnum.PassFail)
        .map((x) => ({ Value: x.Value, Label: x.DisplayName, disabled: false }));
    public thresholdTypeOptions: SelectDropdownOption[] = ObservationThresholdTypes
        .filter((x) => x.Value !== ObservationThresholdTypeEnum.None)
        .map((x) => ({ Value: x.Value, Label: x.DisplayName, disabled: false }));

    public collectionMethodID = signal<number | null>(null);
    public collectionMethod = computed<CollectionMethodType | null>(() => collectionMethodIDToName(this.collectionMethodID()));
    public isPassFail = computed(() => this.collectionMethodID() === ObservationTypeCollectionMethodEnum.PassFail);

    // Schema state per collection method
    public passFailSchema = signal<PassFailSchema>(emptyPassFailSchema());
    public discreteSchema = signal<DiscreteValueSchema>(emptyDiscreteValueSchema());
    public percentageSchema = signal<PercentageSchema>(emptyPercentageSchema());

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.mode = this.ref.data.mode;
        this.isEdit = this.mode === "edit";

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

        if (this.isEdit && this.ref.data.observationTypeID) {
            this.isLoading.set(true);
            this.observationTypeService.getTreatmentBMPAssessmentObservationType(this.ref.data.observationTypeID).subscribe({
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

    public derivedSpecID = computed(() => tripleToSpec({
        CollectionMethodID: this.formGroup.controls.CollectionMethodID.value,
        TargetTypeID: this.formGroup.controls.TargetTypeID.value,
        ThresholdTypeID: this.formGroup.controls.ThresholdTypeID.value,
    }));

    save(): void {
        if (this.formGroup.invalid) return;
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

        const save$ = this.isEdit
            ? this.observationTypeService.updateTreatmentBMPAssessmentObservationType(this.ref.data.observationTypeID, dto)
            : this.observationTypeService.createTreatmentBMPAssessmentObservationType(dto);

        save$.subscribe({
            next: () => this.ref.close(true),
            error: () => {
                this.alertService.pushAlert(new Alert(`Failed to ${this.isEdit ? "update" : "create"} observation type.`, AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.ref.close(null);
    }
}
