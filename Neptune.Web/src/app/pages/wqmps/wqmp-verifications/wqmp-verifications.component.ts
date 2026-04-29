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
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { WaterQualityManagementPlanVerifyService } from "src/app/shared/generated/api/water-quality-management-plan-verify.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
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
        private wqmpService: WaterQualityManagementPlanService,
        private utilityFunctionsService: UtilityFunctionsService,
        private authenticationService: AuthenticationService,
        private alertService: AlertService,
        private confirmService: ConfirmService,
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
                    actions.push({
                        ActionName: "Delete",
                        ActionIcon: "fa fa-trash text-danger",
                        ActionHandler: () => this.confirmDeleteVerification(wqmpID, verifyID, params.data.WaterQualityManagementPlanName, params.data.VerificationDate),
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
                UseCustomDropdownFilter: true,
                ValueGetter: (params) => (params.data?.IsDraft ? "Draft" : "Finalized"),
            }),
        ];

        this.refreshVerifications();
    }

    private refreshVerifications(): void {
        this.isLoading = true;
        this.verifications$ = this.wqmpVerifyService.listAllAsIndexGridWaterQualityManagementPlanVerify().pipe(tap(() => (this.isLoading = false)));
    }

    // NPT-995 rework: Delete moved off the wizard sign-off step into a row action.
    // Mirrors the project-list deleteModal pattern. Gated by currentPersonCanEdit +
    // IsDraft (set on the action push), so finalized verifications stay locked.
    confirmDeleteVerification(wqmpID: number, verifyID: number, wqmpName: string | null | undefined, verificationDate: string | null | undefined): void {
        const dateText = verificationDate ? new Date(verificationDate).toLocaleDateString() : "this draft";
        const nameText = wqmpName ? ` for <strong>${wqmpName}</strong>` : "";
        this.confirmService
            .confirm({
                title: "Delete Verification",
                message: `<p>You are about to delete the verification${nameText} from <strong>${dateText}</strong>.</p><p>Are you sure you wish to proceed?</p>`,
                buttonClassYes: "btn btn-danger",
                buttonTextYes: "Delete",
                buttonTextNo: "Cancel",
            })
            .then((confirmed) => {
                if (!confirmed) return;
                this.wqmpService.deleteVerificationWaterQualityManagementPlan(wqmpID, verifyID).subscribe({
                    next: () => {
                        this.alertService.pushAlert(new Alert("Verification deleted.", AlertContext.Success));
                        this.refreshVerifications();
                    },
                    error: () => {
                        this.alertService.pushAlert(new Alert("An error occurred while deleting the verification.", AlertContext.Danger));
                    },
                });
            });
    }
}
