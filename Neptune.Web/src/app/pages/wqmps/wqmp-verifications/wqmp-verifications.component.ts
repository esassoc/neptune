import { Component } from "@angular/core";
import { Router } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { ColDef } from "ag-grid-community";
import { Observable, tap } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { AuthenticationService } from "src/app/services/authentication.service";
import { WaterQualityManagementPlanVerifyService } from "src/app/shared/generated/api/water-quality-management-plan-verify.service";
import { WaterQualityManagementPlanVerifyIndexGridDto } from "src/app/shared/generated/model/water-quality-management-plan-verify-index-grid-dto";

@Component({
    selector: "wqmp-verifications",
    standalone: true,
    imports: [AsyncPipe, PageHeaderComponent, AlertDisplayComponent, NeptuneGridComponent, LoadingDirective],
    templateUrl: "./wqmp-verifications.component.html",
    styleUrl: "./wqmp-verifications.component.scss",
})
export class WqmpVerificationsComponent {
    public verifications$: Observable<WaterQualityManagementPlanVerifyIndexGridDto[]>;
    public columnDefs: ColDef[];
    public isLoading = true;

    constructor(
        private wqmpVerifyService: WaterQualityManagementPlanVerifyService,
        private utilityFunctionsService: UtilityFunctionsService,
        private authenticationService: AuthenticationService,
        private router: Router
    ) {}

    public get currentPersonCanEdit(): boolean {
        return this.authenticationService.doesCurrentUserHaveJurisdictionEditPermission();
    }

    ngOnInit(): void {
        this.columnDefs = [
            this.utilityFunctionsService.createActionsColumnDef((params: any) => {
                const wqmpID = params.data.WaterQualityManagementPlanID;
                const verifyID = params.data.WaterQualityManagementPlanVerifyID;
                const actions: any[] = [
                    {
                        ActionName: "View",
                        ActionHandler: () => this.router.navigate(["/water-quality-management-plans", wqmpID, "verifications", verifyID, "view"]),
                    },
                ];
                if (this.currentPersonCanEdit && params.data.IsDraft) {
                    actions.push({
                        ActionName: "Edit",
                        ActionIcon: "fas fa-edit",
                        ActionHandler: () => this.router.navigate(["/water-quality-management-plans", wqmpID, "verifications", verifyID]),
                    });
                }
                return actions;
            }),
            this.utilityFunctionsService.createLinkColumnDef("WQMP Name", "WaterQualityManagementPlanName", "WaterQualityManagementPlanID", {
                InRouterLink: "/water-quality-management-plans/",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Jurisdiction", "StormwaterJurisdictionName", { CustomDropdownFilterField: "StormwaterJurisdictionName" }),
            this.utilityFunctionsService.createDateColumnDef("Verification Date", "VerificationDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createDateColumnDef("Last Edited", "LastEditedDate", "MM/dd/yyyy"),
            this.utilityFunctionsService.createBasicColumnDef("Last Edited By", "LastEditedByPersonFullName"),
            this.utilityFunctionsService.createBasicColumnDef("Type", "WaterQualityManagementPlanVerifyTypeDisplayName", { CustomDropdownFilterField: "WaterQualityManagementPlanVerifyTypeDisplayName" }),
            this.utilityFunctionsService.createBasicColumnDef("Visit Status", "WaterQualityManagementPlanVisitStatusDisplayName", {
                CustomDropdownFilterField: "WaterQualityManagementPlanVisitStatusDisplayName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Verify Status", "WaterQualityManagementPlanVerifyStatusDisplayName", {
                CustomDropdownFilterField: "WaterQualityManagementPlanVerifyStatusDisplayName",
            }),
            this.utilityFunctionsService.createBasicColumnDef("Source Control Condition", "SourceControlCondition"),
            this.utilityFunctionsService.createBasicColumnDef("Enforcement / Follow-up", "EnforcementOrFollowupActions"),
            this.utilityFunctionsService.createBasicColumnDef("Draft/Finalized", "IsDraft", {
                CustomDropdownFilterField: "IsDraft",
                ValueGetter: (params) => (params.data?.IsDraft ? "Draft" : "Finalized"),
            }),
        ];

        this.verifications$ = this.wqmpVerifyService.listAllAsIndexGridWaterQualityManagementPlanVerify().pipe(tap(() => (this.isLoading = false)));
    }
}
