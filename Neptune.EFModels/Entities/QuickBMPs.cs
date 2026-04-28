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

        if (bmps.Any(x => x.PercentOfSiteTreated.HasValue) && bmps.Sum(x => x.PercentOfSiteTreated ?? 0) > 100)
        {
            return "The Percent of Site Treated exceeds 100 percent, please correct any errors before saving.";
        }

        return null;
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