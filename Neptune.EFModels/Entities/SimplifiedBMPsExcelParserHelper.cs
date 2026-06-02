using System.Data;
using ClosedXML.Excel;
using Neptune.Common;

namespace Neptune.EFModels.Entities;

public static class SimplifiedBMPsExcelParserHelper
{
    public static List<QuickBMP> ParseWQMPRowsFromXLSX(NeptuneDbContext dbContext, int stormwaterJurisdictionID, 
        DataTable dataTableFromExcel,
        out List<string> errors)
    {
        errors = [];
        var requiredFields = new List<string> { "WQMP Name", "BMP Name", "BMP Type", "Count of BMPs" };
        foreach (var field in requiredFields)
        {
            if (!dataTableFromExcel.Columns.Contains(field))
            {
                errors.Add($"Spreadsheet is missing required column: {field}");
            }
        }

        if (errors.Count > 0)
        {
            return null;
        }
        var numColumns = dataTableFromExcel.Columns.Count;
        var numRows = dataTableFromExcel.Rows.Count;

        // NPT-1073: a pre-pass that re-emitted "WQMP does not exist" lived here. It duplicated
        // every error the per-row parser already produces (with row-number context), so users saw
        // each bad WQMP twice. Removed in favor of the single per-row check.

        // NPT-1073: hoist these out of the per-row loop. `quickBMPNamesInCsv` was being reset
        // every row, so the in-CSV duplicate-name detection never fired and rows slipped through
        // to SaveChanges as a UNIQUE constraint 500. `treatmentBMPTypes` was being re-fetched
        // from the DB on every row.
        var treatmentBMPTypes = TreatmentBMPTypes.List(dbContext);
        var quickBMPNamesInCsv = new List<string>();

        var quickBMPs = new List<QuickBMP>();
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
            quickBMPs.Add(ParseRequiredAndOptionalFieldsAndCreateSimplifiedBMPs(dbContext, row, i+2, out var errorsList,
                treatmentBMPTypes, quickBMPNamesInCsv, stormwaterJurisdictionID));
            errors.AddRange(errorsList);

        }

        if (errors.Count > 0)
        {
            return null;
        }

        return quickBMPs;
    }

    private static QuickBMP ParseRequiredAndOptionalFieldsAndCreateSimplifiedBMPs(NeptuneDbContext dbContext, DataRow row, int rowNumber, out List<string> errorList, List<TreatmentBMPType> treatmentBMPTypes, List<string> quickBMPNamesInCsv, int stormwaterJurisdictionID)
    {
        errorList = new List<string>();

        var wqmpName = ExcelHelper.SetStringValue(row, rowNumber, errorList, "WQMP Name", WaterQualityManagementPlan.FieldLengths.WaterQualityManagementPlanName, true);

        if (string.IsNullOrWhiteSpace(wqmpName))
        {
            // no point in going further if there is no wqmp name
            return null;
        }

        var wqmp = dbContext.WaterQualityManagementPlans.SingleOrDefault(x =>
            x.WaterQualityManagementPlanName == wqmpName && x.StormwaterJurisdictionID == stormwaterJurisdictionID);

        if (wqmp == null)
        {
            errorList.Add($"The WQMP with name '{wqmpName}' in row {rowNumber} was not found.");
            return null;
        }

        var bmpName = ExcelHelper.SetStringValue(row, rowNumber, errorList, "BMP Name",
            QuickBMP.FieldLengths.QuickBMPName, true);

        var quickBMP = dbContext.QuickBMPs.SingleOrDefault(x =>
            x.WaterQualityManagementPlanID == wqmp.WaterQualityManagementPlanID && x.QuickBMPName == bmpName) ?? new QuickBMP() {WaterQualityManagementPlanID = wqmp.WaterQualityManagementPlanID};

        if (!string.IsNullOrWhiteSpace(bmpName))
        {
            if (quickBMPNamesInCsv.Contains(bmpName))
            {
                errorList.Add(
                    $"The Simplified BMP with Name '{bmpName}' was already added in this upload, duplicate name is found at row: {rowNumber}");
            }
            quickBMPNamesInCsv.Add(bmpName);
            quickBMP.QuickBMPName = bmpName;
        }

        var treatmentBMPTypeName = row["BMP Type"].ToString();
        if (!string.IsNullOrWhiteSpace(treatmentBMPTypeName))
        {
            var treatmentBMPType = treatmentBMPTypes.SingleOrDefault(x => x.TreatmentBMPTypeName == treatmentBMPTypeName);
            if (treatmentBMPType == null)
            {
                errorList.Add($"BMP Type {treatmentBMPTypeName} does not exist");
            }
            else
            {
                quickBMP.TreatmentBMPTypeID = treatmentBMPType.TreatmentBMPTypeID;
            }
        }
        else
        {
            errorList.Add($"BMP Type in row {rowNumber} is empty or null");
        }

        var countOfBMPs = ExcelHelper.GetIntFieldValue(row, rowNumber, errorList, "Count of BMPs", true);
        if (countOfBMPs.HasValue)
        {
            // NPT-1073: reject 0 (and negatives) at parse time — they previously slipped through
            // as a valid count.
            if (countOfBMPs.Value < 1)
            {
                errorList.Add($"Count of BMPs '{countOfBMPs.Value}' must be at least 1 at row: {rowNumber}");
            }
            else
            {
                quickBMP.NumberOfIndividualBMPs = countOfBMPs.Value;
            }
        }

        var percentageOfSiteTreated = ExcelHelper.GetDecimalFieldValue(row, rowNumber, errorList, "% of Site Treated", false);
        if (percentageOfSiteTreated.HasValue)
        {
            quickBMP.PercentOfSiteTreated = percentageOfSiteTreated;
        }

        var wetWeatherPercentageCapture = ExcelHelper.GetDecimalFieldValue(row, rowNumber, errorList, "Wet Weather % Capture", false);
        if (wetWeatherPercentageCapture.HasValue)
        {
            quickBMP.PercentCaptured = wetWeatherPercentageCapture;
        }

        var wetWeatherPercentageRetained = ExcelHelper.GetDecimalFieldValue(row, rowNumber, errorList, "Wet Weather % Retained", false);
        if (wetWeatherPercentageRetained.HasValue)
        {
            quickBMP.PercentRetained = wetWeatherPercentageRetained;
        }

        var dryWeatherFlowOverrideName = row["Dry Weather Flow Override?"].ToString();
        if (!string.IsNullOrWhiteSpace(dryWeatherFlowOverrideName))
        {
            var dryWeatherFlowOverride = DryWeatherFlowOverride.All.SingleOrDefault(x => x.DryWeatherFlowOverrideDisplayName == dryWeatherFlowOverrideName);
            if (dryWeatherFlowOverride == null)
            {
                // NPT-1073: was mis-labelled "BMP Type" (copy-paste from the BMP Type branch above).
                errorList.Add($"Dry Weather Flow Override '{dryWeatherFlowOverrideName}' does not exist in row number: {rowNumber}");
            }
            else
            {
                quickBMP.DryWeatherFlowOverrideID = dryWeatherFlowOverride.DryWeatherFlowOverrideID;
            }
        }

        var notes = ExcelHelper.SetStringValue(row, rowNumber, errorList, "Notes", QuickBMP.FieldLengths.QuickBMPNote, false);
        if (notes != null)
        {
            quickBMP.QuickBMPNote = notes;
        }

        return quickBMP;
    }

    

}