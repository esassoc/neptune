import { Component, inject, Input, OnInit } from "@angular/core";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";
import { Router, RouterModule, ActivatedRoute } from "@angular/router";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { FieldDefinitionComponent } from "src/app/shared/components/field-definition/field-definition.component";
import { AsyncPipe } from "@angular/common";
import { Observable, map, firstValueFrom, of, delay, tap, combineLatest } from "rxjs";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { OrganizationService } from "src/app/shared/generated/api/organization.service";
import {
    TreatmentBMPBasicInfoUpdateDto,
    TreatmentBMPBasicInfoUpdateDtoForm,
    TreatmentBMPBasicInfoUpdateDtoFormControls,
} from "src/app/shared/generated/model/treatment-bmp-basic-info-update-dto";
import { TreatmentBMPLifespanTypeEnum, TreatmentBMPLifespanTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/treatment-b-m-p-lifespan-type-enum";
import { SizingBasisTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/sizing-basis-type-enum";
import { TrashCaptureStatusTypeEnum, TrashCaptureStatusTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/trash-capture-status-type-enum";
import { TreatmentBMPDto } from "src/app/shared/generated/model/treatment-bmp-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { validateFormOrAlert } from "src/app/shared/helpers/form-validation-helper";

@Component({
    selector: "treatment-bmp-update-basic-info",
    imports: [PageHeaderComponent, RouterModule, ReactiveFormsModule, FormFieldComponent, FieldDefinitionComponent, AlertDisplayComponent, AsyncPipe],
    templateUrl: "./treatment-bmp-update-basic-info.component.html",
    styleUrls: ["./treatment-bmp-update-basic-info.component.scss"],
})
export class TreatmentBmpUpdateBasicInfoComponent implements OnInit {
    private treatmentBMPService = inject(TreatmentBMPService);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private organizationService = inject(OrganizationService);
    private router = inject(Router);
    private alertService = inject(AlertService);

    @Input() treatmentBMPID?: number;
    public treatmentBMP$: Observable<TreatmentBMPDto>;

    public wqmpSelectOptions$: Observable<SelectDropdownOption[]>;
    public organizationSelectOptions$: Observable<SelectDropdownOption[]>;

    //Without using a async pipe in the template, it was displaying the placeholder text, then the selected value.
    public lifeSpanTypeSelectOptions$: Observable<SelectDropdownOption[]> = of(TreatmentBMPLifespanTypesAsSelectDropdownOptions).pipe(delay(0));
    public sizingBasisTypeSelectOptions$: Observable<SelectDropdownOption[]> = of(SizingBasisTypesAsSelectDropdownOptions).pipe(delay(0));
    public trashCaptureStatusTypeSelectOptions$: Observable<SelectDropdownOption[]> = of(TrashCaptureStatusTypesAsSelectDropdownOptions).pipe(delay(0));

    public showLifeSpanEndDateField$: Observable<boolean> = new Observable<boolean>();
    public showTrashCaptureEffectivenessField$: Observable<boolean> = new Observable<boolean>();

    public isLoadingSubmit: boolean = false;

    public formGroup: FormGroup<TreatmentBMPBasicInfoUpdateDtoForm> = new FormGroup<TreatmentBMPBasicInfoUpdateDtoForm>({
        TreatmentBMPName: TreatmentBMPBasicInfoUpdateDtoFormControls.TreatmentBMPName(undefined),
        OwnerOrganizationID: TreatmentBMPBasicInfoUpdateDtoFormControls.OwnerOrganizationID(undefined),
        YearBuilt: TreatmentBMPBasicInfoUpdateDtoFormControls.YearBuilt(undefined),
        SystemOfRecordID: TreatmentBMPBasicInfoUpdateDtoFormControls.SystemOfRecordID(undefined),
        WaterQualityManagementPlanID: TreatmentBMPBasicInfoUpdateDtoFormControls.WaterQualityManagementPlanID(undefined),
        TreatmentBMPLifespanTypeID: TreatmentBMPBasicInfoUpdateDtoFormControls.TreatmentBMPLifespanTypeID(undefined),
        TreatmentBMPLifespanEndDate: TreatmentBMPBasicInfoUpdateDtoFormControls.TreatmentBMPLifespanEndDate(undefined),
        SizingBasisTypeID: TreatmentBMPBasicInfoUpdateDtoFormControls.SizingBasisTypeID(undefined),
        TrashCaptureStatusTypeID: TreatmentBMPBasicInfoUpdateDtoFormControls.TrashCaptureStatusTypeID(undefined),
        TrashCaptureEffectiveness: TreatmentBMPBasicInfoUpdateDtoFormControls.TrashCaptureEffectiveness(undefined),
        RequiredFieldVisitsPerYear: TreatmentBMPBasicInfoUpdateDtoFormControls.RequiredFieldVisitsPerYear(undefined),
        RequiredPostStormFieldVisitsPerYear: TreatmentBMPBasicInfoUpdateDtoFormControls.RequiredPostStormFieldVisitsPerYear(undefined),
        Notes: TreatmentBMPBasicInfoUpdateDtoFormControls.Notes(undefined),
    });

    public FormFieldType = FormFieldType;

    ngOnInit(): void {
        this.treatmentBMP$ = this.treatmentBMPService.getByIDTreatmentBMP(this.treatmentBMPID!).pipe(
            tap((bmp) => {
                this.formGroup.controls.TreatmentBMPName.setValue(bmp.TreatmentBMPName ?? "");
                this.formGroup.controls.OwnerOrganizationID.setValue(bmp.OwnerOrganizationID ?? null);
                this.formGroup.controls.YearBuilt.setValue(bmp.YearBuilt ?? null);
                this.formGroup.controls.SystemOfRecordID.setValue(bmp.SystemOfRecordID ?? null);
                this.formGroup.controls.WaterQualityManagementPlanID.setValue(bmp.WaterQualityManagementPlanID ?? null);
                this.formGroup.controls.TreatmentBMPLifespanTypeID.setValue(bmp.TreatmentBMPLifespanType?.TreatmentBMPLifeSpanTypeID ?? null);
                const lifespanEndDate = bmp.TreatmentBMPLifespanEndDate ? new Date(bmp.TreatmentBMPLifespanEndDate).toISOString().split("T")[0] : null;
                this.formGroup.controls.TreatmentBMPLifespanEndDate.setValue(lifespanEndDate ?? null);
                this.formGroup.controls.SizingBasisTypeID.setValue(bmp.SizingBasisType?.SizingBasisTypeID ?? null);
                this.formGroup.controls.TrashCaptureStatusTypeID.setValue(bmp.TrashCaptureStatusType?.TrashCaptureStatusTypeID ?? null);
                // API returns TrashCaptureEffectiveness as string on DTO; convert to number when possible
                const trashCaptureEffectiveness = bmp.TrashCaptureEffectiveness != null ? Number(bmp.TrashCaptureEffectiveness) : null;
                this.formGroup.controls.TrashCaptureEffectiveness.setValue(!isNaN(trashCaptureEffectiveness) ? trashCaptureEffectiveness : null);
                this.formGroup.controls.RequiredFieldVisitsPerYear.setValue(bmp.RequiredFieldVisitsPerYear ?? null);
                this.formGroup.controls.RequiredPostStormFieldVisitsPerYear.setValue(bmp.RequiredPostStormFieldVisitsPerYear ?? null);
                this.formGroup.controls.Notes.setValue(bmp.Notes ?? null);
            })
        );

        this.wqmpSelectOptions$ = this.wqmpService
            .listAsDisplayDtosWaterQualityManagementPlan()
            .pipe(
                map((wqmps) =>
                    wqmps.map((wqmp) => ({ Label: wqmp.WaterQualityManagementPlanName, Value: wqmp.WaterQualityManagementPlanID, disabled: false }) as SelectDropdownOption)
                )
            );

        this.organizationSelectOptions$ = this.organizationService
            .listOrganization()
            .pipe(
                map((organizations) =>
                    organizations.map((organization) => ({ Label: organization.OrganizationName, Value: organization.OrganizationID, disabled: false }) as SelectDropdownOption)
                )
            );

        this.showLifeSpanEndDateField$ = combineLatest({ treatmentBMP: this.treatmentBMP$, controlUpdate: this.formGroup.controls.TreatmentBMPLifespanTypeID.valueChanges }).pipe(
            map(
                ({ treatmentBMP, controlUpdate }) =>
                    controlUpdate === TreatmentBMPLifespanTypeEnum.FixedEndDate ||
                    (treatmentBMP.TreatmentBMPLifespanType?.TreatmentBMPLifeSpanTypeID === TreatmentBMPLifespanTypeEnum.FixedEndDate && controlUpdate == null)
            )
        );

        this.showTrashCaptureEffectivenessField$ = combineLatest({
            treatmentBMP: this.treatmentBMP$,
            controlUpdate: this.formGroup.controls.TrashCaptureStatusTypeID.valueChanges,
        }).pipe(
            map(
                ({ treatmentBMP, controlUpdate }) =>
                    controlUpdate === TrashCaptureStatusTypeEnum.Partial ||
                    (treatmentBMP.TrashCaptureStatusType?.TrashCaptureStatusTypeID === TrashCaptureStatusTypeEnum.Partial && controlUpdate == null)
            )
        );
    }

    public canExit(): boolean {
        return this.formGroup.pristine;
    }

    public save(): void {
        if (!this.treatmentBMPID) return;
        if (!validateFormOrAlert(this.formGroup, this.alertService)) return;

        this.isLoadingSubmit = true;
        const updateDto = this.formGroup.value as TreatmentBMPBasicInfoUpdateDto;
        this.treatmentBMPService.updateBasicInfoTreatmentBMP(this.treatmentBMPID, updateDto).subscribe({
            next: (treatmentBMPDto: TreatmentBMPDto) => {
                this.isLoadingSubmit = false;
                this.formGroup.markAsPristine();
                this.router.navigate(["/treatment-bmps", treatmentBMPDto.TreatmentBMPID]).then(() => {
                    this.alertService.pushAlert(new Alert("BMP basic information updated successfully.", AlertContext.Success));
                });
            },
            error: () => {
                this.isLoadingSubmit = false;
            },
        });
    }

    public cancel(): void {
        if (this.treatmentBMPID) this.router.navigate(["/treatment-bmps", this.treatmentBMPID]);
        else this.router.navigate(["/treatment-bmps"]);
    }
}
