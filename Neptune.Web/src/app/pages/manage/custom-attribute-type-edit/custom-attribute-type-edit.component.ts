import { Component, inject, Input, OnInit, signal } from "@angular/core";
import { Router, RouterLink } from "@angular/router";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { PageHeaderComponent } from "src/app/shared/components/page-header/page-header.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { Alert } from "src/app/shared/models/alert";
import { AlertContext } from "src/app/shared/models/enums/alert-context.enum";
import { CustomAttributeTypeService } from "src/app/shared/generated/api/custom-attribute-type.service";
import { CustomAttributeTypeDto } from "src/app/shared/generated/model/custom-attribute-type-dto";
import { CustomAttributeTypeUpsertDto } from "src/app/shared/generated/model/custom-attribute-type-upsert-dto";
import {
    CustomAttributeDataTypeEnum,
    CustomAttributeDataTypesAsSelectDropdownOptions,
} from "src/app/shared/generated/enum/custom-attribute-data-type-enum";
import { CustomAttributeTypePurposeEnum, CustomAttributeTypePurposesAsSelectDropdownOptions } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { MeasurementUnitTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/measurement-unit-type-enum";

/**
 * NPT-1038 Round 3: routed full-page editor for CustomAttributeType, replacing the prior modal.
 * Mirrors the legacy MVC layout (Neptune.WebMvc/Views/CustomAttributeType/Edit.cshtml) and the
 * observation-type-edit sibling pattern. Backs /manage/custom-attributes/new (create) and
 * /manage/custom-attributes/:customAttributeTypeID/edit.
 */
@Component({
    selector: "custom-attribute-type-edit",
    standalone: true,
    imports: [RouterLink, ReactiveFormsModule, PageHeaderComponent, AlertDisplayComponent, FormFieldComponent],
    templateUrl: "./custom-attribute-type-edit.component.html",
    styleUrl: "./custom-attribute-type-edit.component.scss",
})
export class CustomAttributeTypeEditComponent implements OnInit {
    @Input() customAttributeTypeID?: number;

    private customAttributeTypeService = inject(CustomAttributeTypeService);
    private alertService = inject(AlertService);
    private router = inject(Router);

    public FormFieldType = FormFieldType;
    public isEdit = false;
    public isLoading = signal(false);
    public isSaving = signal(false);
    public isModelingAttribute = false;

    public formGroup = new FormGroup({
        CustomAttributeTypeName: new FormControl<string>("", { validators: [Validators.required, Validators.maxLength(100)], nonNullable: true }),
        CustomAttributeDataTypeID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        CustomAttributeTypePurposeID: new FormControl<number>(undefined, { validators: [Validators.required], nonNullable: true }),
        MeasurementUnitTypeID: new FormControl<number>(undefined, { nonNullable: true }),
        IsRequired: new FormControl<boolean>(false, { nonNullable: true }),
        CustomAttributeTypeDescription: new FormControl<string>("", { validators: [Validators.maxLength(500)], nonNullable: true }),
        CustomAttributeTypeDefaultValue: new FormControl<string>("", { validators: [Validators.maxLength(1000)], nonNullable: true }),
    });

    public dataTypeOptions = CustomAttributeDataTypesAsSelectDropdownOptions;
    public purposeOptions = CustomAttributeTypePurposesAsSelectDropdownOptions;
    public unitOptions = MeasurementUnitTypesAsSelectDropdownOptions;
    public booleanOptions: SelectDropdownOption[] = [
        { Label: "Yes", Value: true } as any,
        { Label: "No", Value: false } as any,
    ];

    public optionsList = signal<string[]>([]);
    public newOptionControl = new FormControl<string>("", { nonNullable: true });
    // NPT-1038: inline-edit support for PickFromList/MultiSelect option text. Tracks
    // which option row is currently being edited (or null when nothing is edited).
    public editingOptionIndex = signal<number | null>(null);
    public editingOptionControl = new FormControl<string>("", { nonNullable: true });

    ngOnInit(): void {
        this.alertService.clearAlerts();

        // Route params arrive as strings via withComponentInputBinding — normalize before the
        // isEdit check (matches the observation-type-edit pattern).
        const id = this.customAttributeTypeID != null ? Number(this.customAttributeTypeID) : undefined;
        this.customAttributeTypeID = Number.isFinite(id) ? id : undefined;
        this.isEdit = this.customAttributeTypeID != null;

        // NPT-1038: hide the Modeling purpose on create. Modeling-purpose attributes are
        // system-managed and the backend ValidateForCreate also rejects them.
        if (!this.isEdit) {
            this.purposeOptions = CustomAttributeTypePurposesAsSelectDropdownOptions.filter(
                (o) => (o.Value as number) !== CustomAttributeTypePurposeEnum.Modeling
            );
            return;
        }

        this.isLoading.set(true);
        this.customAttributeTypeService.getCustomAttributeType(this.customAttributeTypeID!).subscribe({
            next: (cat: CustomAttributeTypeDto) => {
                this.applyExisting(cat);
                this.isLoading.set(false);
            },
            error: () => {
                this.isLoading.set(false);
                this.alertService.pushAlert(new Alert("Failed to load custom attribute type.", AlertContext.Danger));
            },
        });
    }

    private applyExisting(cat: CustomAttributeTypeDto): void {
        this.formGroup.patchValue({
            CustomAttributeTypeName: cat.CustomAttributeTypeName,
            CustomAttributeDataTypeID: cat.CustomAttributeDataTypeID,
            CustomAttributeTypePurposeID: cat.CustomAttributeTypePurposeID,
            MeasurementUnitTypeID: cat.MeasurementUnitTypeID,
            IsRequired: cat.IsRequired,
            CustomAttributeTypeDescription: cat.CustomAttributeTypeDescription,
            CustomAttributeTypeDefaultValue: cat.CustomAttributeTypeDefaultValue,
        });

        if (cat.CustomAttributeTypeOptionsSchema) {
            try {
                const parsed = JSON.parse(cat.CustomAttributeTypeOptionsSchema);
                this.optionsList.set(Array.isArray(parsed) ? parsed : []);
            } catch {
                this.optionsList.set([]);
            }
        }

        // Restrict data type changes in edit mode (only String <-> PickFromList/MultiSelect).
        const allowedTypes = [CustomAttributeDataTypeEnum.String, CustomAttributeDataTypeEnum.PickFromList, CustomAttributeDataTypeEnum.MultiSelect];
        if (!allowedTypes.includes(cat.CustomAttributeDataTypeID)) {
            this.formGroup.controls.CustomAttributeDataTypeID.disable();
        } else {
            this.dataTypeOptions = CustomAttributeDataTypesAsSelectDropdownOptions.filter((o) =>
                allowedTypes.includes(o.Value as number)
            );
        }

        // Modeling attributes: restrict to description-only editing.
        if (cat.CustomAttributeTypePurposeID === CustomAttributeTypePurposeEnum.Modeling) {
            this.isModelingAttribute = true;
            this.formGroup.controls.CustomAttributeTypeName.disable();
            this.formGroup.controls.CustomAttributeDataTypeID.disable();
            this.formGroup.controls.CustomAttributeTypePurposeID.disable();
            this.formGroup.controls.MeasurementUnitTypeID.disable();
            this.formGroup.controls.IsRequired.disable();
            this.formGroup.controls.CustomAttributeTypeDefaultValue.disable();
        }
    }

    get showOptionsEditor(): boolean {
        const dataTypeID = this.formGroup.controls.CustomAttributeDataTypeID.value;
        return dataTypeID === CustomAttributeDataTypeEnum.PickFromList || dataTypeID === CustomAttributeDataTypeEnum.MultiSelect;
    }

    addOption(): void {
        const text = (this.newOptionControl.value ?? "").trim();
        if (text && !this.optionsList().includes(text)) {
            this.optionsList.update((list) => [...list, text]);
            this.newOptionControl.setValue("");
        }
    }

    removeOption(index: number): void {
        this.optionsList.update((list) => list.filter((_, i) => i !== index));
        // Keep editingOptionIndex in sync with the post-delete array: deleting the
        // currently-edited row clears the edit; deleting an earlier row shifts the
        // edited index left by one; deleting a later row leaves it alone.
        const editing = this.editingOptionIndex();
        if (editing === null) return;
        if (editing === index) {
            this.editingOptionIndex.set(null);
        } else if (editing > index) {
            this.editingOptionIndex.set(editing - 1);
        }
    }

    startEditOption(index: number): void {
        this.editingOptionControl.setValue(this.optionsList()[index] ?? "");
        this.editingOptionIndex.set(index);
    }

    saveOptionEdit(index: number): void {
        const trimmed = (this.editingOptionControl.value ?? "").trim();
        if (!trimmed) {
            this.editingOptionIndex.set(null);
            return;
        }
        const current = this.optionsList();
        if (current.some((opt, i) => i !== index && opt === trimmed)) {
            this.editingOptionIndex.set(null);
            return;
        }
        this.optionsList.update((list) => list.map((opt, i) => (i === index ? trimmed : opt)));
        this.editingOptionIndex.set(null);
    }

    cancelOptionEdit(): void {
        this.editingOptionIndex.set(null);
    }

    save(): void {
        if (this.formGroup.invalid) {
            this.formGroup.markAllAsTouched();
            this.alertService.pushAlert(new Alert("Please complete the highlighted required fields before saving.", AlertContext.Danger));
            return;
        }
        const raw = this.formGroup.getRawValue();
        const dto: CustomAttributeTypeUpsertDto = {
            CustomAttributeTypeName: raw.CustomAttributeTypeName,
            CustomAttributeDataTypeID: raw.CustomAttributeDataTypeID,
            CustomAttributeTypePurposeID: raw.CustomAttributeTypePurposeID,
            MeasurementUnitTypeID: raw.MeasurementUnitTypeID || undefined,
            IsRequired: raw.IsRequired,
            CustomAttributeTypeDescription: raw.CustomAttributeTypeDescription || undefined,
            CustomAttributeTypeOptionsSchema: this.showOptionsEditor ? JSON.stringify(this.optionsList()) : undefined,
            CustomAttributeTypeDefaultValue: raw.CustomAttributeTypeDefaultValue || undefined,
        };

        this.isSaving.set(true);
        this.alertService.clearAlerts();
        const save$ = this.isEdit
            ? this.customAttributeTypeService.updateCustomAttributeType(this.customAttributeTypeID!, dto)
            : this.customAttributeTypeService.createCustomAttributeType(dto);

        const successMessage = `Custom attribute type ${this.isEdit ? "updated" : "created"}.`;
        save$.subscribe({
            next: (saved: CustomAttributeTypeDto) => {
                this.isSaving.set(false);
                // Mirror the legacy MVC controller: redirect to the detail page on save
                // (both create and edit). Use the returned DTO's ID so create-mode flows
                // land on the freshly-created row. Push the alert AFTER navigation so
                // the detail page's <app-alert-display> sees it.
                const targetID = this.isEdit ? this.customAttributeTypeID! : saved.CustomAttributeTypeID!;
                this.router.navigate(["/manage/custom-attributes", targetID]).then(() => {
                    this.alertService.pushAlert(new Alert(successMessage, AlertContext.Success));
                });
            },
            error: () => {
                this.isSaving.set(false);
                this.alertService.pushAlert(new Alert(`Failed to ${this.isEdit ? "update" : "create"} custom attribute type.`, AlertContext.Danger));
            },
        });
    }

    cancel(): void {
        this.router.navigate(["/manage/custom-attributes"]);
    }
}
