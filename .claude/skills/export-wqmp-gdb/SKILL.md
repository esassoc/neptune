---
name: export-wqmp-gdb
description: Export Water Quality Management Plans for one jurisdiction from the local Neptune database to an Esri File Geodatabase in native projection. Use when a client requests WQMP spatial data for a specific city/jurisdiction.
allowed-tools: [Bash(*sqlcmd*), Bash(*ogr2ogr*), Bash(*ogrinfo*), Bash(rm -rf*), Bash(mkdir*), Bash(ls*), Write]
---

Build a File Geodatabase of WQMPs filtered to one StormwaterJurisdiction, joined to display-name attributes from `dbo.vWaterQualityManagementPlanDetailed`, in native projection EPSG:2771 (NAD83(HARN) / California zone 6, meters). Mirrors the column set the Neptune index grid shows.

## When to Use

Run `/export-wqmp-gdb <jurisdiction>` (e.g. `/export-wqmp-gdb Anaheim`, `/export-wqmp-gdb "City of Tustin"`) when an external client asks for WQMP boundaries for a specific city. The deliverable goes to `C:/Users/<user>/Downloads/`.

## Required Tools (already present on dev machines)

- `sqlcmd` at `C:/Program Files/Microsoft SQL Server/Client SDK/ODBC/170/Tools/Binn/SQLCMD.EXE`
- `ogr2ogr` and `ogrinfo` at `C:/Program Files/QGIS Chugiak/bin/` — this is the **only** locally-available GDAL build with FileGDB **write** support (the system GDAL 2.3.1 is read-only for FileGDB).
- `GDAL_DATA` must be set to `C:/Program Files/QGIS Chugiak/share/gdal` so EPSG codes resolve.

## Steps

1. **Resolve the jurisdiction.** The user gives a name; look it up case-insensitively. Confirm with the user before proceeding if there is more than one match.
   ```sql
   SELECT sj.StormwaterJurisdictionID, o.OrganizationName
   FROM dbo.StormwaterJurisdiction sj
   JOIN dbo.Organization o ON sj.OrganizationID = o.OrganizationID
   WHERE o.OrganizationName LIKE '%<input>%';
   ```
   Capture both the integer ID and a short label (strip a leading "City of " / "County of " / "Town of ") for the output filename.

2. **Pre-flight: report counts so the user can sanity-check before the export runs.** Sparse coverage (lots of null geometries) is normal for some jurisdictions; flag it but continue.
   ```sql
   SELECT COUNT(*) AS Total,
          SUM(CASE WHEN b.GeometryNative IS NULL THEN 1 ELSE 0 END) AS NullGeom
   FROM dbo.WaterQualityManagementPlan w
   LEFT JOIN dbo.WaterQualityManagementPlanBoundary b
       ON w.WaterQualityManagementPlanID = b.WaterQualityManagementPlanID
   WHERE w.StormwaterJurisdictionID = <id>;
   ```

3. **Create the transient export view** by piping the DDL below to sqlcmd. Substitute `<JURISDICTION_ID>` with the integer from step 1.

   Note on invocation: pass the SQL via **stdin pipe** (`cat … | sqlcmd …` or a heredoc). `sqlcmd -i <path>` triggers a Git Bash path-mangling bug that makes sqlcmd think the path is a credential flag.

   ```sql
   IF OBJECT_ID('dbo.vWQMPGdbExport', 'V') IS NOT NULL DROP VIEW dbo.vWQMPGdbExport;
   GO

   CREATE VIEW dbo.vWQMPGdbExport AS
   SELECT
       v.WaterQualityManagementPlanID                          AS WQMPID,
       v.WaterQualityManagementPlanName                        AS Name,
       v.StormwaterJurisdictionName                            AS Jurisdict,
       v.WaterQualityManagementPlanPriorityDisplayName         AS Priority,
       v.WaterQualityManagementPlanStatusDisplayName           AS Status,
       v.WaterQualityManagementPlanDevelopmentTypeDisplayName  AS DevType,
       v.WaterQualityManagementPlanLandUseDisplayName          AS LandUse,
       v.WaterQualityManagementPlanPermitTermDisplayName       AS PermitTerm,
       v.ApprovalDate                                          AS ApprovDate,
       v.DateOfConstruction                                    AS ConstrDate,
       v.HydromodificationAppliesTypeDisplayName               AS HydromodCtrl,
       v.HydrologicSubareaName                                 AS HydroSubarea,
       v.MaintenanceContactOrganization                        AS MaintOrg,
       v.MaintenanceContactName                                AS MaintName,
       LTRIM(RTRIM(
           ISNULL(v.MaintenanceContactAddress1, '') +
           CASE WHEN v.MaintenanceContactAddress2 IS NOT NULL AND v.MaintenanceContactAddress2 <> ''
                THEN ', ' + v.MaintenanceContactAddress2 ELSE '' END +
           CASE WHEN v.MaintenanceContactCity IS NOT NULL AND v.MaintenanceContactCity <> ''
                THEN ', ' + v.MaintenanceContactCity ELSE '' END +
           CASE WHEN v.MaintenanceContactState IS NOT NULL AND v.MaintenanceContactState <> ''
                THEN ', ' + v.MaintenanceContactState ELSE '' END +
           CASE WHEN v.MaintenanceContactZip IS NOT NULL AND v.MaintenanceContactZip <> ''
                THEN ' ' + v.MaintenanceContactZip ELSE '' END
       ))                                                      AS MaintAddr,
       v.MaintenanceContactPhone                               AS MaintPhone,
       v.TreatmentBMPCount                                     AS InvBMPCnt,
       v.QuickBMPCount                                         AS SimpBMPCnt,
       v.WaterQualityManagementPlanModelingApproachDisplayName AS ModelApp,
       v.DocumentCount                                         AS DocCount,
       CAST(v.HasRequiredDocuments AS int)                     AS HasReqDocs,
       v.RecordNumber                                          AS RecordNo,
       CAST(v.RecordedWQMPAreaInAcres AS float)                AS RecAcres,
       CAST(v.CalculatedWQMPAcreage AS float)                  AS CalcAcres,
       v.AssociatedAPNs                                        AS APNs,
       v.VerificationDate                                      AS OMVerify,
       v.TrashCaptureStatusTypeDisplayName                     AS TCStatus,
       v.TrashCaptureEffectiveness                             AS TCEffPct,
       b.GeometryNative                                        AS GeometryNative
   FROM dbo.vWaterQualityManagementPlanDetailed v
   LEFT JOIN dbo.WaterQualityManagementPlanBoundary b
       ON v.WaterQualityManagementPlanID = b.WaterQualityManagementPlanID
   WHERE v.StormwaterJurisdictionID = <JURISDICTION_ID>;
   GO
   ```

4. **Run ogr2ogr.** Two MSSQL config flags are essential:
   - `MSSQLSPATIAL_LIST_ALL_TABLES=YES` — without this, the driver only sees layers registered in `dbo.geometry_columns`, and the transient view is not registered.
   - `MSSQLSPATIAL_USE_GEOMETRY_COLUMNS=NO` — bypass the metadata-table lookup so the view's geometry column is detected via `sys.columns`.

   `-nlt MULTIPOLYGON` is required because the view contains a mix of Polygon and MultiPolygon rows; FileGDB layers must declare a single geometry type. (`-nlt PROMOTE_TO_MULTI` is not supported in this old GDAL 1.11.) Setting `-nlt MULTIPOLYGON` makes ogr2ogr wrap the Polygons.

   ```bash
   export GDAL_DATA="C:/Program Files/QGIS Chugiak/share/gdal"
   GDB="C:/Users/<user>/Downloads/WaterQualityManagementPlans_<ShortName>.gdb"
   rm -rf "$GDB"
   "C:/Program Files/QGIS Chugiak/bin/ogr2ogr.exe" -f "FileGDB" "$GDB" \
       --config MSSQLSPATIAL_LIST_ALL_TABLES YES \
       --config MSSQLSPATIAL_USE_GEOMETRY_COLUMNS NO \
       "MSSQL:server=.;database=NeptuneDB;trusted_connection=yes" \
       -a_srs EPSG:2771 \
       -nlt MULTIPOLYGON \
       -nln "WaterQualityManagementPlans_<ShortName>" \
       dbo.vWQMPGdbExport
   ```

5. **Verify** with `ogrinfo -so` and confirm Feature Count, geometry type (Multi Polygon), and that the SRS WKT ends with `AUTHORITY["EPSG","2771"]`. Spot-check a feature with `-where "WQMPID=<some id>"` to confirm a Name and APNs come through.

6. **Drop the view** to keep the dev DB clean:
   ```sql
   DROP VIEW dbo.vWQMPGdbExport;
   ```

7. **Report** to the user: gdb path, total feature count, null-geometry count, extent, SRID. Then offer to produce a transmittal summary (template below).

## Transmittal summary template

```
Water Quality Management Plans — <Jurisdiction>
File Geodatabase: WaterQualityManagementPlans_<ShortName>.gdb
Feature class:    WaterQualityManagementPlans_<ShortName>

Coverage
<Total> WQMP records, exported from the OC Stormwater Tools (Neptune) database.
<WithGeom> have polygon boundaries; the remaining <NullGeom> have no geometry
assigned in the source system and are included as attribute-only rows for completeness.

Geometry
Multi-polygon. Coordinates preserved in the source projection — NAD83(HARN) /
California State Plane Zone 6, meters (EPSG:2771). No reprojection has been applied.
For the same projection in US Survey feet, use EPSG:2230.

Source
Exported from dbo.vWaterQualityManagementPlanDetailed joined to
dbo.WaterQualityManagementPlanBoundary.GeometryNative on <DATE>. Attribute
formatting matches the WQMP index grid in the OC Stormwater Tools application.
```

The 28-field schema lives at `C:/Users/<user>/Downloads/WaterQualityManagementPlans_Tustin_fields.csv` (generated during the first export — copy and rename per delivery if helpful).

## Notes / Gotchas

- **WFS is not the right path** for bulk export. Geoserver's GML encoder hangs on this dataset over QGIS, and the WFS view (`vGeoServerWaterQualityManagementPlan`) doesn't expose `WaterQualityManagementPlanName`. The Neptune Leaflet map gets away with it because it requests GeoJSON with a viewport BBOX — different traffic profile than a full layer pull.
- **Don't reproject.** Clients ask for spatial data because they want to do their own analysis; reprojecting from the source projection introduces avoidable error. EPSG:2771 is the SRID stored in `dbo.WaterQualityManagementPlanBoundary.GeometryNative`. Only convert to feet (EPSG:2230) on explicit request.
- **The view + config-bypass approach is a workaround** for two GDAL limitations: (a) MSSQL `-sql` queries return geometry as a binary blob in this GDAL version (no auto-detect), and (b) the `geometry_columns` registry doesn't list WQMP boundaries. Reading a transient view as a "table" sidesteps both.
- **Output filename convention:** `WaterQualityManagementPlans_<ShortName>.gdb`, where ShortName is the jurisdiction with leading "City of " / "County of " / "Town of " stripped (e.g., `Tustin`, `Anaheim`, `Orange`, `OrangeCounty`).
- **If the user wants this in feet,** add `-t_srs EPSG:2230` to the ogr2ogr command (note: this *does* reproject, so prefer native unless asked).
- **Long-term direction:** this should become a self-service "Download to GIS" page on the WQMP index, modeled on `Neptune.WebMvc/Views/TreatmentBMP/DownloadBMPsToGIS.cshtml`, calling through `Neptune.GDALAPI`. Until then, this skill is the supported path.
