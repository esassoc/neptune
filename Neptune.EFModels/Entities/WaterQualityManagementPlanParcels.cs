using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class WaterQualityManagementPlanParcels
{
    public static IQueryable<WaterQualityManagementPlanParcel> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.WaterQualityManagementPlanParcels
            .Include(x => x.Parcel)
            .ThenInclude(x => x.ParcelGeometry)
            ;
    }

    public static WaterQualityManagementPlanParcel GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int waterQualityManagementPlanParcelID)
    {
        var waterQualityManagementPlanParcel = GetImpl(dbContext)
            .SingleOrDefault(x => x.WaterQualityManagementPlanParcelID == waterQualityManagementPlanParcelID);
        Check.RequireNotNull(waterQualityManagementPlanParcel,
            $"WaterQualityManagementPlanParcel with ID {waterQualityManagementPlanParcelID} not found!");
        return waterQualityManagementPlanParcel;
    }

    public static WaterQualityManagementPlanParcel GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        WaterQualityManagementPlanParcelPrimaryKey waterQualityManagementPlanParcelPrimaryKey)
    {
        return GetByIDWithChangeTracking(dbContext, waterQualityManagementPlanParcelPrimaryKey.PrimaryKeyValue);
    }

    public static WaterQualityManagementPlanParcel GetByID(NeptuneDbContext dbContext, int waterQualityManagementPlanParcelID)
    {
        var waterQualityManagementPlanParcel = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.WaterQualityManagementPlanParcelID == waterQualityManagementPlanParcelID);
        Check.RequireNotNull(waterQualityManagementPlanParcel,
            $"WaterQualityManagementPlanParcel with ID {waterQualityManagementPlanParcelID} not found!");
        return waterQualityManagementPlanParcel;
    }

    public static WaterQualityManagementPlanParcel GetByID(NeptuneDbContext dbContext,
        WaterQualityManagementPlanParcelPrimaryKey waterQualityManagementPlanParcelPrimaryKey)
    {
        return GetByID(dbContext, waterQualityManagementPlanParcelPrimaryKey.PrimaryKeyValue);
    }

    public static List<WaterQualityManagementPlanParcel> ListByWaterQualityManagementPlanID(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).AsNoTracking()
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID).ToList();
    }

    public static List<WaterQualityManagementPlanParcel> ListByWaterQualityManagementPlanIDWithChangeTracking(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return GetImpl(dbContext).Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID).ToList();
    }

    public static async Task RebuildForWaterQualityManagementPlan(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        var existingParcels = dbContext.WaterQualityManagementPlanParcels
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
        dbContext.WaterQualityManagementPlanParcels.RemoveRange(existingParcels);
        await dbContext.SaveChangesAsync();

        var boundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanID(dbContext, waterQualityManagementPlanID);
        if (boundary?.GeometryNative == null)
        {
            return;
        }

        const int toleranceInSquareMeters = 200;
        var boundaryGeometry = boundary.GeometryNative;

        // Prefilter with Intersects (spatial index) before the expensive Intersection().Area check
        // so we don't compute per-row intersections against ~1M OC parcels.
        var intersectingParcelIDs = dbContext.ParcelGeometries
            .Where(pg => pg.GeometryNative.Intersects(boundaryGeometry))
            .Where(pg => pg.GeometryNative.Intersection(boundaryGeometry).Area > toleranceInSquareMeters)
            .Select(pg => pg.ParcelID)
            .ToList();

        var newParcels = intersectingParcelIDs.Select(parcelID => new WaterQualityManagementPlanParcel
        {
            WaterQualityManagementPlanID = waterQualityManagementPlanID,
            ParcelID = parcelID
        });

        dbContext.WaterQualityManagementPlanParcels.AddRange(newParcels);
        await dbContext.SaveChangesAsync();
    }

    public static List<ParcelDisplayDto> ListAsParcelDisplayDtos(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return dbContext.WaterQualityManagementPlanParcels
            .AsNoTracking()
            .Include(x => x.Parcel)
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(x => new ParcelDisplayDto
            {
                ParcelID = x.ParcelID,
                ParcelNumber = x.Parcel.ParcelNumber
            })
            .ToList();
    }

    public static List<int> ListParcelIDsByWaterQualityManagementPlanID(NeptuneDbContext dbContext, int waterQualityManagementPlanID)
    {
        return dbContext.WaterQualityManagementPlanParcels
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID)
            .Select(x => x.ParcelID)
            .ToList();
    }

    public static async Task<Geometry?> UpdateParcelsAndRecomputeBoundary(NeptuneDbContext dbContext, int waterQualityManagementPlanID, List<int> parcelIDs)
    {
        // Delete existing parcel associations
        var existingParcels = dbContext.WaterQualityManagementPlanParcels
            .Where(x => x.WaterQualityManagementPlanID == waterQualityManagementPlanID);
        dbContext.WaterQualityManagementPlanParcels.RemoveRange(existingParcels);

        // Insert new parcel associations
        var newParcels = parcelIDs.Select(parcelID => new WaterQualityManagementPlanParcel
        {
            WaterQualityManagementPlanID = waterQualityManagementPlanID,
            ParcelID = parcelID
        });
        dbContext.WaterQualityManagementPlanParcels.AddRange(newParcels);

        // Get or create boundary, store old geometry for LGU refresh
        var boundary = WaterQualityManagementPlanBoundaries.GetByWaterQualityManagementPlanIDWithChangeTracking(dbContext, waterQualityManagementPlanID);
        var oldGeometryNative = boundary?.GeometryNative;

        if (boundary == null)
        {
            boundary = new WaterQualityManagementPlanBoundary
            {
                WaterQualityManagementPlanID = waterQualityManagementPlanID
            };
            dbContext.WaterQualityManagementPlanBoundaries.Add(boundary);
        }

        // Recompute boundary as union of selected parcel geometries
        if (parcelIDs.Any())
        {
            boundary.GeometryNative = ParcelGeometries.UnionAggregateByParcelIDs(dbContext, parcelIDs);
            boundary.Geometry4326 = ParcelGeometries.UnionAggregate4326ByParcelIDs(dbContext, parcelIDs);
        }
        else
        {
            boundary.GeometryNative = null;
            boundary.Geometry4326 = null;
        }

        await dbContext.SaveChangesAsync();
        return oldGeometryNative;
    }
}