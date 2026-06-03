import { AsyncPipe } from "@angular/common";
import { Component } from "@angular/core";
import { ColDef } from "ag-grid-community";
import { Observable, tap } from "rxjs";
import { UtilityFunctionsService } from "../../../services/utility-functions.service";
import { AlertDisplayComponent } from "../../../shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "../../../shared/components/neptune-grid/neptune-grid.component";
import { PageHeaderComponent } from "../../../shared/components/page-header/page-header.component";
import { TrashGeneratingUnitService } from "../../../shared/generated/api/trash-generating-unit.service";
import { TrashGeneratingUnitGridDto } from "../../../shared/generated/model/trash-generating-unit-grid-dto";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";

@Component({
    selector: "trash-trash-generating-unit-index",
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe, LoadingDirective],
    templateUrl: "./trash-trash-generating-unit-index.component.html",
    styleUrl: "./trash-trash-generating-unit-index.component.scss",
})
export class TrashTrashGeneratingUnitIndexComponent {
    public trashGeneratingUnits$: Observable<TrashGeneratingUnitGridDto[]>;
    public trashGeneratingUnitsColumnDefs: ColDef[];
    public isLoading = true;

    constructor(private trashGeneratingUnitService: TrashGeneratingUnitService, private utilityFunctionsService: UtilityFunctionsService) {}

    ngOnInit() {
        this.trashGeneratingUnitsColumnDefs = [
            this.utilityFunctionsService.createBasicColumnDef("Trash Analysis Area ID", "TrashGeneratingUnitID"),
            this.utilityFunctionsService.createBasicColumnDef("Land Use Block ID", "LandUseBlockID"),
            this.utilityFunctionsService.createBasicColumnDef("Land Use Type", "PriorityLandUseTypeDisplayName", {
                CustomDropdownFilterField: "PriorityLandUseTypeDisplayName",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Governing OVTA Area", "OnlandVisualTrashAssessmentAreaName", "OnlandVisualTrashAssessmentAreaID", {
                InRouterLink: "../onland-visual-trash-assessment-areas/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Governing OVTA Area Baseline Score", "OnlandVisualTrashAssessmentAreaBaselineScore", {
                CustomDropdownFilterField: "OnlandVisualTrashAssessmentAreaBaselineScore",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Governing Treatment BMP", "TreatmentBMPName", "TreatmentBMPID", {
                InRouterLink: "/treatment-bmps/",
            }),
            this.utilityFunctionsService.createLinkColumnDef("Governing WQMP", "WaterQualityManagementPlanName", "WaterQualityManagementPlanID", {
                InRouterLink: "/water-quality-management-plans/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName", {
                CustomDropdownFilterField: "StormwaterJurisdictionName",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Area", "Area"),
            this.utilityFunctionsService.createDecimalColumnDef("Baseline Trash Generation Rate", "BaselineLoadingRate", {
                CustomDropdownFilterField: "BaselineLoadingRate",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Progress Trash Generation Rate", "ProgressLoadingRate", {
                CustomDropdownFilterField: "ProgressLoadingRate",
            }),
            this.utilityFunctionsService.createDecimalColumnDef("Current Net Loading Rate", "CurrentLoadingRate", {
                CustomDropdownFilterField: "CurrentLoadingRate",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Status via BMP", "TrashCaptureStatusBMP", {
                CustomDropdownFilterField: "TrashCaptureStatusBMP",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Status via WQMP", "TrashCaptureStatusWQMP", {
                CustomDropdownFilterField: "TrashCaptureStatusWQMP",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Effectiveness (%) via BMP", "TrashCaptureEffectivenessBMP"),
            this.utilityFunctionsService.createBasicColumnDef("Trash Capture Effectiveness (%) via WQMP", "TrashCaptureEffectivenessWQMP"),
            this.utilityFunctionsService.createDecimalColumnDef("Median Household Income (Residential)", "MedianHouseholdIncomeResidential"),
            this.utilityFunctionsService.createDecimalColumnDef("Median Household Income (Retail)", "MedianHouseholdIncomeRetail"),
            this.utilityFunctionsService.createBasicColumnDef("Permit Class", "PermitClass", {
                CustomDropdownFilterField: "PermitClass",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Land Use for TGR", "LandUseForTGR", {
                CustomDropdownFilterField: "LandUseForTGR",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Land Use Default TGR", "TrashGenerationRate", {
                CustomDropdownFilterField: "TrashGenerationRate",
            }),
            this.utilityFunctionsService.createDateColumnDef("Last Updated Date", "LastUpdateDate", "MM/dd/yyyy"),
        ];
        this.trashGeneratingUnits$ = this.trashGeneratingUnitService.listForCallingUserTrashGeneratingUnit().pipe(tap((x) => (this.isLoading = false)));
    }
}
