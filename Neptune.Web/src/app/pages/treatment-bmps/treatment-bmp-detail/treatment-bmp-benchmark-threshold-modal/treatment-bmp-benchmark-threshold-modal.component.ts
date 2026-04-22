import { Component, inject, OnInit } from "@angular/core";
import { FormArray, FormControl, FormGroup, ReactiveFormsModule } from "@angular/forms";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { TreatmentBMPBenchmarkAndThresholdService } from "src/app/shared/generated/api/treatment-bmp-benchmark-and-threshold.service";
import { TreatmentBMPBenchmarkAndThresholdDto } from "src/app/shared/generated/model/treatment-bmp-benchmark-and-threshold-dto";
import { TreatmentBMPBenchmarkAndThresholdUpsertDto } from "src/app/shared/generated/model/treatment-bmp-benchmark-and-threshold-upsert-dto";
import { DialogRef } from "@ngneat/dialog";
import { forkJoin } from "rxjs";

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

    public benchmarks: TreatmentBMPBenchmarkAndThresholdDto[] = [];
    public formArray: FormArray<FormGroup<BenchmarkThresholdFormRow>> = new FormArray<FormGroup<BenchmarkThresholdFormRow>>([]);
    public isSaving = false;

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.benchmarks = this.ref.data.benchmarks;

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

        const updates$ = [];
        for (let i = 0; i < this.benchmarks.length; i++) {
            const bt = this.benchmarks[i];
            const row = this.formArray.at(i);
            if (row.dirty) {
                const upsertDto = new TreatmentBMPBenchmarkAndThresholdUpsertDto({
                    TreatmentBMPTypeAssessmentObservationTypeID: bt.TreatmentBMPTypeAssessmentObservationTypeID,
                    TreatmentBMPTypeID: bt.TreatmentBMPTypeID,
                    TreatmentBMPAssessmentObservationTypeID: bt.TreatmentBMPAssessmentObservationTypeID,
                    BenchmarkValue: row.value.BenchmarkValue,
                    ThresholdValue: row.value.ThresholdValue,
                });
                updates$.push(
                    this.benchmarkService.updateTreatmentBMPBenchmarkAndThreshold(
                        this.ref.data.treatmentBMPID,
                        bt.TreatmentBMPBenchmarkAndThresholdID,
                        upsertDto
                    )
                );
            }
        }

        if (updates$.length === 0) {
            this.ref.close(null);
            return;
        }

        forkJoin(updates$).subscribe({
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
    benchmarks: TreatmentBMPBenchmarkAndThresholdDto[];
}
