import { Component, DestroyRef, inject, OnInit, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormControl, ReactiveFormsModule } from "@angular/forms";
import { FormFieldComponent, FormFieldType } from "src/app/shared/components/forms/form-field/form-field.component";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { ConfirmService } from "src/app/shared/services/confirm/confirm.service";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WqmpVerificationWorkflowService } from "src/app/shared/services/wqmp-verification-workflow.service";
import { escapeHtml } from "src/app/shared/helpers/html-escape";
import { fileResourceUrl } from "src/app/shared/helpers/file-resource-url";

/**
 * NPT-995 Round 5: Supporting Documentation step. Mirrors the legacy MVC panel —
 * a single optional FileResource per verification (upload field checklist,
 * self-certification form, O&M records, etc.). Backed by the new
 * supporting-documentation endpoints on WaterQualityManagementPlanController.
 */
@Component({
    selector: "supporting-documentation-step",
    standalone: true,
    imports: [ReactiveFormsModule, PageHeaderComponent, FormFieldComponent],
    templateUrl: "./supporting-documentation-step.component.html",
    styleUrl: "./supporting-documentation-step.component.scss",
})
export class SupportingDocumentationStepComponent implements OnInit {
    public service = inject(WqmpVerificationWorkflowService);
    private wqmpService = inject(WaterQualityManagementPlanService);
    private alertService = inject(AlertService);
    private confirmService = inject(ConfirmService);
    private destroyRef = inject(DestroyRef);

    public FormFieldType = FormFieldType;
    public fileControl = new FormControl<File>(null);
    public uploadFileAccepts = ".pdf,.jpg,.jpeg,.png,.docx,.doc,.xlsx,.csv,.txt";

    public isUploading = signal(false);

    ngOnInit(): void {
        // Trigger the upload off the FormControl's valueChanges (single source of truth) rather
        // than (change) on the form-field — Angular's (change) binding on a custom component
        // catches both the inner input's bubbled DOM event AND the form-field's @Output() change
        // EventEmitter, firing the handler twice (once with a useless Event, once with the File).
        this.fileControl.valueChanges
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe((file) => {
                if (file instanceof File) this.upload(file);
            });
    }

    get readonly(): boolean {
        return this.service.mode() === "view";
    }

    isBusy(): boolean {
        return this.isUploading() || this.service.isSaving();
    }

    getDownloadUrl = fileResourceUrl;

    private upload(file: File): void {
        const wqmpID = this.service.waterQualityManagementPlanID();
        const verifyID = this.service.waterQualityManagementPlanVerifyID();
        if (wqmpID == null || verifyID == null) {
            this.alertService.pushAlert(new Alert("Save the Basics step before attaching a document.", AlertContext.Danger));
            this.fileControl.reset(null, { emitEvent: false });
            return;
        }

        this.isUploading.set(true);
        this.wqmpService.uploadVerificationSupportingDocumentationWaterQualityManagementPlan(wqmpID, verifyID, file).subscribe({
            next: (dto) => {
                this.isUploading.set(false);
                this.service.supportingDocumentationFileResourceGUID.set(dto.FileResourceGUID ?? null);
                this.service.supportingDocumentationFileName.set(dto.FileResourceFileName ?? null);
                this.alertService.pushAlert(new Alert("Supporting documentation uploaded.", AlertContext.Success));
                this.fileControl.reset(null, { emitEvent: false });
            },
            error: (err) => {
                this.isUploading.set(false);
                // ProblemDetails text rendered via [innerHTML] in AlertDisplayComponent — escape
                // server-supplied strings before interpolation. See NPT-1038 / NPT-995 Round 5 review.
                const raw = err?.error?.detail ?? err?.error?.title ?? err?.error ?? "Upload failed.";
                this.alertService.pushAlert(new Alert(escapeHtml(typeof raw === "string" ? raw : "Upload failed."), AlertContext.Danger));
                this.fileControl.reset(null, { emitEvent: false });
            },
        });
    }

    remove(): void {
        const wqmpID = this.service.waterQualityManagementPlanID();
        const verifyID = this.service.waterQualityManagementPlanVerifyID();
        if (wqmpID == null || verifyID == null) return;
        const name = escapeHtml(this.service.supportingDocumentationFileName() || "this document");
        this.confirmService.confirm({
            title: "Remove Supporting Documentation",
            message: `<p>Remove <strong>${name}</strong> from this verification?</p><p>This cannot be undone.</p>`,
            buttonClassYes: "btn btn-danger",
            buttonTextYes: "Remove",
            buttonTextNo: "Cancel",
        }).then((confirmed) => {
            if (!confirmed) return;
            this.isUploading.set(true);
            this.wqmpService.deleteVerificationSupportingDocumentationWaterQualityManagementPlan(wqmpID, verifyID).subscribe({
                next: () => {
                    this.isUploading.set(false);
                    this.service.supportingDocumentationFileResourceGUID.set(null);
                    this.service.supportingDocumentationFileName.set(null);
                    this.alertService.pushAlert(new Alert("Supporting documentation removed.", AlertContext.Success));
                },
                error: (err) => {
                    this.isUploading.set(false);
                    const raw = err?.error?.detail ?? err?.error?.title ?? err?.error ?? "Remove failed.";
                    this.alertService.pushAlert(new Alert(escapeHtml(typeof raw === "string" ? raw : "Remove failed."), AlertContext.Danger));
                },
            });
        }).catch(() => { /* dismissed — no-op */ });
    }

    continueToReview(): void {
        // The file upload itself persists immediately; this button just navigates to Review.
        this.service.save("supporting-documentation", true);
    }
}
