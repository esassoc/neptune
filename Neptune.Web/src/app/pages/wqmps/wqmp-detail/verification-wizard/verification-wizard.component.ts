import { Component, inject, Input, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { AsyncPipe } from "@angular/common";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { forkJoin, map, Observable, of, shareReplay, tap } from "rxjs";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanVerifyDetailDto } from "src/app/shared/generated/model/water-quality-management-plan-verify-detail-dto";
import { WaterQualityManagementPlanVerifyUpsertDto } from "src/app/shared/generated/model/water-quality-management-plan-verify-upsert-dto";
import { WaterQualityManagementPlanVerifyTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-verify-type-enum";
import { WaterQualityManagementPlanVisitStatusesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-visit-status-enum";
import { WaterQualityManagementPlanVerifyStatusesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-verify-status-enum";
import { WaterQualityManagementPlanVerifyStatusEnum } from "src/app/shared/generated/enum/water-quality-management-plan-verify-status-enum";
import { VerificationBasicsStepComponent, VerificationBasicsForm } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/verification-basics-step.component";
import { StructuralBmpsStepComponent, BMPChecklistRow } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/structural-bmps-step.component";
import { SimplifiedBmpsStepComponent } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/simplified-bmps-step.component";
import { SourceControlStepComponent, SourceControlRow } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/source-control-step.component";
import { ReviewStepComponent } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/review-step.component";

@Component({
    selector: "verification-wizard",
    standalone: true,
    imports: [
        PageHeaderComponent, AlertDisplayComponent,
        VerificationBasicsStepComponent, StructuralBmpsStepComponent,
        SimplifiedBmpsStepComponent, SourceControlStepComponent, ReviewStepComponent,
        RouterLink, AsyncPipe,
    ],
    templateUrl: "./verification-wizard.component.html",
    styleUrl: "./verification-wizard.component.scss",
})
export class VerificationWizardComponent implements OnInit {
    @Input() waterQualityManagementPlanID!: number;
    @Input() waterQualityManagementPlanVerifyID: number;

    private router = inject(Router);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private alertService = inject(AlertService);

    public FormFieldType = FormFieldType;
    public loaded$ : Observable<boolean>;
    public mode: "create" | "edit" | "view" = "create";
    public currentStep = signal(1);
    public isSaving = false;

    // Step 1 - Basics form
    public basicsForm = new FormGroup<VerificationBasicsForm>({
        WaterQualityManagementPlanVerifyTypeID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        WaterQualityManagementPlanVisitStatusID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        VerificationDate: new FormControl<string>(undefined, { validators: [Validators.required], nonNullable: true }),
        WaterQualityManagementPlanVerifyStatusID: new FormControl<number>(undefined, { nonNullable: true }),
        EnforcementOrFollowupActions: new FormControl<string>("", { nonNullable: true }),
        SourceControlCondition: new FormControl<string>("", { nonNullable: true }),
    });

    // Lookup options
    public verifyTypeOptions = WaterQualityManagementPlanVerifyTypesAsSelectDropdownOptions;
    public visitStatusOptions = WaterQualityManagementPlanVisitStatusesAsSelectDropdownOptions;
    public verifyStatusOptions = WaterQualityManagementPlanVerifyStatusesAsSelectDropdownOptions;

    // Step 2 & 3 - BMP checklists
    public treatmentBMPRows = signal<BMPChecklistRow[]>([]);
    public quickBMPRows = signal<BMPChecklistRow[]>([]);

    // Step 4 - Source control
    public sourceControlRows = signal<SourceControlRow[]>([]);

    public steps = [
        { number: 1, title: "Basics" },
        { number: 2, title: "Structural BMPs" },
        { number: 3, title: "Simplified BMPs" },
        { number: 4, title: "Source Control" },
        { number: 5, title: "Review & Finalize" },
    ];

    ngOnInit(): void {
        this.mode = this.waterQualityManagementPlanVerifyID ? "edit" : "create";

        const wqmpDetail$ = this.wqmpService.getWaterQualityManagementPlan(this.waterQualityManagementPlanID);
        const quickBMPs$ = this.wqmpService.listQuickBMPsWaterQualityManagementPlan(this.waterQualityManagementPlanID);
        const sourceControlBMPs$ = this.wqmpService.listSourceControlBMPsWaterQualityManagementPlan(this.waterQualityManagementPlanID);

        const verification$ = this.waterQualityManagementPlanVerifyID
            ? this.wqmpService.getVerificationWaterQualityManagementPlan(this.waterQualityManagementPlanID, this.waterQualityManagementPlanVerifyID)
            : of(null as WaterQualityManagementPlanVerifyDetailDto);

        this.loaded$ = forkJoin({
            wqmp: wqmpDetail$,
            quickBMPs: quickBMPs$,
            sourceControlBMPs: sourceControlBMPs$,
            verify: verification$,
        }).pipe(
            tap(({ wqmp, quickBMPs, sourceControlBMPs, verify: existingVerify }) => {

                if (existingVerify && !existingVerify.IsDraft) {
                    this.mode = "view";
                }

                // Build Treatment BMP rows from WQMP's associated BMPs
                const treatmentBMPs = wqmp.TreatmentBMPs ?? [];
                this.treatmentBMPRows.set(treatmentBMPs.map((bmp: any) => {
                    const existing = existingVerify?.TreatmentBMPs?.find((v: any) => v.TreatmentBMPID === bmp.TreatmentBMPID);
                    return {
                        id: bmp.TreatmentBMPID,
                        name: bmp.TreatmentBMPName,
                        type: bmp.TreatmentBMPTypeName,
                        isAdequate: existing?.IsAdequate ?? null,
                        note: existing?.WaterQualityManagementPlanVerifyTreatmentBMPNote ?? "",
                    };
                }));

                // Build Quick BMP rows
                this.quickBMPRows.set(quickBMPs.map((bmp: any) => {
                    const existing = existingVerify?.QuickBMPs?.find((v: any) => v.QuickBMPID === bmp.QuickBMPID);
                    return {
                        id: bmp.QuickBMPID,
                        name: bmp.QuickBMPName,
                        type: bmp.TreatmentBMPTypeName,
                        isAdequate: existing?.IsAdequate ?? null,
                        note: existing?.WaterQualityManagementPlanVerifyQuickBMPNote ?? "",
                    };
                }));

                // Build Source Control BMP rows
                this.sourceControlRows.set(sourceControlBMPs.map((bmp: any) => {
                    const existing = existingVerify?.SourceControlBMPs?.find((v: any) => v.SourceControlBMPID === bmp.SourceControlBMPID);
                    return {
                        sourceControlBMPID: bmp.SourceControlBMPID,
                        attributeName: bmp.SourceControlBMPAttributeName,
                        categoryName: bmp.SourceControlBMPAttributeCategoryName,
                        isPresent: bmp.IsPresent,
                        condition: existing?.WaterQualityManagementPlanSourceControlCondition ?? "",
                    };
                }));

                // Populate basics form from existing verification
                if (existingVerify) {
                    this.basicsForm.patchValue({
                        WaterQualityManagementPlanVerifyTypeID: existingVerify.WaterQualityManagementPlanVerifyTypeID,
                        WaterQualityManagementPlanVisitStatusID: existingVerify.WaterQualityManagementPlanVisitStatusID,
                        VerificationDate: existingVerify.VerificationDate ? new Date(existingVerify.VerificationDate).toISOString().split("T")[0] : undefined,
                        WaterQualityManagementPlanVerifyStatusID: existingVerify.WaterQualityManagementPlanVerifyStatusID,
                        EnforcementOrFollowupActions: existingVerify.EnforcementOrFollowupActions ?? "",
                        SourceControlCondition: existingVerify.SourceControlCondition ?? "",
                    });
                }

                if (this.mode === "view") {
                    this.basicsForm.disable();
                }
            }),
            map(() => true),
            shareReplay(1)
        );
    }

    goToStep(step: number): void {
        if (step >= 1 && step <= this.steps.length) {
            this.currentStep.set(step);
        }
    }

    nextStep(): void {
        this.goToStep(this.currentStep() + 1);
    }

    prevStep(): void {
        this.goToStep(this.currentStep() - 1);
    }

    private buildUpsertDto(isDraft: boolean): WaterQualityManagementPlanVerifyUpsertDto {
        const form = this.basicsForm.getRawValue();
        return {
            WaterQualityManagementPlanVerifyTypeID: form.WaterQualityManagementPlanVerifyTypeID,
            WaterQualityManagementPlanVisitStatusID: form.WaterQualityManagementPlanVisitStatusID,
            VerificationDate: form.VerificationDate,
            WaterQualityManagementPlanVerifyStatusID: form.WaterQualityManagementPlanVerifyStatusID || undefined,
            SourceControlCondition: form.SourceControlCondition || undefined,
            EnforcementOrFollowupActions: form.EnforcementOrFollowupActions || undefined,
            IsDraft: isDraft,
            TreatmentBMPs: this.treatmentBMPRows().map((r) => ({
                TreatmentBMPID: r.id,
                IsAdequate: r.isAdequate,
                WaterQualityManagementPlanVerifyTreatmentBMPNote: r.note || undefined,
            })),
            QuickBMPs: this.quickBMPRows().map((r) => ({
                QuickBMPID: r.id,
                IsAdequate: r.isAdequate,
                WaterQualityManagementPlanVerifyQuickBMPNote: r.note || undefined,
            })),
            SourceControlBMPs: this.sourceControlRows().filter((r) => r.condition).map((r) => ({
                SourceControlBMPID: r.sourceControlBMPID,
                WaterQualityManagementPlanSourceControlCondition: r.condition,
            })),
        };
    }

    saveDraft(): void {
        this.save(true);
    }

    finalize(): void {
        const dto = this.buildUpsertDto(false);
        // Validate: cannot finalize as Adequate if any BMP is marked No
        if (dto.WaterQualityManagementPlanVerifyStatusID === WaterQualityManagementPlanVerifyStatusEnum.AdequateOAndMofWQMPisVerified) {
            const anyInadequate = [...dto.TreatmentBMPs, ...dto.QuickBMPs].some((b) => b.IsAdequate === false);
            if (anyInadequate) {
                this.alertService.pushAlert(new Alert(
                    "Cannot finalize as \"Adequate\" when any BMP is marked as not adequate.",
                    AlertContext.Danger
                ));
                return;
            }
        }
        this.save(false);
    }

    private save(isDraft: boolean): void {
        if (this.basicsForm.invalid) {
            this.alertService.pushAlert(new Alert("Please complete all required fields in the Basics step.", AlertContext.Danger));
            this.currentStep.set(1);
            return;
        }

        this.isSaving = true;
        this.alertService.clearAlerts();
        const dto = this.buildUpsertDto(isDraft);

        const save$ = this.mode === "edit"
            ? this.wqmpService.updateVerificationWaterQualityManagementPlan(this.waterQualityManagementPlanID, this.waterQualityManagementPlanVerifyID, dto)
            : this.wqmpService.createVerificationWaterQualityManagementPlan(this.waterQualityManagementPlanID, dto);

        save$.subscribe({
            next: () => {
                this.isSaving = false;
                const msg = isDraft ? "Verification saved as draft." : "Verification finalized.";
                this.alertService.pushAlert(new Alert(msg, AlertContext.Success));
                this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
            },
            error: () => {
                this.isSaving = false;
                this.alertService.pushAlert(new Alert("An error occurred while saving.", AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
    }

    deleteVerification(): void {
        if (!confirm("Are you sure you want to delete this verification?")) return;
        this.wqmpService.deleteVerificationWaterQualityManagementPlan(this.waterQualityManagementPlanID, this.waterQualityManagementPlanVerifyID).subscribe({
            next: () => {
                this.alertService.pushAlert(new Alert("Verification deleted.", AlertContext.Success));
                this.router.navigate(["/water-quality-management-plans", this.waterQualityManagementPlanID]);
            },
            error: () => {
                this.alertService.pushAlert(new Alert("An error occurred while deleting.", AlertContext.Danger));
            },
        });
    }
}
