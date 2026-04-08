using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Common.DesignByContract;
using Neptune.Common.GeoSpatial;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanBoundaries
{
    public static IQueryable<WaterQualityManagementPlanBoundary> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.WaterQualityManagementPlanBoundaries
            .Include(x => x.WaterQualityManagementPlan);
    }

    public static WaterQualityManagementPlanBoundary GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int waterQualityManagementPlanGeometryID)
    {
        var waterQualityManagementPlanBoundary = GetImpl(dbContext)
            .SingleOrDefault(x => x.WaterQualityManagementPlanGeometryID == waterQualityManagementPlanGeometryID);
        Check.RequireNotNull(waterQualityManagementPlanBoundary,
            $"WaterQualityManagementPlanBoundary with ID {waterQualityManagementPlanGeometryID} not found!");
        return waterQualityManagementPlanBoundary;
    }

    public static WaterQualityManagementPlanBoundary GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        WaterQualityManagementPlanBoundaryPrimaryKey waterQualityManagementPlanBoundaryPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, waterQualityManagementPlanBoundaryPrimaryKey.PrimaryKeyValue);
    }

    public static WaterQualityManagementPlanBoundary GetByID(NeptuneDbContext dbContext, int waterQualityManagementPlanGeometryID)
    {
        var waterQualityManagementPlanBoundary = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.WaterQualityManagementPlanGeometryID == waterQualityManagementPlanGeometryID);
        Check.RequireNotNull(waterQualityManagementPlanBoundary,
            $"WaterQualityManagementPlanBoundary with ID {waterQualityManagementPlanGeometryID} not found!");
        return waterQualityManagementPlanBoundary;
    }

    public static WaterQualityManagementPlanBoundary GetByID(NeptuneDbContext dbContext,
        WaterQualityManagementPlanBoundaryPrimaryKey waterQualityManagementPlanBoundaryPrimaryKey)
    {
        return GetByID(dbContext, waterQualityManagementPlanBoundaryPrimaryKey.PrimaryKeyValue);
    }

    public static WaterQualityManagementPlanBoundary? GetByWaterQualityManagementPlanID(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
    }

    public static WaterQualityManagementPlanBoundary? GetByWaterQualityManagementPlanIDWithChangeTracking(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).SingleOrDefault(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
    }

    public static FeatureCollection GetBoundaryAsFeatureCollection(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var boundary = GetByWaterQualityManagementPlanID(dbContext, waterQualityManagementPlanID);
        if (boundary?.Geometry4326 != null)
        {
            return boundary.Geometry4326.MultiPolygonToFeatureCollection();
        }
        return new FeatureCollection();
    }

    public static async Task<Geometry?> UpdateBoundary(NeptuneDbContext dbContext, int waterQualityManagementPlanID, string? geometryAsGeoJson)
    {
        var boundary = GetByWaterQualityManagementPlanIDWithChangeTracking(dbContext, waterQualityManagementPlanID);
        var oldGeometryNative = boundary?.GeometryNative;

        if (string.IsNullOrWhiteSpace(geometryAsGeoJson))
        {
            if (boundary != null)
            {
                boundary.GeometryNative = null;
                boundary.Geometry4326 = null;
                await dbContext.SaveChangesAsync();
            }
            return oldGeometryNative;
        }

        var feature = GeoJsonSerializer.Deserialize<IFeature>(geometryAsGeoJson);
        feature.Geometry.SRID = Proj4NetHelper.WEB_MERCATOR;
        var geometry4326 = feature.Geometry.MakeValid();
        var geometryNative = geometry4326.ProjectTo2771();

        if (boundary == null)
        {
            boundary = new WaterQualityManagementPlanBoundary
            {
                WaterQualityManagementPlanID = waterQualityManagementPlanID
            };
            dbContext.WaterQualityManagementPlanBoundaries.Add(boundary);
        }

        boundary.Geometry4326 = geometry4326;
        boundary.GeometryNative = geometryNative;
        await dbContext.SaveChangesAsync();

        return oldGeometryNative;
    }

    public static double? CalculateAcreage(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var boundary = GetByWaterQualityManagementPlanID(dbContext, waterQualityManagementPlanID);
        if (boundary?.GeometryNative != null)
        {
            return Math.Round(boundary.GeometryNative.Area * Constants.SquareMetersToAcres, 1);
        }
        return null;
    }
}