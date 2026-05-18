import { Component, inject, OnInit } from "@angular/core";
import { ReactiveFormsModule, FormControl, Validators } from "@angular/forms";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { DialogRef } from "@ngneat/dialog";
import { AlertService } from "src/app/shared/services/alert.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import {
    WaterQualityManagementPlanModelingApproachEnum,
    WaterQualityManagementPlanModelingApproachesAsSelectDropdownOptions,
} from "src/app/shared/generated/enum/water-quality-management-plan-modeling-approach-enum";

@Component({
    selector: "edit-modeling-approach-modal",
    imports: [AlertDisplayComponent, ReactiveFormsModule],
    templateUrl: "./edit-modeling-approach-modal.component.html",
    styleUrl: "./edit-modeling-approach-modal.component.scss",
})
export class EditModelingApproachModalComponent implements OnInit {
    public ref: DialogRef<EditModelingApproachModalContext, boolean> = inject(DialogRef);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private alertService = inject(AlertService);

    public modelingApproachOptions = WaterQualityManagementPlanModelingApproachesAsSelectDropdownOptions;
    public modelingApproachControl = new FormControl<number>(undefined, { nonNullable: true, validators: [Validators.required] });

    public modelingApproachDescriptions: { [key: number]: string } = {
        [WaterQualityManagementPlanModelingApproachEnum.Detailed]:
            "This WQMP is modeled by inventorying the associated structural BMPs and defining their delineations. The performance of each BMP is modeled based on its modeling parameters and the attributes of the delineated tributary area.",
        [WaterQualityManagementPlanModelingApproachEnum.Simplified]:
            "This WQMP is modeled by entering simplified structural BMP modeling parameters directly on this WQMP page.",
    };

    ngOnInit(): void {
        this.alertService.clearAlerts();
        if (this.ref.data?.currentApproachID != null) {
            this.modelingApproachControl.setValue(this.ref.data.currentApproachID);
        }
    }

    public save(): void {
        if (this.modelingApproachControl.invalid) {
            this.modelingApproachControl.markAsTouched();
            return;
        }
        const wqmpID = this.ref.data?.wqmpID;
        const approachID = this.modelingApproachControl.value;
        this.wqmpService.updateModelingApproachWaterQualityManagementPlan(wqmpID, approachID).subscribe(() => {
            this.ref.close(true);
        });
    }

    public cancel(): void {
        this.ref.close(null);
    }
}

export class EditModelingApproachModalContext {
    wqmpID: number;
    currentApproachID?: number;
}
