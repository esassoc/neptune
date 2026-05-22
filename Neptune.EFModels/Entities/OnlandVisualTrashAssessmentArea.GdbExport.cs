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
                existing.OnlandVisualTrashAssessmentAreaGeometry = staging.Geometry.ProjectTo2771();
                existing.OnlandVisualTrashAssessmentAreaGeometry4326 = staging.Geometry.ProjectTo4326();
            }
            else
            {
                dbContext.OnlandVisualTrashAssessmentAreas.Add(new OnlandVisualTrashAssessmentArea
                {
                    OnlandVisualTrashAssessmentAreaName = staging.AreaName,
                    AssessmentAreaDescription = staging.Description,
                    StormwaterJurisdictionID = staging.StormwaterJurisdictionID,
                    OnlandVisualTrashAssessmentAreaGeometry = staging.Geometry,
                    OnlandVisualTrashAssessmentAreaGeometry4326 = staging.Geometry.ProjectTo4326(),
                });
            }
        }

        await dbContext.SaveChangesAsync();
        await dbContext.OnlandVisualTrashAssessmentAreaStagings
            .Where(x => x.UploadedByPersonID == person.PersonID)
            .ExecuteDeleteAsync();
        return stagings.Count;
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
            .Include(x => x.OnlandVisualTrashAssessments)
            .Where(x => x.StormwaterJurisdictionID == stormwaterJurisdictionID)
            .ToList();

        var featureCollection = new FeatureCollection();
        foreach (var area in areas)
        {
            var attrs = new AttributesTable
            {
                { "OVTAAreaName", area.OnlandVisualTrashAssessmentAreaName },
                { "Description", area.AssessmentAreaDescription },
                { "CreatedOn", area.OnlandVisualTrashAssessments?.MaxBy(x => x.CreatedDate)?.CreatedDate },
            };
            featureCollection.Add(new Feature(area.OnlandVisualTrashAssessmentAreaGeometry, attrs));
        }

        if (featureCollection.Count == 0)
        {
            featureCollection.Add(new Feature(null, new AttributesTable
            {
                { "OVTAAreaName", null },
                { "Description", null },
                { "CreatedOn", null },
            }));
        }

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
