import { Component, EventEmitter, inject, Input, OnInit, Output, TemplateRef } from "@angular/core";
import { FormGroup, ReactiveFormsModule } from "@angular/forms";
import { Observable } from "rxjs";
import { tap } from "rxjs/operators";
import { AsyncPipe } from "@angular/common";

import { LatLonPickerComponent } from "src/app/shared/components/lat-lon-picker/lat-lon-picker.component";
import { TreatmentBMPService } from "src/app/shared/generated/api/treatment-bmp.service";
import {
    TreatmentBMPLocationUpdateDto,
    TreatmentBMPLocationUpdateDtoForm,
    TreatmentBMPLocationUpdateDtoFormControls,
} from "src/app/shared/generated/model/treatment-bmp-location-update-dto";
import { TreatmentBMPDto } from "src/app/shared/generated/model/treatment-bmp-dto";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/**
 * Reusable embedded editor for a Treatment BMP's location. Used by the BMP detail
 * routed edit page (/treatment-bmps/:id/edit-location) and by the Field Visit
 * Inventory workflow step's Location panel — both share the form, field-validation,
 * and persistence logic without round-tripping the user to the detail page.
 *
 * The host owns chrome (page-header, breadcrumb) and Save/Cancel buttons. This
 * component exposes pristine state via canExit() and emits (saved)/(cancelled)
 * so the host can route or refresh after persistence.
 */
@Component({
    selector: "treatment-bmp-location-editor",
    standalone: true,
    imports: [ReactiveFormsModule, AsyncPipe, LatLonPickerComponent],
    templateUrl: "./treatment-bmp-location-editor.component.html",
})
export class TreatmentBmpLocationEditorComponent implements OnInit {
    private treatmentBMPService = inject(TreatmentBMPService);
    private alertService = inject(AlertService);

    @Input() treatmentBMPID!: number;
    /** Optional override for the lat-lon-picker instructions template. */
    @Input() instructionsTemplate?: TemplateRef<unknown>;
    /** When true, suppresses the built-in Save/Cancel footer so a host (e.g. the field-visit
     * workflow) can render its own button row and drive saves via @ViewChild + save(). */
    @Input() hideFooter = false;

    @Output() saved = new EventEmitter<TreatmentBMPDto>();
    @Output() cancelled = new EventEmitter<void>();

    public formGroup: FormGroup<TreatmentBMPLocationUpdateDtoForm> = new FormGroup<TreatmentBMPLocationUpdateDtoForm>({
        Latitude: TreatmentBMPLocationUpdateDtoFormControls.Latitude(undefined),
        Longitude: TreatmentBMPLocationUpdateDtoFormControls.Longitude(undefined),
    });

    public treatmentBMP$!: Observable<TreatmentBMPDto>;
    public isLoadingSubmit = false;

    ngOnInit(): void {
        this.treatmentBMP$ = this.treatmentBMPService.getByIDTreatmentBMP(this.treatmentBMPID).pipe(
            tap((bmp) => {
                if (bmp.Latitude != null && bmp.Longitude != null) {
                    this.formGroup.controls.Latitude.setValue(bmp.Latitude);
                    this.formGroup.controls.Longitude.setValue(bmp.Longitude);
                    this.formGroup.markAsPristine();
                }
            })
        );
    }

    public canExit(): boolean {
        return this.formGroup.pristine;
    }

    public save(): void {
        this.isLoadingSubmit = true;
        const dto = this.formGroup.value as TreatmentBMPLocationUpdateDto;
        this.treatmentBMPService.updateLocationTreatmentBMP(this.treatmentBMPID, dto).subscribe({
            next: (bmp) => {
                this.isLoadingSubmit = false;
                this.formGroup.markAsPristine();
                this.alertService.pushAlert(new Alert("Treatment BMP location updated successfully.", AlertContext.Success));
                this.saved.emit(bmp);
            },
            error: () => {
                this.isLoadingSubmit = false;
            },
        });
    }

    public cancel(): void {
        this.cancelled.emit();
    }
}
