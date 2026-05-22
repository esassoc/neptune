//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Source Table: [dbo].[OvtaAreaSourceType]

import { LookupTableEntry } from "src/app/shared/models/lookup-table-entry";
import { SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component"

export enum OvtaAreaSourceTypeEnum {
  Parcel = 1,
  LandUseBlock = 2
}

export const OvtaAreaSourceTypes: LookupTableEntry[]  = [
  { Name: "Parcel", DisplayName: "Parcels", Value: 1 },
  { Name: "LandUseBlock", DisplayName: "Land Use Blocks", Value: 2 }
];
export const OvtaAreaSourceTypesAsSelectDropdownOptions = OvtaAreaSourceTypes.map((x) => ({ Value: x.Value, Label: x.DisplayName } as SelectDropdownOption));
