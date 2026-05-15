import { Component, inject, Input, OnInit } from "@angular/core";
import { AsyncPipe, DatePipe, DecimalPipe } from "@angular/common";
import { ActivatedRoute, Router, RouterLink } from "@angular/router";
import { BehaviorSubject, forkJoin, Observable, of, switchMap } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { ImageCarouselComponent, ImageCarouselItem } from "src/app/shared/components/image-carousel/image-carousel.component";

import { FieldVisitService } from "src/app/shared/generated/api/field-visit.service";
import { TreatmentBMPAssessmentPhotoService } from "src/app/shared/generated/api/treatment-bmp-assessment-photo.service";
import { TreatmentBMPAssessmentService } from "src/app/shared/generated/api/treatment-bmp-assessment.service";
import { MaintenanceRecordService } from "src/app/shared/generated/api/maintenance-record.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { FieldVisitWorkflowDto } from "src/app/shared/generated/model/field-visit-workflow-dto";
import { TreatmentBMPAssessmentDetailDto } from "src/app/shared/generated/model/treatment-bmp-assessment-detail-dto";
import { MaintenanceRecordDetailDto } from "src/app/shared/generated/model/maintenance-record-detail-dto";
import { TreatmentBMPTypeCustomAttributeTypeDto } from "src/app/shared/generated/model/treatment-bmp-type-custom-attribute-type-dto";
import { CustomAttributeTypePurposeEnum } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";

import { AlertService } from "src/app/shared/services/alert.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

/**
 * NPT-984: read-only "View" surface for a wrapped-up Field Visit. Mirrors the legacy MVC
 * `FieldVisit/Detail.cshtml` + `AssessmentDetail.cshtml` layouts. Routed to from the
 * Field Records grid when a visit is **not** in-progress, and from the workflow outlet's
 * Wrap Up handler after `finalizeFieldVisit` succeeds. Existing editable workflow pages
 * (under `/field-visits/:id/inventory|assessment|maintenance|…`) stay as edit-only — this
 * page replaces them once the visit is wrapped.
 *
 * Sections rendered top-to-bottom: visit header, Inventory summary, Initial Assessment
 * (observation table + photos + score), Maintenance Record (type / description / attribute
 * values), Post-Maintenance Assessment (same as initial). Manager-only footer actions
 * (Mark Provisional / Return to Edit) live below the sections.
 */
@Component({
    selector: "field-visit-detail-readonly",
    standalone: true,
    imports: [AsyncPipe, DatePipe, DecimalPipe, RouterLink, PageHeaderComponent, AlertDisplayComponent, LoadingDirective, ImageCarouselComponent],
    templateUrl: "./field-visit-detail-readonly.component.html",
    styleUrl: "./field-visit-detail-readonly.component.scss",
})
export class FieldVisitDetailReadOnlyComponent implements OnInit {
    // NPT-984: services injected via `inject()` so the data$ pipeline can be wired up as a
    // field initializer (see data$ below). Lazy-loaded zoneless routes complete the first
    // CD pass before ngOnInit runs, so observables assigned in ngOnInit raced the template's
    // `| async` subscription. Field initializers run at class instantiation time — well
    // before the first CD — so the async pipe sees a live observable on its first check.
    private fieldVisitService = inject(FieldVisitService);
    private assessmentService = inject(TreatmentBMPAssessmentService);
    private assessmentPhotoService = inject(TreatmentBMPAssessmentPhotoService);
    private maintenanceRecordService = inject(MaintenanceRecordService);
    private treatmentBMPTypeService = inject(TreatmentBMPTypeService);
    private authenticationService = inject(AuthenticationService);
    private confirmService = inject(ConfirmService);
    private alertService = inject(AlertService);
    private router = inject(Router);
    private route = inject(ActivatedRoute);

    private reload$ = new BehaviorSubject<void>(undefined);

    @Input() fieldVisitID!: number;

    public data$: Observable<{
        workflow: FieldVisitWorkflowDto;
        initial: TreatmentBMPAssessmentDetailDto | null;
        initialPhotos: ImageCarouselItem[];
        postMaintenance: TreatmentBMPAssessmentDetailDto | null;
        postMaintenancePhotos: ImageCarouselItem[];
        maintenance: MaintenanceRecordDetailDto | null;
        maintenanceAttributes: TreatmentBMPTypeCustomAttributeTypeDto[];
    } | null> = this.reload$.pipe(
        switchMap(() => this.fieldVisitService.getByIDFieldVisit(this.fieldVisitID)),
        switchMap((workflow) => {
            const initialAssessment$ = workflow.InitialAssessmentID
                ? this.assessmentService.getByIDTreatmentBMPAssessment(workflow.InitialAssessmentID)
                : of(null as TreatmentBMPAssessmentDetailDto | null);
            const postMaintenanceAssessment$ = workflow.PostMaintenanceAssessmentID
                ? this.assessmentService.getByIDTreatmentBMPAssessment(workflow.PostMaintenanceAssessmentID)
                : of(null as TreatmentBMPAssessmentDetailDto | null);
            const initialPhotos$ = workflow.InitialAssessmentID
                ? this.assessmentPhotoService.listTreatmentBMPAssessmentPhoto(workflow.InitialAssessmentID)
                : of([]);
            const postMaintenancePhotos$ = workflow.PostMaintenanceAssessmentID
                ? this.assessmentPhotoService.listTreatmentBMPAssessmentPhoto(workflow.PostMaintenanceAssessmentID)
                : of([]);
            const maintenance$ = this.maintenanceRecordService.getByFieldVisitMaintenanceRecord(this.fieldVisitID);
            const attributes$ = this.treatmentBMPTypeService.listCustomAttributeTypesTreatmentBMPType(workflow.TreatmentBMPTypeID);
            return forkJoin({
                workflow: of(workflow),
                initial: initialAssessment$,
                initialPhotos: initialPhotos$,
                postMaintenance: postMaintenanceAssessment$,
                postMaintenancePhotos: postMaintenancePhotos$,
                maintenance: maintenance$,
                maintenanceAttributes: attributes$,
            });
        }),
        switchMap((payload) => {
            this.isLoading = false;
            const toCarouselItems = (rows: { FileResourceGUID?: string | null; Caption?: string | null }[]): ImageCarouselItem[] =>
                rows.map((p) => ({ FileResourceGUID: p.FileResourceGUID ?? undefined, Caption: p.Caption ?? null }));
            const filteredMaintenanceAttributes = (payload.maintenanceAttributes ?? []).filter(
                (t) => t.CustomAttributeType?.CustomAttributeTypePurposeID === CustomAttributeTypePurposeEnum.Maintenance,
            );
            return of({
                workflow: payload.workflow,
                initial: payload.initial,
                initialPhotos: toCarouselItems(payload.initialPhotos ?? []),
                postMaintenance: payload.postMaintenance,
                postMaintenancePhotos: toCarouselItems(payload.postMaintenancePhotos ?? []),
                maintenance: payload.maintenance,
                maintenanceAttributes: filteredMaintenanceAttributes,
            });
        }),
    );

    public isLoading = true;

    ngOnInit(): void {
        // Nothing required here — data$ is wired via field initializer and subscribes lazily
        // through the template's `| async`. Kept on the class for future per-route work.
    }

    public get canManage(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    /**
     * Parses ObservationData JSON and returns the single observed value formatted for
     * display. Matches the legacy MVC behavior of showing "Pass"/"Fail" for PassFail,
     * the raw number with "%" suffix for Percentage, and the raw number for DiscreteValue.
     */
    public formatObservationValue(observationData: string | null | undefined, collectionMethod: string | null | undefined): string {
        if (!observationData) return "—";
        try {
            const parsed = JSON.parse(observationData);
            const first = parsed?.SingleValueObservations?.[0];
            if (first == null) return "—";
            const value = first.ObservationValue;
            if (value == null || value === "") return "—";
            if (collectionMethod === "PassFail") {
                return value === true || value === "true" ? "Pass" : "Fail";
            }
            if (collectionMethod === "Percentage") {
                return `${value}%`;
            }
            return String(value);
        } catch {
            return "—";
        }
    }

    public formatObservationNotes(observationData: string | null | undefined): string {
        if (!observationData) return "";
        try {
            const parsed = JSON.parse(observationData);
            const notes = (parsed?.SingleValueObservations ?? [])
                .map((s: any) => (s?.Notes ?? "").trim())
                .filter((s: string) => s.length > 0);
            return notes.join("; ");
        } catch {
            return "";
        }
    }

    /**
     * For maintenance attributes the MVC summary shows the attribute name + the recorded
     * observation value (or "—" if not recorded). The observation list lives on the
     * MaintenanceRecordDetailDto under `Observations` keyed by CustomAttributeTypeID.
     */
    public maintenanceAttributeValue(
        attribute: TreatmentBMPTypeCustomAttributeTypeDto,
        maintenance: MaintenanceRecordDetailDto | null,
    ): string {
        const customAttributeTypeID = attribute.CustomAttributeType?.CustomAttributeTypeID;
        if (customAttributeTypeID == null || !maintenance?.Observations?.length) return "—";
        const obs = maintenance.Observations.find((o) => o.CustomAttributeTypeID === customAttributeTypeID);
        const values = (obs?.Values ?? [])
            .map((v) => (v?.ObservationValue ?? "").trim())
            .filter((s) => s.length > 0);
        if (values.length === 0) return "—";
        // NPT-984: append the attribute's measurement unit (e.g., "cu ft", "gal", "%") when
        // one is configured. Mirrors the legacy MVC unit-suffix pattern on attribute values.
        const joined = values.join(", ");
        const unit = attribute.CustomAttributeType?.MeasurementUnitDisplayName?.trim();
        return unit ? `${joined} ${unit}` : joined;
    }

    /**
     * Mark Provisional returns the visit to an editable state. Manager-only, gated both on
     * the frontend (`canManage`) and on the backend (`[JurisdictionManageFeature]` on the
     * endpoint). After flipping the status, navigate back to the workflow outlet so the
     * user can edit.
     */
    public markProvisional(workflow: FieldVisitWorkflowDto): void {
        this.confirmService
            .confirm({
                title: "Mark Provisional",
                message: "Return this Field Visit to an editable state? Status will change to Returned to Edit and edit actions become available again.",
                buttonClassYes: "btn btn-warning",
                buttonTextYes: "Mark Provisional",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.fieldVisitService.markProvisionalFieldVisit(workflow.FieldVisitID!).subscribe(() => {
                    this.alertService.pushAlert(new Alert("Field Visit returned to provisional / editable state.", AlertContext.Success));
                    this.router.navigate(["/field-visits", workflow.FieldVisitID]);
                });
            });
    }

    public verify(workflow: FieldVisitWorkflowDto): void {
        this.confirmService
            .confirm({
                title: "Verify Field Visit",
                message: "Attest that this Field Visit's record is final.",
                buttonClassYes: "btn btn-primary",
                buttonTextYes: "Verify",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.fieldVisitService.verifyFieldVisit(workflow.FieldVisitID!).subscribe(() => {
                    this.alertService.pushAlert(new Alert("Field Visit verified.", AlertContext.Success));
                    // Re-fire the reload trigger to re-fetch the workflow + child data so the
                    // verified pill flips and the Manager actions reflect the new state.
                    this.reload$.next();
                });
            });
    }
}
