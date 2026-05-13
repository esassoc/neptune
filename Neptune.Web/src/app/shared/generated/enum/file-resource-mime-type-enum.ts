//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Source Table: [dbo].[FileResourceMimeType]

import { LookupTableEntry } from "src/app/shared/models/lookup-table-entry";
import { SelectDropdownOption } from "src/app/shared/components/forms/form-field/form-field.component"

export enum FileResourceMimeTypeEnum {
  PDF = 1,
  WordDOCX = 2,
  ExcelXLSX = 3,
  XPNG = 4,
  PNG = 5,
  TIFF = 6,
  BMP = 7,
  GIF = 8,
  JPEG = 9,
  PJPEG = 10,
  PowerpointPPTX = 11,
  PowerpointPPT = 12,
  ExcelXLS = 13,
  WordDOC = 14,
  xExcelXLSX = 15,
  CSS = 16,
  ZIP = 17,
  CSV = 18,
  Text = 19
}

export const FileResourceMimeTypes: LookupTableEntry[]  = [
  { Name: "PDF", DisplayName: "PDF", Value: 1 },
  { Name: "Word (DOCX)", DisplayName: "Word (DOCX)", Value: 2 },
  { Name: "Excel (XLSX)", DisplayName: "Excel (XLSX)", Value: 3 },
  { Name: "X-PNG", DisplayName: "X-PNG", Value: 4 },
  { Name: "PNG", DisplayName: "PNG", Value: 5 },
  { Name: "TIFF", DisplayName: "TIFF", Value: 6 },
  { Name: "BMP", DisplayName: "BMP", Value: 7 },
  { Name: "GIF", DisplayName: "GIF", Value: 8 },
  { Name: "JPEG", DisplayName: "JPEG", Value: 9 },
  { Name: "PJPEG", DisplayName: "PJPEG", Value: 10 },
  { Name: "Powerpoint (PPTX)", DisplayName: "Powerpoint (PPTX)", Value: 11 },
  { Name: "Powerpoint (PPT)", DisplayName: "Powerpoint (PPT)", Value: 12 },
  { Name: "Excel (XLS)", DisplayName: "Excel (XLS)", Value: 13 },
  { Name: "Word (DOC)", DisplayName: "Word (DOC)", Value: 14 },
  { Name: "x-Excel (XLSX)", DisplayName: "x-Excel (XLSX)", Value: 15 },
  { Name: "CSS", DisplayName: "CSS", Value: 16 },
  { Name: "ZIP", DisplayName: "ZIP", Value: 17 },
  { Name: "CSV", DisplayName: "CSV", Value: 18 },
  { Name: "Text", DisplayName: "Text", Value: 19 }
];
export const FileResourceMimeTypesAsSelectDropdownOptions = FileResourceMimeTypes.map((x) => ({ Value: x.Value, Label: x.DisplayName } as SelectDropdownOption));
