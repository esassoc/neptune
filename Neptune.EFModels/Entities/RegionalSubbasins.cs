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