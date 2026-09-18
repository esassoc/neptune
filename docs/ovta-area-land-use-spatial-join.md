# OVTA Area → Land Use Block spatial join: why it is a database view

**Applies to:** `dbo.vOnlandVisualTrashAssessmentAreaLandUseBlock` (`Neptune.Database/dbo/Views/`) and its reader `LandUseBlocks.ListByOnlandVisualTrashAssessmentAreaID` in `Neptune.EFModels/Entities/LandUseBlocks.cs`
**Introduced in:** NPT-1128 rework (PR #678, September 2026)
**Consumers:** the OVTA Area grid (`OnlandVisualTrashAssessmentAreaController.List`) and the OVTA Area GDB download (`OnlandVisualTrashAssessmentAreaGdbExport`)

## What the query does

The OVTA Area grid and GDB export show two land-use columns per area: **Land Use Type** and **Land Use Block ID**, each comma separated when an area spans several blocks. The PO asked for these to come from a live GIS operation against Land Use Blocks, not from `TrashGeneratingUnit`, because TGUs are filtered to Phase I MS4 blocks and are only refreshed by the overlay pipeline, so they can be stale or incomplete.

So for every OVTA Area we need the Land Use Blocks, in the same jurisdiction, whose geometry genuinely overlaps it. Two predicates:

1. `LandUseBlockGeometry.STIntersects(OnlandVisualTrashAssessmentAreaGeometry) = 1`, the candidate filter.
2. `STIntersection(...).STArea() > 10` square metres, which drops blocks that only share an edge. OVTA Areas are usually unions of Land Use Blocks, so every adjacent block "intersects" on its boundary and would otherwise be listed.

Both geometries are SRID 2771 (metres), so no reprojection is needed. On the local full dataset this is roughly 2,000 areas against 66,000 blocks.

## What went wrong with LINQ

The first implementation was ordinary EF Core LINQ:

```csharp
from area in dbContext.OnlandVisualTrashAssessmentAreas
join landUseBlock in dbContext.LandUseBlocks
    on area.StormwaterJurisdictionID equals landUseBlock.StormwaterJurisdictionID
where jurisdictionIDs.Contains(area.StormwaterJurisdictionID)
   && landUseBlock.LandUseBlockGeometry.Intersects(area.OnlandVisualTrashAssessmentAreaGeometry)
   && landUseBlock.LandUseBlockGeometry.Intersection(area.OnlandVisualTrashAssessmentAreaGeometry).Area > 10
select new { area.OnlandVisualTrashAssessmentAreaID, landUseBlock.LandUseBlockID, landUseBlock.PriorityLandUseTypeID }
```

It translated correctly to `STIntersects` and `STIntersection().STArea()` and returned the right rows. It just took about 14 seconds, which is what made the OVTA Area grid look hung during PO testing.

## The measurement

Running the equivalent T-SQL directly against a local restore of the March 2026 production BACPAC (all jurisdictions, measured September 2026):

| Variant | Pairs | Time |
|---|---|---|
| Join with `STIntersects`, no hint | 7,369 | 12.5 s |
| Same, `WITH (INDEX(SPATIAL_LandUseBlock_LandUseBlockGeometry))` | 7,369 | 2.6 s |
| Hinted, plus the `STArea() > 10` overlap check | 6,481 | 3.1 s |
| Single jurisdiction (Laguna Beach), hinted, with overlap check | 5,600 | 1.1 s |

Same predicate and the same result set in each pair of rows. The only difference is whether SQL Server used the spatial index that already exists on `LandUseBlock.LandUseBlockGeometry`.

On a second local database with more areas (9,442 pairs across 776 areas) the finished view returned all jurisdictions in about 0.8 s.

## Why the optimizer got it wrong

SQL Server's cost model for spatial predicates is weak when the geometry on the other side of the predicate comes from another table (a spatial join) rather than a constant or a variable. In that situation it often chooses a nested-loop plan that evaluates `STIntersects` on every candidate row instead of seeking the spatial index. That is exactly what happened here, and it's why the same query with a `WITH (INDEX(...))` hint is five times faster.

## Why the join lives in a view

Index hints are table-level T-SQL syntax. EF Core has no API to emit them, and there is no query tag, interceptor, or `TagWith` trick that inserts a `WITH (INDEX(...))` clause cleanly and safely. The realistic options were:

| Option | Verdict |
|---|---|
| Accept 14 s per grid load | Not acceptable. This is the symptom the PO reported. |
| Raw SQL via `Database.SqlQueryRaw<T>` with the hint | Works and was tried first, but Neptune does not use raw SQL for reads; the convention is a view. |
| Put the join in a database view that carries the hint, and read it with ordinary LINQ | Chosen. Matches how the other spatial and aggregate reads in Neptune are done (`vTrashGeneratingUnitLoadStatistic`, `vOnlandVisualTrashAssessmentAreaProgress`, the `vGeoServer*` views). |

The view `dbo.vOnlandVisualTrashAssessmentAreaLandUseBlock` exposes one row per overlapping (area, block) pair with `StormwaterJurisdictionID`, `OnlandVisualTrashAssessmentAreaID`, `LandUseBlockID`, `PriorityLandUseTypeID`, and a synthetic bigint `PrimaryKey` for the keyless EF entity. The reader filters by jurisdiction IDs in LINQ and groups the rows into a dictionary keyed by area ID. Table hints are legal inside a view definition, so the hint travels with the query wherever it is used.

Adding the view followed the normal database-first flow: the `.sql` file under `Neptune.Database/dbo/Views/`, a `<Build Include>` in `Neptune.Database.sqlproj`, `Build/DatabaseBuild.ps1` to deploy, then the EF scaffold to generate `vOnlandVisualTrashAssessmentAreaLandUseBlock.cs` and the `DbSet` on `NeptuneDbContext`.

## How it is protected

- `Neptune.Tests/LandUseBlocksOvtaAreaIntersectTests.cs` reads the view through the helper against the local database and asserts that every returned block belongs to the requested jurisdiction and that an empty jurisdiction list returns nothing.
- `Neptune.Tests/OnlandVisualTrashAssessmentAreaGridDtoTests.cs` and `OnlandVisualTrashAssessmentAreaGdbExportTests.cs` cover how the pairs are turned into the comma-separated columns.

## Known limitation and the next step

Three seconds per grid load is acceptable but not good, and production has more Land Use Blocks than the local restore. If QA or production feels sluggish, the fix is not a cleverer query. It is to precompute the area-to-block pairs into a table (or indexed view) that is refreshed when an OVTA Area's geometry or a Land Use Block changes, and read that from the grid and export instead. That should be its own card.

## Repro / benchmark

To re-check the plan choice on any database:

```sql
DECLARE @t datetime2 = SYSDATETIME();
SELECT COUNT(*)
FROM dbo.OnlandVisualTrashAssessmentArea a
JOIN dbo.LandUseBlock l /* WITH (INDEX(SPATIAL_LandUseBlock_LandUseBlockGeometry)) */
    ON l.StormwaterJurisdictionID = a.StormwaterJurisdictionID
WHERE l.LandUseBlockGeometry.STIntersects(a.OnlandVisualTrashAssessmentAreaGeometry) = 1
  AND l.LandUseBlockGeometry.STIntersection(a.OnlandVisualTrashAssessmentAreaGeometry).STArea() > 10;
SELECT DATEDIFF(ms, @t, SYSDATETIME()) AS elapsed_ms;
```

Run it once with the hint commented out and once with it in. If the two times ever converge, the hint in the view can be dropped.
