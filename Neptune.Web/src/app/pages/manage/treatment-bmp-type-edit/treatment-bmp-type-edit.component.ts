import { Component, inject, Input, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormGroup, ReactiveFormsModule, Validators, FormsModule } from "@angular/forms";
import { forkJoin, map, Observable, of, shareReplay, tap } from "rxjs";
import { NgSelectModule } from "@ng-select/ng-select";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { CustomAttributeTypeService } from "src/app/shared/generated/api/custom-attribute-type.service";
import { TreatmentBMPTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-detail-dto";
import { TreatmentBMPTypeUpsertDto } from "src/app/shared/generated/model/treatment-bmp-type-upsert-dto";
import { TreatmentBMPTypeObservationTypeUpsertDto } from "src/app/shared/generated/model/treatment-bmp-type-observation-type-upsert-dto";
import { TreatmentBMPTypeCustomAttributeTypeUpsertDto } from "src/app/shared/generated/model/treatment-bmp-type-custom-attribute-type-upsert-dto";
import { TreatmentBMPAssessmentObservationTypeGridDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-grid-dto";
import { CustomAttributeTypeDto } from "src/app/shared/generated/model/custom-attribute-type-dto";

interface ObservationTypeRow {
    TreatmentBMPAssessmentObservationTypeID: number;
    Name: string;
    CollectionMethod: string;
    AssessmentScoreWeight: number | null;
    DefaultThresholdValue: number | null;
    DefaultBenchmarkValue: number | null;
    OverrideAssessmentScoreIfFailing: boolean;
    SortOrder: number | null;
}

interface CustomAttributeRow {
    CustomAttributeTypeID: number;
    Name: string;
    Purpose: string;
    PurposeID: number;
    SortOrder: number | null;
}

@Component({
    selector: "treatment-bmp-type-edit",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, FormFieldComponent, RouterLink, AsyncPipe, ReactiveFormsModule, FormsModule, NgSelectModule],
    templateUrl: "./treatment-bmp-type-edit.component.html",
    styleUrl: "./treatment-bmp-type-edit.component.scss",
})
export class TreatmentBmpTypeEditComponent implements OnInit {
    @Input() treatmentBMPTypeID: number;

    private router = inject(Router);
    private bmpTypeService = inject(TreatmentBMPTypeService);
    private observationTypeService = inject(TreatmentBMPAssessmentObservationTypeService);
    private customAttributeTypeService = inject(CustomAttributeTypeService);
    private alertService = inject(AlertService);

    public FormFieldType = FormFieldType;
    public loaded$: Observable<boolean>;
    public isCreate = false;
    public isSaving = false;

    public nameControl = new FormControl<string>("", { validators: [Validators.required, Validators.maxLength(100)], nonNullable: true });
    public descriptionControl = new FormControl<string>("", { validators: [Validators.maxLength(1000)], nonNullable: true });

    // Observation types
    public observationTypeRows = signal<ObservationTypeRow[]>([]);
    public allObservationTypes: TreatmentBMPAssessmentObservationTypeGridDto[] = [];
    public pickerObservationTypeID: number | null = null;

    // Custom attribute types
    public customAttributeRows = signal<CustomAttributeRow[]>([]);
    public allCustomAttributes: CustomAttributeTypeDto[] = [];
    public pickerCustomAttributeID: number | null = null;

    get availableObservationTypes(): TreatmentBMPAssessmentObservationTypeGridDto[] {
        const usedIDs = new Set(this.observationTypeRows().map((r) => r.TreatmentBMPAssessmentObservationTypeID));
        return this.allObservationTypes.filter((o) => !usedIDs.has(o.TreatmentBMPAssessmentObservationTypeID));
    }

    get availableCustomAttributes(): CustomAttributeTypeDto[] {
        const usedIDs = new Set(this.customAttributeRows().map((r) => r.CustomAttributeTypeID));
        return this.allCustomAttributes.filter((c) => !usedIDs.has(c.CustomAttributeTypeID));
    }

    get customAttributesByPurpose(): { purpose: string; rows: CustomAttributeRow[] }[] {
        const grouped: Record<string, CustomAttributeRow[]> = {};
        for (const row of this.customAttributeRows()) {
            if (!grouped[row.Purpose]) grouped[row.Purpose] = [];
            grouped[row.Purpose].push(row);
        }
        return Object.entries(grouped).map(([purpose, rows]) => ({ purpose, rows }));
    }

    ngOnInit(): void {
        this.isCreate = !this.treatmentBMPTypeID;

        const detail$ = this.treatmentBMPTypeID
            ? this.bmpTypeService.getDetailTreatmentBMPType(this.treatmentBMPTypeID)
            : of(null as TreatmentBMPTypeDetailDto);
        const obsTypes$ = this.observationTypeService.listTreatmentBMPAssessmentObservationType();
        const catTypes$ = this.customAttributeTypeService.listCustomAttributeType();

        this.loaded$ = forkJoin({ detail: detail$, obsTypes: obsTypes$, catTypes: catTypes$ }).pipe(
            tap(({ detail, obsTypes, catTypes }) => {
                this.allObservationTypes = obsTypes;
                this.allCustomAttributes = catTypes;

                if (detail) {
                    this.nameControl.setValue(detail.TreatmentBMPTypeName);
                    this.descriptionControl.setValue(detail.TreatmentBMPTypeDescription ?? "");

                    this.observationTypeRows.set(detail.ObservationTypes.map((o) => ({
                        TreatmentBMPAssessmentObservationTypeID: o.TreatmentBMPAssessmentObservationTypeID,
                        Name: o.TreatmentBMPAssessmentObservationTypeName,
                        CollectionMethod: o.ObservationTypeCollectionMethodDisplayName,
                        AssessmentScoreWeight: o.AssessmentScoreWeight,
                        DefaultThresholdValue: o.DefaultThresholdValue,
                        DefaultBenchmarkValue: o.DefaultBenchmarkValue,
                        OverrideAssessmentScoreIfFailing: o.OverrideAssessmentScoreIfFailing,
                        SortOrder: o.SortOrder,
                    })));

                    this.customAttributeRows.set(detail.CustomAttributeTypes.map((c) => ({
                        CustomAttributeTypeID: c.CustomAttributeTypeID,
                        Name: c.CustomAttributeTypeName,
                        Purpose: c.CustomAttributeTypePurposeDisplayName,
                        PurposeID: c.CustomAttributeTypePurposeID,
                        SortOrder: c.SortOrder,
                    })));
                }
            }),
            map(() => true),
            shareReplay(1)
        );
    }

    addObservationType(): void {
        if (this.pickerObservationTypeID == null) return;
        const ot = this.allObservationTypes.find((o) => o.TreatmentBMPAssessmentObservationTypeID === this.pickerObservationTypeID);
        if (!ot) return;
        this.observationTypeRows.update((rows) => [...rows, {
            TreatmentBMPAssessmentObservationTypeID: ot.TreatmentBMPAssessmentObservationTypeID,
            Name: ot.TreatmentBMPAssessmentObservationTypeName,
            CollectionMethod: ot.ObservationTypeCollectionMethodDisplayName,
            AssessmentScoreWeight: null,
            DefaultThresholdValue: null,
            DefaultBenchmarkValue: null,
            OverrideAssessmentScoreIfFailing: false,
            SortOrder: rows.length + 1,
        }]);
        this.pickerObservationTypeID = null;
    }

    removeObservationType(id: number): void {
        this.observationTypeRows.update((rows) => rows.filter((r) => r.TreatmentBMPAssessmentObservationTypeID !== id));
    }

    addCustomAttribute(): void {
        if (this.pickerCustomAttributeID == null) return;
        const cat = this.allCustomAttributes.find((c) => c.CustomAttributeTypeID === this.pickerCustomAttributeID);
        if (!cat) return;
        this.customAttributeRows.update((rows) => [...rows, {
            CustomAttributeTypeID: cat.CustomAttributeTypeID,
            Name: cat.CustomAttributeTypeName,
            Purpose: cat.Purpose,
            PurposeID: cat.CustomAttributeTypePurposeID,
            SortOrder: rows.length + 1,
        }]);
        this.pickerCustomAttributeID = null;
    }

    removeCustomAttribute(id: number): void {
        this.customAttributeRows.update((rows) => rows.filter((r) => r.CustomAttributeTypeID !== id));
    }

    save(): void {
        if (this.nameControl.invalid) return;
        this.isSaving = true;
        this.alertService.clearAlerts();

        const dto: TreatmentBMPTypeUpsertDto = {
            TreatmentBMPTypeName: this.nameControl.value,
            TreatmentBMPTypeDescription: this.descriptionControl.value || undefined,
            ObservationTypes: this.observationTypeRows().map((r) => ({
                TreatmentBMPAssessmentObservationTypeID: r.TreatmentBMPAssessmentObservationTypeID,
                AssessmentScoreWeight: r.AssessmentScoreWeight,
                DefaultThresholdValue: r.DefaultThresholdValue,
                DefaultBenchmarkValue: r.DefaultBenchmarkValue,
                OverrideAssessmentScoreIfFailing: r.OverrideAssessmentScoreIfFailing,
                SortOrder: r.SortOrder,
            } as TreatmentBMPTypeObservationTypeUpsertDto)),
            CustomAttributeTypes: this.customAttributeRows().map((r) => ({
                CustomAttributeTypeID: r.CustomAttributeTypeID,
                SortOrder: r.SortOrder,
            } as TreatmentBMPTypeCustomAttributeTypeUpsertDto)),
        };

        const save$ = this.isCreate
            ? this.bmpTypeService.createTreatmentBMPType(dto)
            : this.bmpTypeService.updateTreatmentBMPType(this.treatmentBMPTypeID, dto);

        save$.subscribe({
            next: () => {
                this.isSaving = false;
                this.alertService.pushAlert(new Alert(`Treatment BMP Type ${this.isCreate ? "created" : "updated"}.`, AlertContext.Success));
                this.router.navigate(["/manage/treatment-bmp-types"]);
            },
            error: () => {
                this.isSaving = false;
                this.alertService.pushAlert(new Alert("An error occurred while saving.", AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.router.navigate(["/manage/treatment-bmp-types"]);
    }
}
