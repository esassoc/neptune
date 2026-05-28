import { Component, inject, OnInit } from "@angular/core";
import { FormArray, FormControl, FormGroup, ReactiveFormsModule } from "@angular/forms";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { TreatmentBMPBenchmarkAndThresholdService } from "src/app/shared/generated/api/treatment-bmp-benchmark-and-threshold.service";
import { TreatmentBMPBenchmarkAndThresholdWithObservationTypeDto } from "src/app/shared/generated/model/treatment-bmp-benchmark-and-threshold-with-observation-type-dto";
import { TreatmentBMPBenchmarkAndThresholdUpsertDto } from "src/app/shared/generated/model/treatment-bmp-benchmark-and-threshold-upsert-dto";
import { DialogRef } from "@ngneat/dialog";
import { forkJoin, Observable } from "rxjs";

@Component({
    selector: "treatment-bmp-benchmark-threshold-modal",
    standalone: true,
    imports: [ReactiveFormsModule, AlertDisplayComponent],
    templateUrl: "./treatment-bmp-benchmark-threshold-modal.component.html",
})
export class TreatmentBmpBenchmarkThresholdModalComponent implements OnInit {
    public ref: DialogRef<TreatmentBmpBenchmarkThresholdModalContext, boolean> = inject(DialogRef);
    private benchmarkService = inject(TreatmentBMPBenchmarkAndThresholdService);
    private alertService = inject(AlertService);

    public benchmarks: TreatmentBMPBenchmarkAndThresholdWithObservationTypeDto[] = [];
    public formArray: FormArray<FormGroup<BenchmarkThresholdFormRow>> = new FormArray<FormGroup<BenchmarkThresholdFormRow>>([]);
    public isSaving = false;

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.benchmarks = this.ref.data.benchmarks;

        // NPT-1061: build a row for every benchmark/threshold observation type the BMP's type has
        // (set or not), so types with multiple/complex observation types (e.g. Permeable Pavement)
        // are fully editable rather than showing nothing.
        for (const bt of this.benchmarks) {
            this.formArray.push(
                new FormGroup<BenchmarkThresholdFormRow>({
                    BenchmarkValue: new FormControl<number>(bt.BenchmarkValue),
                    ThresholdValue: new FormControl<number>(bt.ThresholdValue),
                })
            );
        }
    }

    public save(): void {
        if (this.isSaving) return;
        this.isSaving = true;

        const saves$: Observable<unknown>[] = [];
        for (let i = 0; i < this.benchmarks.length; i++) {
            const bt = this.benchmarks[i];
            const row = this.formArray.at(i);
            if (!row.dirty) continue;

            const upsertDto = new TreatmentBMPBenchmarkAndThresholdUpsertDto({
                TreatmentBMPTypeAssessmentObservationTypeID: bt.TreatmentBMPTypeAssessmentObservationTypeID,
                TreatmentBMPTypeID: bt.TreatmentBMPTypeID,
                TreatmentBMPAssessmentObservationTypeID: bt.TreatmentBMPAssessmentObservationTypeID,
                BenchmarkValue: row.value.BenchmarkValue,
                ThresholdValue: row.value.ThresholdValue,
            });

            // Existing rows are updated in place; previously-unset rows the user filled in are created.
            saves$.push(
                bt.TreatmentBMPBenchmarkAndThresholdID != null
                    ? this.benchmarkService.updateTreatmentBMPBenchmarkAndThreshold(this.ref.data.treatmentBMPID, bt.TreatmentBMPBenchmarkAndThresholdID, upsertDto)
                    : this.benchmarkService.createTreatmentBMPBenchmarkAndThreshold(this.ref.data.treatmentBMPID, upsertDto)
            );
        }

        if (saves$.length === 0) {
            this.ref.close(null);
            return;
        }

        forkJoin(saves$).subscribe({
            next: () => {
                this.ref.close(true);
            },
            error: () => {
                this.isSaving = false;
            },
        });
    }

    public cancel(): void {
        this.ref.close(null);
    }
}

export interface BenchmarkThresholdFormRow {
    BenchmarkValue: FormControl<number>;
    ThresholdValue: FormControl<number>;
}

export interface TreatmentBmpBenchmarkThresholdModalContext {
    treatmentBMPID: number;
    benchmarks: TreatmentBMPBenchmarkAndThresholdWithObservationTypeDto[];
}
