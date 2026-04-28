//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Source Table: [dbo].[WaterQualityManagementPlanStatus]

import { LookupTableEntry } from "src/app/shared/models/lookup-table-entry";
import { SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component"

export enum WaterQualityManagementPlanStatusEnum {
  Active = 1,
  Inactive = 2,
  Draft = 3
}

export const WaterQualityManagementPlanStatuses: LookupTableEntry[]  = [
  { Name: "Active", DisplayName: "Active", Value: 1 },
  { Name: "Inactive", DisplayName: "Inactive", Value: 2 },
  { Name: "Draft", DisplayName: "Draft", Value: 3 }
];
export const WaterQualityManagementPlanStatusesAsSelectDropdownOptions = WaterQualityManagementPlanStatuses.map((x) => ({ Value: x.Value, Label: x.DisplayName } as SelectDropdownOption));
