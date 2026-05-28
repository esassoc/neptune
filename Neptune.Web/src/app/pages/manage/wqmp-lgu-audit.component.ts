import { Component, OnInit } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";

import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanLGUAuditGridDto } from "src/app/shared/generated/model/water-quality-management-plan-lgu-audit-grid-dto";

import { UtilityFunctionsService } from "src/app/services/utility-functions.service";

@Component({
    selector: "wqmp-lgu-audit",
    standalone: true,
    imports: [PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, AsyncPipe],
    templateUrl: "./wqmp-lgu-audit.component.html",
    styleUrl: "./wqmp-lgu-audit.component.scss",
})
export class WqmpLguAuditComponent implements OnInit {
    public rows$: Observable<WaterQualityManagementPlanLGUAuditGridDto[]>;
    public columnDefs: ColDef[];

    constructor(
        private wqmpService: WaterQualityManagementPlanService,
        private utility: UtilityFunctionsService
    ) {}

    ngOnInit(): void {
        this.columnDefs = [
            this.utility.createLinkColumnDef("Water Quality Management Plan", "WaterQualityManagementPlanName", "WaterQualityManagementPlanID", {
                InRouterLink: "/water-quality-management-plans/",
            }),
            this.utility.createBooleanColumnDef("Are LGUs Populated?", "LoadGeneratingUnitsPopulated", { UseCustomDropdownFilter: true }),
            this.utility.createBooleanColumnDef("Is Boundary Defined?", "BoundaryIsDefined", { UseCustomDropdownFilter: true }),
            this.utility.createBooleanColumnDef("Intersects Model Basin(s)?", "IntersectsModelBasins", { UseCustomDropdownFilter: true }),
        ];
        this.rows$ = this.wqmpService.listLGUAuditAsGridDtoWaterQualityManagementPlan();
    }
}
