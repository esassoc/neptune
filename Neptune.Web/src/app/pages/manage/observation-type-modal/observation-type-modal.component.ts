import { Component, inject, OnInit, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { TreatmentBMPAssessmentObservationTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-detail-dto";
import { TreatmentBMPAssessmentObservationTypeUpsertDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-upsert-dto";
import { ObservationTypeSpecificationsAsSelectDropdownOptions } from "src/app/shared/generated/enum/observation-type-specification-enum";

@Component({
    selector: "observation-type-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent],
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

    public formGroup = new FormGroup({
        TreatmentBMPAssessmentObservationTypeName: new FormControl<string>("", { validators: [Validators.required, Validators.maxLength(100)], nonNullable: true }),
        ObservationTypeSpecificationID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        TreatmentBMPAssessmentObservationTypeSchema: new FormControl<string>("", { nonNullable: true }),
    });

    public specificationOptions = ObservationTypeSpecificationsAsSelectDropdownOptions;

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.mode = this.ref.data.mode;
        this.isEdit = this.mode === "edit";

        if (this.isEdit && this.ref.data.observationTypeID) {
            this.isLoading.set(true);
            this.observationTypeService.getTreatmentBMPAssessmentObservationType(this.ref.data.observationTypeID).subscribe({
                next: (detail: TreatmentBMPAssessmentObservationTypeDetailDto) => {
                    this.formGroup.patchValue({
                        TreatmentBMPAssessmentObservationTypeName: detail.TreatmentBMPAssessmentObservationTypeName,
                        ObservationTypeSpecificationID: detail.ObservationTypeSpecificationID,
                        TreatmentBMPAssessmentObservationTypeSchema: detail.TreatmentBMPAssessmentObservationTypeSchema
                            ? this.formatJson(detail.TreatmentBMPAssessmentObservationTypeSchema) : "",
                    });
                    this.isLoading.set(false);
                },
                error: () => this.isLoading.set(false),
            });
        }
    }

    save(): void {
        if (this.formGroup.invalid) return;
        const raw = this.formGroup.getRawValue();

        // Validate JSON if provided
        if (raw.TreatmentBMPAssessmentObservationTypeSchema?.trim()) {
            try {
                JSON.parse(raw.TreatmentBMPAssessmentObservationTypeSchema);
            } catch {
                this.alertService.pushAlert({ Message: "Schema is not valid JSON.", AlertContext: 3 } as any);
                return;
            }
        }

        const dto: TreatmentBMPAssessmentObservationTypeUpsertDto = {
            TreatmentBMPAssessmentObservationTypeName: raw.TreatmentBMPAssessmentObservationTypeName,
            ObservationTypeSpecificationID: raw.ObservationTypeSpecificationID,
            TreatmentBMPAssessmentObservationTypeSchema: raw.TreatmentBMPAssessmentObservationTypeSchema?.trim() || undefined,
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

    private formatJson(json: string): string {
        try {
            return JSON.stringify(JSON.parse(json), null, 2);
        } catch {
            return json;
        }
    }
}
