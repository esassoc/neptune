using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class SourceControlBMPs
{
    public static IQueryable<SourceControlBMP> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.SourceControlBMPs.Include(x => x.SourceControlBMPAttribute);
    }

    public static SourceControlBMP GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int sourceControlBMPID)
    {
        var sourceControlBMP = GetImpl(dbContext)
            .SingleOrDefault(x => x.SourceControlBMPID == sourceControlBMPID);
        Check.RequireNotNull(sourceControlBMP,
            $"SourceControlBMP with ID {sourceControlBMPID} not found!");
        return sourceControlBMP;
    }

    public static SourceControlBMP GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        SourceControlBMPPrimaryKey sourceControlBMPPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, sourceControlBMPPrimaryKey.PrimaryKeyValue);
    }

    public static SourceControlBMP GetByID(NeptuneDbContext dbContext, int sourceControlBMPID)
    {
        var sourceControlBMP = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.SourceControlBMPID == sourceControlBMPID);
        Check.RequireNotNull(sourceControlBMP,
            $"SourceControlBMP with ID {sourceControlBMPID} not found!");
        return sourceControlBMP;
    }

    public static SourceControlBMP GetByID(NeptuneDbContext dbContext,
        SourceControlBMPPrimaryKey sourceControlBMPPrimaryKey)
    {
        return GetByID(dbContext, sourceControlBMPPrimaryKey.PrimaryKeyValue);
    }

    public static List<SourceControlBMP> ListByWaterQualityManagementPlanID(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID).ToList();
    }

    public static List<SourceControlBMP> ListByWaterQualityManagementPlanIDWithChangeTracking(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID).ToList();
    }

    public static async Task<List<SourceControlBMPDto>> ListByWaterQualityManagementPlanIDAsDtoAsync(
        NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var dtos = await dbContext.SourceControlBMPs
            .AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Where(x => x.SourceControlBMPNote != null || x.IsPresent == true)
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
}