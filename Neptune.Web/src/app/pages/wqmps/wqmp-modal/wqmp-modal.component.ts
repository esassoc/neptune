import { AsyncPipe } from "@angular/common";
import { Component, inject, OnInit } from "@angular/core";
import { FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { map, Observable } from "rxjs";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import {
    WaterQualityManagementPlanUpsertDto,
    WaterQualityManagementPlanUpsertDtoForm,
    WaterQualityManagementPlanUpsertDtoFormControls,
} from "src/app/shared/generated/model/water-quality-management-plan-upsert-dto";
import { WaterQualityManagementPlanStatusesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-status-enum";
import { WaterQualityManagementPlanPrioritiesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-priority-enum";
import { WaterQualityManagementPlanDevelopmentTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-development-type-enum";
import { WaterQualityManagementPlanLandUsesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-land-use-enum";
import { WaterQualityManagementPlanPermitTermsAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-permit-term-enum";
import {
    WaterQualityManagementPlanModelingApproachEnum,
    WaterQualityManagementPlanModelingApproachesAsSelectDropdownOptions,
} from "src/app/shared/generated/enum/water-quality-management-plan-modeling-approach-enum";
import { HydromodificationAppliesTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/hydromodification-applies-type-enum";
import { TrashCaptureStatusTypeEnum, TrashCaptureStatusTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/trash-capture-status-type-enum";
import { DialogRef } from "@ngneat/dialog";

@Component({
    selector: "wqmp-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent, AsyncPipe],
    templateUrl: "./wqmp-modal.component.html",
    styleUrls: ["./wqmp-modal.component.scss"],
})
export class WqmpModalComponent implements OnInit {
    public ref: DialogRef<{ mode: "add" }, boolean> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public mode: "add";

    public formGroup = new FormGroup<WaterQualityManagementPlanUpsertDtoForm>({
        WaterQualityManagementPlanName: WaterQualityManagementPlanUpsertDtoFormControls.WaterQualityManagementPlanName(undefined, { validators: [Validators.required] }),
        StormwaterJurisdictionID: WaterQualityManagementPlanUpsertDtoFormControls.StormwaterJurisdictionID(undefined, { validators: [Validators.required] }),
        WaterQualityManagementPlanPriorityID: WaterQualityManagementPlanUpsertDtoFormControls.WaterQualityManagementPlanPriorityID(),
        WaterQualityManagementPlanStatusID: WaterQualityManagementPlanUpsertDtoFormControls.WaterQualityManagementPlanStatusID(),
        WaterQualityManagementPlanDevelopmentTypeID: WaterQualityManagementPlanUpsertDtoFormControls.WaterQualityManagementPlanDevelopmentTypeID(),
        WaterQualityManagementPlanLandUseID: WaterQualityManagementPlanUpsertDtoFormControls.WaterQualityManagementPlanLandUseID(),
        WaterQualityManagementPlanPermitTermID: WaterQualityManagementPlanUpsertDtoFormControls.WaterQualityManagementPlanPermitTermID(),
        WaterQualityManagementPlanModelingApproachID: WaterQualityManagementPlanUpsertDtoFormControls.WaterQualityManagementPlanModelingApproachID(
            WaterQualityManagementPlanModelingApproachEnum.Detailed,
            { validators: [Validators.required] }
        ),
        ApprovalDate: WaterQualityManagementPlanUpsertDtoFormControls.ApprovalDate(),
        DateOfConstruction: WaterQualityManagementPlanUpsertDtoFormControls.DateOfConstruction(),
        HydromodificationAppliesTypeID: WaterQualityManagementPlanUpsertDtoFormControls.HydromodificationAppliesTypeID(),
        HydrologicSubareaID: WaterQualityManagementPlanUpsertDtoFormControls.HydrologicSubareaID(),
        RecordNumber: WaterQualityManagementPlanUpsertDtoFormControls.RecordNumber(),
        RecordedWQMPAreaInAcres: WaterQualityManagementPlanUpsertDtoFormControls.RecordedWQMPAreaInAcres(),
        TrashCaptureStatusTypeID: WaterQualityManagementPlanUpsertDtoFormControls.TrashCaptureStatusTypeID(
            TrashCaptureStatusTypeEnum.NotProvided,
            { validators: [Validators.required] }
        ),
        TrashCaptureEffectiveness: WaterQualityManagementPlanUpsertDtoFormControls.TrashCaptureEffectiveness(),
        MaintenanceContactName: WaterQualityManagementPlanUpsertDtoFormControls.MaintenanceContactName(),
        MaintenanceContactOrganization: WaterQualityManagementPlanUpsertDtoFormControls.MaintenanceContactOrganization(),
        MaintenanceContactPhone: WaterQualityManagementPlanUpsertDtoFormControls.MaintenanceContactPhone(),
        MaintenanceContactAddress1: WaterQualityManagementPlanUpsertDtoFormControls.MaintenanceContactAddress1(),
        MaintenanceContactAddress2: WaterQualityManagementPlanUpsertDtoFormControls.MaintenanceContactAddress2(),
        MaintenanceContactCity: WaterQualityManagementPlanUpsertDtoFormControls.MaintenanceContactCity(),
        MaintenanceContactState: WaterQualityManagementPlanUpsertDtoFormControls.MaintenanceContactState(),
        MaintenanceContactZip: WaterQualityManagementPlanUpsertDtoFormControls.MaintenanceContactZip(),
    });

    // Static enum options
    public statusOptions = WaterQualityManagementPlanStatusesAsSelectDropdownOptions;
    public priorityOptions = WaterQualityManagementPlanPrioritiesAsSelectDropdownOptions;
    public developmentTypeOptions = WaterQualityManagementPlanDevelopmentTypesAsSelectDropdownOptions;
    public landUseOptions = WaterQualityManagementPlanLandUsesAsSelectDropdownOptions;
    public permitTermOptions = WaterQualityManagementPlanPermitTermsAsSelectDropdownOptions;
    public modelingApproachOptions = WaterQualityManagementPlanModelingApproachesAsSelectDropdownOptions;
    public hydromodificationOptions = HydromodificationAppliesTypesAsSelectDropdownOptions;
    public trashCaptureStatusOptions = TrashCaptureStatusTypesAsSelectDropdownOptions;

    // Dynamic options loaded from API
    public jurisdictionOptions$: Observable<SelectDropdownOption[]>;
    public hydrologicSubareaOptions$: Observable<SelectDropdownOption[]>;

    // Conditional field visibility
    public showTrashCaptureEffectiveness$: Observable<boolean>;

    constructor(
        private alertService: AlertService,
        private wqmpService: WaterQualityManagementPlanService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService
    ) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.mode = this.ref.data.mode;

        this.jurisdictionOptions$ = this.stormwaterJurisdictionService.listStormwaterJurisdiction().pipe(
            map((jurisdictions) =>
                jurisdictions.map(
                    (j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as SelectDropdownOption
                )
            )
        );

        this.hydrologicSubareaOptions$ = this.wqmpService.listHydrologicSubareasWaterQualityManagementPlan().pipe(
            map((subareas) =>
                subareas.map((s) => ({ Label: s.HydrologicSubareaName, Value: s.HydrologicSubareaID, disabled: false }) as SelectDropdownOption)
            )
        );

        this.showTrashCaptureEffectiveness$ = this.formGroup.controls.TrashCaptureStatusTypeID.valueChanges.pipe(
            map((value) => value === TrashCaptureStatusTypeEnum.Partial)
        );
    }

    save(): void {
        if (this.formGroup.invalid) return;
        const dto = new WaterQualityManagementPlanUpsertDto(this.formGroup.value);
        this.wqmpService.createWaterQualityManagementPlan(dto).subscribe({
            next: () => {
                this.ref.close(true);
            },
            error: () => {},
        });
    }

    cancel(): void {
        this.ref.close(null);
    }
}
