import { ChangeDetectionStrategy, Component, OnInit, inject } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanBoundaryOverlapGridDto } from "src/app/shared/generated/model/water-quality-management-plan-boundary-overlap-grid-dto";

// NPT-1155: pairs of WQMP boundaries that cover the same ground, for the jurisdiction to reconcile.
// Sibling of the Delineation Reconciliation Report.
@Component({
    selector: "wqmp-boundary-overlap-report",
    standalone: true,
    templateUrl: "./wqmp-boundary-overlap-report.component.html",
    styleUrl: "./wqmp-boundary-overlap-report.component.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [AsyncPipe, PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent],
})
export class WqmpBoundaryOverlapReportComponent implements OnInit {
    public overlaps$: Observable<WaterQualityManagementPlanBoundaryOverlapGridDto[]>;
    public columnDefs: ColDef[] = [];

    private readonly waterQualityManagementPlanService = inject(WaterQualityManagementPlanService);
    private readonly utility = inject(UtilityFunctionsService);

    ngOnInit(): void {
        this.columnDefs = this.buildColumnDefs();
        this.overlaps$ = this.waterQualityManagementPlanService.listBoundaryOverlapsWaterQualityManagementPlan();
    }

    private buildColumnDefs(): ColDef[] {
        return [
            this.utility.createLinkColumnDef("WQMP", "WaterQualityManagementPlanName", "WaterQualityManagementPlanID", {
                InRouterLink: "../water-quality-management-plans/",
            }),
            this.utility.createLinkColumnDef("Jurisdiction", "StormwaterJurisdictionName", "StormwaterJurisdictionID", {
                InRouterLink: "../jurisdictions/",
                FieldDefinitionType: "Jurisdiction",
                CustomDropdownFilterField: "StormwaterJurisdictionName",
            }),
            this.utility.createBooleanColumnDef("Treats Runoff?", "TreatsRunoff", { CustomDropdownFilterField: "TreatsRunoff" }),
            this.utility.createLinkColumnDef("Overlapping WQMP", "OverlappingWaterQualityManagementPlanName", "OverlappingWaterQualityManagementPlanID", {
                InRouterLink: "../water-quality-management-plans/",
            }),
            this.utility.createLinkColumnDef("Overlapping WQMP Jurisdiction", "OverlappingStormwaterJurisdictionName", "OverlappingStormwaterJurisdictionID", {
                InRouterLink: "../jurisdictions/",
                CustomDropdownFilterField: "OverlappingStormwaterJurisdictionName",
            }),
            this.utility.createBooleanColumnDef("Overlapping WQMP Treats Runoff?", "OverlappingTreatsRunoff", { CustomDropdownFilterField: "OverlappingTreatsRunoff" }),
            this.utility.createDecimalColumnDef("Area of Overlap (ac)", "AreaOfOverlapInAcres", {
                DecimalPlacesToDisplay: 2,
                FieldDefinitionType: "Area",
                FieldDefinitionLabelOverride: "Area of Overlap (ac)",
                Width: 180,
            }),
            this.utility.createBooleanColumnDef("Counted Twice in Model?", "IsCountedTwice", { CustomDropdownFilterField: "IsCountedTwice" }),
        ];
    }
}
