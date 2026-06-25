using Microsoft.EntityFrameworkCore;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using Neptune.Common;
using NetTopologySuite.Features;

namespace Neptune.EFModels.Entities;

public static class TreatmentBMPGdbExport
{
    public static FeatureCollection ToExportGeoJsonFeatureCollection(this IEnumerable<vTreatmentBMPGdbExport> treatmentBMPs)
    {
        var featureCollection = new FeatureCollection();
        foreach (var treatmentBMP in treatmentBMPs)
        {
            var attributesTable = AddAllCommonPropertiesToTreatmentBMPFeature(treatmentBMP);
            var feature = new Feature(treatmentBMP.LocationPoint, attributesTable);
            featureCollection.Add(feature);
        }
        return featureCollection;
    }

    public static FeatureCollection ToExportGeoJsonFeatureCollection(
        this IEnumerable<vTreatmentBMPGdbExport> treatmentBMPs,
        ICollection<TreatmentBMPTypeCustomAttributeType> treatmentBMPTypeCustomAttributeTypes,
        ILookup<int, CustomAttribute> customAttributes)
    {
        var featureCollection = new FeatureCollection();
        foreach (var treatmentBMP in treatmentBMPs)
        {
            var attributesTable = AddAllCommonPropertiesToTreatmentBMPFeature(treatmentBMP);
            var attributes = customAttributes[treatmentBMP.TreatmentBMPID].ToList();
            foreach (var treatmentBMPTypeCustomAttributeType in treatmentBMPTypeCustomAttributeTypes.OrderBy(x => x.SortOrder))
            {
                attributesTable.Add(treatmentBMPTypeCustomAttributeType.CustomAttributeType.CustomAttributeTypeName.SanitizeStringForGdb(),
                    TreatmentBMP.GetCustomAttributeValueWithUnits(treatmentBMPTypeCustomAttributeType, attributes));
            }
            var feature = new Feature(treatmentBMP.LocationPoint, attributesTable);
            featureCollection.Add(feature);
        }
        return featureCollection;
    }

    private static AttributesTable AddAllCommonPropertiesToTreatmentBMPFeature(vTreatmentBMPGdbExport x)
    {
        return new AttributesTable
        {
            { "Name", x.TreatmentBMPName },
            { "Jurisdiction", x.OrganizationName },
            { "Type", x.TreatmentBMPTypeName },
            { "Owner", x.OwnerOrganizationName },
            { "Year_Built", x.YearBuilt },
            { "ID_in_System_of_Record", x.SystemOfRecordID },
            { "Water_Quality_Management_Plan", x.WaterQualityManagementPlanName },
            { "Trash_Capture_Effectiveness", x.TrashCaptureEffectiveness },
            { "Notes", x.Notes },
            { "Last_Assessment_Date", x.LatestAssessmentDate },
            { "Last_Assessed_Score", x.LatestAssessmentScore },
            { "Benchmark_and_Threshold_Set", x.NumberOfBenchmarkAndThresholds > 0 ? "Yes" : "No" },
            { "Required_Lifespan_of_Installation", x.TreatmentBMPLifespanTypeDisplayName ?? "Unknown" },
            { "Lifespan_End_Date", x.TreatmentBMPLifespanEndDate },
            { "Required_Field_Visits_Per_Year", x.RequiredFieldVisitsPerYear },
            { "Required_Post_Storm_Visits_Per_Year", x.RequiredPostStormFieldVisitsPerYear },
        };
    }

    public static async Task<(byte[] Bytes, string FileName)> BuildBMPInventoryGdbExportAsync(
        NeptuneDbContext dbContext,
        GDALAPIService gdalApiService,
        Person currentPerson)
    {
        var treatmentBMPs = dbContext.vTreatmentBMPGdbExports.AsNoTracking().ToList()
            .Where(x => currentPerson.IsAssignedToStormwaterJurisdiction(x.StormwaterJurisdictionID))
            .ToList();
        var treatmentBMPTypes = TreatmentBMPTypes.List(dbContext).ToDictionary(x => x.TreatmentBMPTypeID);
        var customAttributes = CustomAttributes.List(dbContext).ToLookup(x => x.TreatmentBMPID);

        var allTreatmentBMPsFeatureCollection = treatmentBMPs.ToExportGeoJsonFeatureCollection();
        var outputLayerName = $"TreatmentBMPs_Export_{DateTime.Now:yyyyMMdd}";
        var jsonSerializerOptions = GeoJsonSerializer.DefaultSerializerOptions;

        var gdbInputs = new List<GdbInput>
        {
            new()
            {
                FileContents = GeoJsonSerializer.SerializeToByteArray(allTreatmentBMPsFeatureCollection, jsonSerializerOptions),
                LayerName = "AllTreatmentBMPs",
                CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
                GeometryTypeName = "POINT",
            },
        };
        gdbInputs.AddRange(treatmentBMPs.GroupBy(x => x.TreatmentBMPTypeID).Select(grouping =>
        {
            var treatmentBMPType = treatmentBMPTypes[grouping.Key];
            return new GdbInput
            {
                FileContents = GeoJsonSerializer.SerializeToByteArray(
                    grouping.ToExportGeoJsonFeatureCollection(treatmentBMPType.TreatmentBMPTypeCustomAttributeTypes, customAttributes),
                    jsonSerializerOptions),
                LayerName = treatmentBMPType.TreatmentBMPTypeName,
                CoordinateSystemID = Proj4NetHelper.NAD_83_HARN_CA_ZONE_VI_SRID,
                GeometryTypeName = "POINT",
            };
        }));

        var apiRequest = new GdbInputsToGdbRequestDto
        {
            GdbName = outputLayerName,
            GdbInputs = gdbInputs,
        };

        var bytes = await gdalApiService.Ogr2OgrInputToGdbAsZip(apiRequest);
        return (bytes, $"{outputLayerName}.zip");
    }
}
