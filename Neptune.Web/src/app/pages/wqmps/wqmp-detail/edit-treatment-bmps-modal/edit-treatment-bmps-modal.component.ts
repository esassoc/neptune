import { Component, inject, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { FormsModule } from "@angular/forms";
import { NgSelectModule } from "@ng-select/ng-select";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { DialogRef } from "@ngneat/dialog";
import { AlertService } from "src/app/shared/services/alert.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { TreatmentBMPMinimalDto } from "src/app/shared/generated/model/treatment-bmp-minimal-dto";
import { BehaviorSubject, combineLatest, map, Observable, shareReplay } from "rxjs";

@Component({
    selector: "edit-treatment-bmps-modal",
    imports: [AlertDisplayComponent, FormsModule, NgSelectModule, AsyncPipe],
    templateUrl: "./edit-treatment-bmps-modal.component.html",
})
export class EditTreatmentBMPsModalComponent implements OnInit {
    public ref: DialogRef<EditTreatmentBMPsModalContext, boolean> = inject(DialogRef);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private alertService = inject(AlertService);

    private selectedBMPIDs$ = new BehaviorSubject<Set<number>>(new Set());

    public selectedBMPs$: Observable<TreatmentBMPMinimalDto[]>;
    public availableBMPs$: Observable<TreatmentBMPMinimalDto[]>;
    public pickerBMPID: number | null = null;

    ngOnInit(): void {
        this.alertService.clearAlerts();
        const currentBMPIDs = new Set(this.ref.data?.currentBMPIDs ?? []);
        this.selectedBMPIDs$.next(currentBMPIDs);

        const allBMPs$ = this.wqmpService.listAvailableTreatmentBMPsWaterQualityManagementPlan(this.ref.data?.wqmpID).pipe(shareReplay(1));

        this.selectedBMPs$ = combineLatest([allBMPs$, this.selectedBMPIDs$]).pipe(map(([bmps, ids]) => bmps.filter((b) => ids.has(b.TreatmentBMPID))));

        this.availableBMPs$ = combineLatest([allBMPs$, this.selectedBMPIDs$]).pipe(map(([bmps, ids]) => bmps.filter((b) => !ids.has(b.TreatmentBMPID))));
    }

    public addBMP(): void {
        if (this.pickerBMPID == null) return;
        const ids = new Set(this.selectedBMPIDs$.value);
        ids.add(this.pickerBMPID);
        this.selectedBMPIDs$.next(ids);
        this.pickerBMPID = null;
    }

    public removeBMP(bmpID: number): void {
        const ids = new Set(this.selectedBMPIDs$.value);
        ids.delete(bmpID);
        this.selectedBMPIDs$.next(ids);
    }

    public save(): void {
        const wqmpID = this.ref.data?.wqmpID;
        const ids = Array.from(this.selectedBMPIDs$.value);
        this.wqmpService.updateTreatmentBMPsWaterQualityManagementPlan(wqmpID, ids).subscribe(() => {
            this.ref.close(true);
        });
    }

    public cancel(): void {
        this.ref.close(null);
    }
}

export class EditTreatmentBMPsModalContext {
    wqmpID: number;
    currentBMPIDs: number[];
}
