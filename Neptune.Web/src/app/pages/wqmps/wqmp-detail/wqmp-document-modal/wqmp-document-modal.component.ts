import { Component, inject, OnInit, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { HttpErrorResponse } from "@angular/common/http";
import { DialogRef } from "@ngneat/dialog";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { WaterQualityManagementPlanService } from "src/app/shared/generated/api/water-quality-management-plan.service";
import { WaterQualityManagementPlanDocumentDto } from "src/app/shared/generated/model/water-quality-management-plan-document-dto";
import { WaterQualityManagementPlanDocumentTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/water-quality-management-plan-document-type-enum";

export interface WqmpDocumentModalContext {
    waterQualityManagementPlanID: number;
    mode: "add" | "edit";
    document?: WaterQualityManagementPlanDocumentDto;
}

// NPT-1068: per-WQMP document upload/edit. Replaces the legacy MVC New.cshtml + Edit.cshtml
// pair with a single modal — file required on add, optional on edit (drop a new file to
// swap it out). Matches the wqmp-modal add/edit pattern for consistency.
@Component({
    selector: "wqmp-document-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent],
    templateUrl: "./wqmp-document-modal.component.html",
    styleUrl: "./wqmp-document-modal.component.scss",
})
export class WqmpDocumentModalComponent implements OnInit {
    public ref: DialogRef<WqmpDocumentModalContext, boolean> = inject(DialogRef);
    public FormFieldType = FormFieldType;
    public mode: "add" | "edit" = "add";

    public fileControl = new FormControl<File | null>(null);
    public displayNameControl = new FormControl<string>("", { nonNullable: true, validators: [Validators.required, Validators.maxLength(255)] });
    public documentTypeIDControl = new FormControl<number | null>(null, { validators: [Validators.required] });
    public descriptionControl = new FormControl<string>("", { nonNullable: true });

    public formGroup = new FormGroup({
        file: this.fileControl,
        displayName: this.displayNameControl,
        documentTypeID: this.documentTypeIDControl,
        description: this.descriptionControl,
    });

    public documentTypeOptions: SelectDropdownOption[] = WaterQualityManagementPlanDocumentTypesAsSelectDropdownOptions;
    public isSaving = signal(false);
    public uploadFileAccepts = ".pdf,.zip,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png";

    constructor(
        private alertService: AlertService,
        private wqmpService: WaterQualityManagementPlanService,
    ) {}

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.mode = this.ref.data.mode;

        if (this.mode === "add") {
            this.fileControl.setValidators([Validators.required]);
        }

        if (this.mode === "edit" && this.ref.data.document) {
            const doc = this.ref.data.document;
            this.displayNameControl.setValue(doc.DisplayName ?? "");
            this.documentTypeIDControl.setValue(doc.WaterQualityManagementPlanDocumentTypeID);
            this.descriptionControl.setValue(doc.Description ?? "");
        }
    }

    // Auto-fill DisplayName from filename on first add so users get a sensible default. Only
    // overwrites when DisplayName is empty so it doesn't clobber an admin edit.
    onFileChange(file: File | null): void {
        if (!file) return;
        if (!this.displayNameControl.value || this.displayNameControl.value.trim() === "") {
            this.displayNameControl.setValue(file.name);
        }
    }

    save(): void {
        if (this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            return;
        }

        this.isSaving.set(true);
        this.alertService.clearAlerts();

        const wqmpID = this.ref.data.waterQualityManagementPlanID;
        const file = this.fileControl.value;
        const displayName = this.displayNameControl.value;
        const documentTypeID = this.documentTypeIDControl.value!;
        const description = this.descriptionControl.value || undefined;

        const obs =
            this.mode === "add"
                ? this.wqmpService.createDocumentWaterQualityManagementPlan(wqmpID, file as Blob, displayName, documentTypeID, description)
                : this.wqmpService.updateDocumentWaterQualityManagementPlan(
                      wqmpID,
                      this.ref.data.document!.WaterQualityManagementPlanDocumentID,
                      displayName,
                      documentTypeID,
                      file ? (file as Blob) : undefined,
                      description,
                  );

        obs.subscribe({
            next: () => {
                this.isSaving.set(false);
                this.alertService.pushAlert(new Alert(this.mode === "add" ? "Document uploaded." : "Document updated.", AlertContext.Success));
                this.ref.close(true);
            },
            error: (err: HttpErrorResponse) => {
                this.isSaving.set(false);
                const raw = err?.error?.detail ?? err?.error?.title ?? err?.error ?? "Save failed.";
                this.alertService.pushAlert(new Alert(typeof raw === "string" ? raw : "Save failed.", AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.ref.close(null);
    }
}
