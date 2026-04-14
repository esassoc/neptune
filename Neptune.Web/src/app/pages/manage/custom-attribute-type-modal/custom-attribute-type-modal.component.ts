import { Component, inject, OnInit, signal } from "@angular/core";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { DialogRef } from "@ngneat/dialog";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component";
import { AlertDisplayComponent } from "src/app/shared/components/alert-display/alert-display.component";
import { AlertService } from "src/app/shared/services/alert.service";
import { CustomAttributeTypeService } from "src/app/shared/generated/api/custom-attribute-type.service";
import { CustomAttributeTypeDto } from "src/app/shared/generated/model/custom-attribute-type-dto";
import { CustomAttributeTypeUpsertDto } from "src/app/shared/generated/model/custom-attribute-type-upsert-dto";
import {
    CustomAttributeDataTypeEnum,
    CustomAttributeDataTypesAsSelectDropdownOptions,
} from "src/app/shared/generated/enum/custom-attribute-data-type-enum";
import { CustomAttributeTypePurposeEnum, CustomAttributeTypePurposesAsSelectDropdownOptions } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { MeasurementUnitTypesAsSelectDropdownOptions } from "src/app/shared/generated/enum/measurement-unit-type-enum";

@Component({
    selector: "custom-attribute-type-modal",
    standalone: true,
    imports: [ReactiveFormsModule, FormFieldComponent, AlertDisplayComponent],
    templateUrl: "./custom-attribute-type-modal.component.html",
})
export class CustomAttributeTypeModalComponent implements OnInit {
    public ref: DialogRef<{ mode: "add" | "edit"; customAttributeType?: CustomAttributeTypeDto }, boolean> = inject(DialogRef);
    private customAttributeTypeService = inject(CustomAttributeTypeService);
    private alertService = inject(AlertService);

    public FormFieldType = FormFieldType;
    public mode: "add" | "edit";
    public isEdit = false;
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
    public newOptionText = signal("");

    ngOnInit(): void {
        this.alertService.clearAlerts();
        this.mode = this.ref.data.mode;
        this.isEdit = this.mode === "edit";

        if (this.isEdit && this.ref.data.customAttributeType) {
            const cat = this.ref.data.customAttributeType;
            this.formGroup.patchValue({
                CustomAttributeTypeName: cat.CustomAttributeTypeName,
                CustomAttributeDataTypeID: cat.CustomAttributeDataTypeID,
                CustomAttributeTypePurposeID: cat.CustomAttributeTypePurposeID,
                MeasurementUnitTypeID: cat.MeasurementUnitTypeID,
                IsRequired: cat.IsRequired,
                CustomAttributeTypeDescription: cat.CustomAttributeTypeDescription,
                CustomAttributeTypeDefaultValue: cat.CustomAttributeTypeDefaultValue,
            });

            // Parse existing options schema
            if (cat.CustomAttributeTypeOptionsSchema) {
                try {
                    this.optionsList.set(JSON.parse(cat.CustomAttributeTypeOptionsSchema));
                } catch {
                    this.optionsList.set([]);
                }
            }

            // Restrict data type changes in edit mode (only String ↔ PickFromList/MultiSelect)
            const allowedTypes = [CustomAttributeDataTypeEnum.String, CustomAttributeDataTypeEnum.PickFromList, CustomAttributeDataTypeEnum.MultiSelect];
            if (!allowedTypes.includes(cat.CustomAttributeDataTypeID)) {
                this.formGroup.controls.CustomAttributeDataTypeID.disable();
            } else {
                this.dataTypeOptions = CustomAttributeDataTypesAsSelectDropdownOptions.filter((o) =>
                    allowedTypes.includes(o.Value as number)
                );
            }

            // Modeling attributes: restrict to description-only editing
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
    }

    get showOptionsEditor(): boolean {
        const dataTypeID = this.formGroup.controls.CustomAttributeDataTypeID.value;
        return dataTypeID === CustomAttributeDataTypeEnum.PickFromList || dataTypeID === CustomAttributeDataTypeEnum.MultiSelect;
    }

    addOption(): void {
        const text = this.newOptionText().trim();
        if (text && !this.optionsList().includes(text)) {
            this.optionsList.update((list) => [...list, text]);
            this.newOptionText.set("");
        }
    }

    removeOption(index: number): void {
        this.optionsList.update((list) => list.filter((_, i) => i !== index));
    }

    save(): void {
        if (this.formGroup.invalid) return;
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

        const save$ = this.isEdit
            ? this.customAttributeTypeService.updateCustomAttributeType(this.ref.data.customAttributeType.CustomAttributeTypeID, dto)
            : this.customAttributeTypeService.createCustomAttributeType(dto);

        save$.subscribe({
            next: () => this.ref.close(true),
            error: () => {},
        });
    }

    cancel(): void {
        this.ref.close(null);
    }
}
