using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class SourceControlBMPs
{
    public static IQueryable<SourceControlBMP> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.SourceControlBMPs.Include(x => x.SourceControlBMPAttribute);
    }

    public static List<SourceControlBMP> ListByWaterQualityManagementPlanIDWithChangeTracking(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID).ToList();
    }

    public static async Task<List<SourceControlBMPDto>> ListByWaterQualityManagementPlanIDAsDtoAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        // NPT-1106 round 2: return every persisted row, including IsPresent=false with no note.
        // MergeAsync stores explicit "No" answers, but this list used to filter them out —
        // so the AI-review Step 5 compared a saved "No" against an apparently-empty record
        // ("Pending Save" forever), and the SC editor prefilled those rows as unset (a
        // subsequent replace-all save then deleted the persisted "No" via MergeDelete).
        // Display surfaces that only want affirmative rows (WQMP detail panel, verification
        // workflow) filter client-side.
        var dtos = await dbContext.SourceControlBMPs
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .OrderBy(x => x.SourceControlBMPAttributeID)
            .Select(SourceControlBMPProjections.AsDto)
            .ToListAsync();

        // Resolve category display names from the static binding class (not available in LINQ-to-SQL)
        foreach (var dto in dtos)
        {
            if (SourceControlBMPAttributeCategory.AllLookupDictionary.TryGetValue(dto.SourceControlBMPAttributeCategoryID, out var category))
            {
                dto.SourceControlBMPAttributeCategoryName = category.SourceControlBMPAttributeCategoryName;
            }
        }

        return dtos;
    }

    public static async Task MergeAsync(NeptuneDbContext dbContext, int waterQualityManagementPlanID, List<SourceControlBMPUpsertDto> dtos)
    {
        var existingSourceControlBMPs = ListByWaterQualityManagementPlanIDWithChangeTracking(dbContext, waterQualityManagementPlanID);
        var sourceControlBMPsInDatabase = dbContext.SourceControlBMPs;
        var sourceControlBMPsToUpdate = (dtos ?? new List<SourceControlBMPUpsertDto>()).Select(x => new SourceControlBMP
        {
            WaterQualityManagementPlanID = waterQualityManagementPlanID,
            SourceControlBMPAttributeID = x.SourceControlBMPAttributeID,
            IsPresent = x.IsPresent,
            SourceControlBMPNote = x.SourceControlBMPNote
        }).ToList();

        existingSourceControlBMPs.Merge(sourceControlBMPsToUpdate, sourceControlBMPsInDatabase,
            (x, y) => x.WaterQualityManagementPlanID == y.WaterQualityManagementPlanID && x.SourceControlBMPAttributeID == y.SourceControlBMPAttributeID,
            (x, y) =>
            {
                x.IsPresent = y.IsPresent;
                x.SourceControlBMPNote = y.SourceControlBMPNote;
            });

        await dbContext.SaveChangesAsync();
    }
}