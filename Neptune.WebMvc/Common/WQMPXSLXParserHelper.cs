using Microsoft.EntityFrameworkCore;
using Neptune.EFModels.Entities;
using System.Data;
using Neptune.Common;

namespace Neptune.WebMvc.Common
{
    public static class WQMPXSLXParserHelper
    {
        public static List<WaterQualityManagementPlan> ParseWQMPRowsFromXLSX(NeptuneDbContext dbContext,  DataTable dataTableFromExcel, int stormwaterJurisdictionID,
        out List<string> errors)
        {
            errors = [];
            var requiredFields = new List<string> { "WQMP Name", "Land Use", "Priority", "Status", "Development Type", "Trash Capture Status" };
            var optionalFields = new List<string> { "Maintenance Contact Name", "Maintenance Contact Organization", "Maintenance Contact Phone",
                "Maintenance Contact Address 1", "Maintenance Contact Address 2", "Maintenance Contact City", "Maintenance Contact State", "Maintenance Contact Zip",
                "Permit Term", "Hydromodification Controls Apply", "Approval Date", "Date of Construction", "Hydrologic Subarea", "Record Number", "Recorded WQMP Area (Acres)",
                "Trash Capture Effectiveness", "Modeling Approach"
            };
            errors.AddRange(from field in requiredFields where !dataTableFromExcel.Columns.Contains(field) select $"Spreadsheet is missing required column: {field}");

            if (errors.Count > 0)
            {
                return null;
            }
            var numColumns = dataTableFromExcel.Columns.Count;
            var numRows = dataTableFromExcel.Rows.Count;

            if (errors.Count > 0)
            {
                return null;
            }

            var wqmps = new List<WaterQualityManagementPlan>();
            var wqmpNamesInCsv = new List<string>();
            var hydrologicSubareas = dbContext.HydrologicSubareas.AsNoTracking().ToList();
            for (var i = 0; i < numRows; i++)
            {
                var row = dataTableFromExcel.Rows[i];
                var rowEmpty = true;
                for (var j = 0; j < numColumns; j++)
                {
                    rowEmpty = string.IsNullOrWhiteSpace(row[j].ToString());
                    if (!rowEmpty)
                    {
                        break;
                    }
                }

                if (rowEmpty)
                {
                    continue;
                }
                wqmps.Add(ParseRequiredAndOptionalFieldsAndCreateWQMP(dbContext, row, i+2, out var errorsList,
                    stormwaterJurisdictionID, wqmpNamesInCsv, hydrologicSubareas));
                errors.AddRange(errorsList);

            }

            return errors.Count > 0 ? [] : wqmps;
        }

        private static WaterQualityManagementPlan ParseRequiredAndOptionalFieldsAndCreateWQMP(NeptuneDbContext dbContext, DataRow row,
            int rowNumber, out List<string> errorList,
            int stormwaterJurisdictionID, List<string> wqmpNamesInCsv, List<HydrologicSubarea> hydrologicSubareas)
        {
            errorList = [];

            var wqmpName = ExcelHelper.SetStringValue(row, rowNumber, errorList, "WQMP Name", WaterQualityManagementPlan.FieldLengths.WaterQualityManagementPlanName, true);

            if (string.IsNullOrWhiteSpace(wqmpName))
            {
                // no point in going further if we don't have a name and jurisdiction
                return null;
            }

            if (!string.IsNullOrWhiteSpace(wqmpName))
            {
                if (wqmpNamesInCsv.Contains(wqmpName))
                {
                    errorList.Add(
                        $"The WQMP with Name '{wqmpName}' was already added in this upload, duplicate name is found at row: {rowNumber}");
                }
                wqmpNamesInCsv.Add(wqmpName);
            }

            var wqmp = dbContext.WaterQualityManagementPlans.SingleOrDefault(x =>
                x.WaterQualityManagementPlanName == wqmpName &&
                x.StormwaterJurisdictionID == stormwaterJurisdictionID) ?? new WaterQualityManagementPlan
            {
                StormwaterJurisdictionID = stormwaterJurisdictionID,
                WaterQualityManagementPlanName = wqmpName,
                // KE 5/13/26 decision: default new WQMPs to Simplified modeling approach.
                WaterQualityManagementPlanModelingApproachID = (int)WaterQualityManagementPlanModelingApproachEnum.Simplified
            };

            var landUseName = row["Land Use"].ToString();
            if (!string.IsNullOrWhiteSpace(landUseName))
            {
                var landUseID = WaterQualityManagementPlanLandUse.All.SingleOrDefault(x =>
                    x.WaterQualityManagementPlanLandUseDisplayName == landUseName)?.WaterQualityManagementPlanLandUseID;
                if (landUseID == null)
                {
                    errorList.Add($"Land Use {landUseName} does not exist in row number: {rowNumber}");
                }
                else
                {
                    wqmp.WaterQualityManagementPlanLandUseID = landUseID;
                }
            }
            else
            {
                errorList.Add($"Land Use in row {rowNumber} is empty or null");
            }

            var priority = row["Priority"].ToString();
            if (!string.IsNullOrWhiteSpace(priority))
            {
                var priorityID = WaterQualityManagementPlanPriority.All.SingleOrDefault(x =>
                    x.WaterQualityManagementPlanPriorityDisplayName == priority)?.WaterQualityManagementPlanPriorityID;
                if (priorityID == null)
                {
                    errorList.Add($"Priority {priority} does not exist in row number: {rowNumber}");
                }
                else
                {
                    wqmp.WaterQualityManagementPlanPriorityID = priorityID;
                }
            }
            else
            {
                errorList.Add($"Priority in row {rowNumber} is empty or null");
            }

            var status = row["Status"].ToString();
            if (!string.IsNullOrWhiteSpace(status))
            {
                var statusID = WaterQualityManagementPlanStatus.All.SingleOrDefault(x =>
                    x.WaterQualityManagementPlanStatusDisplayName == status)?.WaterQualityManagementPlanStatusID;
                if (statusID == null)
                {
                    errorList.Add($"Status {status} does not exist in row number: {rowNumber}");
                }
                else
                {
                    wqmp.WaterQualityManagementPlanStatusID = statusID;
                }
            }
            else
            {
                errorList.Add($"Status in row {rowNumber} is empty or null");
            }

            var developmentType = row["Development Type"].ToString();
            if (!string.IsNullOrWhiteSpace(developmentType))
            {
                var developmentTypeID = WaterQualityManagementPlanDevelopmentType.All.SingleOrDefault(x =>
                    x.WaterQualityManagementPlanDevelopmentTypeDisplayName == developmentType)?.WaterQualityManagementPlanDevelopmentTypeID;
                if (developmentTypeID == null)
                {
                    errorList.Add($"Development Type {developmentType} does not exist in row number: {rowNumber}");
                }
                else
                {
                    wqmp.WaterQualityManagementPlanDevelopmentTypeID = developmentTypeID;
                }
            }
            else
            {
                errorList.Add($"Development Type in row {rowNumber} is empty or null");
            }

            var trashCaptureStatus = row["Trash Capture Status"].ToString();
            if (!string.IsNullOrWhiteSpace(trashCaptureStatus))
            {
                var developmentTypeID = TrashCaptureStatusType.All.SingleOrDefault(x =>
                    x.TrashCaptureStatusTypeDisplayName == trashCaptureStatus)?.TrashCaptureStatusTypeID;
                if (developmentTypeID == null)
                {
                    errorList.Add($"Trash Capture Status Type {trashCaptureStatus} does not exist in row number: {rowNumber}");
                }
                else
                {
                    wqmp.TrashCaptureStatusTypeID = (int)developmentTypeID;
                }
            }
            else
            {
                errorList.Add($"Trash Capture Status Type in row {rowNumber} is empty or null");
            }

            //start of Optional Fields

            var maintenanceContactName = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Maintenance Contact Name", 
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactName, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactName))
            {
                wqmp.MaintenanceContactName = maintenanceContactName;
            }

            var maintenanceContactOrganization = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Maintenance Contact Organization",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactOrganization, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactOrganization))
            {
                wqmp.MaintenanceContactOrganization = maintenanceContactOrganization;
            }

            var maintenanceContactPhone = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Maintenance Contact Phone",
                 WaterQualityManagementPlan.FieldLengths.MaintenanceContactPhone, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactPhone))
            {
                wqmp.MaintenanceContactPhone = maintenanceContactPhone;
            }

            var maintenanceContactAddress1 = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Maintenance Contact Address 1",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactAddress1, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactAddress1))
            {
                wqmp.MaintenanceContactAddress1 = maintenanceContactAddress1;
            }

            var maintenanceContactAddress2 = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Maintenance Contact Address 2",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactAddress2, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactAddress2))
            {
                wqmp.MaintenanceContactAddress2 = maintenanceContactAddress2;
            }

            var maintenanceContactCity = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Maintenance Contact City",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactCity, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactCity))
            {
                wqmp.MaintenanceContactCity = maintenanceContactCity;
            }
            
            var maintenanceContactState = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Maintenance Contact State",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactState, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactState))
            {
                wqmp.MaintenanceContactState = maintenanceContactState;
            }

            var maintenanceContactZip = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Maintenance Contact Zip",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactZip, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactZip))
            {
                wqmp.MaintenanceContactZip = maintenanceContactZip;
            }


            var permitTermName = row["Permit Term"].ToString();
            if (!string.IsNullOrWhiteSpace(permitTermName))
            {
                var permitID = WaterQualityManagementPlanPermitTerm.All.SingleOrDefault(x =>
                    x.WaterQualityManagementPlanPermitTermDisplayName == permitTermName)?.WaterQualityManagementPlanPermitTermID;
                if (permitID == null)
                {
                    errorList.Add($"Permit term {permitTermName} does not exist in row number: {rowNumber}");
                }
                else
                {
                    wqmp.WaterQualityManagementPlanPermitTermID = (int)permitID;
                }
            }

            var hydromodificationAppliesType = row["Hydromodification Controls Apply"].ToString();
            if (!string.IsNullOrWhiteSpace(hydromodificationAppliesType))
            {
                var hydromodificationAppliesTypeID = HydromodificationAppliesType.All.SingleOrDefault(x =>
                    x.HydromodificationAppliesTypeDisplayName == hydromodificationAppliesType)?.HydromodificationAppliesTypeID;
                if (hydromodificationAppliesTypeID == null)
                {
                    errorList.Add($"Hydromodification Controls Apply {hydromodificationAppliesType} does not exist in row number: {rowNumber}");
                }
                else
                {
                    wqmp.HydromodificationAppliesTypeID = (int)hydromodificationAppliesTypeID;
                }
            }

            var approvalDateString = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Approval Date", 100, false);
            if (!string.IsNullOrWhiteSpace(approvalDateString))
            {
                if (!DateTime.TryParse(approvalDateString,
                        out var approvalDateParsed))
                {
                    errorList.Add(
                        $"{approvalDateParsed} can not be converted to Date Time format at row: {rowNumber}");
                }
                else
                {
                    wqmp.ApprovalDate = approvalDateParsed.ConvertTimeFromPSTToUTC();
                }
            }

            var hydrologicSubarea = row["Hydrologic Subarea"].ToString();
            if (!string.IsNullOrWhiteSpace(hydrologicSubarea))
            {
                var hydrologicSubareaID = hydrologicSubareas.SingleOrDefault(x =>
                    x.HydrologicSubareaName == hydrologicSubarea)?.HydrologicSubareaID;
                if (hydrologicSubareaID == null)
                {
                    errorList.Add($"Hydrologic Subarea {hydrologicSubarea} does not exist in row number: {rowNumber}");
                }
                else
                {
                    wqmp.HydrologicSubareaID = (int)hydrologicSubareaID;
                }
            }

            var recordNumber = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Record Number",
                WaterQualityManagementPlan.FieldLengths.RecordNumber, false);
            if (!string.IsNullOrWhiteSpace(recordNumber))
            {
                wqmp.RecordNumber = recordNumber;
            }

            var recordedWqmpArea = ExcelHelper.GetOptionalDecimalFieldValue(row, rowNumber, errorList, "Recorded WQMP Area (Acres)");
            if (recordedWqmpArea.HasValue)
            {
                wqmp.RecordedWQMPAreaInAcres = recordedWqmpArea;
            }

            var trashCaptureEffectiveness = ExcelHelper.GetOptionalIntFieldValue(row, rowNumber, errorList, "Trash Capture Effectiveness");
            if (trashCaptureEffectiveness.HasValue)
            {
                wqmp.TrashCaptureEffectiveness = trashCaptureEffectiveness;
            }

            var modelingApproach = row["Modeling Approach"].ToString();
            if (!string.IsNullOrWhiteSpace(modelingApproach))
            {
                var modelingApproachID = WaterQualityManagementPlanModelingApproach.All.SingleOrDefault(x =>
                    x.WaterQualityManagementPlanModelingApproachDisplayName == modelingApproach)?.WaterQualityManagementPlanModelingApproachID;
                if (modelingApproachID == null)
                {
                    errorList.Add($"Modeling approach {modelingApproach} does not exist in row number: {rowNumber}");
                }
                else
                {
                    wqmp.WaterQualityManagementPlanModelingApproachID = (int)modelingApproachID;
                }
            }

            var constructionDateString = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Date of Construction", 100, false);
            if (!string.IsNullOrWhiteSpace(constructionDateString))
            {
                if (!DateTime.TryParse(constructionDateString,
                        out var constructionDateParsed))
                {
                    errorList.Add(
                        $"{constructionDateParsed} can not be converted to Date Time format at row: {rowNumber}");
                }
                else
                {
                    wqmp.DateOfConstruction = constructionDateParsed.ConvertTimeFromPSTToUTC();
                }
            }

            //End of Optional Fields

            return wqmp;
        }
    }

}