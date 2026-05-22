import { ChangeDetectionStrategy, Component, OnInit, inject } from "@angular/core";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormGroup, ReactiveFormsModule } from "@angular/forms";
import { ColDef } from "ag-grid-community";
import { BehaviorSubject, Observable, combineLatest, of, startWith, switchMap, tap } from "rxjs";

import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { NeptuneGridComponent } from "src/app/shared/components/neptune-grid/neptune-grid.component";
import { LoadingDirective } from "src/app/shared/directives/loading.directive";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { CustomRichTextComponent } from "src/app/shared/components/custom-rich-text/custom-rich-text.component";

import { UtilityFunctionsService } from "src/app/services/utility-functions.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanAnnualReportOptionsDto } from "src/app/shared/generated/model/water-quality-management-plan-annual-report-options-dto";
import { WaterQualityManagementPlanApprovalSummaryGridDto } from "src/app/shared/generated/model/water-quality-management-plan-approval-summary-grid-dto";
import { WaterQualityManagementPlanPostConstructionVerificationGridDto } from "src/app/shared/generated/model/water-quality-management-plan-post-construction-verification-grid-dto";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

const ALL_JURISDICTIONS = -1;

@Component({
    selector: "wqmp-annual-report",
    standalone: true,
    templateUrl: "./wqmp-annual-report.component.html",
    styleUrl: "./wqmp-annual-report.component.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [
        AsyncPipe,
        ReactiveFormsModule,
        PageHeaderComponent,
        AlertDisplayComponent,
        NeptuneGridComponent,
        LoadingDirective,
        FormFieldComponent,
        CustomRichTextComponent,
    ],
})
export class WqmpAnnualReportComponent implements OnInit {
    public FormFieldType = FormFieldType;
    public approvalSummaryCustomRichTextID = NeptunePageTypeEnum.WQMPApprovalSummary;
    public postConstructionCustomRichTextID = NeptunePageTypeEnum.WQMPPostConstructionInspectionAndVerification;

    public filterForm = new FormGroup({
        reportingYear: new FormControl<number | null>(null),
        stormwaterJurisdictionID: new FormControl<number>(ALL_JURISDICTIONS, { nonNullable: true }),
    });

    public options$: Observable<WaterQualityManagementPlanAnnualReportOptionsDto>;
    public reportingYearOptions: SelectDropdownOption[] = [];
    public jurisdictionOptions: SelectDropdownOption[] = [];

    public approvalSummary$: Observable<WaterQualityManagementPlanApprovalSummaryGridDto[]>;
    public postConstruction$: Observable<WaterQualityManagementPlanPostConstructionVerificationGridDto[]>;

    public isLoadingApprovalSummary = true;
    public isLoadingPostConstruction = true;

    public approvalSummaryColumnDefs: ColDef[] = [];
    public postConstructionColumnDefs: ColDef[] = [];

    private optionsReady$ = new BehaviorSubject<boolean>(false);

    private readonly wqmpService = inject(WaterQualityManagementPlanService);
    private readonly utility = inject(UtilityFunctionsService);

    ngOnInit(): void {
        this.approvalSummaryColumnDefs = this.buildApprovalSummaryColumnDefs();
        this.postConstructionColumnDefs = this.buildPostConstructionColumnDefs();

        this.options$ = this.wqmpService.getAnnualReportOptionsWaterQualityManagementPlan().pipe(
            tap((options) => {
                this.reportingYearOptions = (options.ReportingYears ?? []).map((y) => ({
                    Value: y.ReportingYear,
                    Label: y.ReportingYearDisplay,
                    disabled: false,
                }));
                this.jurisdictionOptions = (options.StormwaterJurisdictions ?? []).map((j) => ({
                    Value: j.StormwaterJurisdictionID,
                    Label: j.StormwaterJurisdictionName,
                    disabled: false,
                }));
                this.filterForm.patchValue(
                    {
                        reportingYear: options.DefaultReportingYear,
                        stormwaterJurisdictionID: ALL_JURISDICTIONS,
                    },
                    { emitEvent: false }
                );
                this.optionsReady$.next(true);
            })
        );

        // Single source of truth: options-loaded + form changes -> reload both grids.
        const filters$ = combineLatest([
            this.optionsReady$,
            this.filterForm.valueChanges.pipe(startWith(this.filterForm.getRawValue())),
        ]).pipe(
            switchMap(([ready]) => {
                if (!ready) return of(null);
                const { reportingYear, stormwaterJurisdictionID } = this.filterForm.getRawValue();
                if (reportingYear == null) return of(null);
                return of({ reportingYear, stormwaterJurisdictionID: stormwaterJurisdictionID ?? ALL_JURISDICTIONS });
            })
        );

        this.approvalSummary$ = filters$.pipe(
            tap(() => (this.isLoadingApprovalSummary = true)),
            switchMap((v) => {
                if (!v) return of([] as WaterQualityManagementPlanApprovalSummaryGridDto[]);
                return this.wqmpService.getAnnualReportApprovalSummaryWaterQualityManagementPlan(v.reportingYear, v.stormwaterJurisdictionID);
            }),
            tap(() => (this.isLoadingApprovalSummary = false))
        );

        this.postConstruction$ = filters$.pipe(
            tap(() => (this.isLoadingPostConstruction = true)),
            switchMap((v) => {
                if (!v) return of([] as WaterQualityManagementPlanPostConstructionVerificationGridDto[]);
                return this.wqmpService.getAnnualReportPostConstructionVerificationsWaterQualityManagementPlan(v.reportingYear, v.stormwaterJurisdictionID);
            }),
            tap(() => (this.isLoadingPostConstruction = false))
        );
    }

    private buildApprovalSummaryColumnDefs(): ColDef[] {
        return [
            this.utility.createLinkColumnDef("Water Quality Management Plan", "WaterQualityManagementPlanName", "WaterQualityManagementPlanID", {
                InRouterLink: "../water-quality-management-plans/",
            }),
            this.utility.createBasicColumnDef("Priority", "Priority", { CustomDropdownFilterField: "Priority" }),
            this.utility.createBasicColumnDef("Land Use", "LandUse", { CustomDropdownFilterField: "LandUse" }),
            this.utility.createBasicColumnDef("Hydrologic Subarea", "HydrologicSubareaName", { CustomDropdownFilterField: "HydrologicSubareaName" }),
            this.utility.createDecimalColumnDef("Acres (user-entered)", "RecordedWQMPAreaInAcres", { DecimalPlacesToDisplay: 2 }),
            this.utility.createDateColumnDef("Date Approved", "ApprovalDate", "shortDate"),
        ];
    }

    private buildPostConstructionColumnDefs(): ColDef[] {
        return [
            this.utility.createLinkColumnDef("Water Quality Management Plan", "WaterQualityManagementPlanName", "WaterQualityManagementPlanID", {
                InRouterLink: "../water-quality-management-plans/",
            }),
            this.utility.createBasicColumnDef("WQMP Status at End of Period", "WaterQualityManagementPlanVerifyStatusName", {
                CustomDropdownFilterField: "WaterQualityManagementPlanVerifyStatusName",
            }),
            this.utility.createBasicColumnDef("# of BMPs", "NumberOfBMPs"),
            this.utility.createBasicColumnDef("BMPs Adequate", "NumberOfBMPsAdequate"),
            this.utility.createBasicColumnDef("BMPs Deficient", "NumberOfBMPsDeficient"),
            this.utility.createBasicColumnDef("WQMP O&M Verification Comments", "WQMPVerificationComments"),
        ];
    }
}
