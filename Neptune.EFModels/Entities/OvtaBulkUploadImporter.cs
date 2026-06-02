using System.Data;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

// Ported from Neptune.WebMvc.Controllers.OnlandVisualTrashAssessmentController.BulkUploadOTVAs so the
// SPA Data Hub can drive the same upload from the API. Behavior mirrors the legacy controller exactly
// until PR 5 retires the MVC entry point.
public static class OvtaBulkUploadImporter
{
    public static async Task<OvtaBulkUploadResultDto> BulkUploadAsync(NeptuneDbContext dbContext, Stream xlsxStream, Person currentPerson)
    {
        var result = new OvtaBulkUploadResultDto();

        DataTable dataTable;
        try
        {
            dataTable = ReadDataTableFromExcel(xlsxStream, "OVTA Assessments");
        }
        catch (Exception)
        {
            result.Errors.Add("Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
            return result;
        }

        var stormwaterJurisdictionsPersonCanView = StormwaterJurisdictions.ListViewableByPersonForBMPs(dbContext, currentPerson);
        var ovtaAreas = dbContext.OnlandVisualTrashAssessmentAreas.ToList();
        var users = dbContext.People.ToList();

        if (!currentPerson.IsAdministrator())
        {
            foreach (DataRow row in dataTable.Rows)
            {
                var rowJurisdiction = row["Jurisdiction Name"].ToString();
                if (!string.IsNullOrWhiteSpace(rowJurisdiction)
                    && !stormwaterJurisdictionsPersonCanView.Select(x => x.Organization.OrganizationName).Contains(rowJurisdiction))
                {
                    result.Errors.Add($"You attempted to upload a spreadsheet containing OVTAs in Jurisdiction {rowJurisdiction}, which you do not have permission to manage.");
                    return result;
                }
            }
        }

        var numRows = dataTable.Rows.Count;
        var numColumns = dataTable.Columns.Count;
        var errors = new List<string>();
        var ovtaAreaIDsForScoreRecalculation = new List<int?>();
        // NPT-1076 Bug #2: track actually-processed (non-blank) rows so we can surface a
        // row-error when the file has nothing to import. Covers header-only XLSX and the case
        // where Excel's used range stretches past data leaving trailing blank rows.
        var processedRowCount = 0;

        try
        {
            for (var i = 0; i < numRows; i++)
            {
                try
                {
                    var row = dataTable.Rows[i];
                    var rowEmpty = true;
                    for (var j = 0; j < numColumns; j++)
                    {
                        rowEmpty = string.IsNullOrWhiteSpace(row[j].ToString());
                        if (!rowEmpty) break;
                    }
                    if (rowEmpty) continue;

                    var areaID = ovtaAreas.SingleOrDefault(x => x.OnlandVisualTrashAssessmentAreaName == row["Area Name"].ToString())?.OnlandVisualTrashAssessmentAreaID;
                    var createdByPersonID = users.SingleOrDefault(x => x.Email == row["Created By Person"].ToString().Trim())?.PersonID;

                    var rowErrors = CheckDataFromRow(areaID, i, createdByPersonID, row);
                    if (rowErrors.Count > 0)
                    {
                        errors.AddRange(rowErrors);
                        continue;
                    }
                    ovtaAreaIDsForScoreRecalculation.Add(areaID);

                    var categories = PreliminarySourceIdentificationCategory.All
                        .Select(x => x.PreliminarySourceIdentificationCategoryDisplayName)
                        .ToList();
                    var assessmentPreliminarySourceIdentificationTypes = new List<OnlandVisualTrashAssessmentPreliminarySourceIdentificationType>();
                    foreach (var category in categories)
                    {
                        if (string.IsNullOrWhiteSpace(row[category].ToString())) continue;
                        // NPT-1076 Bug #1: previously `cell.Trim().Split(',')` only trimmed the
                        // full cell, so "Parked Cars, Uncovered Loads" produced a leading-space
                        // second entry that failed the `==` compare. Trim each entry and compare
                        // case-insensitively so the template's display values round-trip cleanly.
                        var identificationTypes = row[category].ToString()
                            .Split(',')
                            .Select(x => x.Trim())
                            .Where(x => x.Length > 0)
                            .ToList();
                        foreach (var identificationType in identificationTypes)
                        {
                            if (identificationType.Contains("other", StringComparison.OrdinalIgnoreCase))
                            {
                                errors.Add($"Cannot use {identificationType} in row {i + 1} as bulk uploader does not allow for Other as a preliminary type.");
                                continue;
                            }
                            var id = PreliminarySourceIdentificationType.All.SingleOrDefault(x =>
                                string.Equals(x.PreliminarySourceIdentificationTypeDisplayName.Trim(), identificationType, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(x.PreliminarySourceIdentificationCategory.PreliminarySourceIdentificationCategoryDisplayName.Trim(), category, StringComparison.OrdinalIgnoreCase));
                            if (id == null)
                            {
                                errors.Add($"{identificationType} is not a valid Preliminary Source Identification Type for {category} in row {i + 1}");
                                continue;
                            }
                            assessmentPreliminarySourceIdentificationTypes.Add(new OnlandVisualTrashAssessmentPreliminarySourceIdentificationType
                            {
                                PreliminarySourceIdentificationTypeID = id.PreliminarySourceIdentificationTypeID,
                            });
                        }
                    }

                    var jurisdictionName = row["Jurisdiction Name"].ToString();
                    var stormwaterJurisdictionID = stormwaterJurisdictionsPersonCanView
                        .Single(x => x.Organization.OrganizationName == jurisdictionName || x.Organization.OrganizationShortName == jurisdictionName)
                        .StormwaterJurisdictionID;

                    var assessment = new OnlandVisualTrashAssessment
                    {
                        OnlandVisualTrashAssessmentAreaID = areaID,
                        CreatedByPersonID = (int)createdByPersonID!,
                        StormwaterJurisdictionID = stormwaterJurisdictionID,
                        OnlandVisualTrashAssessmentStatusID = row["Status"].ToString().Trim() == "Finalized"
                            ? (int)OnlandVisualTrashAssessmentStatusEnum.Complete
                            : (int)OnlandVisualTrashAssessmentStatusEnum.InProgress,
                        CreatedDate = DateTime.UtcNow,
                        CompletedDate = DateOnly.FromDateTime(DateTime.Parse(row["Completed Date"].ToString().Trim())),
                        OnlandVisualTrashAssessmentScoreID = OnlandVisualTrashAssessmentScore.All
                            .Single(x => x.OnlandVisualTrashAssessmentScoreDisplayName == row["Score"].ToString().Trim())
                            .OnlandVisualTrashAssessmentScoreID,
                        IsProgressAssessment = row["Is Progress Assessment"].ToString().Trim() == "Yes",
                        OnlandVisualTrashAssessmentPreliminarySourceIdentificationTypes = assessmentPreliminarySourceIdentificationTypes,
                        IsTransectBackingAssessment = false,
                    };
                    dbContext.Add(assessment);
                    processedRowCount++;
                }
                catch (InvalidOperationException ioe)
                {
                    errors.Add(ioe.Message + $" (row {i})");
                }
            }
        }
        catch (Exception)
        {
            result.Errors.Add("Unexpected error parsing Excel Spreadsheet upload. Make sure the file matches the provided template and try again.");
            return result;
        }

        if (errors.Count > 0)
        {
            result.Errors = errors;
            return result;
        }

        // NPT-1076 Bug #2: file had no actionable data (header-only or all-blank rows). Surface
        // an error so the SPA doesn't show "Successfully bulk uploaded OVTAs from 0 row(s)".
        if (processedRowCount == 0)
        {
            result.Errors.Add("The uploaded file had no data rows. Add at least one OVTA Assessment row beneath the header and try again.");
            return result;
        }

        await dbContext.SaveChangesAsync();

        // Recalculate scores for all OVTAs in the areas that were uploaded.
        foreach (var ovtaAreaID in ovtaAreaIDsForScoreRecalculation.Distinct())
        {
            var ovtaArea = ovtaAreas.SingleOrDefault(x => x.OnlandVisualTrashAssessmentAreaID == ovtaAreaID);
            if (ovtaArea == null) continue;
            var assessments = dbContext.OnlandVisualTrashAssessments
                .Where(x => x.OnlandVisualTrashAssessmentAreaID == ovtaAreaID)
                .ToList();
            ovtaArea.OnlandVisualTrashAssessmentBaselineScoreID = OnlandVisualTrashAssessmentAreas
                .CalculateBaselineScoreFromBackingData(assessments)?.OnlandVisualTrashAssessmentScoreID;
            ovtaArea.OnlandVisualTrashAssessmentProgressScoreID = OnlandVisualTrashAssessments
                .CalculateProgressScore(assessments)?.OnlandVisualTrashAssessmentScoreID;
        }
        await dbContext.SaveChangesAsync();

        result.RowsProcessed = numRows;
        return result;
    }

    private static List<string> CheckDataFromRow(int? areaID, int rowIndex, int? createdByPersonID, DataRow row)
    {
        var errors = new List<string>();
        if (areaID == null)
        {
            errors.Add($"Cannot find OVTA area name in row {rowIndex + 1}");
        }
        if (createdByPersonID == null)
        {
            errors.Add($"Cannot find Person in row {rowIndex + 1}");
        }
        var isProgressRaw = row["Is Progress Assessment"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(isProgressRaw) || (isProgressRaw != "Yes" && isProgressRaw != "No"))
        {
            errors.Add($"Is Progress Assessment is not a valid value in row {rowIndex + 1}. It must be either Yes or No.");
        }
        var statusRaw = row["Status"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(statusRaw) || (statusRaw != "Finalized" && statusRaw != "Draft"))
        {
            errors.Add($"Status is not a valid value in row {rowIndex + 1}. It must be either Finalized or Draft.");
        }
        var scoreRaw = row["Score"].ToString().Trim();
        var validScores = OnlandVisualTrashAssessmentScore.All
            .Select(x => x.OnlandVisualTrashAssessmentScoreDisplayName)
            .ToList();
        if (string.IsNullOrWhiteSpace(scoreRaw) || !validScores.Contains(scoreRaw))
        {
            errors.Add($"Score is not a valid value in row {rowIndex + 1}. It must be one of the following A, B, C or D.");
        }
        // NPT-1076 Bug #3: a non-date Completed Date used to throw FormatException inside the
        // OnlandVisualTrashAssessment initializer (line below) and fall through to the outer
        // catch, which surfaced the generic "Unexpected error parsing Excel Spreadsheet upload"
        // and bailed the whole upload. Pre-validate here so the user gets a row-scoped message.
        var completedDateRaw = row["Completed Date"].ToString().Trim();
        if (string.IsNullOrWhiteSpace(completedDateRaw) || !DateTime.TryParse(completedDateRaw, out _))
        {
            errors.Add($"Invalid Completed Date '{completedDateRaw}' in row {rowIndex + 1}.");
        }
        return errors;
    }

    private static DataTable ReadDataTableFromExcel(Stream inputStream, string worksheetName)
    {
        var dataTable = new DataTable();
        using var workbook = new XLWorkbook(inputStream);
        var worksheet = workbook.Worksheet(worksheetName);

        var firstRow = true;
        foreach (var row in worksheet.Rows())
        {
            if (firstRow)
            {
                foreach (var cell in row.Cells())
                {
                    if (!string.IsNullOrEmpty(cell.Value.ToString()))
                    {
                        dataTable.Columns.Add(cell.Value.ToString());
                    }
                    else
                    {
                        break;
                    }
                }
                firstRow = false;
            }
            else
            {
                var i = 0;
                var toInsert = dataTable.NewRow();
                foreach (var cell in row.Cells(1, dataTable.Columns.Count))
                {
                    toInsert[i] = cell.Value.ToString();
                    i++;
                }
                dataTable.Rows.Add(toInsert);
            }
        }
        return dataTable;
    }
}
