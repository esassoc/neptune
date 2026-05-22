import { Component, Input, OnChanges, SimpleChanges } from "@angular/core";

import { CustomAttributeTypePurposeEnum, CustomAttributeTypePurposes } from "src/app/shared/generated/enum/custom-attribute-type-purpose-enum";
import { RouterLink } from "@angular/router";
import { CustomAttributeDto, TreatmentBMPTypeCustomAttributeTypeDto } from "../../generated/model/models";
import { PopperDirective } from "src/app/shared/directives/popper.directive";

@Component({
    selector: "custom-attributes-display",
    standalone: true,
    imports: [RouterLink, PopperDirective],
    templateUrl: "./custom-attributes-display.component.html",
    styleUrls: ["./custom-attributes-display.component.scss"],
})
export class CustomAttributesDisplayComponent implements OnChanges {
    @Input() isAdmin = false;

    @Input() customAttributeTypePurposeEnum: CustomAttributeTypePurposeEnum = CustomAttributeTypePurposeEnum.OtherDesignAttributes;
    @Input() treatmentBMPTypeCustomAttributeTypes: TreatmentBMPTypeCustomAttributeTypeDto[] = [];
    @Input() customAttributes: CustomAttributeDto[] = [];

    public customAttributeTypePurposeDisplayName: string;
    public hasAttributes: boolean = false;
    public filteredAttributeTypes: TreatmentBMPTypeCustomAttributeTypeDto[] = [];

    public attributeValuesByTypeID: { [key: number]: string };

    ngOnChanges(changes: SimpleChanges) {
        if (changes.customAttributeTypePurposeEnum?.currentValue) {
            const entry = CustomAttributeTypePurposes.find((x) => x.Value === this.customAttributeTypePurposeEnum);
            this.customAttributeTypePurposeDisplayName = entry ? entry.DisplayName : String(this.customAttributeTypePurposeEnum);
        }

        if (changes.treatmentBMPTypeCustomAttributeTypes?.currentValue) {
            this.hasAttributes = this.treatmentBMPTypeCustomAttributeTypes.some((x) => {
                const val = x.CustomAttributeType.CustomAttributeTypePurposeID;
                return Number(val) === this.customAttributeTypePurposeEnum;
            });

            this.filteredAttributeTypes = this.treatmentBMPTypeCustomAttributeTypes
                .filter((x) => {
                    const val = x.CustomAttributeType.CustomAttributeTypePurposeID;
                    return Number(val) === this.customAttributeTypePurposeEnum;
                })
                .sort((a, b) => a.SortOrder - b.SortOrder || a.CustomAttributeType.CustomAttributeTypeName.localeCompare(b.CustomAttributeType.CustomAttributeTypeName));
        }

        if (changes.customAttributes?.currentValue) {
            this.attributeValuesByTypeID = {};
            for (const attr of this.customAttributes) {
                this.attributeValuesByTypeID[attr.CustomAttributeTypeID] = attr.CustomAttributeValueWithUnits;
            }
        }
    }
}
