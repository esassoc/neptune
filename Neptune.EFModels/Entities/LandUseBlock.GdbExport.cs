using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using NetTopologySuite.Features;

namespace Neptune.EFModels.Entities;

public static class LandUseBlockGdbExport
{
    public static async Task<(byte[] Bytes, string FileName)> BuildJurisdictionGdbExportAsync(
        NeptuneDbContext dbContext,
        GDALAPIService gdalApiService,
        int stormwaterJurisdictionID)
    {
        var stormwaterJurisdiction = dbContext.StormwaterJurisdictions.AsNoTracking()
            .Include(x => x.Organization)
            .Single(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        var jurisdictionName = stormwaterJurisdiction.GetOrganizationDisplayName().Replace(' ', '-');

        var landUseBlocks = dbContext.LandUseBlocks.AsNoTracking()
            .Include(x => x.StormwaterJurisdiction).ThenInclude(x => x.Organization)
            .Include(x => x.TrashGeneratingUnits)
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .ToList();

        var featureCollection = new FeatureCollection();
        foreach (var landUseBlock in landUseBlocks)
        {
            var attributesTable = new AttributesTable
            {
                { "LandUseBlockID", landUseBlock.LandUseBlockID },
                { "PriorityLandUseType", PriorityLandUseType.AllLookupDictionary[(int)landUseBlock.PriorityLandUseTypeID].PriorityLandUseTypeDisplayName },
                { "BlockArea", landUseBlock.LandUseBlockGeometry.Area * Constants.SquareMetersToAcres },
                { "LandUseDescription", landUseBlock.LandUseDescription },
                { "TrashGenerationRate", landUseBlock.TrashGenerationRate },
                { "TrashResultsArea", landUseBlock.TrashGeneratingUnits.Sum(y => y.TrashGeneratingUnitGeometry.Area) * Constants.SquareMetersToAcres },
                { "LandUseForTGR", landUseBlock.LandUseForTGR },
                { "MedianHouseholdIncomeRetail", landUseBlock.MedianHouseholdIncomeRetail },
                { "MedianHouseholdIncomeResidential", landUseBlock.MedianHouseholdIncomeResidential },
                { "StormwaterJurisdiction", landUseBlock.StormwaterJurisdiction.GetOrganizationDisplayName() },
                { "PermitType", PermitType.AllLookupDictionary[landUseBlock.PermitTypeID].PermitTypeDisplayName },
            };
            featureCollection.Add(new Feature(landUseBlock.LandUseBlockGeometry, attributesTable));
        }

        if (featureCollection.Count == 0)
        {
            featureCollection.Add(new Feature(null, new AttributesTable
            {
                { "LandUseBlockID", null },
                { "PriorityLandUseType", null },
                { "BlockArea", null },
                { "LandUseDescription", null },
                { "TrashGenerationRate", null },
                { "TrashResultsArea", null },
                { "LandUseForTGR", null },
                { "MedianHouseholdIncomeRetail", null },
                { "MedianHouseholdIncomeResidential", null },
                { "StormwaterJurisdiction", null },
                { "PermitType", null },
            }));
        }

        var gdbName = $"{jurisdictionName}-land-use-blocks-export";
        var gdbInput = new GdbInput
        {
            FileContents = GeoJsonSerializer.SerializeToByteArray(featureCollection, GeoJsonSerializer.DefaultSerializerOptions),
            LayerName = "land-use-blocks",
            CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
            GeometryTypeName = "POLYGON",
        };

        var bytes = await gdalApiService.Ogr2OgrInputToGdbAsZip(new GdbInputsToGdbRequestDto
        {
            GdbInputs = new List<GdbInput> { gdbInput },
            GdbName = gdbName,
        });
        return (bytes, $"{jurisdictionName}-land-use-block.zip");
    }
}
