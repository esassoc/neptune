using Microsoft.EntityFrameworkCore;
using Neptune.Common.DesignByContract;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class RegionalSubbasinRevisionRequests
{
    public static IQueryable<RegionalSubbasinRevisionRequest> GetImpl(NeptuneDbContext dbContext)
    {
        return dbContext.RegionalSubbasinRevisionRequests
                .Include(x => x.TreatmentBMP)
                .ThenInclude(x => x.TreatmentBMPType)
                .Include(x => x.RequestPerson)
                .Include(x => x.ClosedByPerson)
            ;
    }

    public static RegionalSubbasinRevisionRequest GetByIDWithChangeTracking(NeptuneDbContext dbContext,
        int regionalSubbasinRevisionRequestID)
    {
        var regionalSubbasinRevisionRequest = GetImpl(dbContext)
            .SingleOrDefault(x => x.RegionalSubbasinRevisionRequestID == regionalSubbasinRevisionRequestID);
        Check.RequireNotNull(regionalSubbasinRevisionRequest,
            $"RegionalSubbasinRevisionRequest with ID {regionalSubbasinRevisionRequestID} not found!");
        return regionalSubbasinRevisionRequest;
    }

    public static RegionalSubbasinRevisionRequest GetByID(NeptuneDbContext dbContext, int regionalSubbasinRevisionRequestID)
    {
        var regionalSubbasinRevisionRequest = GetImpl(dbContext).AsNoTracking()
            .SingleOrDefault(x => x.RegionalSubbasinRevisionRequestID == regionalSubbasinRevisionRequestID);
        Check.RequireNotNull(regionalSubbasinRevisionRequest,
            $"RegionalSubbasinRevisionRequest with ID {regionalSubbasinRevisionRequestID} not found!");
        return regionalSubbasinRevisionRequest;
    }

    public static List<RegionalSubbasinRevisionRequestDto> ListAsDto(NeptuneDbContext dbContext, Person currentPerson)
    {
        var jurisdictionIDs = currentPerson.IsAdministrator()
            ? null
            : currentPerson.StormwaterJurisdictionPeople.Select(x => x.StormwaterJurisdictionID).ToHashSet();

        var dtos = dbContext.RegionalSubbasinRevisionRequests.AsNoTracking()
            .Where(x => jurisdictionIDs == null || jurisdictionIDs.Contains(x.TreatmentBMP.StormwaterJurisdictionID))
            .OrderByDescending(x => x.RequestDate)
            .Select(RegionalSubbasinRevisionRequestDtoProjections.AsDto)
            .ToList();

        foreach (var dto in dtos)
        {
            dto.ResolveLookups();
        }

        return dtos;
    }

    public static RegionalSubbasinRevisionRequestDto? GetByIDAsDto(NeptuneDbContext dbContext, int regionalSubbasinRevisionRequestID)
    {
        var entity = dbContext.RegionalSubbasinRevisionRequests.AsNoTracking()
            .Include(x => x.TreatmentBMP)
            .Include(x => x.RequestPerson)
            .Include(x => x.ClosedByPerson)
            .SingleOrDefault(x => x.RegionalSubbasinRevisionRequestID == regionalSubbasinRevisionRequestID);

        if (entity == null)
        {
            return null;
        }

        var dto = new RegionalSubbasinRevisionRequestDto
        {
            RegionalSubbasinRevisionRequestID = entity.RegionalSubbasinRevisionRequestID,
            RegionalSubbasinRevisionRequestStatusID = entity.RegionalSubbasinRevisionRequestStatusID,
            TreatmentBMPID = entity.TreatmentBMPID,
            TreatmentBMPName = entity.TreatmentBMP.TreatmentBMPName,
            RequestPersonID = entity.RequestPersonID,
            RequestPersonName = entity.RequestPerson.LastName + ", " + entity.RequestPerson.FirstName,
            RequestDate = entity.RequestDate,
            ClosedByPersonID = entity.ClosedByPersonID,
            ClosedByPersonName = entity.ClosedByPerson != null ? entity.ClosedByPerson.LastName + ", " + entity.ClosedByPerson.FirstName : null,
            ClosedDate = entity.ClosedDate,
            Notes = entity.Notes,
            CloseNotes = entity.CloseNotes,
            GeometryGeoJson = SerializeGeometryAs4326GeoJson(entity.RegionalSubbasinRevisionRequestGeometry),
        };

        return dto.ResolveLookups();
    }

    public static bool HasOpenRequestForTreatmentBMP(NeptuneDbContext dbContext, int treatmentBMPID)
    {
        return dbContext.RegionalSubbasinRevisionRequests.AsNoTracking()
            .Any(x => x.TreatmentBMPID == treatmentBMPID
                      && x.RegionalSubbasinRevisionRequestStatusID == (int)RegionalSubbasinRevisionRequestStatusEnum.Open);
    }

    public static async Task<RegionalSubbasinRevisionRequest> CreateAsync(NeptuneDbContext dbContext, int treatmentBMPID, string geoJson, string? notes, Person currentPerson)
    {
        Check.Assert(!HasOpenRequestForTreatmentBMP(dbContext, treatmentBMPID),
            "You cannot open a new revision request for this BMP because there is already an open revision request.");

        var feature = GeoJsonSerializer.Deserialize<IFeature>(geoJson);
        Check.RequireNotNull(feature?.Geometry, "GeoJson did not contain a valid geometry.");

        var geometry4326 = feature.Geometry;
        geometry4326.SRID = Proj4NetHelper.WEB_MERCATOR;
        var geometry2771 = geometry4326.ProjectTo2771();

        var entity = new RegionalSubbasinRevisionRequest
        {
            TreatmentBMPID = treatmentBMPID,
            RegionalSubbasinRevisionRequestGeometry = geometry2771,
            RequestPersonID = currentPerson.PersonID,
            RequestDate = DateTime.UtcNow,
            RegionalSubbasinRevisionRequestStatusID = (int)RegionalSubbasinRevisionRequestStatusEnum.Open,
            Notes = notes,
        };

        await dbContext.RegionalSubbasinRevisionRequests.AddAsync(entity);
        await dbContext.SaveChangesAsync();

        return entity;
    }

    public static async Task<RegionalSubbasinRevisionRequest> CloseAsync(NeptuneDbContext dbContext, int regionalSubbasinRevisionRequestID, string? closeNotes, Person currentPerson)
    {
        var entity = GetByIDWithChangeTracking(dbContext, regionalSubbasinRevisionRequestID);

        entity.CloseNotes = closeNotes;
        entity.RegionalSubbasinRevisionRequestStatusID = (int)RegionalSubbasinRevisionRequestStatusEnum.Closed;
        entity.ClosedByPersonID = currentPerson.PersonID;
        entity.ClosedDate = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        return entity;
    }

    public static async Task<byte[]> GetGdbExportAsync(NeptuneDbContext dbContext, GDALAPIService gdalApiService, int regionalSubbasinRevisionRequestID)
    {
        var entity = GetByID(dbContext, regionalSubbasinRevisionRequestID);
        var geometry = entity.RegionalSubbasinRevisionRequestGeometry;

        var reprojected = geometry.ProjectTo2230();
        var feature = new Feature(reprojected, new AttributesTable());
        var serialized = GeoJsonSerializer.WriteFeaturesToByteArray(new[] { feature }, GeoJsonSerializer.DefaultSerializerOptions);

        var outputLayerName = $"BMP_{entity.TreatmentBMP.TreatmentBMPID}_RevisionRequest";

        var apiRequest = new GdbInputsToGdbRequestDto
        {
            GdbName = outputLayerName,
            GdbInputs = new List<GdbInput>
            {
                new()
                {
                    FileContents = serialized,
                    LayerName = outputLayerName,
                    CoordinateSystemID = Proj4NetHelper.NAD_83_CA_ZONE_VI_SRID,
                    GeometryTypeName = geometry.GeometryType,
                },
            },
        };

        return await gdalApiService.Ogr2OgrInputToGdbAsZip(apiRequest);
    }

    private static string SerializeGeometryAs4326GeoJson(Geometry geometry)
    {
        var geometry4326 = geometry.ProjectTo4326();
        var feature = new Feature(geometry4326, new AttributesTable());
        return GeoJsonSerializer.Serialize(feature);
    }
}