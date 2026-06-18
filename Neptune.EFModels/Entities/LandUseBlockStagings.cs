using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class LandUseBlockStagings
{
    public static IQueryable<LandUseBlockStaging> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.LandUseBlockStagings;
    }

    public static List<LandUseBlockStaging> ListByPersonID(NeptuneDbContext dbContext, int personID)
    {
        return GetImpl(dbContext).AsNoTracking()
            .Where(x => x.UploadedByPersonID == personID).ToList();
    }

    /// <summary>
    /// NPT-1077: build the pre-commit validation report for a user's staging batch. Runs the same
    /// PriorityLandUseType + PermitType checks the background job used to email, and counts the
    /// existing <c>LandUseBlock</c> rows in the affected jurisdictions that the wholesale-replace
    /// commit would delete. Modeled on Delineation's
    /// <c>DelineationStagings.BuildReportForCurrentUser</c>.
    /// </summary>
    public static async Task<LandUseBlockGdbUploadValidationDto> BuildReportForCurrentUserAsync(NeptuneDbContext dbContext, int personID)
    {
        var stagings = ListByPersonID(dbContext, personID);
        var dto = new LandUseBlockGdbUploadValidationDto
        {
            TotalStagedRowCount = stagings.Count,
            Errors = ValidateStagings(stagings),
        };
        if (stagings.Count == 0)
        {
            return dto;
        }

        var affectedJurisdictionIDs = stagings.Select(x => x.StormwaterJurisdictionID).Distinct().ToList();
        dto.ExistingRowsToReplace = await dbContext.LandUseBlocks
            .AsNoTracking()
            .CountAsync(x => affectedJurisdictionIDs.Contains(x.StormwaterJurisdictionID));
        return dto;
    }

    /// <summary>
    /// NPT-1077: validate the staging rows' lookup-table fields (PriorityLandUseType + PermitType).
    /// Errors are aggregated by (field, invalid value) — one line per distinct problem with a row
    /// count and a sample of affected row numbers — so a 10k-row upload with 5k bad values
    /// produces one digestible line instead of 5k repetitive ones. Row numbers are 1-indexed so
    /// they match what a user would count in a feature-class browser.
    /// </summary>
    public static List<string> ValidateStagings(IReadOnlyList<LandUseBlockStaging> stagings)
    {
        const int sampleRows = 5;
        var allowedPriorityNames = PriorityLandUseType.All.Select(x => x.PriorityLandUseTypeDisplayName).ToList();
        var allowedPermitNames = PermitType.All.Select(x => x.PermitTypeDisplayName).ToList();
        var allowedPriorityNamesText = string.Join(", ", allowedPriorityNames);
        var allowedPermitNamesText = string.Join(", ", allowedPermitNames);

        // (errorKey -> { rowNumbers }). errorKey is a synthetic string we group by; we keep the
        // first <sampleRows> 1-based row numbers per group for display in the error message.
        var priorityGroups = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        var permitMissingRows = new List<int>();
        var permitGroups = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < stagings.Count; i++)
        {
            var staging = stagings[i];
            var rowNumber = i + 1;

            var priorityName = staging.PriorityLandUseType;
            if (!allowedPriorityNames.Contains(priorityName, StringComparer.InvariantCultureIgnoreCase))
            {
                if (!priorityGroups.TryGetValue(priorityName ?? string.Empty, out var rows))
                {
                    rows = new List<int>();
                    priorityGroups[priorityName ?? string.Empty] = rows;
                }
                rows.Add(rowNumber);
            }

            var permitName = staging.PermitType;
            if (string.IsNullOrWhiteSpace(permitName))
            {
                permitMissingRows.Add(rowNumber);
            }
            else if (!allowedPermitNames.Contains(permitName, StringComparer.InvariantCultureIgnoreCase))
            {
                if (!permitGroups.TryGetValue(permitName, out var rows))
                {
                    rows = new List<int>();
                    permitGroups[permitName] = rows;
                }
                rows.Add(rowNumber);
            }
        }

        var errors = new List<string>();
        foreach (var (badValue, rows) in priorityGroups)
        {
            errors.Add(FormatAggregatedError("PriorityLandUseType", badValue, rows, sampleRows, allowedPriorityNamesText));
        }
        if (permitMissingRows.Count > 0)
        {
            errors.Add($"PermitType is blank in {permitMissingRows.Count} row(s) (e.g. row(s) {FormatSampleRows(permitMissingRows, sampleRows)}). A value is required.");
        }
        foreach (var (badValue, rows) in permitGroups)
        {
            errors.Add(FormatAggregatedError("PermitType", badValue, rows, sampleRows, allowedPermitNamesText));
        }
        return errors;
    }

    private static string FormatAggregatedError(string fieldName, string badValue, List<int> rows, int sampleRows, string allowedValuesText)
    {
        var rowSample = FormatSampleRows(rows, sampleRows);
        var displayValue = string.IsNullOrEmpty(badValue) ? "(empty)" : badValue;
        return $"Invalid {fieldName}: '{displayValue}' in {rows.Count} row(s) (e.g. row(s) {rowSample}). Allowed values: {allowedValuesText}.";
    }

    private static string FormatSampleRows(List<int> rows, int sampleRows)
    {
        if (rows.Count <= sampleRows)
        {
            return string.Join(", ", rows);
        }
        return string.Join(", ", rows.Take(sampleRows)) + $", … and {rows.Count - sampleRows} more";
    }

    /// <summary>
    /// NPT-1077: shared discard helper for the controller and (potentially) the job. Wraps the
    /// existing stored proc that scopes deletion to a single person's staging rows.
    /// </summary>
    public static async Task DiscardForUserAsync(NeptuneDbContext dbContext, int personID)
    {
        await dbContext.Database.ExecuteSqlAsync($"dbo.pLandUseBlockStagingDeleteByPersonID @PersonID = {personID}");
    }
}