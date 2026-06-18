using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Neptune.EFModels.Entities;

public static class RegionalSubbasins
{
    private static IQueryable<RegionalSubbasin> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.RegionalSubbasins;
    }

    public static RegionalSubbasin GetByIDWithChangeTracking(NeptuneDbContext dbContext, int regionalSubbasinID)
    {
        var regionalSubbasin = GetImpl(dbContext)
            .SingleOrDefault(x => x.RegionalSubbasinID == regionalSubbasinID);
        Check.RequireNotNull(regionalSubbasin, $"RegionalSubbasin with ID {regionalSubbasinID} not found!");
        return regionalSubbasin;
    }

    public static RegionalSubbasin GetByID(NeptuneDbContext dbContext, int regionalSubbasinID)
    {
        var regionalSubbasin = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.RegionalSubbasinID == regionalSubbasinID);
        Check.RequireNotNull(regionalSubbasin, $"RegionalSubbasin with ID {regionalSubbasinID} not found!");
        return regionalSubbasin;
    }

    public static async Task<List<RegionalSubbasinDto>> ListAsDtoAsync(NeptuneDbContext dbContext)
    {
        var entities = await dbContext.RegionalSubbasins.Include(x => x.OCSurveyDownstreamCatchment).AsNoTracking().OrderBy(x => x.RegionalSubbasinID).ToListAsync();
        return entities.Select(x => x.AsDto()).ToList();
    }

    public static async Task<RegionalSubbasinDto?> GetByIDAsDtoAsync(NeptuneDbContext dbContext, int regionalSubbasinID)
    {
        var entity = await dbContext.RegionalSubbasins.Include(x => x.OCSurveyDownstreamCatchment).AsNoTracking().SingleOrDefaultAsync(x => x.RegionalSubbasinID == regionalSubbasinID);
        return entity?.AsDto();
    }

    public static async Task<RegionalSubbasinDto> CreateAsync(NeptuneDbContext dbContext, RegionalSubbasinUpsertDto dto)
    {
        var entity = dto.AsEntity();
        dbContext.RegionalSubbasins.Add(entity);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.RegionalSubbasinID);
    }

    public static async Task<RegionalSubbasinDto?> UpdateAsync(NeptuneDbContext dbContext, int regionalSubbasinID, RegionalSubbasinUpsertDto dto)
    {
        var entity = await dbContext.RegionalSubbasins.FirstOrDefaultAsync(x => x.RegionalSubbasinID == regionalSubbasinID);
        if (entity == null) return null;
        entity.UpdateFromUpsertDto(dto);
        await dbContext.SaveChangesAsync();
        return await GetByIDAsDtoAsync(dbContext, entity.RegionalSubbasinID);
    }

    public static async Task<bool> DeleteAsync(NeptuneDbContext dbContext, int regionalSubbasinID)
    {
        var entity = await dbContext.RegionalSubbasins.FirstOrDefaultAsync(x => x.RegionalSubbasinID == regionalSubbasinID);
        if (entity == null) return false;
        // Delete dependent entities
//        await dbContext.RegionalSubbasinRevisionRequests.Where(x => x.RegionalSubbasinID == regionalSubbasinID).ExecuteDeleteAsync();
        await dbContext.LoadGeneratingUnits.Where(x => x.RegionalSubbasinID == regionalSubbasinID).ExecuteDeleteAsync();
        await dbContext.ProjectLoadGeneratingUnits.Where(x => x.RegionalSubbasinID == regionalSubbasinID).ExecuteDeleteAsync();
        await dbContext.ProjectNereidResults.Where(x => x.RegionalSubbasinID == regionalSubbasinID).ExecuteDeleteAsync();
        await dbContext.NereidResults.Where(x => x.RegionalSubbasinID == regionalSubbasinID).ExecuteDeleteAsync();
        await dbContext.DirtyModelNodes.Where(x => x.RegionalSubbasinID == regionalSubbasinID).ExecuteDeleteAsync();
        await dbContext.TreatmentBMPs.Where(x => x.RegionalSubbasinID == regionalSubbasinID).ExecuteDeleteAsync();
        await dbContext.RegionalSubbasins.Where(x => x.RegionalSubbasinID == regionalSubbasinID).ExecuteDeleteAsync();
        return true;
    }

    public static async Task<DateTime?> GetLatestUpdateAsync(NeptuneDbContext dbContext)
    {
        return await dbContext.RegionalSubbasins.AsNoTracking().MaxAsync(x => x.LastUpdate);
    }

    public static RegionalSubbasin GetFirstByContainsGeometry(NeptuneDbContext dbContext, Geometry dBGeometry)
    {
        return dbContext.RegionalSubbasins.SingleOrDefault(x => x.CatchmentGeometry.Contains(dBGeometry));
    }

    public static GeometryGeoJSONAndAreaDto GetUpstreamCatchmentGeometry4326GeoJSONAndArea(
        NeptuneDbContext dbContext, int regionalSubbasinID, int treatmentBMPID, int? delineationID)
    {
        return dbContext.vRegionalSubbasinUpstreamCatchmentGeometry4326s.SingleOrDefault(x => x.PrimaryKey == regionalSubbasinID)?.AsGeometryGeoJSONAndAreaDto(treatmentBMPID, delineationID);
    }

    public static FeatureCollection GetRegionalSubbasinGraphTraceAsFeatureCollection(NeptuneDbContext dbContext, CoordinateDto coordinate, bool upstreamOnly = false, bool downstreamOnly = false)
    {
        var featureCollection = new FeatureCollection();
        var regionalSubbasinGraphTrace = dbContext.RegionalSubbasinNetworkResults.FromSql($"EXECUTE dbo.pRegionalSubbasinGenerateNetwork {coordinate.Latitude}, {coordinate.Longitude}, {upstreamOnly}, {downstreamOnly}").ToList();

        regionalSubbasinGraphTrace.ForEach(x =>
        {

            //First the RSB itself
            var attributesTable = new AttributesTable
            {
                { "RegionalSubbasinID", x.CurrentNodeRegionalSubbasinID},
                { "Depth", x.Depth}
            };

            featureCollection.Add(new Feature(x.CatchmentGeometry4326, attributesTable));
            if (x.DownstreamLineGeometry != null)
            {
                //Then the downstream line
                attributesTable = new AttributesTable
                {
                    { "RegionalSubbasinID", x.CurrentNodeRegionalSubbasinID},
                    { "OCSurveyCatchmentID" , x.OCSurveyCatchmentID},
                    { "OCSurveyDownstreamCatchmentID", x.OCSurveyDownstreamCatchmentID},
                    { "Depth", x.Depth}
                };
                featureCollection.Add(new Feature(x.DownstreamLineGeometry, attributesTable));
            }
        });

        return featureCollection;
    }
}