import { Component, inject, OnInit } from "@angular/core";
import { FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { Router, RouterModule } from "@angular/router";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { LatLonPickerComponent } from "src/app/shared/components/lat-lon-picker/lat-lon-picker.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { TreatmentBMPCreateDto, TreatmentBMPCreateDtoForm, TreatmentBMPCreateDtoFormControls } from "src/app/shared/generated/model/treatment-bmp-create-dto";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { map, Observable } from "rxjs";
import { AsyncPipe } from "@angular/common";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { OrganizationService } from "src/app/shared/generated/api/organization.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { TreatmentBMPLifespanTypeEnum, TreatmentBMPLifespanTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/treatment-b-m-p-lifespan-type-enum";
import { SizingBasisTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/sizing-basis-type-enum";
import { TrashCaptureStatusTypeEnum, TrashCaptureStatusTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/trash-capture-status-type-enum";
import { IDeactivateComponent } from "src/app/shared/guards/unsaved-changes.guard";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPDto } from "src/app/shared/generated/model/treatment-bmp-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { validateFormOrAlert } from "src/app/shared/helpers/form-validation-helper";

@Component({
    selector: "create-treatment-bmp",
    imports: [PageHeaderComponent, RouterModule, ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent, AsyncPipe, LatLonPickerComponent],
    templateUrl: "./create-treatment-bmp.component.html",
    styleUrls: ["./create-treatment-bmp.component.scss"],
})
export class CreateTreatmentBmpComponent implements OnInit, IDeactivateComponent {
    private treatmentBMPService = inject(TreatmentBMPService);
    private treatmentBMPTypeService = inject(TreatmentBMPTypeService);
    private stormwaterJurisdictionService = inject(StormwaterJurisdictionService);
    private organizationService = inject(OrganizationService);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private router = inject(Router);
    private alertService = inject(AlertService);

    public treatmentBMPTypeSelectOptions$: Observable<SelectDropdownOption[]>;
    public stormwaterJurisdictionSelectOptions$: Observable<SelectDropdownOption[]>;
    public organizationSelectOptions$: Observable<SelectDropdownOption[]>;
    public wqmpSelectOptions$: Observable<SelectDropdownOption[]>;

    public lifeSpanTypeSelectOptions: SelectDropdownOption[];
    public showLifeSpanEndDateField$: Observable<boolean> = new Observable<boolean>();

    public sizingBasisTypeSelectOptions: SelectDropdownOption[];

    public trashCaptureStatusTypeSelectOptions: SelectDropdownOption[];
    public showTrashCaptureEffectivenessField$: Observable<boolean> = new Observable<boolean>();

    public isLoadingSubmit: boolean = false;

    public formGroup: FormGroup<TreatmentBMPCreateDtoForm> = new FormGroup<TreatmentBMPCreateDtoForm>({
        TreatmentBMPName: TreatmentBMPCreateDtoFormControls.TreatmentBMPName(undefined),
        TreatmentBMPTypeID: TreatmentBMPCreateDtoFormControls.TreatmentBMPTypeID(undefined),
        StormwaterJurisdictionID: TreatmentBMPCreateDtoFormControls.StormwaterJurisdictionID(undefined),
        OwnerOrganizationID: TreatmentBMPCreateDtoFormControls.OwnerOrganizationID(undefined),
        YearBuilt: TreatmentBMPCreateDtoFormControls.YearBuilt(undefined),
        SystemOfRecordID: TreatmentBMPCreateDtoFormControls.SystemOfRecordID(undefined),
        WaterQualityManagementPlanID: TreatmentBMPCreateDtoFormControls.WaterQualityManagementPlanID(undefined),
        TreatmentBMPLifespanTypeID: TreatmentBMPCreateDtoFormControls.TreatmentBMPLifespanTypeID(undefined),
        TreatmentBMPLifespanEndDate: TreatmentBMPCreateDtoFormControls.TreatmentBMPLifespanEndDate(undefined),
        SizingBasisTypeID: TreatmentBMPCreateDtoFormControls.SizingBasisTypeID(undefined),
        TrashCaptureStatusTypeID: TreatmentBMPCreateDtoFormControls.TrashCaptureStatusTypeID(undefined),
        TrashCaptureEffectiveness: TreatmentBMPCreateDtoFormControls.TrashCaptureEffectiveness(undefined),
        RequiredFieldVisitsPerYear: TreatmentBMPCreateDtoFormControls.RequiredFieldVisitsPerYear(undefined),
        RequiredPostStormFieldVisitsPerYear: TreatmentBMPCreateDtoFormControls.RequiredPostStormFieldVisitsPerYear(undefined),
        Notes: TreatmentBMPCreateDtoFormControls.Notes(undefined),
        Latitude: TreatmentBMPCreateDtoFormControls.Latitude(undefined),
        Longitude: TreatmentBMPCreateDtoFormControls.Longitude(undefined),
    });

    public FormFieldType = FormFieldType;

    ngOnInit(): void {
        this.treatmentBMPTypeSelectOptions$ = this.treatmentBMPTypeService
            .listTreatmentBMPType()
            .pipe(map((types) => types.map((type) => ({ Label: type.TreatmentBMPTypeName, Value: type.TreatmentBMPTypeID, disabled: false }) as SelectDropdownOption)));

        this.stormwaterJurisdictionSelectOptions$ = this.stormwaterJurisdictionService
            .listStormwaterJurisdiction()
            .pipe(
                map((jurisdictions) =>
                    jurisdictions.map(
                        (jurisdiction) =>
                            ({ Label: jurisdiction.StormwaterJurisdictionName, Value: jurisdiction.StormwaterJurisdictionID, disabled: false }) as SelectDropdownOption
                    )
                )
            );

        this.organizationSelectOptions$ = this.organizationService.listOrganization().pipe(
            map((organizations) => {
                let sameAsJurisdictionOptions: SelectDropdownOption = { Label: "Same as the Jurisdiction's Organization", Value: null, disabled: false };
                return [
                    sameAsJurisdictionOptions,
                    ...organizations.map((organization) => ({ Label: organization.OrganizationName, Value: organization.OrganizationID, disabled: false }) as SelectDropdownOption),
                ];
            })
        );

        this.wqmpSelectOptions$ = this.wqmpService
            .listAsDisplayDtosWaterQualityManagementPlan()
            .pipe(
                map((wqmps) =>
                    wqmps.map((wqmp) => ({ Label: wqmp.WaterQualityManagementPlanName, Value: wqmp.WaterQualityManagementPlanID, disabled: false }) as SelectDropdownOption)
                )
            );

        this.lifeSpanTypeSelectOptions = TreatmentBMPLifespanTypesAsSelectDropdownOptions;
        this.showLifeSpanEndDateField$ = this.formGroup.controls.TreatmentBMPLifespanTypeID.valueChanges.pipe(map((value) => value === TreatmentBMPLifespanTypeEnum.FixedEndDate));

        this.sizingBasisTypeSelectOptions = SizingBasisTypesAsSelectDropdownOptions;
        this.trashCaptureStatusTypeSelectOptions = TrashCaptureStatusTypesAsSelectDropdownOptions;

        this.showTrashCaptureEffectivenessField$ = this.formGroup.controls.TrashCaptureStatusTypeID.valueChanges.pipe(
            map((value) => {
                return value === TrashCaptureStatusTypeEnum.Partial;
            })
        );
    }

    public canExit(): boolean {
        return this.formGroup.pristine;
    }

    public save(): void {
        if (!validateFormOrAlert(this.formGroup, this.alertService)) return;

        this.isLoadingSubmit = true;

        let createDto = this.formGroup.value as TreatmentBMPCreateDto;
        this.treatmentBMPService.createTreatmentBMP(createDto).subscribe({
            next: (treatmentBMPDto: TreatmentBMPDto) => {
                this.isLoadingSubmit = false;
                this.formGroup.markAsPristine();
                this.router.navigate(["/treatment-bmps", treatmentBMPDto.TreatmentBMPID]);
            },
            error: () => {
                this.isLoadingSubmit = false;
            },
        });
    }

    public cancel(): void {
        this.router.navigate(["/treatment-bmps"]);
    }
}
