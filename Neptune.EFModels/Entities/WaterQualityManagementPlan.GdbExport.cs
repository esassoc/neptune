using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Neptune.Common.GeoSpatial;
using Neptune.Common.Services.GDAL;
using NetTopologySuite.Features;

namespace Neptune.EFModels.Entities;

// NPT-943: export WQMP boundary polygons + attributes to an Esri File Geodatabase, mirroring
// Delineation.GdbExport (single polygon layer) and reusing GDALAPIService.Ogr2OgrInputToGdbAsZip.
public static class WaterQualityManagementPlanGdbExport
{
    public const string LayerName = "WaterQualityManagementPlan";

    // WQMPs to export: must have a recorded boundary (req 6), be in the caller's viewable
    // jurisdictions, and (when ids is non-empty) be in the requested set.
    public static List<WaterQualityManagementPlan> ListForGdbExport(NeptuneDbContext dbContext, ICollection<int> viewableJurisdictionIDs, ICollection<int> ids)
    {
        var jurisdictionIDs = viewableJurisdictionIDs.ToList();
        var idFilter = ids.ToList();
        // Most lookups (Priority/Status/LandUse/…) are in-memory computed getters off AllLookupDictionary
        // — resolved from the FK with no Include. Only WaterQualityManagementPlanBoundary and
        // HydrologicSubarea are real EF navigations, so those are the only Includes.
        return dbContext.WaterQualityManagementPlans.AsNoTracking()
            .Include(x => x.WaterQualityManagementPlanBoundary)
            .Include(x => x.HydrologicSubarea)
            .Where(x => x.WaterQualityManagementPlanBoundary != null
                        && x.WaterQualityManagementPlanBoundary.GeometryNative != null
                        && jurisdictionIDs.Contains(x.StormwaterJurisdictionID)
                        && (idFilter.Count == 0 || idFilter.Contains(x.WaterQualityManagementPlanID)))
            .ToList();
    }

    public static FeatureCollection ToFeatureCollection(IEnumerable<WaterQualityManagementPlan> waterQualityManagementPlans)
    {
        var featureCollection = new FeatureCollection();
        foreach (var wqmp in waterQualityManagementPlans)
        {
            featureCollection.Add(new Feature(wqmp.WaterQualityManagementPlanBoundary.GeometryNative, BuildAttributes(wqmp)));
        }
        return featureCollection;
    }

    // GDB column names must be GDB-safe (letters/digits/underscores).
    private static AttributesTable BuildAttributes(WaterQualityManagementPlan wqmp)
    {
        return new AttributesTable
        {
            { "Name", wqmp.WaterQualityManagementPlanName },
            { "Land_Use", wqmp.WaterQualityManagementPlanLandUse?.WaterQualityManagementPlanLandUseDisplayName },
            { "Priority", wqmp.WaterQualityManagementPlanPriority?.WaterQualityManagementPlanPriorityDisplayName },
            { "Status", wqmp.WaterQualityManagementPlanStatus?.WaterQualityManagementPlanStatusDisplayName },
            { "Development_Type", wqmp.WaterQualityManagementPlanDevelopmentType?.WaterQualityManagementPlanDevelopmentTypeDisplayName },
            { "Trash_Capture_Status", wqmp.TrashCaptureStatusType?.TrashCaptureStatusTypeDisplayName },
            { "Permit_Term", wqmp.WaterQualityManagementPlanPermitTerm?.WaterQualityManagementPlanPermitTermDisplayName },
            { "Hydromodification_Controls_Applies", wqmp.HydromodificationAppliesType?.HydromodificationAppliesTypeDisplayName },
            { "Hydrologic_Subarea", wqmp.HydrologicSubarea?.HydrologicSubareaName },
            { "Modeling_Approach", wqmp.WaterQualityManagementPlanModelingApproach?.WaterQualityManagementPlanModelingApproachDisplayName },
            { "Approval_Date", wqmp.ApprovalDate },
            { "Date_of_Construction", wqmp.DateOfConstruction },
            { "Recorded_WQMP_Area_Acres", wqmp.RecordedWQMPAreaInAcres },
            { "Trash_Capture_Effectiveness", wqmp.TrashCaptureEffectiveness },
            { "Maintenance_Contact_Name", wqmp.MaintenanceContactName },
            { "Maintenance_Contact_Organization", wqmp.MaintenanceContactOrganization },
            { "Maintenance_Contact_Phone", wqmp.MaintenanceContactPhone },
            { "Maintenance_Contact_Address_1", wqmp.MaintenanceContactAddress1 },
            { "Maintenance_Contact_Address_2", wqmp.MaintenanceContactAddress2 },
            { "Maintenance_Contact_City", wqmp.MaintenanceContactCity },
            { "Maintenance_Contact_State", wqmp.MaintenanceContactState },
            { "Maintenance_Contact_Zip", wqmp.MaintenanceContactZip },
        };
    }

    private static AttributesTable EmptyAttributes()
    {
        var attrs = new AttributesTable();
        foreach (var key in new[]
                 {
                     "Name", "Land_Use", "Priority", "Status", "Development_Type", "Trash_Capture_Status",
                     "Permit_Term", "Hydromodification_Controls_Applies", "Hydrologic_Subarea", "Modeling_Approach",
                     "Approval_Date", "Date_of_Construction", "Recorded_WQMP_Area_Acres", "Trash_Capture_Effectiveness",
                     "Maintenance_Contact_Name", "Maintenance_Contact_Organization", "Maintenance_Contact_Phone",
                     "Maintenance_Contact_Address_1", "Maintenance_Contact_Address_2", "Maintenance_Contact_City",
                     "Maintenance_Contact_State", "Maintenance_Contact_Zip",
                 })
        {
            attrs.Add(key, null);
        }
        return attrs;
    }

    public static async Task<(byte[] Bytes, string FileName)> BuildGdbExportAsync(
        NeptuneDbContext dbContext,
        GDALAPIService gdalApiService,
        ICollection<int> viewableJurisdictionIDs,
        ICollection<int> ids)
    {
        var wqmps = ListForGdbExport(dbContext, viewableJurisdictionIDs, ids);

        var featureCollection = ToFeatureCollection(wqmps);
        if (featureCollection.Count == 0)
        {
            // Emit one empty feature so the GDB still contains the layer (matches the sibling exporters).
            featureCollection.Add(new Feature(null, EmptyAttributes()));
        }

        var gdbName = $"WaterQualityManagementPlans_Export_{DateTime.Now:yyyyMMdd}";
        var gdbInput = new GdbInput
        {
            FileContents = GeoJsonSerializer.SerializeToByteArray(featureCollection, GeoJsonSerializer.DefaultSerializerOptions),
            LayerName = LayerName,
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
