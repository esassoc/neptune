-- NPT-1128 rework: Land Use Blocks that genuinely overlap each OVTA Area, for the OVTA Area grid and GDB export
-- (Land Use Type / Land Use Block ID columns). Deliberately does not go through TrashGeneratingUnit, which is
-- filtered to Phase I MS4 blocks and only refreshed by the overlay pipeline.
--
-- Scoped to the same jurisdiction. The STArea() > 10 m^2 check drops shared-edge neighbours: OVTA Areas are
-- usually unions of Land Use Blocks, so every adjacent block "intersects" on its boundary. Both geometries are
-- SRID 2771 (metres).
--
-- The INDEX hint matters. Without it the optimizer evaluates STIntersects row by row instead of seeking the
-- spatial index (about 14s vs 3s across all jurisdictions on a full dataset); EF Core cannot emit table hints,
-- which is why this lives in a view rather than LINQ. See docs/ovta-area-land-use-spatial-join.md.
Create View dbo.vOnlandVisualTrashAssessmentAreaLandUseBlock
as
Select
	-- synthetic key for the keyless EF entity; bigint so the composite never overflows int
	cast(cast(a.OnlandVisualTrashAssessmentAreaID as bigint) * 10000000000 + l.LandUseBlockID as bigint) as PrimaryKey,
	a.StormwaterJurisdictionID,
	a.OnlandVisualTrashAssessmentAreaID,
	l.LandUseBlockID,
	l.PriorityLandUseTypeID
From dbo.OnlandVisualTrashAssessmentArea a
	inner join dbo.LandUseBlock l with (index(SPATIAL_LandUseBlock_LandUseBlockGeometry))
		on l.StormwaterJurisdictionID = a.StormwaterJurisdictionID
Where l.LandUseBlockGeometry.STIntersects(a.OnlandVisualTrashAssessmentAreaGeometry) = 1
	and l.LandUseBlockGeometry.STIntersection(a.OnlandVisualTrashAssessmentAreaGeometry).STArea() > 10
Go

/* select * from dbo.vOnlandVisualTrashAssessmentAreaLandUseBlock where StormwaterJurisdictionID = 3 */
