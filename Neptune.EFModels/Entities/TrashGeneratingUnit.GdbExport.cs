using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace Neptune.EFModels.Entities;

public static class TrashGeneratingUnitGdbExport
{
    // NPT-1134: one field per Trash Analysis Areas grid column, plus the IDs behind the three link columns
    // so GIS users can join back to BMP, WQMP and OVTA exports. Values come from the grid's own projection
    // (TrashGeneratingUnits.AsGridDtoProjection) so the download can't drift from the grid.
    // GDB column names must be GDB-safe (letters/digits/underscores).
    private static readonly string[] AttributeNames =
    [
        "TrashAnalysisAreaID", "LandUseBlockID", "LandUseType", "OVTAAreaName", "OVTAAreaID", "OVTAAreaBaselineScore",
        "TreatmentBMPName", "TreatmentBMPID", "WQMPName", "WQMPID", "Jurisdiction", "AreaAcres",
        "BaselineTGR", "ProgressTGR", "CurrentNetLoadingRate", "TrashCaptureStatusBMP", "TrashCaptureStatusWQMP",
        "TrashCaptureEffectivenessBMP", "TrashCaptureEffectivenessWQMP", "MedianHouseholdIncomeResidential",
        "MedianHouseholdIncomeRetail", "PermitClass", "LandUseForTGR", "LandUseDefaultTGR", "LastUpdateDate",
    ];

    private static AttributesTable BuildAttributes(TrashGeneratingUnitGridDto dto)
    {
        return new AttributesTable
        {
            { "TrashAnalysisAreaID", dto.TrashGeneratingUnitID },
            { "LandUseBlockID", dto.LandUseBlockID },
            { "LandUseType", dto.PriorityLandUseTypeDisplayName },
            { "OVTAAreaName", dto.OnlandVisualTrashAssessmentAreaName },
            { "OVTAAreaID", dto.OnlandVisualTrashAssessmentAreaID },
            { "OVTAAreaBaselineScore", dto.OnlandVisualTrashAssessmentAreaBaselineScore },
            { "TreatmentBMPName", dto.TreatmentBMPName },
            { "TreatmentBMPID", dto.TreatmentBMPID },
            { "WQMPName", dto.WaterQualityManagementPlanName },
            { "WQMPID", dto.WaterQualityManagementPlanID },
            { "Jurisdiction", dto.StormwaterJurisdictionName },
            { "AreaAcres", dto.Area },
            { "BaselineTGR", dto.BaselineLoadingRate },
            { "ProgressTGR", dto.ProgressLoadingRate },
            { "CurrentNetLoadingRate", dto.CurrentLoadingRate },
            { "TrashCaptureStatusBMP", dto.TrashCaptureStatusBMP },
            { "TrashCaptureStatusWQMP", dto.TrashCaptureStatusWQMP },
            { "TrashCaptureEffectivenessBMP", dto.TrashCaptureEffectivenessBMP },
            { "TrashCaptureEffectivenessWQMP", dto.TrashCaptureEffectivenessWQMP },
            { "MedianHouseholdIncomeResidential", dto.MedianHouseholdIncomeResidential },
            { "MedianHouseholdIncomeRetail", dto.MedianHouseholdIncomeRetail },
            { "PermitClass", dto.PermitClass },
            { "LandUseForTGR", dto.LandUseForTGR },
            { "LandUseDefaultTGR", dto.TrashGenerationRate },
            // Date-only string so no time or time zone is carried over; the GDB field itself is DateTime
            // (GDAL's OpenFileGDB writer has no Date-only type by default), with midnight as the time.
            { "LastUpdateDate", dto.LastUpdateDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
        };
    }

    // OGR infers each GDB field's type from the GeoJSON values, so an all-null column (or an empty jurisdiction)
    // would come out as a String field and the schema would vary by jurisdiction. Every export therefore leads
    // with this typed row (non-integer numbers so decimal fields are always Real), and the GDAL API drops it
    // with SchemaRowFilter, so an empty jurisdiction gets the full schema and zero features.
    public const int SchemaRowID = -1;
    public static readonly string SchemaRowFilter = $"TrashAnalysisAreaID <> {SchemaRowID}";

    private static AttributesTable SchemaRowAttributes()
    {
        const string text = "";
        const int integer = 0;
        const double real = 0.5;
        var attrs = new AttributesTable();
        foreach (var name in AttributeNames)
        {
            attrs.Add(name, name switch
            {
                "TrashAnalysisAreaID" => SchemaRowID,
                "LandUseBlockID" or "OVTAAreaID" or "TreatmentBMPID" or "WQMPID"
                    or "TrashCaptureEffectivenessBMP" or "TrashCaptureEffectivenessWQMP" => integer,
                "AreaAcres" or "BaselineTGR" or "ProgressTGR" or "CurrentNetLoadingRate"
                    or "MedianHouseholdIncomeResidential" or "MedianHouseholdIncomeRetail" or "LandUseDefaultTGR" => real,
                "LastUpdateDate" => "1900-01-01",
                _ => text,
            });
        }
        return attrs;
    }

    /// <summary>
    /// Projects Trash Analysis Areas to the exported FeatureCollection. Separated from the GDAL step so it is unit-testable.
    /// The first feature is always the null-geometry schema row (see <see cref="SchemaRowID"/>).
    /// </summary>
    public static FeatureCollection ToFeatureCollection(IEnumerable<(TrashGeneratingUnitGridDto Dto, Geometry Geometry)> rows)
    {
        var featureCollection = new FeatureCollection { new Feature(null, SchemaRowAttributes()) };
        foreach (var (dto, geometry) in rows)
        {
            featureCollection.Add(new Feature(geometry, BuildAttributes(dto)));
        }
        return featureCollection;
    }

    /// <summary>
    /// The download is limited to the jurisdictions the person sees in the Trash Analysis Areas grid
    /// (same list as <see cref="TrashGeneratingUnits.ListAsGridDtoAsync"/>; Admin/SitkaAdmin see all).
    /// </summary>
    public static async Task<bool> CanDownloadJurisdictionAsync(NeptuneDbContext dbContext, int personID, int stormwaterJurisdictionID)
    {
        var viewableJurisdictionIDs = await StormwaterJurisdictionPeople.ListViewableStormwaterJurisdictionIDsByPersonIDForBMPsAsync(dbContext, personID);
        return viewableJurisdictionIDs.Contains(stormwaterJurisdictionID);
    }

    public static async Task<(byte[] Bytes, string FileName)> BuildJurisdictionGdbExportAsync(
        NeptuneDbContext dbContext,
        GDALAPIService gdalApiService,
        int stormwaterJurisdictionID)
    {
        var stormwaterJurisdiction = dbContext.StormwaterJurisdictions.AsNoTracking()
            .Include(x => x.Organization)
            .Single(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID);
        var jurisdictionName = stormwaterJurisdiction.GetOrganizationDisplayName().Replace(' ', '-');

        // The view drives the row set (it applies the grid's Phase 1 MS4 / polygon filters), but its
        // TrashGeneratingUnitGeometry column is the 4326 geometry, so the 2771 polygon comes from the table.
        var dtos = await dbContext.vTrashGeneratingUnitLoadStatistics.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .Select(TrashGeneratingUnits.AsGridDtoProjection)
            .ToListAsync();
        var geometriesByID = await dbContext.TrashGeneratingUnits.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .Select(x => new { x.TrashGeneratingUnitID, x.TrashGeneratingUnitGeometry })
            .ToDictionaryAsync(x => x.TrashGeneratingUnitID, x => x.TrashGeneratingUnitGeometry);

        var featureCollection = ToFeatureCollection(dtos.Select(dto => (dto, geometriesByID[dto.TrashGeneratingUnitID])));

        var gdbName = $"trash-analysis-areas-export-{jurisdictionName}";
        var gdbInput = new GdbInput
        {
            FileContents = GeoJsonSerializer.SerializeToByteArray(featureCollection, GeoJsonSerializer.DefaultSerializerOptions),
            // GDB feature class names can't contain hyphens; the GDAL API keeps letters/digits and turns spaces
            // into underscores, so this becomes "trash_analysis_areas".
            LayerName = "trash analysis areas",
            CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
            GeometryTypeName = "POLYGON",
            Where = SchemaRowFilter,
        };

        var bytes = await gdalApiService.Ogr2OgrInputToGdbAsZip(new GdbInputsToGdbRequestDto
        {
            GdbInputs = new List<GdbInput> { gdbInput },
            GdbName = gdbName,
        });
        return (bytes, $"{gdbName}.zip");
    }
}
