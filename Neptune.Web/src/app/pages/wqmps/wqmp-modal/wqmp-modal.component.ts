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
import { US_STATES } from "src/app/shared/constants/us-states";
import { DialogRef } from "@ngneat/dialog";
import { WaterQualityManagementPlanDto } from "src/app/shared/generated/model/water-quality-management-plan-dto";

@Component({
    selector: "wqmp-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent, AsyncPipe],
    templateUrl: "./wqmp-modal.component.html",
    styleUrls: ["./wqmp-modal.component.scss"],
})
export class WqmpModalComponent implements OnInit {
    public ref: DialogRef<{ mode: "add" | "edit"; wqmp?: WaterQualityManagementPlanDto }, boolean> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public US_STATES = US_STATES;
    public mode: "add" | "edit";

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

        this.jurisdictionOptions$ = this.stormwaterJurisdictionService.listViewableStormwaterJurisdiction().pipe(
            map((jurisdictions) => {
                const options = jurisdictions.map(
                    (j) => ({ Label: j.StormwaterJurisdictionName, Value: j.StormwaterJurisdictionID, disabled: false }) as SelectDropdownOption
                );
                if (options.length === 1 && this.mode === "add") {
                    this.formGroup.controls.StormwaterJurisdictionID.setValue(options[0].Value);
                }
                return options;
            })
        );

        this.hydrologicSubareaOptions$ = this.wqmpService.listHydrologicSubareasWaterQualityManagementPlan().pipe(
            map((subareas) =>
                subareas.map((s) => ({ Label: s.HydrologicSubareaName, Value: s.HydrologicSubareaID, disabled: false }) as SelectDropdownOption)
            )
        );

        this.showTrashCaptureEffectiveness$ = this.formGroup.controls.TrashCaptureStatusTypeID.valueChanges.pipe(
            map((value) => value === TrashCaptureStatusTypeEnum.Partial)
        );

        if (this.mode === "edit" && this.ref.data.wqmp) {
            this.formGroup.controls.StormwaterJurisdictionID.disable();
            const wqmp = this.ref.data.wqmp;
            this.formGroup.patchValue({
                WaterQualityManagementPlanName: wqmp.WaterQualityManagementPlanName,
                StormwaterJurisdictionID: wqmp.StormwaterJurisdictionID,
                WaterQualityManagementPlanPriorityID: wqmp.WaterQualityManagementPlanPriorityID,
                WaterQualityManagementPlanStatusID: wqmp.WaterQualityManagementPlanStatusID,
                WaterQualityManagementPlanDevelopmentTypeID: wqmp.WaterQualityManagementPlanDevelopmentTypeID,
                WaterQualityManagementPlanLandUseID: wqmp.WaterQualityManagementPlanLandUseID,
                WaterQualityManagementPlanPermitTermID: wqmp.WaterQualityManagementPlanPermitTermID,
                WaterQualityManagementPlanModelingApproachID: wqmp.WaterQualityManagementPlanModelingApproachID,
                ApprovalDate: wqmp.ApprovalDate,
                DateOfConstruction: wqmp.DateOfConstruction,
                HydromodificationAppliesTypeID: wqmp.HydromodificationAppliesTypeID,
                HydrologicSubareaID: wqmp.HydrologicSubareaID,
                RecordNumber: wqmp.RecordNumber,
                RecordedWQMPAreaInAcres: wqmp.RecordedWQMPAreaInAcres,
                TrashCaptureStatusTypeID: wqmp.TrashCaptureStatusTypeID,
                TrashCaptureEffectiveness: wqmp.TrashCaptureEffectiveness,
                MaintenanceContactName: wqmp.MaintenanceContactName,
                MaintenanceContactOrganization: wqmp.MaintenanceContactOrganization,
                MaintenanceContactPhone: wqmp.MaintenanceContactPhone,
                MaintenanceContactAddress1: wqmp.MaintenanceContactAddress1,
                MaintenanceContactAddress2: wqmp.MaintenanceContactAddress2,
                MaintenanceContactCity: wqmp.MaintenanceContactCity,
                MaintenanceContactState: wqmp.MaintenanceContactState,
                MaintenanceContactZip: wqmp.MaintenanceContactZip,
            });
        }
    }

    save(): void {
        if (this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            return;
        }
        const dto = new WaterQualityManagementPlanUpsertDto(this.formGroup.getRawValue());

        if (this.mode === "edit") {
            const wqmpID = this.ref.data.wqmp?.WaterQualityManagementPlanID;
            this.wqmpService.updateWaterQualityManagementPlan(wqmpID, dto).subscribe({
                next: () => {
                    this.ref.close(true);
                },
                error: () => {},
            });
        } else {
            this.wqmpService.createWaterQualityManagementPlan(dto).subscribe({
                next: () => {
                    this.ref.close(true);
                },
                error: () => {},
            });
        }
    }

    cancel(): void {
        this.ref.close(null);
    }
}
