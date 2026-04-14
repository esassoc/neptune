import { Component, inject, OnInit, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { TreatmentBMPAssessmentObservationTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-detail-dto";
import { TreatmentBMPAssessmentObservationTypeUpsertDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-upsert-dto";
import { ObservationTypeSpecificationsAsSelectDropdownOptions } from "src/app/shared/generated/enum/observation-type-specification-enum";
import {
    CollectionMethodType, getCollectionMethod,
    DiscreteValueSchema, PassFailSchema, PercentageSchema,
    emptyDiscreteValueSchema, emptyPassFailSchema, emptyPercentageSchema,
} from "./schema-types";
import { PassFailSchemaBuilderComponent } from "./schema-builders/pass-fail-schema-builder.component";
import { DiscreteSchemaBuilderComponent } from "./schema-builders/discrete-schema-builder.component";
import { PercentageSchemaBuilderComponent } from "./schema-builders/percentage-schema-builder.component";
import { SchemaPreviewComponent } from "./schema-preview.component";

@Component({
    selector: "observation-type-modal",
    standalone: true,
    imports: [
        ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent,
        PassFailSchemaBuilderComponent, DiscreteSchemaBuilderComponent,
        PercentageSchemaBuilderComponent, SchemaPreviewComponent,
    ],
    templateUrl: "./observation-type-modal.component.html",
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
        ObservationTypeSpecificationID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
    });

    public specificationOptions = ObservationTypeSpecificationsAsSelectDropdownOptions;
    public collectionMethod = signal<CollectionMethodType | null>(null);

    // Schema state per collection method
    public passFailSchema = signal<PassFailSchema>(emptyPassFailSchema());
    public discreteSchema = signal<DiscreteValueSchema>(emptyDiscreteValueSchema());
    public percentageSchema = signal<PercentageSchema>(emptyPercentageSchema());

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.mode = this.ref.data.mode;
        this.isEdit = this.mode === "edit";

        // Watch for specification changes to update the collection method
        this.formGroup.controls.ObservationTypeSpecificationID.valueChanges.subscribe((specID) => {
            const cm = getCollectionMethod(specID);
            if (cm !== this.collectionMethod()) {
                this.collectionMethod.set(cm);
                // Reset schemas when switching collection method
                this.passFailSchema.set(emptyPassFailSchema());
                this.discreteSchema.set(emptyDiscreteValueSchema());
                this.percentageSchema.set(emptyPercentageSchema());
            }
        });

        if (this.isEdit && this.ref.data.observationTypeID) {
            this.isLoading.set(true);
            this.observationTypeService.getTreatmentBMPAssessmentObservationType(this.ref.data.observationTypeID).subscribe({
                next: (detail: TreatmentBMPAssessmentObservationTypeDetailDto) => {
                    this.formGroup.patchValue({
                        TreatmentBMPAssessmentObservationTypeName: detail.TreatmentBMPAssessmentObservationTypeName,
                        ObservationTypeSpecificationID: detail.ObservationTypeSpecificationID,
                    });

                    // Parse existing schema into the correct builder
                    const cm = getCollectionMethod(detail.ObservationTypeSpecificationID);
                    this.collectionMethod.set(cm);
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
                error: () => this.isLoading.set(false),
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
        if (this.formGroup.invalid) return;
        const raw = this.formGroup.getRawValue();
        const schema = this.serializeSchema();

        const dto: TreatmentBMPAssessmentObservationTypeUpsertDto = {
            TreatmentBMPAssessmentObservationTypeName: raw.TreatmentBMPAssessmentObservationTypeName,
            ObservationTypeSpecificationID: raw.ObservationTypeSpecificationID,
            TreatmentBMPAssessmentObservationTypeSchema: schema,
        };

        const save$ = this.isEdit
            ? this.observationTypeService.updateTreatmentBMPAssessmentObservationType(this.ref.data.observationTypeID, dto)
            : this.observationTypeService.createTreatmentBMPAssessmentObservationType(dto);

        save$.subscribe({
            next: () => this.ref.close(true),
            error: () => {},
        });
    }

    cancel(): void {
        this.ref.close(null);
    }
}
