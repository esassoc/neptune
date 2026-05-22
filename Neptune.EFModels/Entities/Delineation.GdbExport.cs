using Microsoft.EntityFrameworkCore;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using NetTopologySuite.Features;

namespace Neptune.EFModels.Entities;

public static class DelineationGdbExport
{
    public static async Task<(byte[] Bytes, string FileName)> BuildJurisdictionGdbExportAsync(
        NeptuneDbContext dbContext,
        GDALAPIService gdalApiService,
        int stormwaterJurisdictionID,
        int delineationTypeID)
    {
        var stormwaterJurisdiction = dbContext.StormwaterJurisdictions.AsNoTracking()
            .Include(x => x.Organization)
            .Single(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        var jurisdictionName = stormwaterJurisdiction.GetOrganizationDisplayName().Replace(' ', '-');
        var delineationTypeName = DelineationType.AllLookupDictionary[delineationTypeID].DelineationTypeDisplayName;

        var delineations = dbContext.Delineations.AsNoTracking()
            .Include(x => x.TreatmentBMP).ThenInclude(x => x.StormwaterJurisdiction).ThenInclude(x => x.Organization)
            .Include(x => x.TreatmentBMP).ThenInclude(x => x.TreatmentBMPType)
            .Where(x => x.TreatmentBMP.ProjectID == null
                        && x.TreatmentBMP.StormwaterJurisdictionID == stormwaterJurisdictionID
                        && x.DelineationTypeID == delineationTypeID)
            .ToList();

        var featureCollection = new FeatureCollection();
        foreach (var delineation in delineations)
        {
            var attrs = new AttributesTable
            {
                { "DelineationID", delineation.DelineationID },
                { "TreatmentBMPName", delineation.TreatmentBMP.TreatmentBMPName },
                { "Jurisdiction", delineation.TreatmentBMP.StormwaterJurisdiction.GetOrganizationDisplayName() },
                { "BMPType", delineation.TreatmentBMP.TreatmentBMPType.TreatmentBMPTypeName },
                { "DelineationStatus", delineation.GetDelineationStatus() },
                { "DelineationArea", delineation.GetDelineationArea() },
                { "DateOfLastDelineationModification", delineation.DateLastModified },
                { "DateOfLastDelineationVerification", delineation.DateLastVerified },
            };
            featureCollection.Add(new Feature(delineation.DelineationGeometry, attrs));
        }

        if (featureCollection.Count == 0)
        {
            featureCollection.Add(new Feature(null, new AttributesTable
            {
                { "DelineationID", null },
                { "TreatmentBMPName", null },
                { "Jurisdiction", null },
                { "BMPType", null },
                { "DelineationStatus", null },
                { "DelineationArea", null },
                { "DateOfLastDelineationModification", null },
                { "DateOfLastDelineationVerification", null },
            }));
        }

        var gdbName = $"{delineationTypeName.ToLower()}-{jurisdictionName}-delineation-export";
        var gdbInput = new GdbInput
        {
            FileContents = GeoJsonSerializer.SerializeToByteArray(featureCollection, GeoJsonSerializer.DefaultSerializerOptions),
            LayerName = $"{delineationTypeName.ToLower()}-delineations",
            CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
            GeometryTypeName = "POLYGON",
        };

        var bytes = await gdalApiService.Ogr2OgrInputToGdbAsZip(new GdbInputsToGdbRequestDto
        {
            GdbInputs = new List<GdbInput> { gdbInput },
            GdbName = gdbName,
        });

        return (bytes, $"{gdbName}.zip");
    }
}
