using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;
using Neptune.EFModels.Entities;
using Neptune.Models.DataTransferObjects;

namespace Neptune.WebMvc.Common
{
    public static class WQMPCsvParserHelper
    {
        public static List<WaterQualityManagementPlan> CSVUpload(NeptuneDbContext dbContext, Stream fileStream,
            out List<string> errorList)
        {
            var streamReader = new StreamReader(fileStream);
            var parser = new TextFieldParser(streamReader);
            return ParseWqmpRowsFromCsv(dbContext, parser, out errorList);
        }

        public static List<WaterQualityManagementPlan> ParseWqmpRowsFromCsv(NeptuneDbContext dbContext, TextFieldParser parser, out List<string> errorList)
        {
            parser.SetDelimiters(",");
            errorList = new List<string>();
            var wqmpsToUpload = new List<WaterQualityManagementPlan>();
            var fieldsDict = new Dictionary<string, int>();

            var requiredFields = new List<string> { "WQMP Name", "Jurisdiction", "Land Use", "Priority", "Status", "Development Type", "Trash Capture Status" };
            var optionalFields = new List<string> { "Maintenance Contact Name", "Maintenance Contact Organization", "Maintenance Contact Phone",
                "Maintenance Contact Address 1", "Maintenance Contact Address 2", "Maintenance Contact City", "Maintenance Contact State", "Maintenance Contact Zip",
                "Permit Term", "Hydromodification Controls Apply", "Approval Date", "Date of Construction", "Hydrologic Subarea", "Record Number", "Recorded WQMP Area (Acres)",
                "Trash Capture Effectiveness", "Modeling Approach"
            };

            try
            {
                var header = parser.ReadFields();
                fieldsDict = ValidateHeader(header, requiredFields, optionalFields, out errorList);
                if (errorList.Any())
                {
                    return null;
                }
            }
            catch
            {
                errorList.Add("Unable to read file and/or parse header");
            }

            // if the fields don't match throw an exception
            var rowCount = 1;
            var stormwaterJurisdictions = StormwaterJurisdictions.List(dbContext).ToList();
            var wqmpNamesInCsv = new List<string>();
            var hydrologicSubareas = dbContext.HydrologicSubareas.AsNoTracking().ToList();
            while (!parser.EndOfData)
            {
                var currentRow = parser.ReadFields();

                var currentWQMP = ParseRequiredAndOptionalFieldsAndCreateWQMP(dbContext, currentRow, fieldsDict, rowCount, out var currentErrorList, stormwaterJurisdictions, wqmpNamesInCsv, hydrologicSubareas);
                if (currentWQMP != null)
                {
                    wqmpsToUpload.Add(currentWQMP);
                }

                errorList.AddRange(currentErrorList);
                rowCount++;
            }

            return wqmpsToUpload;
        }

        private static WaterQualityManagementPlan ParseRequiredAndOptionalFieldsAndCreateWQMP(NeptuneDbContext dbContext, string[] row,
            Dictionary<string, int> fieldsDict, int rowNumber, out List<string> errorList,
            List<StormwaterJurisdiction> stormwaterJurisdictions, List<string> wqmpNamesInCsv, List<HydrologicSubarea> hydrologicSubareas)
        {
            errorList = new List<string>();

            var wqmpName = SetStringValue(row, fieldsDict, rowNumber, errorList, "WQMP Name", WaterQualityManagementPlan.FieldLengths.WaterQualityManagementPlanName, true);
            var stormwaterJurisdictionID = FindLookupValue(row, fieldsDict, "Jurisdiction", rowNumber, errorList, stormwaterJurisdictions, x => x.Organization.OrganizationName, x => x.StormwaterJurisdictionID, false, true);

            if (!stormwaterJurisdictionID.HasValue || string.IsNullOrWhiteSpace(wqmpName))
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
                x.StormwaterJurisdictionID == stormwaterJurisdictionID.Value);

            if (wqmp == null)
            {
                wqmp = new WaterQualityManagementPlan
                {
                    StormwaterJurisdictionID = stormwaterJurisdictionID.Value,
                    WaterQualityManagementPlanName = wqmpName,
                    // KE 5/13/26 decision: default new WQMPs to Simplified modeling approach.
                    WaterQualityManagementPlanModelingApproachID = (int)WaterQualityManagementPlanModelingApproachEnum.Simplified
                };
            }

            var landUseID = FindLookupValue(row, fieldsDict, "Land Use", rowNumber, errorList, WaterQualityManagementPlanLandUse.All,
                x => x.WaterQualityManagementPlanLandUseDisplayName, x => x.WaterQualityManagementPlanLandUseID, true, true);
            if (landUseID.HasValue)
            {
                wqmp.WaterQualityManagementPlanLandUseID = landUseID.Value;
            }

            var priorityID = FindLookupValue(row, fieldsDict, "Priority", rowNumber, errorList, WaterQualityManagementPlanPriority.All,
                x => x.WaterQualityManagementPlanPriorityDisplayName, x => x.WaterQualityManagementPlanPriorityID, true, true);
            if (priorityID.HasValue)
            {
                wqmp.WaterQualityManagementPlanPriorityID = priorityID.Value;
            }

            var statusID = FindLookupValue(row, fieldsDict, "Status", rowNumber, errorList, WaterQualityManagementPlanStatus.All,
                x => x.WaterQualityManagementPlanStatusDisplayName, x => x.WaterQualityManagementPlanStatusID, true, true);
            if (statusID.HasValue)
            {
                wqmp.WaterQualityManagementPlanStatusID = statusID.Value;
            }

            var developmentTypeID = FindLookupValue(row, fieldsDict, "Development Type", rowNumber, errorList, WaterQualityManagementPlanDevelopmentType.All,
                x => x.WaterQualityManagementPlanDevelopmentTypeDisplayName, x => x.WaterQualityManagementPlanDevelopmentTypeID, true, true);
            if (developmentTypeID.HasValue)
            {
                wqmp.WaterQualityManagementPlanDevelopmentTypeID = developmentTypeID.Value;
            }

            var trashCaptureStatusTypeID = FindLookupValue(row, fieldsDict, "Trash Capture Status", rowNumber,
                errorList, TrashCaptureStatusType.All, x => x.TrashCaptureStatusTypeDisplayName,
                x => x.TrashCaptureStatusTypeID, true, true);
            if (trashCaptureStatusTypeID.HasValue)
            {
                wqmp.TrashCaptureStatusTypeID = trashCaptureStatusTypeID.Value;
            }

            //start of Optional Fields

            var maintenanceContactName = SetStringValue(row, fieldsDict, rowNumber, errorList, "Maintenance Contact Name", 
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactName, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactName))
            {
                wqmp.MaintenanceContactName = maintenanceContactName;
            }

            var maintenanceContactOrganization = SetStringValue(row, fieldsDict, rowNumber, errorList, "Maintenance Contact Organization",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactOrganization, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactOrganization))
            {
                wqmp.MaintenanceContactOrganization = maintenanceContactOrganization;
            }

            var maintenanceContactPhone = SetStringValue(row, fieldsDict, rowNumber, errorList, "Maintenance Contact Phone",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactPhone, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactPhone))
            {
                wqmp.MaintenanceContactPhone = maintenanceContactPhone;
            }

            var maintenanceContactAddress1 = SetStringValue(row, fieldsDict, rowNumber, errorList, "Maintenance Contact Address 1",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactAddress1, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactAddress1))
            {
                wqmp.MaintenanceContactAddress1 = maintenanceContactAddress1;
            }

            var maintenanceContactAddress2 = SetStringValue(row, fieldsDict, rowNumber, errorList, "Maintenance Contact Address 2",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactAddress2, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactAddress2))
            {
                wqmp.MaintenanceContactAddress2 = maintenanceContactAddress2;
            }

            var maintenanceContactCity = SetStringValue(row, fieldsDict, rowNumber, errorList, "Maintenance Contact City",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactCity, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactCity))
            {
                wqmp.MaintenanceContactCity = maintenanceContactCity;
            }
            
            var maintenanceContactState = SetStringValue(row, fieldsDict, rowNumber, errorList, "Maintenance Contact State",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactState, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactState))
            {
                wqmp.MaintenanceContactState = maintenanceContactState;
            }

            var maintenanceContactZip = SetStringValue(row, fieldsDict, rowNumber, errorList, "Maintenance Contact Zip",
                WaterQualityManagementPlan.FieldLengths.MaintenanceContactZip, false);
            if (!string.IsNullOrWhiteSpace(maintenanceContactZip))
            {
                wqmp.MaintenanceContactZip = maintenanceContactZip;
            }

            var permitTermID = FindLookupValue(row, fieldsDict, "Permit Term", rowNumber,
                errorList, WaterQualityManagementPlanPermitTerm.All, x => x.WaterQualityManagementPlanPermitTermDisplayName,
                x => x.WaterQualityManagementPlanPermitTermID, true, false);
            if (permitTermID.HasValue)
            {
                wqmp.WaterQualityManagementPlanPermitTermID = permitTermID.Value;
            }

            var hydromodificationAppliesTypeID = FindLookupValue(row, fieldsDict, "Hydromodification Controls Apply", rowNumber,
                errorList, HydromodificationAppliesType.All, x => x.HydromodificationAppliesTypeDisplayName,
                x => x.HydromodificationAppliesTypeID, true, false);
            if (hydromodificationAppliesTypeID.HasValue)
            {
                wqmp.HydromodificationAppliesTypeID = hydromodificationAppliesTypeID.Value;
            }

            var approvalDateString = SetStringValue(row, fieldsDict, rowNumber, errorList, "Approval Date", 100, false);
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

            var hydrologicSubareaID = FindLookupValue(row, fieldsDict, "Hydrologic Subarea", rowNumber,
                errorList, hydrologicSubareas, x => x.HydrologicSubareaName,
                x => x.HydrologicSubareaID, true, false);
            if (hydrologicSubareaID.HasValue)
            {
                wqmp.HydrologicSubareaID = hydrologicSubareaID.Value;
            }

            var recordNumber = SetStringValue(row, fieldsDict, rowNumber, errorList, "Record Number",
                WaterQualityManagementPlan.FieldLengths.RecordNumber, false);
            if (!string.IsNullOrWhiteSpace(recordNumber))
            {
                wqmp.RecordNumber = recordNumber;
            }

            var recordedWqmpArea = GetOptionalDecimalFieldValue(row, fieldsDict, rowNumber, errorList, "Recorded WQMP Area (Acres)");
            if (recordedWqmpArea.HasValue)
            {
                wqmp.RecordedWQMPAreaInAcres = recordedWqmpArea;
            }

            var trashCaptureEffectiveness = GetOptionalIntFieldValue(row, fieldsDict, rowNumber, errorList, "Trash Capture Effectiveness");
            if (trashCaptureEffectiveness.HasValue)
            {
                wqmp.TrashCaptureEffectiveness = trashCaptureEffectiveness;
            }

            var modelingApproachID = FindLookupValue(row, fieldsDict, "Modeling Approach", rowNumber,
                errorList, WaterQualityManagementPlanModelingApproach.All, x => x.WaterQualityManagementPlanModelingApproachDisplayName,
                x => x.WaterQualityManagementPlanModelingApproachID, true, false);
            if (modelingApproachID.HasValue)
            {
                wqmp.WaterQualityManagementPlanModelingApproachID = modelingApproachID.Value;
            }

            var constructionDateString = SetStringValue(row, fieldsDict, rowNumber, errorList, "Date of Construction", 100, false);
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

        private static string SetStringValue(string[] row, Dictionary<string, int> fieldsDict, int rowNumber, List<string> errorList, string fieldName,
            int fieldLength, bool requireNotEmpty)
        {
            if (fieldsDict.ContainsKey(fieldName))
            {
                var fieldValue = row[fieldsDict[fieldName]];
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    if (fieldValue.Length > fieldLength)
                    {
                        errorList.Add($"{fieldName} is too long at row: {rowNumber}. It must be {fieldLength} characters or less. Current Length is {fieldValue.Length}.");
                    }
                    else
                    {
                        return fieldValue;
                    }
                }
                else
                {
                    if (requireNotEmpty)
                    {
                        errorList.Add($"{fieldName} is null, empty, or just whitespaces for row: {rowNumber}");
                    }
                }
            }
            return null;
        }

        private static int? FindLookupValue<T>(string[] row, Dictionary<string, int> fieldsDict, string fieldName, int rowNumber, List<string> errorList,
            List<T> lookupValues, Func<T, string> funcDisplayName, Func<T, int> funcID, bool showAvailableValuesInErrorMessage, bool requireNotEmpty)
        {
            if (fieldsDict.ContainsKey(fieldName))
            {
                var fieldValue = row[fieldsDict[fieldName]];
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    if (!lookupValues.Select(funcDisplayName.Invoke).Contains(fieldValue))
                    {
                        var errorMessage = $"No {fieldName} with the name '{fieldValue}' exists in our records, row: {rowNumber}.";
                        if (showAvailableValuesInErrorMessage)
                        {
                            errorMessage += $" Acceptable Values Are: {string.Join(", ", lookupValues.Select(funcDisplayName.Invoke))}";
                        }
                        errorList.Add(errorMessage);
                    }
                    else
                    {
                        var entity = lookupValues.Single(x => funcDisplayName.Invoke(x) == fieldValue);
                        return funcID.Invoke(entity);
                    }
                }
                else
                {
                    if (requireNotEmpty)
                    {
                        errorList.Add($"{fieldName} is null, empty, or just whitespaces for row: {rowNumber}");
                    }
                }
            }

            return null;
        }
        
        private static decimal? GetOptionalDecimalFieldValue(string[] row, Dictionary<string, int> fieldsDict, int rowNumber, List<string> errorList, string fieldName)
        {
            if (fieldsDict.ContainsKey(fieldName))
            {
                var fieldValue = row[fieldsDict[fieldName]];
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    if (!decimal.TryParse(fieldValue, out var fieldValueAsInt))
                    {
                        errorList.Add($"{fieldName} can not be converted to decimal at row: {rowNumber}");
                    }
                    else
                    {
                        return fieldValueAsInt;
                    }
                }
            }

            return null;
        }

        private static int? GetOptionalIntFieldValue(string[] row, Dictionary<string, int> fieldsDict, int rowNumber, List<string> errorList, string fieldName)
        {
            if (fieldsDict.ContainsKey(fieldName))
            {
                var fieldValue = row[fieldsDict[fieldName]];
                if (!string.IsNullOrWhiteSpace(fieldValue))
                {
                    if (!int.TryParse(fieldValue, out var fieldValueAsInt))
                    {
                        errorList.Add($"{fieldName} can not be converted to Int at row: {rowNumber}");
                    }
                    else
                    {
                        return fieldValueAsInt;
                    }
                }
            }

            return null;
        }

        private static Dictionary<string, int> ValidateHeader(string[] row, List<string> requiredFields, List<string> optionalFields, out List<string> errorList)
        {
            errorList = new List<string>();
            var fieldsDict = new Dictionary<string, int>();

            for (var fieldIndex = 0; fieldIndex < row.Length; fieldIndex++)
            {
                var temp = row[fieldIndex].Trim();
                if (!string.IsNullOrWhiteSpace(temp))
                {
                    fieldsDict.Add(temp, fieldIndex);
                }
            }

            var headers = fieldsDict.Keys.ToList();
            var requiredFieldDifference = requiredFields.Except(headers).ToList();
            var optionalFieldDifference = headers.Except(requiredFields).Except(optionalFields).ToList();

            if (requiredFieldDifference.Any())
            {
                errorList.Add("One or more required headers have not been provided. Required Fields are: " +
                              string.Join(", ", requiredFieldDifference));
            }

            if (optionalFieldDifference.Any())
            {
                errorList.Add($"The provided fields '{string.Join(", ", optionalFieldDifference)}' did not match a property of Water Quality Management Plan");
            }

            return fieldsDict;
        }

    }

}