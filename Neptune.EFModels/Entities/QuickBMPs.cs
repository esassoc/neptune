using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class QuickBMPs
{
    public static IQueryable<QuickBMP> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.QuickBMPs
            .Include(x => x.TreatmentBMPType);
    }

    public static QuickBMP GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int quickBMPID)
    {
        var quickBMP = GetImpl(dbContext)
            .SingleOrDefault(x => x.QuickBMPID == quickBMPID);
        Check.RequireNotNull(quickBMP,
            $"QuickBMP with ID {quickBMPID} not found!");
        return quickBMP;
    }

    public static QuickBMP GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        QuickBMPPrimaryKey quickBMPPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, quickBMPPrimaryKey.PrimaryKeyValue);
    }

    public static QuickBMP GetByID(NeptuneDbContext dbContext, int quickBMPID)
    {
        var quickBMP = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.QuickBMPID == quickBMPID);
        Check.RequireNotNull(quickBMP,
            $"QuickBMP with ID {quickBMPID} not found!");
        return quickBMP;
    }

    public static QuickBMP GetByID(NeptuneDbContext dbContext,
        QuickBMPPrimaryKey quickBMPPrimaryKey)
    {
        return GetByID(dbContext, quickBMPPrimaryKey.PrimaryKeyValue);
    }

    public static List<QuickBMP> ListByWaterQualityManagementPlanID(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID).OrderBy(x => x.QuickBMPName).ToList();
    }

    public static List<QuickBMP> ListByWaterQualityManagementPlanIDWithChangeTracking(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID).OrderBy(x => x.QuickBMPName).ToList();
    }

    public static List<QuickBMP> GetFullyParameterized(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().Where(x =>
                x.PercentOfSiteTreated != null && x.PercentCaptured != null && x.PercentRetained != null &&
                x.TreatmentBMPType.IsAnalyzedInModelingModule)
            .ToList();
    }

    public static List<QuickBMP> List(NeptuneDbContext dbContext)
    {
        return GetImpl(dbContext).AsNoTracking().ToList();
    }

    public static async Task<List<QuickBMPDto>> ListByWaterQualityManagementPlanIDAsDtoAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return await dbContext.QuickBMPs
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(QuickBMPProjections.AsDto)
            .OrderBy(x => x.QuickBMPName)
            .ToListAsync();
    }

    /// <summary>
    /// Validates a list of QuickBMPUpsertDto against the rules shared between the manual
    /// merge endpoint and the AI-extraction approve endpoint (NPT-1047): unique names per
    /// WQMP, note length, percent ranges, captured ≥ retained, and total site treated ≤ 100%.
    /// Returns null on success, or an error message suitable for a BadRequest body.
    /// </summary>
    public static string? Validate(List<QuickBMPUpsertDto>? dtos)
    {
        var bmps = dtos ?? new List<QuickBMPUpsertDto>();

        var duplicateNames = bmps.GroupBy(x => x.QuickBMPName).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateNames.Any())
        {
            return $"Duplicate BMP names found: {string.Join(", ", duplicateNames)}. All names must be unique.";
        }

        var quickBMPNoteMaxLength = QuickBMP.FieldLengths.QuickBMPNote;
        foreach (var bmp in bmps)
        {
            if (bmp.QuickBMPNote?.Length > quickBMPNoteMaxLength)
            {
                return $"\"{bmp.QuickBMPName}\"'s note exceeds the maximum of {quickBMPNoteMaxLength} characters.";
            }
        }

        if (bmps.Any(x => x.PercentRetained > x.PercentCaptured))
        {
            return "Percent Captured needs to be greater than or equal to Percent Retained.";
        }

        if (bmps.Any(x => x.PercentOfSiteTreated < 0 || x.PercentOfSiteTreated > 100))
        {
            return "Percent of Site Treated needs to be between 0 and 100.";
        }

        if (bmps.Any(x => x.PercentCaptured < 0 || x.PercentCaptured > 100))
        {
            return "Percent Captured needs to be between 0 and 100.";
        }

        if (bmps.Any(x => x.PercentRetained < 0 || x.PercentRetained > 100))
        {
            return "Percent Retained needs to be between 0 and 100.";
        }

        // NPT-1051: round to 2 decimals before comparing. The actual floating-point
        // overshoot (33.3+33.3+33.4 = 100.00000000000001) only happens client-side in
        // JS Number arithmetic; on the server these are decimals and sum exactly to 100.
        // Mirror the rounding here so the client and server agree on the comparison
        // boundary at the API edge — otherwise a payload that the client trims to 100.00
        // and accepts could in principle be rejected by a stricter server check, or vice
        // versa across decimal-precision changes.
        if (bmps.Any(x => x.PercentOfSiteTreated.HasValue) && Math.Round(bmps.Sum(x => x.PercentOfSiteTreated ?? 0), 2) > 100)
        {
            return "The Percent of Site Treated exceeds 100 percent, please correct any errors before saving.";
        }

        return null;
    }

    /// <summary>
    /// NPT-1020: single-row equivalent of <see cref="MergeAsync"/>. Inserts the BMP if no
    /// row matches by (WQMPID, QuickBMPName); otherwise updates the existing row in place.
    /// Validates via <see cref="Validate"/> against the full live list with this DTO substituted
    /// in, so the same rules (unique names, % captured ≥ retained, sum of % site treated ≤ 100)
    /// apply on a per-row save the same way they do on Approve All.
    /// </summary>
    public static async Task UpsertSingleAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, QuickBMPUpsertDto dto)
    {
        var existingForValidation = await dbContext.QuickBMPs
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(x => new QuickBMPUpsertDto
            {
                TreatmentBMPTypeID = x.TreatmentBMPTypeID,
                QuickBMPName = x.QuickBMPName,
                QuickBMPNote = x.QuickBMPNote,
                PercentOfSiteTreated = x.PercentOfSiteTreated,
                PercentCaptured = x.PercentCaptured,
                PercentRetained = x.PercentRetained,
                DryWeatherFlowOverrideID = x.DryWeatherFlowOverrideID,
                NumberOfIndividualBMPs = x.NumberOfIndividualBMPs,
            })
            .ToListAsync();

        var rebuilt = existingForValidation
            .Where(x => !string.Equals(x.QuickBMPName, dto.QuickBMPName, StringComparison.OrdinalIgnoreCase))
            .ToList();
        rebuilt.Add(dto);

        var validationError = Validate(rebuilt);
        if (validationError != null)
        {
            throw new InvalidOperationException(validationError);
        }

        var existing = await dbContext.QuickBMPs
            .SingleOrDefaultAsync(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID && x.QuickBMPName == dto.QuickBMPName);

        if (existing == null)
        {
            dbContext.QuickBMPs.Add(new QuickBMP
            {
                WaterQualityManagementPlanID = waterQualityManagementPlanID,
                QuickBMPName = dto.QuickBMPName,
                TreatmentBMPTypeID = dto.TreatmentBMPTypeID!.Value,
                QuickBMPNote = dto.QuickBMPNote,
                DryWeatherFlowOverrideID = dto.DryWeatherFlowOverrideID,
                PercentOfSiteTreated = dto.PercentOfSiteTreated,
                PercentCaptured = dto.PercentCaptured,
                PercentRetained = dto.PercentRetained,
                NumberOfIndividualBMPs = dto.NumberOfIndividualBMPs!.Value,
            });
        }
        else
        {
            existing.TreatmentBMPTypeID = dto.TreatmentBMPTypeID!.Value;
            existing.QuickBMPNote = dto.QuickBMPNote;
            existing.DryWeatherFlowOverrideID = dto.DryWeatherFlowOverrideID;
            existing.PercentOfSiteTreated = dto.PercentOfSiteTreated;
            existing.PercentCaptured = dto.PercentCaptured;
            existing.PercentRetained = dto.PercentRetained;
            existing.NumberOfIndividualBMPs = dto.NumberOfIndividualBMPs!.Value;
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// NPT-1020: removes a single QuickBMP by name (used when the reviewer rejects a BMP
    /// card in Step 3 of the AI workflow). No-op if no row matches.
    /// </summary>
    public static async Task DeleteByNameAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, string quickBMPName)
    {
        var existing = await dbContext.QuickBMPs
            .SingleOrDefaultAsync(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID && x.QuickBMPName == quickBMPName);

        if (existing != null)
        {
            dbContext.QuickBMPs.Remove(existing);
            await dbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// NPT-1020 item 3: pure partition that splits a candidate BMP list into a "merge-ready"
    /// list (all required fields present) and a list of skipped entries naming each missing
    /// field. Extracted from <see cref="MergeWithReportAsync"/> so it can be unit-tested
    /// without a database. Required fields: <c>QuickBMPName</c>, <c>TreatmentBMPTypeID</c>,
    /// <c>NumberOfIndividualBMPs</c>.
    /// </summary>
    public static (List<QuickBMPUpsertDto> Mergeable, List<QuickBMPMergeSkipDto> Skipped) PartitionForMerge(
        IEnumerable<QuickBMPUpsertDto>? dtos)
    {
        var mergeable = new List<QuickBMPUpsertDto>();
        var skipped = new List<QuickBMPMergeSkipDto>();

        foreach (var dto in dtos ?? Enumerable.Empty<QuickBMPUpsertDto>())
        {
            var reasons = new List<string>();
            if (string.IsNullOrWhiteSpace(dto.QuickBMPName)) reasons.Add("Name");
            if (dto.TreatmentBMPTypeID == null) reasons.Add("Treatment BMP Type");
            if (dto.NumberOfIndividualBMPs == null) reasons.Add("# of Individual BMPs");

            if (reasons.Count > 0)
            {
                skipped.Add(new QuickBMPMergeSkipDto
                {
                    ProposedName = string.IsNullOrWhiteSpace(dto.QuickBMPName) ? "(unnamed)" : dto.QuickBMPName!,
                    Reasons = reasons,
                });
                continue;
            }

            mergeable.Add(dto);
        }

        return (mergeable, skipped);
    }

    /// <summary>
    /// NPT-1020 item 3: variant of <see cref="MergeAsync"/> that returns a report rather
    /// than throwing on rows missing required fields. Per Kathleen's tester feedback on
    /// the "Approve All → treatment bmps" AC, one BMP missing a TreatmentBMPType should
    /// not roll back the whole approval — surface a warning naming the BMP + missing
    /// fields instead, and merge the rest. Validation rules already enforced by
    /// <see cref="Validate"/> (out-of-range %, duplicate names, etc.) still hard-fail.
    /// </summary>
    public static async Task<QuickBMPMergeReport> MergeWithReportAsync(
        NeptuneDbContext dbContext,
        int waterQualityManagementPlanID,
        List<QuickBMPUpsertDto> dtos)
    {
        var (mergeable, skipped) = PartitionForMerge(dtos);
        await MergeAsync(dbContext, waterQualityManagementPlanID, mergeable);
        return new QuickBMPMergeReport { Skipped = skipped };
    }

    public static async Task MergeAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, List<QuickBMPUpsertDto> dtos)
    {
        var existingQuickBMPs = ListByWaterQualityManagementPlanIDWithChangeTracking(dbContext, waterQualityManagementPlanID);
        var quickBMPsInDatabase = dbContext.QuickBMPs;
        var quickBMPsToUpdate = dtos != null
            ? dtos.Select(x => new QuickBMP
            {
                WaterQualityManagementPlanID = waterQualityManagementPlanID,
                QuickBMPName = x.QuickBMPName,
                TreatmentBMPTypeID = x.TreatmentBMPTypeID.Value,
                QuickBMPNote = x.QuickBMPNote,
                DryWeatherFlowOverrideID = x.DryWeatherFlowOverrideID,
                PercentOfSiteTreated = x.PercentOfSiteTreated,
                PercentCaptured = x.PercentCaptured,
                PercentRetained = x.PercentRetained,
                NumberOfIndividualBMPs = x.NumberOfIndividualBMPs.Value
            }).ToList()
            : new List<QuickBMP>();

        existingQuickBMPs.Merge(quickBMPsToUpdate, quickBMPsInDatabase,
            (x, y) => x.WaterQualityManagementPlanID == y.WaterQualityManagementPlanID &&
                      x.QuickBMPName == y.QuickBMPName, (x, y) =>
            {
                x.QuickBMPName = y.QuickBMPName;
                x.QuickBMPNote = y.QuickBMPNote;
                x.DryWeatherFlowOverrideID = y.DryWeatherFlowOverrideID;
                x.TreatmentBMPTypeID = y.TreatmentBMPTypeID;
                x.PercentOfSiteTreated = y.PercentOfSiteTreated;
                x.PercentCaptured = y.PercentCaptured;
                x.PercentRetained = y.PercentRetained;
                x.NumberOfIndividualBMPs = y.NumberOfIndividualBMPs;
            });

        await dbContext.SaveChangesAsync();
    }
}