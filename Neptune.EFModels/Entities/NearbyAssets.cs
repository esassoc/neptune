using Microsoft.EntityFrameworkCore;
using Neptune.Common.GeoSpatial;
using Neptune.Models.DataTransferObjects.NearbyAsset;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class NearbyAssets
{
    public const double RadiusMeters = 100;

    // NaN fails every range comparison, so reject non-finite values explicitly before they reach projection
    public static bool AreValidCoordinates(double latitude, double longitude)
    {
        return double.IsFinite(latitude) && double.IsFinite(longitude)
            && latitude is >= -90 and <= 90
            && longitude is >= -180 and <= 180;
    }

    // Proximity runs against the native SRID 2771 columns (units are metres, and they carry the spatial
    // indexes); the 4326 columns are only read to place markers on the map.
    public static async Task<NearbyAssetsResultDto> ListWithinRadiusAsync(NeptuneDbContext dbContext, Person person, double latitude, double longitude)
    {
        var searchPoint = GeometryHelper.CreateLocationPoint4326FromLatLong(latitude, longitude).ProjectTo2771();
        if (searchPoint.SRID != Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID)
        {
            // STDistance returns NULL across mismatched SRIDs, which would silently return nothing
            throw new InvalidOperationException($"Nearby search point must be SRID {Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID}, was {searchPoint.SRID}.");
        }

        var bmpJurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, person.PersonID);
        var wqmpJurisdictionIDs = StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonForWQMPs(dbContext, person).ToList();

        var assets = new List<NearbyAssetDto>();
        assets.AddRange(await ListTreatmentBMPsAsync(dbContext, searchPoint, bmpJurisdictionIDs));
        assets.AddRange(await ListWaterQualityManagementPlansAsync(dbContext, searchPoint, wqmpJurisdictionIDs));
        assets.AddRange(await ListOnlandVisualTrashAssessmentAreasAsync(dbContext, searchPoint, bmpJurisdictionIDs));

        return new NearbyAssetsResultDto
        {
            RadiusMeters = RadiusMeters,
            Assets = assets.OrderBy(x => x.DistanceMeters).ThenBy(x => x.AssetName).ToList()
        };
    }

    private static async Task<List<NearbyAssetDto>> ListTreatmentBMPsAsync(NeptuneDbContext dbContext, Geometry searchPoint, List<int> jurisdictionIDs)
    {
        var rows = await dbContext.TreatmentBMPs.AsNoTracking()
            .Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID)
                        && x.LocationPoint != null
                        && x.LocationPoint.IsWithinDistance(searchPoint, RadiusMeters))
            .Select(x => new
            {
                x.TreatmentBMPID,
                x.TreatmentBMPName,
                x.StormwaterJurisdictionID,
                x.LocationPoint,
                x.LocationPoint4326,
                DistanceMeters = x.LocationPoint!.Distance(searchPoint)
            })
            .ToListAsync();

        return rows.Select(x =>
        {
            var markerPoint = (x.LocationPoint4326 ?? x.LocationPoint!.ProjectTo4326()).Coordinate;
            return new NearbyAssetDto
            {
                AssetType = NearbyAssetDto.TreatmentBMPAssetType,
                AssetID = x.TreatmentBMPID,
                AssetName = x.TreatmentBMPName ?? string.Empty,
                StormwaterJurisdictionID = x.StormwaterJurisdictionID,
                Latitude = markerPoint.Y,
                Longitude = markerPoint.X,
                DistanceMeters = x.DistanceMeters
            };
        }).ToList();
    }

    private static async Task<List<NearbyAssetDto>> ListWaterQualityManagementPlansAsync(NeptuneDbContext dbContext, Geometry searchPoint, List<int> jurisdictionIDs)
    {
        var rows = await dbContext.WaterQualityManagementPlanBoundaries.AsNoTracking()
            .Where(x => jurisdictionIDs.Contains(x.WaterQualityManagementPlan.StormwaterJurisdictionID)
                        && x.GeometryNative != null
                        && x.GeometryNative.IsWithinDistance(searchPoint, RadiusMeters))
            .Select(x => new
            {
                x.WaterQualityManagementPlanID,
                x.WaterQualityManagementPlan.WaterQualityManagementPlanName,
                x.WaterQualityManagementPlan.StormwaterJurisdictionID,
                x.GeometryNative,
                x.Geometry4326,
                DistanceMeters = x.GeometryNative!.Distance(searchPoint)
            })
            .ToListAsync();

        return rows.Select(x =>
        {
            var markerPoint = PolygonMarkerPoint(x.Geometry4326, x.GeometryNative!);
            return new NearbyAssetDto
            {
                AssetType = NearbyAssetDto.WaterQualityManagementPlanAssetType,
                AssetID = x.WaterQualityManagementPlanID,
                AssetName = x.WaterQualityManagementPlanName ?? string.Empty,
                StormwaterJurisdictionID = x.StormwaterJurisdictionID,
                Latitude = markerPoint.Y,
                Longitude = markerPoint.X,
                DistanceMeters = x.DistanceMeters
            };
        }).ToList();
    }

    private static async Task<List<NearbyAssetDto>> ListOnlandVisualTrashAssessmentAreasAsync(NeptuneDbContext dbContext, Geometry searchPoint, List<int> jurisdictionIDs)
    {
        var rows = await dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
            .Where(x => jurisdictionIDs.Contains(x.StormwaterJurisdictionID)
                        && x.OnlandVisualTrashAssessmentAreaGeometry.IsWithinDistance(searchPoint, RadiusMeters))
            .Select(x => new
            {
                x.OnlandVisualTrashAssessmentAreaID,
                x.OnlandVisualTrashAssessmentAreaName,
                x.StormwaterJurisdictionID,
                x.OnlandVisualTrashAssessmentAreaGeometry,
                x.OnlandVisualTrashAssessmentAreaGeometry4326,
                DistanceMeters = x.OnlandVisualTrashAssessmentAreaGeometry.Distance(searchPoint)
            })
            .ToListAsync();

        return rows.Select(x =>
        {
            var markerPoint = PolygonMarkerPoint(x.OnlandVisualTrashAssessmentAreaGeometry4326, x.OnlandVisualTrashAssessmentAreaGeometry);
            return new NearbyAssetDto
            {
                AssetType = NearbyAssetDto.OnlandVisualTrashAssessmentAreaAssetType,
                AssetID = x.OnlandVisualTrashAssessmentAreaID,
                AssetName = x.OnlandVisualTrashAssessmentAreaName ?? string.Empty,
                StormwaterJurisdictionID = x.StormwaterJurisdictionID,
                Latitude = markerPoint.Y,
                Longitude = markerPoint.X,
                DistanceMeters = x.DistanceMeters
            };
        }).ToList();
    }

    // InteriorPoint rather than Centroid so the pin always lands inside a concave polygon
    private static Coordinate PolygonMarkerPoint(Geometry? geometry4326, Geometry geometryNative)
    {
        return (geometry4326 ?? geometryNative.ProjectTo4326()).InteriorPoint.Coordinate;
    }
}
