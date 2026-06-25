using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;

namespace Neptune.EFModels.Entities;

public static class SourceControlBMPAttributes
{

    public static async Task<List<SourceControlBMPUpsertDto>> ListAsUpsertDtoAsync(NeptuneDbContext dbContext)
    {
        var attributes = await dbContext.SourceControlBMPAttributes
            .AsNoTracking()
            .OrderBy(x => x.SourceControlBMPAttributeID)
            .ToListAsync();

        return attributes.Select(x =>
        {
            // Resolve category from static binding class since it's not a DB navigation property
            var categoryName = SourceControlBMPAttributeCategory.AllLookupDictionary.TryGetValue(x.SourceControlBMPAttributeCategoryID, out var category)
                ? category.SourceControlBMPAttributeCategoryName
                : null;

            return new SourceControlBMPUpsertDto
            {
                SourceControlBMPID = null,
                SourceControlBMPAttributeCategoryID = x.SourceControlBMPAttributeCategoryID,
                SourceControlBMPAttributeCategoryName = categoryName,
                SourceControlBMPAttributeID = x.SourceControlBMPAttributeID,
                SourceControlBMPAttributeName = x.SourceControlBMPAttributeName,
                IsPresent = null,
                SourceControlBMPNote = null
            };
        }).ToList();
    }
}