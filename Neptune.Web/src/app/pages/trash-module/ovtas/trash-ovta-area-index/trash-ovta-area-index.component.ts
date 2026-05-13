import { Component } from "@angular/core";
import { PageHeaderComponent } from "../../../../shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "../../../../shared/components/alert-display/alert-display.component";
import { ColDef } from "ag-grid-community";
import { BehaviorSubject, Observable, switchMap, tap } from "rxjs";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { OnlandVisualTrashAssessmentAreaService } from "src/app/shared/generated/api/onland-visual-trash-assessment-area.service";
import { OnlandVisualTrashAssessmentAreaGridDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-area-grid-dto";
import { AsyncPipe } from "@angular/common";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { environment } from "src/environments/environment";
import { IconComponent } from "../../../../shared/components/icon/icon.component";
import { HybridMapGridComponent } from "../../../../shared/components/hybrid-map-grid/hybrid-map-grid.component";
import { NeptuneMapInitEvent } from "src/app/shared/components/leaflet/neptune-map/neptune-map.component";
import { Map, Control } from "leaflet";
import { SelectedOvtaAreaLayerComponent } from "src/app/shared/components/leaflet/layers/selected-ovta-area-layer/selected-ovta-area-layer.component";
import { Router, RouterLink } from "@angular/router";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { OnlandVisualTrashAssessmentService } from "src/app/shared/generated/api/onland-visual-trash-assessment.service";
import { OnlandVisualTrashAssessmentSimpleDto } from "src/app/shared/generated/model/onland-visual-trash-assessment-simple-dto";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { AlertService } from "src/app/shared/services/alert.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { StormwaterJurisdictionService } from "src/app/shared/generated/api/stormwater-jurisdiction.service";
import { BoundingBoxDto } from "src/app/shared/generated/model/bounding-box-dto";
import { escapeHtml } from "src/app/shared/helpers/html-escape";

@Component({
    selector: "trash-ovta-area-index",
    imports: [PageHeaderComponent, AlertDisplayComponent, AsyncPipe, LoadingDirective, IconComponent, HybridMapGridComponent, SelectedOvtaAreaLayerComponent, RouterLink],
    templateUrl: "./trash-ovta-area-index.component.html",
    styleUrl: "./trash-ovta-area-index.component.scss",
})
export class TrashOvtaAreaIndexComponent {
    public onlandVisualTrashAssessmentAreas$: Observable<OnlandVisualTrashAssessmentAreaGridDto[]>;
    public ovtaAreaColumnDefs: ColDef[];
    public isLoading: boolean = true;
    public url = environment.ocStormwaterToolsBaseUrl;
    public ovtaAreaID: number;
    public selectionFromMap: boolean;

    public map: Map;
    public layerControl: Control.Layers;
    public bounds: any;
    public mapIsReady: boolean = false;
    public boundingBox$: Observable<BoundingBoxDto>;

    private refreshGridTrigger$ = new BehaviorSubject<void>(null);

    constructor(
        private onlandVisualTrashAssessmentService: OnlandVisualTrashAssessmentService,
        private onlandVisualTrashAssessmentAreaService: OnlandVisualTrashAssessmentAreaService,
        private utilityFunctionsService: UtilityFunctionsService,
        private router: Router,
        private alertService: AlertService,
        private confirmService: ConfirmService,
        private authenticationService: AuthenticationService,
        private stormwaterJurisdictionService: StormwaterJurisdictionService
    ) {}

    ngOnInit(): void {
        this.ovtaAreaColumnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                const totalAssessments = (params.data.NumberOfAssessmentsInProgress ?? 0) + (params.data.NumberOfAssessmentsCompleted ?? 0);
                return [
                    {
                        ActionName: "View",
                        ActionHandler: () => this.router.navigate(["trash", "onland-visual-trash-assessment-areas", params.data.OnlandVisualTrashAssessmentAreaID]),
                    },
                    {
                        ActionName: "Add New OVTA",
                        ActionHandler: () =>
                            this.addNewOVTA(params.data.OnlandVisualTrashAssessmentAreaID, params.data.OnlandVisualTrashAssessmentAreaName, params.data.StormwaterJurisdictionID),
                    },
                    {
                        ActionName: "Delete",
                        ActionHandler: () =>
                            this.deleteOVTAArea(params.data.OnlandVisualTrashAssessmentAreaID, params.data.OnlandVisualTrashAssessmentAreaName, totalAssessments),
                    },
                ];
            }),
            this.utilityFunctionsService.createLinkColumnDef("Assessment Area Name", "OnlandVisualTrashAssessmentAreaName", "OnlandVisualTrashAssessmentAreaID", {
                InRouterLink: "../onland-visual-trash-assessment-areas/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Baseline Score", "OnlandVisualTrashAssessmentBaselineScoreName", {
                CustomDropdownFilterField: "OnlandVisualTrashAssessmentBaselineScoreName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Progress Score", "OnlandVisualTrashAssessmentProgressScoreName", {
                CustomDropdownFilterField: "OnlandVisualTrashAssessmentProgressScoreName",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("# of Assessments in Progress", "NumberOfAssessmentsInProgress", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createDecimalColumnDef("# of Completed Assessments", "NumberOfAssessmentsCompleted", { DecimalPlacesToDisplay: 0 }),
            this.utilityFunctionsService.createDateColumnDef("Last Assessment Date", "LastAssessmentDate", "shortDate"),
            this.utilityFunctionsService.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName", {
                CustomDropdownFilterField: "StormwaterJurisdictionName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Description", "AssessmentAreaDescription"),
        ];
        this.onlandVisualTrashAssessmentAreas$ = this.refreshGridTrigger$.pipe(
            tap(() => (this.isLoading = true)),
            switchMap(() => this.onlandVisualTrashAssessmentAreaService.listOnlandVisualTrashAssessmentArea()),
            tap(() => (this.isLoading = false))
        );
        this.boundingBox$ = this.stormwaterJurisdictionService.getBoundingBoxStormwaterJurisdiction();
    }

    public handleMapReady(event: NeptuneMapInitEvent) {
        this.map = event.map;
        this.layerControl = event.layerControl;
        this.mapIsReady = true;
    }

    public onSelectedOVTAAreaChangedFromGrid(selectedOVTAAreaID) {
        if (this.ovtaAreaID == selectedOVTAAreaID) return;

        this.ovtaAreaID = selectedOVTAAreaID;
        this.selectionFromMap = false;
        return this.ovtaAreaID;
    }

    public onSelectedOVTAAreaChanged(selectedOVTAAreaID) {
        if (this.ovtaAreaID == selectedOVTAAreaID) return;

        this.ovtaAreaID = selectedOVTAAreaID;
        this.selectionFromMap = true;
        return this.ovtaAreaID;
    }

    public handleLayerBoundsCalculated(bounds: any) {
        this.bounds = bounds;
    }

    public addNewOVTA(onlandVisualTrashAssessmentAreaID: number, onlandVisualTrashAssessmentAreaName: string, stormwaterJurisdictionID: number) {
        const modalContents = `<p>You are about to begin a new OVTA for Assessment Area: ${onlandVisualTrashAssessmentAreaName}. 
            This will create a new Assessment record and allow you to start entering Assessment Observations on the next page.</p>`;
        this.confirmService
            .confirm({ buttonClassYes: "btn-primary", buttonTextYes: "Continue", buttonTextNo: "Cancel", title: "Add New OVTA", message: modalContents })
            .then((confirmed) => {
                if (confirmed) {
                    var onlandVisualTrashAssessmentSimpleDto = new OnlandVisualTrashAssessmentSimpleDto();
                    onlandVisualTrashAssessmentSimpleDto.OnlandVisualTrashAssessmentAreaID = onlandVisualTrashAssessmentAreaID;
                    onlandVisualTrashAssessmentSimpleDto.StormwaterJurisdictionID = stormwaterJurisdictionID;
                    onlandVisualTrashAssessmentSimpleDto.AssessingNewArea = false;
                    this.onlandVisualTrashAssessmentService.createOnlandVisualTrashAssessment(onlandVisualTrashAssessmentSimpleDto).subscribe((response) => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("Your OVTA was successfully created.", AlertContext.Success));
                        this.router.navigate([`/trash/onland-visual-trash-assessments/edit/${response.OnlandVisualTrashAssessmentID}/record-observations`]);
                    });
                }
            });
    }

    public deleteOVTAArea(onlandVisualTrashAssessmentAreaID: number, onlandVisualTrashAssessmentAreaName: string, assessmentCount: number) {
        const safeAreaName = escapeHtml(onlandVisualTrashAssessmentAreaName ?? "");
        const cascadeWarning =
            assessmentCount > 0
                ? `<br/><p>This Area has <strong>${assessmentCount} associated assessment${assessmentCount === 1 ? "" : "s"}</strong> which will also be deleted.</p>`
                : "";
        const message = `<p>Are you sure you want to delete the OVTA Area <strong>${safeAreaName}</strong>? This cannot be undone.</p>${cascadeWarning}`;
        this.confirmService
            .confirm({ buttonClassYes: "btn-danger", buttonTextYes: "Delete", buttonTextNo: "Cancel", title: "Delete OVTA Area", message })
            .then((confirmed) => {
                if (confirmed) {
                    this.onlandVisualTrashAssessmentAreaService.deleteOnlandVisualTrashAssessmentArea(onlandVisualTrashAssessmentAreaID).subscribe(() => {
                        this.alertService.clearAlerts();
                        this.alertService.pushAlert(new Alert("OVTA Area was successfully deleted.", AlertContext.Success));
                        this.refreshGridTrigger$.next();
                    });
                }
            });
    }

    public currentUserHasJurisdictionManagePermission(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionManagePermission();
    }

    public currentUserHasJurisdictionEditPermission(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    }
}
