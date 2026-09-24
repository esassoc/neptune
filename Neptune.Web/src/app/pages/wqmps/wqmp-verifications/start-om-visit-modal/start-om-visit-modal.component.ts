import { Component, inject, OnInit } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { NgSelectModule } from "@ng-select/ng-select";
import { Observable, shareReplay } from "rxjs";
import { todayLocalDateString } from "src/app/shared/helpers/local-date";

import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";

import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanDisplayDto } from "src/app/shared/generated/model/water-quality-management-plan-display-dto";

export interface StartOMVisitModalResult {
    waterQualityManagementPlanID: number;
    verificationDate: string;
}

/**
 * NPT-1122: "Start O&M Visit" from the WQMP O&M Verifications list. Pick a WQMP + Verification Date and hand
 * them back to the caller, which navigates into the verification wizard at `/verifications/new/basics`.
 * Deliberately writes nothing — the verification is still created lazily on first save in the wizard.
 */
@Component({
    selector: "start-om-visit-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent, NgSelectModule, AsyncPipe],
    templateUrl: "./start-om-visit-modal.component.html",
    styleUrl: "./start-om-visit-modal.component.scss",
})
export class StartOMVisitModalComponent implements OnInit {
    public ref: DialogRef<void, StartOMVisitModalResult | null> = inject(DialogRef);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private alertService = inject(AlertService);
    public FormFieldType = FormFieldType;

    public formGroup = new FormGroup({
        WaterQualityManagementPlanID: new FormControl<number | null>(null, { validators: [Validators.required] }),
        VerificationDate: new FormControl<string>(todayLocalDateString(), { validators: [Validators.required], nonNullable: true }),
    });

    public wqmps$: Observable<WaterQualityManagementPlanDisplayDto[]> = this.wqmpService.listForPickerWaterQualityManagementPlan().pipe(shareReplay(1));

    // Signal so the Start button's disabled state tracks the form under zoneless CD.
    private formStatus = toSignal(this.formGroup.statusChanges, { initialValue: this.formGroup.status });

    // Name-only, case-insensitive, match-anywhere search (NPT-1122 AC 16).
    public searchWQMPs = (term: string, item: WaterQualityManagementPlanDisplayDto): boolean =>
        (item.WaterQualityManagementPlanName ?? "").toLowerCase().includes(term.toLowerCase());

    ngOnInit(): void {
        this.alertService.clearAlerts();
    }

    public get canStart(): boolean {
        return this.formStatus() === "VALID";
    }

    start(): void {
        if (this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            return;
        }
        this.ref.close({
            waterQualityManagementPlanID: this.formGroup.controls.WaterQualityManagementPlanID.value,
            verificationDate: this.formGroup.controls.VerificationDate.value,
        });
    }

    cancel(): void {
        this.ref.close(null);
    }
}
