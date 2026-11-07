using Microsoft.EntityFrameworkCore;
using Neptune.Common;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class LandUseBlockGdbExport
{
    public static readonly string[] AttributeNames =
    {
        "LandUseBlockID", "PriorityLandUseType", "BlockArea", "LandUseDescription", "TrashGenerationRate", "TrashResultsArea",
        "LandUseForTGR", "MedianHouseholdIncomeRetail", "MedianHouseholdIncomeResidential", "StormwaterJurisdiction", "PermitType",
    };

    public static async Task<(byte[] Bytes, string FileName)> BuildJurisdictionGdbExportAsync(
        NeptuneDbContext dbContext,
        GDALAPIService gdalApiService,
        int stormwaterJurisdictionID)
    {
        var stormwaterJurisdiction = dbContext.StormwaterJurisdictions.AsNoTracking()
            .Include(x => x.Organization)
            .Single(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        var stormwaterJurisdictionName = stormwaterJurisdiction.GetOrganizationDisplayName();
        var jurisdictionName = stormwaterJurisdictionName.Replace(' ', '-');

        var rows = await ListExportRowsAsync(dbContext, stormwaterJurisdictionID);
        var featureCollection = ToFeatureCollection(rows, stormwaterJurisdictionName);

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

    /// <summary>
    /// NPT-1135 rework: reads only the columns the export writes instead of whole Land Use Block and Trash Generating
    /// Unit entities. The TGU areas are still summed here rather than with SQL STArea, which rounds differently in the
    /// last few digits, so the file keeps the same values as before.
    /// </summary>
    public static async Task<List<ExportRow>> ListExportRowsAsync(NeptuneDbContext dbContext, int stormwaterJurisdictionID)
    {
        var rows = await dbContext.LandUseBlocks.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .Select(x => new
            {
                Row = new ExportRow
                {
                    LandUseBlockID = x.LandUseBlockID,
                    PriorityLandUseTypeID = x.PriorityLandUseTypeID,
                    LandUseDescription = x.LandUseDescription,
                    TrashGenerationRate = x.TrashGenerationRate,
                    LandUseForTGR = x.LandUseForTGR,
                    MedianHouseholdIncomeRetail = x.MedianHouseholdIncomeRetail,
                    MedianHouseholdIncomeResidential = x.MedianHouseholdIncomeResidential,
                    PermitTypeID = x.PermitTypeID,
                    Geometry = x.LandUseBlockGeometry,
                },
                TrashGeneratingUnitGeometries = x.TrashGeneratingUnits.Select(y => y.TrashGeneratingUnitGeometry).ToList(),
            })
            .ToListAsync();

        return rows.Select(x =>
        {
            x.Row.TrashResultsArea = x.TrashGeneratingUnitGeometries.Sum(y => y.Area);
            return x.Row;
        }).ToList();
    }

    /// <summary>
    /// Projects Land Use Blocks to the exported FeatureCollection. Separated from the GDAL step so it is unit-testable.
    /// A jurisdiction with no blocks gets one null-geometry row so the GDB still has the schema.
    /// </summary>
    public static FeatureCollection ToFeatureCollection(IEnumerable<ExportRow> rows, string stormwaterJurisdictionName)
    {
        var featureCollection = new FeatureCollection();
        foreach (var row in rows)
        {
            var attributesTable = new AttributesTable
            {
                { "LandUseBlockID", row.LandUseBlockID },
                { "PriorityLandUseType", row.PriorityLandUseTypeID.HasValue && PriorityLandUseType.AllLookupDictionary.TryGetValue(row.PriorityLandUseTypeID.Value, out var priorityLandUseType)
                    ? priorityLandUseType.PriorityLandUseTypeDisplayName
                    : null },
                { "BlockArea", row.Geometry.Area * Constants.SquareMetersToAcres },
                { "LandUseDescription", row.LandUseDescription },
                { "TrashGenerationRate", row.TrashGenerationRate },
                { "TrashResultsArea", row.TrashResultsArea * Constants.SquareMetersToAcres },
                { "LandUseForTGR", row.LandUseForTGR },
                { "MedianHouseholdIncomeRetail", row.MedianHouseholdIncomeRetail },
                { "MedianHouseholdIncomeResidential", row.MedianHouseholdIncomeResidential },
                { "StormwaterJurisdiction", stormwaterJurisdictionName },
                { "PermitType", PermitType.AllLookupDictionary[row.PermitTypeID].PermitTypeDisplayName },
            };
            featureCollection.Add(new Feature(row.Geometry, attributesTable));
        }

        if (featureCollection.Count == 0)
        {
            var emptyAttributes = new AttributesTable();
            foreach (var attributeName in AttributeNames)
            {
                emptyAttributes.Add(attributeName, null);
            }
            featureCollection.Add(new Feature(null, emptyAttributes));
        }

        return featureCollection;
    }

    public class ExportRow
    {
        public int LandUseBlockID { get; set; }
        public int? PriorityLandUseTypeID { get; set; }
        public string? LandUseDescription { get; set; }
        public decimal? TrashGenerationRate { get; set; }
        public string? LandUseForTGR { get; set; }
        public decimal? MedianHouseholdIncomeRetail { get; set; }
        public decimal? MedianHouseholdIncomeResidential { get; set; }
        public int PermitTypeID { get; set; }
        public Geometry Geometry { get; set; } = null!;
        public double TrashResultsArea { get; set; }
    }
}
