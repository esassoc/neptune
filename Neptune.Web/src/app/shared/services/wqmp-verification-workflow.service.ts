import { Injectable, computed, inject, signal } from "@angular/core";
import { Router } from "@angular/router";
import { FormControl, FormGroup, Validators } from "@angular/forms";
import { Observable, forkJoin, map, of, shareReplay, tap } from "rxjs";

import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanVerifyDetailDto } from "src/app/shared/generated/model/water-quality-management-plan-verify-detail-dto";
import { WaterQualityManagementPlanVerifyUpsertDto } from "src/app/shared/generated/model/water-quality-management-plan-verify-upsert-dto";
import {
    WaterQualityManagementPlanVerifyTypesAsSelectDropdownOptions,
} from "src/app/shared/generated/enum/water-quality-management-plan-verify-type-enum";
import {
    WaterQualityManagementPlanVisitStatusesAsSelectDropdownOptions,
} from "src/app/shared/generated/enum/water-quality-management-plan-visit-status-enum";
import {
    WaterQualityManagementPlanVerifyStatusEnum,
    WaterQualityManagementPlanVerifyStatusesAsSelectDropdownOptions,
} from "src/app/shared/generated/enum/water-quality-management-plan-verify-status-enum";
import { NeptunePageTypeEnum } from "src/app/shared/generated/enum/neptune-page-type-enum";

import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";

import { VerificationBasicsForm } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/verification-basics-step.component";
import { BMPChecklistRow } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/structural-bmps-step.component";
import { SourceControlRow } from "src/app/pages/wqmps/wqmp-detail/verification-wizard/steps/source-control-step.component";

export type VerificationWizardMode = "create" | "edit" | "view";

export const VERIFICATION_STEP_KEYS = [
    "basics",
    "structural-bmps",
    "simplified-bmps",
    "source-control",
    "supporting-documentation",
    "review-and-finalize",
] as const;
export type VerificationStepKey = (typeof VERIFICATION_STEP_KEYS)[number];

export interface VerificationStep {
    key: VerificationStepKey;
    title: string;
    helpID: number;
}

export interface WorkflowStepStatus {
    completed: boolean;
    disabled: boolean;
}

@Injectable({ providedIn: "root" })
export class WqmpVerificationWorkflowService {
    private router = inject(Router);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private alertService = inject(AlertService);

    public readonly steps: VerificationStep[] = [
        { key: "basics", title: "Basics", helpID: NeptunePageTypeEnum.WQMPVerificationBasics },
        { key: "structural-bmps", title: "Structural BMPs", helpID: NeptunePageTypeEnum.WQMPVerificationStructuralBmps },
        { key: "simplified-bmps", title: "Simplified BMPs", helpID: NeptunePageTypeEnum.WQMPVerificationSimplifiedBmps },
        { key: "source-control", title: "Source Control", helpID: NeptunePageTypeEnum.WQMPVerificationSourceControl },
        { key: "supporting-documentation", title: "Supporting Documentation", helpID: NeptunePageTypeEnum.WQMPVerificationSupportingDocumentation },
        { key: "review-and-finalize", title: "Review & Finalize", helpID: NeptunePageTypeEnum.WQMPVerificationReview },
    ];

    // -- identity & mode --
    public waterQualityManagementPlanID = signal<number | null>(null);
    public waterQualityManagementPlanVerifyID = signal<number | null>(null);
    public mode = signal<VerificationWizardMode>("create");

    // -- header context --
    public wqmpName = signal<string>("");
    public isFinalized = signal<boolean>(false);

    // -- transient state --
    public isSaving = signal<boolean>(false);

    // -- form state --
    public basicsForm = new FormGroup<VerificationBasicsForm>({
        WaterQualityManagementPlanVerifyTypeID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        WaterQualityManagementPlanVisitStatusID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        VerificationDate: new FormControl<string>(undefined, { validators: [Validators.required], nonNullable: true }),
        WaterQualityManagementPlanVerifyStatusID: new FormControl<number>(undefined, { nonNullable: true }),
        EnforcementOrFollowupActions: new FormControl<string>("", { nonNullable: true }),
        SourceControlCondition: new FormControl<string>("", { nonNullable: true }),
    });
    private basicsFormValidSignal = signal<boolean>(this.basicsForm.valid);
    public basicsFormValid = this.basicsFormValidSignal.asReadonly();

    public treatmentBMPRows = signal<BMPChecklistRow[]>([]);
    public quickBMPRows = signal<BMPChecklistRow[]>([]);
    public sourceControlRows = signal<SourceControlRow[]>([]);

    // NPT-995 Round 5: Supporting Documentation — single FileResource per verification.
    // Mirrors the legacy MVC supporting-documentation panel.
    public supportingDocumentationFileResourceGUID = signal<string | null>(null);
    public supportingDocumentationFileName = signal<string | null>(null);

    // -- lookup options --
    public verifyTypeOptions = WaterQualityManagementPlanVerifyTypesAsSelectDropdownOptions;
    public visitStatusOptions = WaterQualityManagementPlanVisitStatusesAsSelectDropdownOptions;
    public verifyStatusOptions = WaterQualityManagementPlanVerifyStatusesAsSelectDropdownOptions;

    constructor() {
        // Drive a signal from the FormGroup so the progress-aware sidebar updates under zoneless change detection.
        this.basicsForm.statusChanges.subscribe(() => this.basicsFormValidSignal.set(this.basicsForm.valid));
    }

    /** Per-step Completed/Disabled state for the workflow nav. */
    public progress = computed<Record<VerificationStepKey, WorkflowStepStatus>>(() => {
        const hasVerify = this.waterQualityManagementPlanVerifyID() != null;
        const basicsValid = this.basicsFormValid();
        const finalized = this.isFinalized();

        const tbmp = this.treatmentBMPRows();
        const qbmp = this.quickBMPRows();
        const scbmp = this.sourceControlRows();

        const tbmpHasAny = tbmp.some((r) => r.isAdequate !== null || (r.note && r.note.length > 0));
        const qbmpHasAny = qbmp.some((r) => r.isAdequate !== null || (r.note && r.note.length > 0));
        const scbmpHasAny = scbmp.some((r) => r.condition && r.condition.length > 0);

        return {
            "basics": {
                completed: basicsValid && hasVerify,
                disabled: false,
            },
            "structural-bmps": {
                completed: hasVerify && tbmpHasAny,
                disabled: !hasVerify,
            },
            "simplified-bmps": {
                completed: hasVerify && qbmpHasAny,
                disabled: !hasVerify,
            },
            "source-control": {
                completed: hasVerify && scbmpHasAny,
                disabled: !hasVerify,
            },
            "supporting-documentation": {
                // Optional step — "completed" simply means a file has been attached.
                // Not required to enable the next step.
                completed: hasVerify && this.supportingDocumentationFileResourceGUID() != null,
                disabled: !hasVerify,
            },
            "review-and-finalize": {
                completed: finalized,
                disabled: !hasVerify || !basicsValid,
            },
        };
    });

    /** Load WQMP context + (optional) verification record. Returns a hot, replayed observable.
     *  Mode auto-resolves: "create" when no verifyID, "edit" for a draft, and "view" once the load
     *  detects a finalized record below. */
    public load(wqmpID: number, verifyID: number | null): Observable<boolean> {
        // The service is providedIn: "root", so it survives wizard remounts. Reset state up-front
        // so values from a prior session can't bleed into a new wizard before forkJoin resolves.
        this.resetState();

        this.waterQualityManagementPlanID.set(wqmpID);
        this.waterQualityManagementPlanVerifyID.set(verifyID ?? null);
        this.mode.set(verifyID ? "edit" : "create");

        const wqmp$ = this.wqmpService.getWaterQualityManagementPlan(wqmpID);
        const quickBMPs$ = this.wqmpService.listQuickBMPsWaterQualityManagementPlan(wqmpID);
        const sourceControlBMPs$ = this.wqmpService.listSourceControlBMPsWaterQualityManagementPlan(wqmpID);
        const verification$ = verifyID
            ? this.wqmpService.getVerificationWaterQualityManagementPlan(wqmpID, verifyID)
            : of(null as WaterQualityManagementPlanVerifyDetailDto);

        return forkJoin({ wqmp: wqmp$, quickBMPs: quickBMPs$, sourceControlBMPs: sourceControlBMPs$, verify: verification$ }).pipe(
            tap(({ wqmp, quickBMPs, sourceControlBMPs, verify }) => {
                this.wqmpName.set(wqmp.WaterQualityManagementPlanName ?? "");

                if (verify && !verify.IsDraft) {
                    this.mode.set("view");
                }
                if (verify) {
                    this.isFinalized.set(!verify.IsDraft);
                }

                const treatmentBMPs = wqmp.TreatmentBMPs ?? [];
                this.treatmentBMPRows.set(treatmentBMPs.map((bmp: any) => {
                    const existing = verify?.TreatmentBMPs?.find((v: any) => v.TreatmentBMPID === bmp.TreatmentBMPID);
                    return {
                        id: bmp.TreatmentBMPID,
                        name: bmp.TreatmentBMPName,
                        type: bmp.TreatmentBMPTypeName,
                        isAdequate: existing?.IsAdequate ?? null,
                        note: existing?.WaterQualityManagementPlanVerifyTreatmentBMPNote ?? "",
                    };
                }));

                this.quickBMPRows.set(quickBMPs.map((bmp: any) => {
                    const existing = verify?.QuickBMPs?.find((v: any) => v.QuickBMPID === bmp.QuickBMPID);
                    return {
                        id: bmp.QuickBMPID,
                        name: bmp.QuickBMPName,
                        type: bmp.TreatmentBMPTypeName,
                        isAdequate: existing?.IsAdequate ?? null,
                        note: existing?.WaterQualityManagementPlanVerifyQuickBMPNote ?? "",
                    };
                }));

                this.sourceControlRows.set(sourceControlBMPs.map((bmp: any) => {
                    const existing = verify?.SourceControlBMPs?.find((v: any) => v.SourceControlBMPID === bmp.SourceControlBMPID);
                    return {
                        sourceControlBMPID: bmp.SourceControlBMPID,
                        attributeName: bmp.SourceControlBMPAttributeName,
                        categoryName: bmp.SourceControlBMPAttributeCategoryName,
                        isPresent: bmp.IsPresent,
                        condition: existing?.WaterQualityManagementPlanSourceControlCondition ?? "",
                    };
                }));

                if (verify) {
                    this.basicsForm.patchValue({
                        WaterQualityManagementPlanVerifyTypeID: verify.WaterQualityManagementPlanVerifyTypeID,
                        WaterQualityManagementPlanVisitStatusID: verify.WaterQualityManagementPlanVisitStatusID,
                        VerificationDate: verify.VerificationDate ? new Date(verify.VerificationDate).toISOString().split("T")[0] : undefined,
                        WaterQualityManagementPlanVerifyStatusID: verify.WaterQualityManagementPlanVerifyStatusID,
                        EnforcementOrFollowupActions: verify.EnforcementOrFollowupActions ?? "",
                        SourceControlCondition: verify.SourceControlCondition ?? "",
                    });
                    this.supportingDocumentationFileResourceGUID.set(verify.FileResourceGUID ?? null);
                    this.supportingDocumentationFileName.set(verify.FileResourceFileName ?? null);
                }

                if (this.mode() === "view") {
                    this.basicsForm.disable({ emitEvent: false });
                } else {
                    this.basicsForm.enable({ emitEvent: false });
                }
                this.basicsFormValidSignal.set(this.basicsForm.valid);
            }),
            map(() => true),
            shareReplay(1)
        );
    }

    /** Persist current state as a draft. If `andContinue`, navigate to the next step on success. */
    public save(currentStepKey: VerificationStepKey, andContinue: boolean): void {
        this.persist({ isDraft: true, andContinue, returnToDetail: false, currentStepKey });
    }

    /** Persist as final and return to the WQMP detail page. */
    public finalize(): void {
        const dto = this.buildUpsertDto(false);
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
        this.persist({ isDraft: false, andContinue: false, returnToDetail: true, currentStepKey: "review-and-finalize" });
    }

    public stepHelpID(key: VerificationStepKey): number {
        return this.steps.find((s) => s.key === key)?.helpID ?? null;
    }

    private resetState(): void {
        this.wqmpName.set("");
        this.isFinalized.set(false);
        this.isSaving.set(false);
        this.treatmentBMPRows.set([]);
        this.quickBMPRows.set([]);
        this.sourceControlRows.set([]);
        this.supportingDocumentationFileResourceGUID.set(null);
        this.supportingDocumentationFileName.set(null);
        this.basicsForm.reset({}, { emitEvent: false });
        this.basicsForm.enable({ emitEvent: false });
        this.basicsFormValidSignal.set(this.basicsForm.valid);
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

    private persist(opts: {
        isDraft: boolean;
        andContinue: boolean;
        returnToDetail: boolean;
        currentStepKey: VerificationStepKey;
    }): void {
        const wqmpID = this.waterQualityManagementPlanID();
        if (wqmpID == null) return;

        if (this.basicsForm.invalid) {
            this.alertService.pushAlert(new Alert("Please complete all required fields in the Basics step.", AlertContext.Danger));
            this.router.navigate(["/water-quality-management-plans", wqmpID, "verifications", ...this.verifyIDPathSegment(), "basics"]);
            return;
        }

        this.isSaving.set(true);
        this.alertService.clearAlerts();
        const dto = this.buildUpsertDto(opts.isDraft);
        const verifyID = this.waterQualityManagementPlanVerifyID();
        const isCreate = verifyID == null;

        const save$ = isCreate
            ? this.wqmpService.createVerificationWaterQualityManagementPlan(wqmpID, dto)
            : this.wqmpService.updateVerificationWaterQualityManagementPlan(wqmpID, verifyID, dto);

        save$.subscribe({
            next: (saved) => {
                this.isSaving.set(false);
                this.isFinalized.set(!saved?.IsDraft);

                if (saved?.WaterQualityManagementPlanVerifyID && this.waterQualityManagementPlanVerifyID() == null) {
                    this.waterQualityManagementPlanVerifyID.set(saved.WaterQualityManagementPlanVerifyID);
                    this.mode.set("edit");
                }

                // Push the success alert *after* navigation completes — the alert-display on
                // the source page calls clearAlerts() in ngOnDestroy, so a pre-navigate push
                // gets wiped during route teardown (notably on initial create where the URL
                // changes from .../new/... to .../{id}/... and on returnToDetail). Pushing
                // post-navigate lands the alert on the destination page after its
                // alert-display has mounted and subscribed.
                const successMsg = opts.isDraft ? "Verification saved." : "Verification finalized.";
                const navigation$ = opts.returnToDetail
                    ? this.router.navigate(["/water-quality-management-plans", wqmpID])
                    : this.router.navigate([
                        "/water-quality-management-plans", wqmpID, "verifications",
                        ...this.verifyIDPathSegment(),
                        opts.andContinue ? this.nextStepKey(opts.currentStepKey) ?? opts.currentStepKey : opts.currentStepKey,
                    ], { replaceUrl: isCreate });
                navigation$.then(() => {
                    this.alertService.pushAlert(new Alert(successMsg, AlertContext.Success));
                });
            },
            error: () => {
                this.isSaving.set(false);
                this.alertService.pushAlert(new Alert("An error occurred while saving.", AlertContext.Danger));
            },
        });
    }

    private nextStepKey(key: VerificationStepKey): VerificationStepKey | null {
        const idx = this.steps.findIndex((s) => s.key === key);
        return idx >= 0 && idx < this.steps.length - 1 ? this.steps[idx + 1].key : null;
    }

    private verifyIDPathSegment(): (string | number)[] {
        const id = this.waterQualityManagementPlanVerifyID();
        return id == null ? ["new"] : [id];
    }
}
