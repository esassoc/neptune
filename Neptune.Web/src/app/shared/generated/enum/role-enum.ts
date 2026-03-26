//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Source Table: [dbo].[Role]

import { LookupTableEntry } from "src/app/shared/models/lookup-table-entry";
import { SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component"

export enum RoleEnum {
  Admin = 1,
  Unassigned = 3,
  SitkaAdmin = 4,
  JurisdictionManager = 5,
  JurisdictionEditor = 6
}

export const Roles: LookupTableEntry[]  = [
  { Name: "Admin", DisplayName: "Administrator", Value: 1 },
  { Name: "Unassigned", DisplayName: "Unassigned", Value: 3 },
  { Name: "SitkaAdmin", DisplayName: "Sitka Administrator", Value: 4 },
  { Name: "JurisdictionManager", DisplayName: "Jurisdiction Manager", Value: 5 },
  { Name: "JurisdictionEditor", DisplayName: "Jurisdiction Editor", Value: 6 }
];
export const RolesAsSelectDropdownOptions = Roles.map((x) => ({ Value: x.Value, Label: x.DisplayName } as SelectDropdownOption));
