import { Component, computed, inject, Input, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, ReactiveFormsModule, Validators, FormsModule } from "@angular/forms";
import { forkJoin, map, Observable, of, shareReplay, tap } from "rxjs";
import { NgSelectModule } from "@ng-select/ng-select";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { CustomAttributeTypeService } from "src/app/shared/generated/api/custom-attribute-type.service";
import { TreatmentBMPTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-type-detail-dto";
import { TreatmentBMPTypeUpsertDto } from "src/app/shared/generated/model/treatment-bmp-type-upsert-dto";
import { TreatmentBMPTypeObservationTypeUpsertDto } from "src/app/shared/generated/model/treatment-bmp-type-observation-type-upsert-dto";
import { TreatmentBMPTypeCustomAttributeTypeUpsertDto } from "src/app/shared/generated/model/treatment-bmp-type-custom-attribute-type-upsert-dto";
import { TreatmentBMPAssessmentObservationTypeGridDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-grid-dto";
import { CustomAttributeTypeDto } from "src/app/shared/generated/model/custom-attribute-type-dto";
import { ObservationTypeCollectionMethodEnum } from "src/app/shared/generated/enum/observation-type-collection-method-enum";
import { ObservationThresholdTypeEnum } from "src/app/shared/generated/enum/observation-threshold-type-enum";
import { ObservationTargetTypeEnum } from "src/app/shared/generated/enum/observation-target-type-enum";
import { specToTriple } from "src/app/shared/observation-types/schema-types";

interface ObservationTypeRow {
    TreatmentBMPAssessmentObservationTypeID: number;
    Name: string;
    CollectionMethod: string;
    CollectionMethodID: number;
    TargetTypeID: number;
    ThresholdTypeID: number;
    HasBenchmarkAndThreshold: boolean;
    BenchmarkUnitDisplayName: string | null;
    ThresholdUnitDisplayName: string | null;
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
    DataTypeDisplayName: string;
    MeasurementUnitDisplayName: string;
    IsRequired: boolean;
    Description: string;
    SortOrder: number | null;
}

@Component({
    selector: "treatment-bmp-type-edit",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, FormFieldComponent, FieldDefinitionComponent, RouterLink, AsyncPipe, ReactiveFormsModule, FormsModule, NgSelectModule],
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
    private confirmService = inject(ConfirmService);

    public FormFieldType = FormFieldType;
    public ObservationTypeCollectionMethodEnum = ObservationTypeCollectionMethodEnum;
    public loaded$: Observable<boolean>;
    public isCreate = false;
    public isSaving = false;

    public nameControl = new FormControl<string>("", { validators: [Validators.required, Validators.maxLength(100)], nonNullable: true });
    public descriptionControl = new FormControl<string>("", { validators: [Validators.required, Validators.maxLength(1000)], nonNullable: true });

    // Observation types
    public observationTypeRows = signal<ObservationTypeRow[]>([]);
    public allObservationTypes = signal<TreatmentBMPAssessmentObservationTypeGridDto[]>([]);
    public pickerObservationTypeID: number | null = null;

    // Custom attribute types
    public customAttributeRows = signal<CustomAttributeRow[]>([]);
    public allCustomAttributes = signal<CustomAttributeTypeDto[]>([]);
    public pickerCustomAttributeID: number | null = null;

    public availableObservationTypes = computed(() => {
        const usedIDs = new Set(this.observationTypeRows().map((r) => r.TreatmentBMPAssessmentObservationTypeID));
        return this.allObservationTypes().filter((o) => !usedIDs.has(o.TreatmentBMPAssessmentObservationTypeID));
    });

    public availableCustomAttributes = computed(() => {
        const usedIDs = new Set(this.customAttributeRows().map((r) => r.CustomAttributeTypeID));
        return this.allCustomAttributes().filter((c) => !usedIDs.has(c.CustomAttributeTypeID));
    });

    public weightTotal = computed(() => this.observationTypeRows()
        .filter((r) => this.rowCountsTowardWeight(r) && r.AssessmentScoreWeight != null)
        .reduce((sum, r) => sum + Number(r.AssessmentScoreWeight), 0));

    public hasWeightError = computed(() => {
        const rows = this.observationTypeRows().filter((r) => this.rowCountsTowardWeight(r));
        const rowsWithWeight = rows.filter((r) => r.AssessmentScoreWeight != null);
        // If any rows count toward weight, the total of the weighted rows must be exactly 100.
        return rowsWithWeight.length > 0 && this.weightTotal() !== 100;
    });

    ngOnInit(): void {
        this.isCreate = !this.treatmentBMPTypeID;

        const detail$ = this.treatmentBMPTypeID
            ? this.bmpTypeService.getDetailTreatmentBMPType(this.treatmentBMPTypeID)
            : of(null as TreatmentBMPTypeDetailDto);
        const obsTypes$ = this.observationTypeService.listTreatmentBMPAssessmentObservationType();
        const catTypes$ = this.customAttributeTypeService.listCustomAttributeType();

        this.loaded$ = forkJoin({ detail: detail$, obsTypes: obsTypes$, catTypes: catTypes$ }).pipe(
            tap(({ detail, obsTypes, catTypes }) => {
                this.allObservationTypes.set(obsTypes);
                this.allCustomAttributes.set(catTypes);

                if (detail) {
                    this.nameControl.setValue(detail.TreatmentBMPTypeName);
                    this.descriptionControl.setValue(detail.TreatmentBMPTypeDescription ?? "");

                    this.observationTypeRows.set(detail.ObservationTypes.map((o) => ({
                        TreatmentBMPAssessmentObservationTypeID: o.TreatmentBMPAssessmentObservationTypeID,
                        Name: o.TreatmentBMPAssessmentObservationTypeName,
                        CollectionMethod: o.ObservationTypeCollectionMethodDisplayName,
                        CollectionMethodID: o.ObservationTypeCollectionMethodID,
                        TargetTypeID: o.ObservationTargetTypeID,
                        ThresholdTypeID: o.ObservationThresholdTypeID,
                        HasBenchmarkAndThreshold: o.HasBenchmarkAndThreshold,
                        BenchmarkUnitDisplayName: o.BenchmarkUnitDisplayName,
                        ThresholdUnitDisplayName: o.ThresholdUnitDisplayName,
                        AssessmentScoreWeight: o.AssessmentScoreWeight ?? null,
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
                        DataTypeDisplayName: c.CustomAttributeDataTypeDisplayName ?? "",
                        MeasurementUnitDisplayName: c.MeasurementUnitDisplayName ?? "",
                        IsRequired: c.IsRequired,
                        Description: c.CustomAttributeTypeDescription ?? "",
                        SortOrder: c.SortOrder,
                    })));
                }
            }),
            map(() => true),
            shareReplay(1),
        );
    }

    /**
     * Pass/Fail rows with the "Assessment Fails if Observation Fails" flag set don't carry a
     * weight — they short-circuit assessment scoring instead. Discrete/Percentage rows always
     * carry weight. Used for both the weight-total and the disable-input logic.
     */
    private rowCountsTowardWeight(row: ObservationTypeRow): boolean {
        const isPassFail = row.CollectionMethodID === ObservationTypeCollectionMethodEnum.PassFail;
        return !(isPassFail && row.OverrideAssessmentScoreIfFailing);
    }

    public isPassFail(row: ObservationTypeRow): boolean {
        return row.CollectionMethodID === ObservationTypeCollectionMethodEnum.PassFail;
    }

    addObservationType(): void {
        if (this.pickerObservationTypeID == null) return;
        const ot = this.allObservationTypes().find((o) => o.TreatmentBMPAssessmentObservationTypeID === this.pickerObservationTypeID);
        if (!ot) return;
        const triple = specToTriple(ot.ObservationTypeSpecificationID);
        const collectionMethodID = triple?.CollectionMethodID ?? 0;
        const targetTypeID = triple?.TargetTypeID ?? 0;
        const thresholdTypeID = triple?.ThresholdTypeID ?? 0;
        const isPassFail = collectionMethodID === ObservationTypeCollectionMethodEnum.PassFail;
        this.observationTypeRows.update((rows) => [...rows, {
            TreatmentBMPAssessmentObservationTypeID: ot.TreatmentBMPAssessmentObservationTypeID,
            Name: ot.TreatmentBMPAssessmentObservationTypeName,
            CollectionMethod: ot.ObservationTypeCollectionMethodDisplayName,
            CollectionMethodID: collectionMethodID,
            TargetTypeID: targetTypeID,
            ThresholdTypeID: thresholdTypeID,
            HasBenchmarkAndThreshold: !isPassFail && thresholdTypeID !== ObservationThresholdTypeEnum.None,
            // Unit labels come from server-side schema parsing; new rows show no suffix until save+reload
            // for the SpecificValue case (we don't have schema JSON client-side). For RelativeToBenchmark
            // we can derive the percentage unit from the target type.
            BenchmarkUnitDisplayName: null,
            ThresholdUnitDisplayName: this.derivePercentThresholdLabel(targetTypeID, thresholdTypeID),
            AssessmentScoreWeight: null,
            DefaultThresholdValue: null,
            DefaultBenchmarkValue: null,
            OverrideAssessmentScoreIfFailing: false,
            SortOrder: rows.length + 1,
        }]);
        this.pickerObservationTypeID = null;
    }

    private derivePercentThresholdLabel(targetTypeID: number, thresholdTypeID: number): string | null {
        if (thresholdTypeID !== ObservationThresholdTypeEnum.RelativeToBenchmark) return null;
        switch (targetTypeID) {
            case ObservationTargetTypeEnum.High: return "% decline from benchmark";
            case ObservationTargetTypeEnum.Low: return "% increase from benchmark";
            case ObservationTargetTypeEnum.SpecificValue: return "% of benchmark";
            default: return null;
        }
    }

    removeObservationType(id: number): void {
        this.observationTypeRows.update((rows) =>
            rows.filter((r) => r.TreatmentBMPAssessmentObservationTypeID !== id)
                // Renumber after removal so SortOrder stays sequential
                .map((r, index) => ({ ...r, SortOrder: index + 1 })),
        );
    }

    addCustomAttribute(): void {
        if (this.pickerCustomAttributeID == null) return;
        const cat = this.allCustomAttributes().find((c) => c.CustomAttributeTypeID === this.pickerCustomAttributeID);
        if (!cat) return;
        this.customAttributeRows.update((rows) => [...rows, {
            CustomAttributeTypeID: cat.CustomAttributeTypeID,
            Name: cat.CustomAttributeTypeName,
            Purpose: cat.Purpose,
            PurposeID: cat.CustomAttributeTypePurposeID,
            DataTypeDisplayName: cat.DataTypeDisplayName ?? "",
            MeasurementUnitDisplayName: cat.MeasurementUnitDisplayName ?? "",
            IsRequired: cat.IsRequired,
            Description: cat.CustomAttributeTypeDescription ?? "",
            SortOrder: rows.length + 1,
        }]);
        this.pickerCustomAttributeID = null;
    }

    removeCustomAttribute(id: number): void {
        this.customAttributeRows.update((rows) =>
            rows.filter((r) => r.CustomAttributeTypeID !== id)
                .map((r, index) => ({ ...r, SortOrder: index + 1 })),
        );
    }

    save(): void {
        if (this.nameControl.invalid || this.descriptionControl.invalid) return;
        if (this.hasWeightError()) {
            this.alertService.pushAlert(new Alert(`Observation type weights must sum to 100%. Currently: ${this.weightTotal()}%.`, AlertContext.Danger));
            return;
        }
        this.isSaving = true;
        this.alertService.clearAlerts();

        const dto: TreatmentBMPTypeUpsertDto = {
            TreatmentBMPTypeName: this.nameControl.value,
            TreatmentBMPTypeDescription: this.descriptionControl.value,
            ObservationTypes: this.observationTypeRows().map((r) => ({
                TreatmentBMPAssessmentObservationTypeID: r.TreatmentBMPAssessmentObservationTypeID,
                // Pass/Fail-with-override rows have null weight (the row's failure short-circuits scoring).
                AssessmentScoreWeight: this.rowCountsTowardWeight(r) ? r.AssessmentScoreWeight : null,
                DefaultThresholdValue: r.HasBenchmarkAndThreshold ? r.DefaultThresholdValue : null,
                DefaultBenchmarkValue: r.HasBenchmarkAndThreshold ? r.DefaultBenchmarkValue : null,
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
                this.alertService.pushAlert(new Alert("An error occurred while saving the Treatment BMP Type.", AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.router.navigate(["/manage/treatment-bmp-types"]);
    }

    deleteBMPType(): void {
        const name = this.escapeHtml(this.nameControl.value || "this BMP type");
        const obsCount = this.observationTypeRows().length;
        const catCount = this.customAttributeRows().length;
        this.confirmService
            .confirm({
                title: "Delete Treatment BMP Type",
                message:
                    `<p>You are about to delete <strong>${name}</strong>.</p>` +
                    `<p>This will permanently remove all Treatment BMPs of this type, all of their assessments / observations / photos / delineations / custom attribute values, plus any Quick BMPs and maintenance records for this type.</p>` +
                    `<p>It will also remove this BMP type's associations to ${obsCount} Observation Type${obsCount === 1 ? "" : "s"} and ${catCount} Custom Attribute${catCount === 1 ? "" : "s"}.</p>` +
                    `<p>This cannot be undone. Are you sure you wish to proceed?</p>`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.bmpTypeService.deleteTreatmentBMPType(this.treatmentBMPTypeID).subscribe({
                    next: () => {
                        this.alertService.pushAlert(new Alert("Treatment BMP Type deleted.", AlertContext.Success));
                        this.router.navigate(["/manage/treatment-bmp-types"]);
                    },
                    error: () => {
                        this.alertService.pushAlert(new Alert("An error occurred while deleting the Treatment BMP Type.", AlertContext.Danger));
                    },
                });
            });
    }

    private escapeHtml(s: string): string {
        return s
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#39;");
    }
}
