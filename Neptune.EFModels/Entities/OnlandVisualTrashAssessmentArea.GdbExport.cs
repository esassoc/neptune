using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.Models.DataTransferObjects;
using NetTopologySuite.Features;

namespace Neptune.EFModels.Entities;

public static class OnlandVisualTrashAssessmentAreaGdbExport
{
    public static OvtaAreaGdbStagingReportDto BuildStagingReportForCurrentUser(NeptuneDbContext dbContext, Person person)
    {
        var stagings = dbContext.OnlandVisualTrashAssessmentAreaStagings
            .Include(x => x.StormwaterJurisdiction)
            .Where(x => x.UploadedByPersonID == person.PersonID)
            .ToList();

        var report = new OvtaAreaGdbStagingReportDto();
        if (stagings.Count == 0)
        {
            return report;
        }

        var jurisdictions = stagings.Select(x => x.StormwaterJurisdiction).Distinct().ToList();
        if (jurisdictions.Count > 1)
        {
            report.Errors.Add($"Multiple Stormwater Jurisdictions staged for user {person.PersonID}.");
            return report;
        }

        var stormwaterJurisdictionID = jurisdictions[0].StormwaterJurisdictionID;
        report.StormwaterJurisdictionID = stormwaterJurisdictionID;

        var candidateNames = stagings.Select(x => x.AreaName).ToList();
        if (candidateNames.Distinct().Count() != candidateNames.Count)
        {
            report.Errors.Add("The OVTA Area Name must be unique for each feature in the upload.");
        }

        var badGeometryNames = stagings.Where(x => !x.Geometry.IsValid).Select(x => x.AreaName).ToList();
        if (badGeometryNames.Count > 0)
        {
            report.Errors.Add($"The following Areas have invalid geometries: {string.Join(", ", badGeometryNames)}");
        }

        var existingAreaNames = dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .Select(x => x.OnlandVisualTrashAssessmentAreaName)
            .ToList();
        var toBeUpdated = candidateNames.Intersect(existingAreaNames).Count();

        report.NumberOfOvtaAreas = stagings.Count;
        report.NumberOfOvtaAreasToBeUpdated = toBeUpdated;
        report.NumberOfOvtaAreasToBeCreated = stagings.Count - toBeUpdated;
        return report;
    }

    public static async Task DiscardStagingForUserAsync(NeptuneDbContext dbContext, Person person)
    {
        await dbContext.OnlandVisualTrashAssessmentAreaStagings
            .Where(x => x.UploadedByPersonID == person.PersonID)
            .ExecuteDeleteAsync();
    }

    public static async Task<int> ApproveStagingForUserAsync(NeptuneDbContext dbContext, Person person)
    {
        var stagings = dbContext.OnlandVisualTrashAssessmentAreaStagings
            .Where(x => x.UploadedByPersonID == person.PersonID)
            .ToList();
        if (stagings.Count == 0)
        {
            return 0;
        }

        var stormwaterJurisdictionID = stagings[0].StormwaterJurisdictionID;
        var existingAreas = dbContext.OnlandVisualTrashAssessmentAreas
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .ToDictionary(x => x.OnlandVisualTrashAssessmentAreaName);

        foreach (var staging in stagings)
        {
            if (existingAreas.TryGetValue(staging.AreaName, out var existing))
            {
                existing.AssessmentAreaDescription = staging.Description;
                // NPT-1099: MakeValid before storing — the GDB validator checks NTS IsValid, but that
                // disagrees with SQL STIsValid in edge cases and reprojection can re-introduce invalidity,
                // which would blank the OVTA GeoServer layer (SQL 24144).
                existing.OnlandVisualTrashAssessmentAreaGeometry = staging.Geometry.ProjectTo2771().MakeValid();
                existing.OnlandVisualTrashAssessmentAreaGeometry4326 = staging.Geometry.ProjectTo4326().MakeValid();
            }
            else
            {
                dbContext.OnlandVisualTrashAssessmentAreas.Add(new OnlandVisualTrashAssessmentArea
                {
                    OnlandVisualTrashAssessmentAreaName = staging.AreaName,
                    AssessmentAreaDescription = staging.Description,
                    StormwaterJurisdictionID = staging.StormwaterJurisdictionID,
                    OnlandVisualTrashAssessmentAreaGeometry = staging.Geometry.MakeValid(),
                    OnlandVisualTrashAssessmentAreaGeometry4326 = staging.Geometry.ProjectTo4326().MakeValid(),
                });
            }
        }

        await dbContext.SaveChangesAsync();
        await dbContext.OnlandVisualTrashAssessmentAreaStagings
            .Where(x => x.UploadedByPersonID == person.PersonID)
            .ExecuteDeleteAsync();
        return stagings.Count;
    }

    // NPT-1128 rework: the export carries every column shown on the OVTA Area grid, built from the same
    // AsGridDto projection so the two cannot drift. "CreatedOn" was removed: it was the newest *assessment's*
    // creation date, not the area's, and the area table has no creation date at all.
    // GDB column names must be GDB-safe (letters/digits/underscores).
    private static readonly string[] AttributeNames =
    [
        "OVTAAreaID", "OVTAAreaName", "Jurisdiction", "BaselineScore", "ProgressScore",
        "AssessmentsInProgress", "CompletedBaselineAssessments", "CompletedProgressAssessments",
        "Area_Acres", "LastAssessmentDate", "LandUseTypes", "LandUseBlockIDs", "Description",
    ];

    private static AttributesTable BuildAttributes(OnlandVisualTrashAssessmentAreaGridDto dto)
    {
        return new AttributesTable
        {
            { "OVTAAreaID", dto.OnlandVisualTrashAssessmentAreaID },
            { "OVTAAreaName", dto.OnlandVisualTrashAssessmentAreaName },
            { "Jurisdiction", dto.StormwaterJurisdictionName },
            { "BaselineScore", dto.OnlandVisualTrashAssessmentBaselineScoreName },
            { "ProgressScore", dto.OnlandVisualTrashAssessmentProgressScoreName },
            { "AssessmentsInProgress", dto.NumberOfAssessmentsInProgress },
            { "CompletedBaselineAssessments", dto.NumberOfBaselineAssessmentsCompleted },
            { "CompletedProgressAssessments", dto.NumberOfProgressAssessmentsCompleted },
            { "Area_Acres", dto.AreaAcres },
            // Date-only string so OGR types it as a timezone-naive Date (see WaterQualityManagementPlan.GdbExport).
            { "LastAssessmentDate", dto.LastAssessmentDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
            { "LandUseTypes", dto.LandUseTypes },
            { "LandUseBlockIDs", dto.LandUseBlockIDs },
            { "Description", dto.AssessmentAreaDescription },
        };
    }

    private static AttributesTable EmptyAttributes()
    {
        var attrs = new AttributesTable();
        foreach (var key in AttributeNames)
        {
            attrs.Add(key, null);
        }
        return attrs;
    }

    /// <summary>
    /// Projects OVTA Areas to the exported FeatureCollection. Separated from the GDAL step so it is unit-testable.
    /// A jurisdiction with no areas yields a single null-geometry feature so the layer schema is still emitted.
    /// </summary>
    public static FeatureCollection ToFeatureCollection(
        IEnumerable<OnlandVisualTrashAssessmentArea> areas,
        IReadOnlyDictionary<int, List<OnlandVisualTrashAssessmentAreaLandUseBlock>> landUseBlocksByAreaID)
    {
        var featureCollection = new FeatureCollection();
        foreach (var area in areas)
        {
            var dto = area.AsGridDto(landUseBlocksByAreaID.GetValueOrDefault(area.OnlandVisualTrashAssessmentAreaID) ?? []);
            featureCollection.Add(new Feature(area.OnlandVisualTrashAssessmentAreaGeometry, BuildAttributes(dto)));
        }

        if (featureCollection.Count == 0)
        {
            featureCollection.Add(new Feature(null, EmptyAttributes()));
        }
        return featureCollection;
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

        var areas = dbContext.OnlandVisualTrashAssessmentAreas.AsNoTracking()
            .Include(x => x.StormwaterJurisdiction).ThenInclude(x => x.Organization)
            .Include(x => x.OnlandVisualTrashAssessments)
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .ToList();
        var landUseBlocksByAreaID = LandUseBlocks.ListByOnlandVisualTrashAssessmentAreaID(dbContext, [stormwaterJurisdictionID]);

        var featureCollection = ToFeatureCollection(areas, landUseBlocksByAreaID);

        var gdbName = $"ovta-export-{jurisdictionName}";
        var gdbInput = new GdbInput
        {
            FileContents = GeoJsonSerializer.SerializeToByteArray(featureCollection, GeoJsonSerializer.DefaultSerializerOptions),
            LayerName = "ovta-areas",
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
