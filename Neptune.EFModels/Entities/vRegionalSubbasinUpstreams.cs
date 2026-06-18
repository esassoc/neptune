using Microsoft.EntityFrameworkCore;

namespace Neptune.EFModels.Entities;

public static class vRegionalSubbasinUpstreams
{
    public static List<int?> ListUpstreamRegionalBasinIDs(NeptuneDbContext dbContext, RegionalSubbasin regionalSubbasin)
    {
        return ListUpstreamRegionalBasinIDs(dbContext, regionalSubbasin.RegionalSubbasinID);
    }

    public static List<int?> ListUpstreamRegionalBasinIDs(NeptuneDbContext dbContext, int regionalSubbasinID)
    {
        return dbContext.vRegionalSubbasinUpstreams.AsNoTracking().Where(x => x.PrimaryKey == regionalSubbasinID).Select(x => x.RegionalSubbasinID).ToList();
    }
}