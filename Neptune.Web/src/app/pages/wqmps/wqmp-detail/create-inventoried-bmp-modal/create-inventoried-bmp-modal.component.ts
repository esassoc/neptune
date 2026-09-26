import { Component, DestroyRef, inject, OnDestroy, OnInit } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { AsyncPipe } from "@angular/common";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { map, Observable, of, startWith } from "rxjs";
import * as L from "leaflet";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { LatLonPickerComponent } from "src/app/shared/components/lat-lon-picker/lat-lon-picker.component";
import { NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { WqmpsLayerComponent } from "src/app/shared/components/leaflet/layers/wqmps-layer/wqmps-layer.component";
import { TreatmentBMPReferenceLayerComponent } from "src/app/shared/components/leaflet/layers/treatment-bmp-reference-layer/treatment-bmp-reference-layer.component";
import { OverlayMode } from "src/app/shared/components/leaflet/layers/generic-wms-wfs-layer/overlay-mode.enum";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { TreatmentBMPMinimalDto } from "src/app/shared/generated/model/treatment-bmp-minimal-dto";
import { TreatmentBMPCreateDto, TreatmentBMPCreateDtoForm, TreatmentBMPCreateDtoFormControls } from "src/app/shared/generated/model/treatment-bmp-create-dto";
import { SizingBasisTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/sizing-basis-type-enum";
import { TrashCaptureStatusTypeEnum, TrashCaptureStatusTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/trash-capture-status-type-enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { validateFormOrAlert } from "src/app/shared/helpers/form-validation-helper";

@Component({
    selector: "create-inventoried-bmp-modal",
    imports: [AlertDisplayComponent, ReactiveFormsModule, FormFieldComponent, AsyncPipe, LatLonPickerComponent, WqmpsLayerComponent, TreatmentBMPReferenceLayerComponent],
    templateUrl: "./create-inventoried-bmp-modal.component.html",
    styleUrls: ["./create-inventoried-bmp-modal.component.scss"],
})
export class CreateInventoriedBMPModalComponent implements OnInit, OnDestroy {
    public ref: DialogRef<CreateInventoriedBMPModalContext, boolean> = inject(DialogRef);
    private treatmentBMPService = inject(TreatmentBMPService);
    private treatmentBMPTypeService = inject(TreatmentBMPTypeService);
    private stormwaterJurisdictionService = inject(StormwaterJurisdictionService);
    private alertService = inject(AlertService);
    private destroyRef = inject(DestroyRef);

    public FormFieldType = FormFieldType;
    public OverlayMode = OverlayMode;

    public treatmentBMPTypeSelectOptions$: Observable<SelectDropdownOption[]>;
    public sizingBasisTypeSelectOptions: SelectDropdownOption[] = SizingBasisTypesAsSelectDropdownOptions;
    public trashCaptureStatusTypeSelectOptions: SelectDropdownOption[] = TrashCaptureStatusTypesAsSelectDropdownOptions;
    public showTrashCaptureEffectivenessField$: Observable<boolean>;

    // The WQMP boundary's extent when it has one, otherwise the jurisdiction's. neptune-map only reads
    // its bounding box at init, so the picker isn't rendered until this resolves.
    public boundingBox$: Observable<BoundingBoxDto>;
    public existingBMPsWithCoordinates: TreatmentBMPMinimalDto[] = [];

    public map: L.Map;
    public layerControl: any;
    public mapIsReady: boolean = false;
    private mapResizeTimeout: ReturnType<typeof setTimeout>;

    public isLoadingSubmit: boolean = false;

    public formGroup: FormGroup<TreatmentBMPCreateDtoForm> = new FormGroup<TreatmentBMPCreateDtoForm>({
        TreatmentBMPName: TreatmentBMPCreateDtoFormControls.TreatmentBMPName(undefined),
        TreatmentBMPTypeID: TreatmentBMPCreateDtoFormControls.TreatmentBMPTypeID(undefined),
        StormwaterJurisdictionID: TreatmentBMPCreateDtoFormControls.StormwaterJurisdictionID(undefined),
        WaterQualityManagementPlanID: TreatmentBMPCreateDtoFormControls.WaterQualityManagementPlanID(undefined),
        SizingBasisTypeID: TreatmentBMPCreateDtoFormControls.SizingBasisTypeID(undefined),
        TrashCaptureStatusTypeID: TreatmentBMPCreateDtoFormControls.TrashCaptureStatusTypeID(undefined),
        TrashCaptureEffectiveness: TreatmentBMPCreateDtoFormControls.TrashCaptureEffectiveness(undefined),
        Latitude: TreatmentBMPCreateDtoFormControls.Latitude(undefined),
        Longitude: TreatmentBMPCreateDtoFormControls.Longitude(undefined),
    });

    ngOnInit(): void {
        this.alertService.clearAlerts();

        const data = this.ref.data;
        this.formGroup.patchValue({
            StormwaterJurisdictionID: data.stormwaterJurisdictionID,
            WaterQualityManagementPlanID: data.wqmpID,
        });

        this.treatmentBMPTypeSelectOptions$ = this.treatmentBMPTypeService
            .listTreatmentBMPType()
            .pipe(map((types) => types.map((type) => ({ Label: type.TreatmentBMPTypeName, Value: type.TreatmentBMPTypeID, disabled: false }) as SelectDropdownOption)));

        const trashCaptureStatusControl = this.formGroup.controls.TrashCaptureStatusTypeID;
        this.showTrashCaptureEffectivenessField$ = trashCaptureStatusControl.valueChanges.pipe(
            startWith(trashCaptureStatusControl.value),
            map((value) => value === TrashCaptureStatusTypeEnum.Partial)
        );
        // Effectiveness only applies to Partial (the server discards it otherwise). Clear it when the field
        // is hidden so a stale out-of-range value can't fail validation on a control the user can't see.
        trashCaptureStatusControl.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((value) => {
            if (value !== TrashCaptureStatusTypeEnum.Partial) {
                this.formGroup.controls.TrashCaptureEffectiveness.reset();
            }
        });

        this.boundingBox$ = data.boundingBox
            ? of(data.boundingBox)
            : this.stormwaterJurisdictionService.getBoundingBoxByJurisdictionIDStormwaterJurisdiction(data.stormwaterJurisdictionID);

        this.existingBMPsWithCoordinates = (data.existingTreatmentBMPs ?? []).filter((bmp) => bmp.Latitude != null && bmp.Longitude != null);
    }

    public handleMapLoad(event: NeptuneMapInitEvent, boundingBox: BoundingBoxDto): void {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;

        // The dialog's open animation leaves Leaflet measuring a container that is still resizing,
        // so re-measure and re-fit once it has settled. Cancelled in ngOnDestroy in case the modal
        // closes first and the map is torn down.
        this.mapResizeTimeout = setTimeout(() => {
            this.map.invalidateSize(true);
            if (boundingBox) {
                this.map.fitBounds([
                    [boundingBox.Bottom, boundingBox.Left],
                    [boundingBox.Top, boundingBox.Right],
                ]);
            }
        }, 300);
    }

    public save(): void {
        if (!validateFormOrAlert(this.formGroup, this.alertService)) return;

        this.isLoadingSubmit = true;
        this.treatmentBMPService.createTreatmentBMP(this.formGroup.getRawValue() as TreatmentBMPCreateDto).subscribe({
            next: () => {
                this.ref.close(true);
            },
            error: () => {
                // httpErrorInterceptor surfaces the server's validation messages in the modal's alert display
                this.isLoadingSubmit = false;
            },
        });
    }

    public cancel(): void {
        this.ref.close(null);
    }

    ngOnDestroy(): void {
        clearTimeout(this.mapResizeTimeout);
    }
}

export class CreateInventoriedBMPModalContext {
    wqmpID: number;
    wqmpName: string;
    stormwaterJurisdictionID: number;
    stormwaterJurisdictionName: string;
    // the WQMP boundary's extent; unset when the WQMP has no parcels
    boundingBox?: BoundingBoxDto;
    existingTreatmentBMPs: TreatmentBMPMinimalDto[];
}
