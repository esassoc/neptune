import { Component, Input, OnChanges, OnInit, SimpleChanges } from "@angular/core";

import { CustomAttributeTypePurposeEnum, CustomAttributeTypePurposes } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { CustomAttributeDto, TreatmentBMPTypeCustomAttributeTypeDto } from "../../generated/model/models";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { FormFieldComponent, FormFieldType, SelectDropdownOption } from "../forms/form-field/form-field.component";

@Component({
    selector: "custom-attributes-editor",
    standalone: true,
    imports: [FormFieldComponent, ReactiveFormsModule],
    templateUrl: "./custom-attributes-editor.component.html",
    styleUrls: ["./custom-attributes-editor.component.scss"],
})
export class CustomAttributesEditorComponent implements OnInit, OnChanges {
    @Input() isAdmin = false;

    @Input() customAttributeTypePurposeEnum: CustomAttributeTypePurposeEnum = CustomAttributeTypePurposeEnum.OtherDesignAttributes;
    @Input() treatmentBMPTypeCustomAttributeTypes: TreatmentBMPTypeCustomAttributeTypeDto[] = [];
    @Input() customAttributes: CustomAttributeDto[] = [];

    @Input() formGroup: FormGroup<any>;

    public customAttributeTypePurposeDisplayName: string;
    public hasAttributes: boolean = false;
    public filteredAttributeTypes: TreatmentBMPTypeCustomAttributeTypeDto[] = [];

    public attributeValuesByTypeIDFormControls: { [key: number]: FormControl } = {};
    public attributeFormTypeByTypeID: { [key: number]: FormFieldType } = {};
    public selectOptionsByTypeID: { [key: number]: SelectDropdownOption[] } = {};

    ngOnInit(): void {}

    ngOnChanges(changes: SimpleChanges) {
        if (changes.customAttributeTypePurposeEnum?.currentValue) {
            const entry = CustomAttributeTypePurposes.find((x) => x.Value == this.customAttributeTypePurposeEnum);
            this.customAttributeTypePurposeDisplayName = entry ? entry.DisplayName : String(this.customAttributeTypePurposeEnum);
        }

        if (changes.treatmentBMPTypeCustomAttributeTypes?.currentValue) {
            this.hasAttributes = this.treatmentBMPTypeCustomAttributeTypes.some((x) => {
                const val = x.CustomAttributeType.CustomAttributeTypePurposeID;
                return Number(val) == this.customAttributeTypePurposeEnum;
            });

            this.filteredAttributeTypes = this.treatmentBMPTypeCustomAttributeTypes
                .filter((x) => {
                    const val = x.CustomAttributeType.CustomAttributeTypePurposeID;
                    return Number(val) == this.customAttributeTypePurposeEnum;
                })
                .sort((a, b) => a.SortOrder - b.SortOrder || a.CustomAttributeType.CustomAttributeTypeName.localeCompare(b.CustomAttributeType.CustomAttributeTypeName));

            this.filteredAttributeTypes.forEach((attrType) => {
                const controlName = `CustomAttributeType_${attrType.CustomAttributeTypeID}`;
                if (!this.formGroup.contains(controlName)) {
                    if (attrType.CustomAttributeType.IsRequired) {
                        let formControl = new FormControl(null, [Validators.required]);
                        this.formGroup.setControl(controlName, formControl);
                        this.attributeValuesByTypeIDFormControls[attrType.CustomAttributeTypeID] = formControl;
                    } else {
                        let formControl = new FormControl(null, []);
                        this.formGroup.setControl(controlName, formControl);
                        this.attributeValuesByTypeIDFormControls[attrType.CustomAttributeTypeID] = formControl;
                    }

                    switch (attrType.CustomAttributeType?.DataTypeName) {
                        case "String": {
                            this.attributeFormTypeByTypeID[attrType.CustomAttributeTypeID] = FormFieldType.Text;
                            break;
                        }
                        case "Integer":
                        case "Decimal": {
                            this.attributeFormTypeByTypeID[attrType.CustomAttributeTypeID] = FormFieldType.Number;
                            break;
                        }
                        case "DateTime": {
                            this.attributeFormTypeByTypeID[attrType.CustomAttributeTypeID] = FormFieldType.Date;
                            break;
                        }
                        case "PickFromList":
                        case "MultiSelect": {
                            this.attributeFormTypeByTypeID[attrType.CustomAttributeTypeID] = FormFieldType.Select;
                            // populate select options
                            var optionsAsListOfStrings = JSON.parse(attrType.CustomAttributeType!.CustomAttributeTypeOptionsSchema || "[]") as string[];
                            this.selectOptionsByTypeID[attrType.CustomAttributeTypeID] = optionsAsListOfStrings.map((option) => {
                                return { Label: option, Value: option, disabled: false } as SelectDropdownOption;
                            });

                            break;
                        }

                        default:
                            break;
                    }
                }
            });
        }

        if (changes.customAttributes?.currentValue) {
            for (const attr of this.customAttributes) {
                let formControl = this.attributeValuesByTypeIDFormControls[attr.CustomAttributeTypeID];
                if (formControl) {
                    let customAttributeType = this.filteredAttributeTypes.find((x) => x.CustomAttributeTypeID == attr.CustomAttributeTypeID)?.CustomAttributeType;
                    if (customAttributeType?.DataTypeName == "MultiSelect") {
                        formControl.setValue(attr.CustomAttributeValues);
                    } else {
                        formControl.setValue(attr.CustomAttributeValues?.length > 0 ? attr.CustomAttributeValues[0] : null);
                    }
                }
            }
        }
    }
}
