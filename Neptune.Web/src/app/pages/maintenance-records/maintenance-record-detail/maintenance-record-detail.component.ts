import { Component, inject, Input } from "@angular/core";
import { AsyncPipe, DatePipe } from "@angular/common";
import { Router, RouterLink } from "@angular/router";
import { BehaviorSubject, catchError, forkJoin, Observable, of, switchMap } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";

import { MaintenanceRecordService } from "src/app/shared/generated/api/maintenance-record.service";
import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { MaintenanceRecordDetailDto } from "src/app/shared/generated/model/maintenance-record-detail-dto";
import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { TreatmentBMPTypeCustomAttributeTypeDto } from "src/app/shared/generated/model/treatment-bmp-type-custom-attribute-type-dto";
import { CustomAttributeTypePurposeEnum } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";

import { AuthenticationService } from "src/app/services/authentication.service";
import { AlertService } from "src/app/shared/services/alert.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

interface MaintenanceRecordDetailVm {
    record: MaintenanceRecordDetailDto;
    workflow: FieldVisitWorkflowDto;
    attributes: TreatmentBMPTypeCustomAttributeTypeDto[];
}

/**
 * NPT-1056: SPA port of the legacy MVC `MaintenanceRecord/Detail.cshtml`. Surfaces a single
 * maintenance record — basics (type / performed by / description / date) and the BMP type's
 * maintenance custom-attribute values. Edit + Delete are manager-gated; Edit jumps into the
 * existing workflow maintenance-edit step.
 */
@Component({
    selector: "maintenance-record-detail",
    standalone: true,
    imports: [AsyncPipe, DatePipe, RouterLink, PageHeaderComponent, AlertDisplayComponent, LoadingDirective],
    templateUrl: "./maintenance-record-detail.component.html",
    styleUrl: "./maintenance-record-detail.component.scss",
})
export class MaintenanceRecordDetailComponent {
    private maintenanceRecordService = inject(MaintenanceRecordService);
    private fieldVisitService = inject(FieldVisitService);
    private treatmentBMPTypeService = inject(TreatmentBMPTypeService);
    private authenticationService = inject(AuthenticationService);
    private confirmService = inject(ConfirmService);
    private alertService = inject(AlertService);
    private router = inject(Router);

    @Input() maintenanceRecordID!: number;

    private reload$ = new BehaviorSubject<void>(undefined);

    public vm$: Observable<MaintenanceRecordDetailVm | null> = this.reload$.pipe(
        switchMap(() =>
            this.maintenanceRecordService.getByIDMaintenanceRecord(this.maintenanceRecordID).pipe(
                switchMap((record) => {
                    if (record.FieldVisitID == null || record.TreatmentBMPTypeID == null) {
                        return of(null as MaintenanceRecordDetailVm | null);
                    }
                    return forkJoin({
                        workflow: this.fieldVisitService.getByIDFieldVisit(record.FieldVisitID),
                        attributes: this.treatmentBMPTypeService.listCustomAttributeTypesTreatmentBMPType(record.TreatmentBMPTypeID),
                    }).pipe(
                        switchMap(({ workflow, attributes }) => {
                            this.isLoading = false;
                            const maintenanceAttributes = (attributes ?? []).filter(
                                (t) => t.CustomAttributeType?.CustomAttributeTypePurposeID === CustomAttributeTypePurposeEnum.Maintenance,
                            );
                            return of({ record, workflow, attributes: maintenanceAttributes } satisfies MaintenanceRecordDetailVm);
                        }),
                    );
                }),
                catchError(() => {
                    this.isLoading = false;
                    return of(null as MaintenanceRecordDetailVm | null);
                }),
            ),
        ),
    );

    public isLoading = true;

    /** Edit gate. Mirrors the legacy MVC `MaintenanceRecordManageFeature` (Editor + Manager +
     * Admin + SitkaAdmin). */
    public get canEdit(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    }

    /** Delete gate. The API's `DELETE /maintenance-records/{id}` is `[JurisdictionManageFeature]`
     * — tighter than Edit (excludes JurisdictionEditor). Matches the API's "destructive
     * attestation action" intent. */
    public get canDelete(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    /**
     * Resolves the recorded value for a maintenance custom-attribute. Mirrors the legacy
     * `MaintenanceRecord.GetObservationValueForAttributeType` — comma-joins multi-value
     * attributes and appends the measurement unit when one is configured (skipping the
     * "None" sentinel for unitless boolean attributes). Duplicated from
     * `field-visit-detail-readonly` intentionally.
     */
    public maintenanceAttributeValue(attribute: TreatmentBMPTypeCustomAttributeTypeDto, record: MaintenanceRecordDetailDto): string {
        const customAttributeTypeID = attribute.CustomAttributeType?.CustomAttributeTypeID;
        if (customAttributeTypeID == null || !record.Observations?.length) return "—";
        const obs = record.Observations.find((o) => o.CustomAttributeTypeID === customAttributeTypeID);
        const values = (obs?.Values ?? [])
            .map((v) => (v?.ObservationValue ?? "").trim())
            .filter((s) => s.length > 0);
        if (values.length === 0) return "—";
        const joined = values.join(", ");
        const unit = attribute.CustomAttributeType?.MeasurementUnitDisplayName?.trim();
        const hasMeaningfulUnit = !!unit && unit.toLowerCase() !== "none";
        return hasMeaningfulUnit ? `${joined} ${unit}` : joined;
    }

    public delete(vm: MaintenanceRecordDetailVm): void {
        this.confirmService
            .confirm({
                title: "Delete Maintenance Record",
                message: "Delete this Maintenance Record? Observation values attached to this record will also be deleted. This cannot be undone.",
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.maintenanceRecordService.deleteMaintenanceRecord(vm.record.MaintenanceRecordID!).subscribe({
                    next: () => {
                        this.alertService.pushAlert(new Alert("Maintenance Record deleted.", AlertContext.Success));
                        this.router.navigate(["/treatment-bmps", vm.record.TreatmentBMPID]);
                    },
                    error: () => this.alertService.pushAlert(new Alert("Failed to delete Maintenance Record.", AlertContext.Danger)),
                });
            });
    }
}
