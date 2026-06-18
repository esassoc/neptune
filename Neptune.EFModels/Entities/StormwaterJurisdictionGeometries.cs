using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;

namespace Neptune.EFModels.Entities;

public static class StormwaterJurisdictionGeometries
{

    public static StormwaterJurisdictionGeometry GetByIDWithChangeTracking(NeptuneDbContext dbContext, int stormwaterJurisdictionGeometryID)
    {
        var stormwaterJurisdictionGeometry = GetImpl(dbContext)
            .SingleOrDefault(x => x.StormwaterJurisdictionGeometryID == stormwaterJurisdictionGeometryID);
        Check.RequireNotNull(stormwaterJurisdictionGeometry, $"StormwaterJurisdictionGeometry with ID {stormwaterJurisdictionGeometryID} not found!");
        return stormwaterJurisdictionGeometry;
    }

    public static StormwaterJurisdictionGeometry GetByID(NeptuneDbContext dbContext, int stormwaterJurisdictionGeometryID)
    {
        var stormwaterJurisdictionGeometry = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.StormwaterJurisdictionGeometryID == stormwaterJurisdictionGeometryID);
        Check.RequireNotNull(stormwaterJurisdictionGeometry, $"StormwaterJurisdictionGeometry with ID {stormwaterJurisdictionGeometryID} not found!");
        return stormwaterJurisdictionGeometry;
    }

    private static IQueryable<StormwaterJurisdictionGeometry> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.StormwaterJurisdictionGeometries
            .Include(x => x.StormwaterJurisdiction)
            .ThenInclude(x => x.Organization)
            .ThenInclude(x => x.OrganizationType);
    }
}