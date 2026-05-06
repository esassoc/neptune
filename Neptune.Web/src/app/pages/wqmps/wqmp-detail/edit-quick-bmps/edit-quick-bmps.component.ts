import { Component, inject, OnInit } from "@angular/core";
import { Router, ActivatedRoute, RouterLink } from "@angular/router";
import { ReactiveFormsModule, FormGroup, FormArray, FormControl, Validators } from "@angular/forms";
import { AsyncPipe } from "@angular/common";
import { Observable, map, forkJoin, tap, shareReplay } from "rxjs";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { TreatmentBMPTypeService } from "src/app/shared/generated/api/treatment-bmp-type.service";
import { QuickBMPUpsertDto } from "src/app/shared/generated/model/quick-bmp-upsert-dto";
import { DryWeatherFlowOverrideEnum, DryWeatherFlowOverridesAsSelectDropdownOptions } from "src/app/shared/generated/enum/dry-weather-flow-override-enum";
import { routeParams } from "src/app/app.routes";

@Component({
    selector: "edit-quick-bmps",
    imports: [AlertDisplayComponent, PageHeaderComponent, RouterLink, ReactiveFormsModule, FormFieldComponent, AsyncPipe],
    templateUrl: "./edit-quick-bmps.component.html",
    styleUrls: ["./edit-quick-bmps.component.scss"],
})
export class EditQuickBMPsComponent implements OnInit {
    private router = inject(Router);
    private route = inject(ActivatedRoute);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private treatmentBMPTypeService = inject(TreatmentBMPTypeService);
    private alertService = inject(AlertService);

    public FormFieldType = FormFieldType;
    public dryWeatherFlowOverrideOptions = DryWeatherFlowOverridesAsSelectDropdownOptions;
    public treatmentBMPTypeOptions: SelectDropdownOption[] = [];
    public waterQualityManagementPlanID: number;
    public quickBMPRows = new FormArray<FormGroup>([]);
    public validationErrors: string[] = [];
    public loaded$: Observable<boolean>;

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.waterQualityManagementPlanID = +this.route.snapshot.paramMap.get(routeParams.waterQualityManagementPlanID);

        const treatmentBMPTypeOptions$ = this.treatmentBMPTypeService.listTreatmentBMPType().pipe(
            map((types) => types.map((type) => ({ Label: type.TreatmentBMPTypeName, Value: type.TreatmentBMPTypeID, disabled: false }) as SelectDropdownOption)),
            shareReplay(1)
        );

        const existingBMPs$ = this.wqmpService.listQuickBMPsWaterQualityManagementPlan(this.waterQualityManagementPlanID);

        this.loaded$ = forkJoin({ types: treatmentBMPTypeOptions$, bmps: existingBMPs$ }).pipe(
            tap(({ types, bmps }) => {
                this.treatmentBMPTypeOptions = types;
                for (const bmp of bmps) {
                    this.addRow({
                        TreatmentBMPTypeID: bmp.TreatmentBMPTypeID,
                        QuickBMPName: bmp.QuickBMPName,
                        QuickBMPNote: bmp.QuickBMPNote,
                        NumberOfIndividualBMPs: bmp.NumberOfIndividualBMPs,
                        PercentOfSiteTreated: bmp.PercentOfSiteTreated,
                        PercentCaptured: bmp.PercentCaptured,
                        PercentRetained: bmp.PercentRetained,
                        DryWeatherFlowOverrideID: bmp.DryWeatherFlowOverrideID,
                    });
                }
            }),
            map(() => true),
            shareReplay(1)
        );
    }

    public addRow(data?: Partial<QuickBMPUpsertDto>): void {
        const row = new FormGroup({
            QuickBMPName: new FormControl<string>(data?.QuickBMPName ?? "", { nonNullable: true, validators: [Validators.required, Validators.maxLength(100)] }),
            TreatmentBMPTypeID: new FormControl<number>(data?.TreatmentBMPTypeID ?? undefined, { nonNullable: true, validators: [Validators.required] }),
            QuickBMPNote: new FormControl<string>(data?.QuickBMPNote ?? "", { nonNullable: false, validators: [Validators.maxLength(200)] }),
            NumberOfIndividualBMPs: new FormControl<number>(data?.NumberOfIndividualBMPs ?? 1, { nonNullable: true, validators: [Validators.required, Validators.min(1)] }),
            PercentOfSiteTreated: new FormControl<number>(data?.PercentOfSiteTreated ?? undefined, { nonNullable: false, validators: [Validators.min(0), Validators.max(100)] }),
            PercentCaptured: new FormControl<number>(data?.PercentCaptured ?? undefined, { nonNullable: false, validators: [Validators.min(0), Validators.max(100)] }),
            PercentRetained: new FormControl<number>(data?.PercentRetained ?? undefined, { nonNullable: false, validators: [Validators.min(0), Validators.max(100)] }),
            DryWeatherFlowOverrideID: new FormControl<number>(data?.DryWeatherFlowOverrideID ?? undefined, { nonNullable: false }),
        });
        this.quickBMPRows.push(row);
    }

    public removeRow(index: number): void {
        this.quickBMPRows.removeAt(index);
    }

    public setAllDryWeatherOverridesToYes(): void {
        for (const row of this.quickBMPRows.controls) {
            row.get("DryWeatherFlowOverrideID").setValue(DryWeatherFlowOverrideEnum.Yes);
        }
    }

    public calculateUntreatedPercentage(): number {
        const total = this.quickBMPRows.controls
            .map((r) => r.get("PercentOfSiteTreated").value)
            .filter((v) => v != null)
            .reduce((sum, v) => sum + v, 0);
        return Math.round((100 - total) * 100) / 100;
    }

    private validate(): boolean {
        this.validationErrors = [];
        const rows = this.quickBMPRows.controls;

        // Duplicate names
        const names = rows.map((r) => r.get("QuickBMPName").value).filter((n) => n);
        const duplicates = names.filter((name, i) => names.indexOf(name) !== i);
        const uniqueDuplicates = [...new Set(duplicates)];
        for (const dup of uniqueDuplicates) {
            this.validationErrors.push(`"${dup}" has already been used. Make sure that all names are unique.`);
        }

        // Percent captured >= percent retained
        for (const row of rows) {
            const captured = row.get("PercentCaptured").value;
            const retained = row.get("PercentRetained").value;
            if (captured != null && retained != null && retained > captured) {
                this.validationErrors.push("Percent Captured needs to be greater than or equal to Percent Retained.");
                break;
            }
        }

        // Sum of percent of site treated <= 100
        const totalPercentOfSiteTreated = rows
            .map((r) => r.get("PercentOfSiteTreated").value)
            .filter((v) => v != null)
            .reduce((sum, v) => sum + v, 0);
        if (totalPercentOfSiteTreated > 100) {
            this.validationErrors.push("The Percent of Site Treated exceeds 100 percent, please correct any errors before saving.");
        }

        return this.validationErrors.length === 0;
    }

    public save(): void {
        if (this.quickBMPRows.invalid) {
            this.quickBMPRows.markAllAsTouched();
            this.alertService.pushAlert(new Alert("Please complete the highlighted required fields before saving.", AlertContext.Danger));
            return;
        }
        if (!this.validate()) {
            return;
        }

        const dtos: QuickBMPUpsertDto[] = this.quickBMPRows.controls.map((row) => {
            return new QuickBMPUpsertDto({
                QuickBMPName: row.get("QuickBMPName").value,
                TreatmentBMPTypeID: row.get("TreatmentBMPTypeID").value,
                QuickBMPNote: row.get("QuickBMPNote").value || null,
                NumberOfIndividualBMPs: row.get("NumberOfIndividualBMPs").value,
                PercentOfSiteTreated: row.get("PercentOfSiteTreated").value ?? null,
                PercentCaptured: row.get("PercentCaptured").value ?? null,
                PercentRetained: row.get("PercentRetained").value ?? null,
                DryWeatherFlowOverrideID: row.get("DryWeatherFlowOverrideID").value ?? null,
            });
        });

        this.wqmpService.mergeQuickBMPsWaterQualityManagementPlan(this.waterQualityManagementPlanID, dtos).subscribe(() => {
            this.alertService.pushAlert(new Alert("Simplified structural BMPs updated successfully.", AlertContext.Success));
            this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
        });
    }

    public cancel(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
    }
}
