import { Component, inject, Input, numberAttribute, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { RouterLink } from "@angular/router";
import { BehaviorSubject, catchError, EMPTY, map, Observable, shareReplay, switchMap, tap } from "rxjs";
import { DialogService } from "@ngneat/dialog";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AuthenticationService } from "src/app/services/authentication.service";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";
import { MeasurementUnitTypes } from "src/app/shared/generated/enum/measurement-unit-type-enum";
import { TreatmentBMPAssessmentObservationTypeService } from "src/app/shared/generated/api/treatment-bmp-assessment-observation-type.service";
import { TreatmentBMPAssessmentObservationTypeDetailDto } from "src/app/shared/generated/model/treatment-bmp-assessment-observation-type-detail-dto";
import {
    CollectionMethodType, collectionMethodIDToName, specToTriple,
    DiscreteValueSchema, PassFailSchema, PercentageSchema,
} from "src/app/shared/observation-types/schema-types";
import {
    ObservationTypePreviewModalComponent,
    ObservationTypePreviewModalData,
} from "src/app/shared/observation-types/observation-type-preview-modal.component";
import { ObservationTypeModalComponent } from "src/app/pages/manage/observation-type-modal/observation-type-modal.component";

interface ObservationTypeDetailViewModel {
    detail: TreatmentBMPAssessmentObservationTypeDetailDto;
    collectionMethod: CollectionMethodType | null;
    passFailSchema: PassFailSchema | null;
    discreteSchema: DiscreteValueSchema | null;
    percentageSchema: PercentageSchema | null;
    propertiesToObserve: string[];
    measurementUnitTypeDisplayName: string | null;
}

@Component({
    selector: "observation-type-detail",
    standalone: true,
    imports: [AsyncPipe, RouterLink, PageHeaderComponent, LoadingDirective],
    templateUrl: "./observation-type-detail.component.html",
})
export class ObservationTypeDetailComponent implements OnInit {
    @Input({ transform: numberAttribute }) observationTypeID!: number;

    private observationTypeService = inject(TreatmentBMPAssessmentObservationTypeService);
    private alertService = inject(AlertService);
    private authenticationService = inject(AuthenticationService);
    private dialogService = inject(DialogService);

    private reload$ = new BehaviorSubject<void>(undefined);
    public viewModel$: Observable<ObservationTypeDetailViewModel>;
    public isLoading = true;
    public NeptunePageTypeEnum = NeptunePageTypeEnum;

    public get isAdmin(): boolean {
        return this.authenticationService.isCurrentUserAnAdministrator();
    }

    ngOnInit(): void {
        this.viewModel$ = this.reload$.pipe(
            tap(() => (this.isLoading = true)),
            switchMap(() => this.observationTypeService.getTreatmentBMPAssessmentObservationType(this.observationTypeID).pipe(
                tap(() => (this.isLoading = false)),
                catchError(() => {
                    this.isLoading = false;
                    this.alertService.pushAlert(new Alert("Failed to load observation type.", AlertContext.Danger));
                    return EMPTY;
                }),
            )),
            map((detail) => this.toViewModel(detail)),
            shareReplay(1),
        );
    }

    public openEdit(): void {
        const dialogRef = this.dialogService.open(ObservationTypeModalComponent, {
            data: { mode: "edit", observationTypeID: this.observationTypeID },
            width: "800px",
        });
        dialogRef.afterClosed$.subscribe((result) => {
            if (result) {
                this.alertService.pushAlert(new Alert("Observation type updated.", AlertContext.Success));
                this.reload$.next();
            }
        });
    }

    public openPreview(vm: ObservationTypeDetailViewModel): void {
        const data: ObservationTypePreviewModalData = {
            observationTypeName: vm.detail.TreatmentBMPAssessmentObservationTypeName ?? "",
            collectionMethod: vm.collectionMethod,
            passFailSchema: vm.passFailSchema,
            discreteSchema: vm.discreteSchema,
            percentageSchema: vm.percentageSchema,
        };
        this.dialogService.open(ObservationTypePreviewModalComponent, { data, width: "700px" });
    }

    private toViewModel(detail: TreatmentBMPAssessmentObservationTypeDetailDto): ObservationTypeDetailViewModel {
        const triple = specToTriple(detail.ObservationTypeSpecificationID);
        const cm = collectionMethodIDToName(triple?.CollectionMethodID);
        const vm: ObservationTypeDetailViewModel = {
            detail,
            collectionMethod: cm,
            passFailSchema: null,
            discreteSchema: null,
            percentageSchema: null,
            propertiesToObserve: [],
            measurementUnitTypeDisplayName: null,
        };
        if (!detail.TreatmentBMPAssessmentObservationTypeSchema) return vm;
        try {
            const parsed = JSON.parse(detail.TreatmentBMPAssessmentObservationTypeSchema);
            if (cm === "PassFail") vm.passFailSchema = parsed;
            else if (cm === "DiscreteValue") vm.discreteSchema = parsed;
            else if (cm === "Percentage") vm.percentageSchema = parsed;
            // Sorted alphabetically to match MVC formatting (PropertiesToObserveFormatted).
            const props: string[] = parsed?.PropertiesToObserve ?? [];
            vm.propertiesToObserve = [...props].sort((a, b) => a.localeCompare(b));
            // Discrete schemas store a MeasurementUnitTypeID; resolve to its display name from the lookup table.
            const unitTypeID: number | undefined = parsed?.MeasurementUnitTypeID;
            if (unitTypeID != null) {
                vm.measurementUnitTypeDisplayName = MeasurementUnitTypes.find((u) => u.Value === unitTypeID)?.DisplayName ?? null;
            }
        } catch {
            // Malformed schema JSON — leave the schema fields null; sections will skip rendering.
        }
        return vm;
    }
}
