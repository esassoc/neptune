-- NPT-1126 rework: how much of each Land Use Block lies in OVTA Areas with at least 1, 2 and 3 completed
-- assessments, for the OVTA-Based Results "Acres of Land Use Type by # of Assessments" view.
--
-- Deliberately does not go through TrashGeneratingUnit: the overlay pipeline only links a TGU to an OVTA Area
-- with 2 or more assessments (vPyQgisOnlandVisualTrashAssessmentAreaDated), so land assessed once would be
-- reported as not yet assessed.
--
-- Overlapping OVTA Areas are unioned per tier before intersecting, so overlapped land counts once, at the highest
-- assessment count among the areas covering it. Areas are SRID 2771 (metres); the areas are square metres.
-- Invalid OVTA Area geometry is skipped, as in vGeoServerOnlandVisualTrashAssessmentArea (NPT-1099). Invalid Land
-- Use Block geometry is repaired with MakeValid instead, because the block still counts in Total Mapped Land Use and
-- skipping it would report covered land as not yet assessed.
-- No spatial index hint: forcing SPATIAL_OnlandVisualTrashAssessmentArea_OnlandVisualTrashAssessmentAreaGeometry made
-- Irvine about 4x slower locally than the jurisdiction seek the optimizer picks.
Create View dbo.vLandUseBlockOVTAAssessmentCoverage
as
With OnlandVisualTrashAssessmentAreaCompletedCount as
(
	Select
		a.OnlandVisualTrashAssessmentAreaID,
		a.StormwaterJurisdictionID,
		a.OnlandVisualTrashAssessmentAreaGeometry,
		(
			Select count(*)
			From dbo.OnlandVisualTrashAssessment o
			Where o.OnlandVisualTrashAssessmentAreaID = a.OnlandVisualTrashAssessmentAreaID
				and o.OnlandVisualTrashAssessmentStatusID = 2 -- Complete
		) as CompletedAssessmentCount
	From dbo.OnlandVisualTrashAssessmentArea a
	Where a.OnlandVisualTrashAssessmentAreaGeometry.STIsValid() = 1
)
Select
	l.LandUseBlockID,
	l.StormwaterJurisdictionID,
	l.PriorityLandUseTypeID,
	l.LandUseForTGR,
	l.PermitTypeID,
	isnull(b.LandUseBlockGeometry.STIntersection(t.OnceOrMoreUnion).STArea(), 0) as AreaAssessedOnceOrMore,
	isnull(b.LandUseBlockGeometry.STIntersection(t.TwiceOrMoreUnion).STArea(), 0) as AreaAssessedTwiceOrMore,
	isnull(b.LandUseBlockGeometry.STIntersection(t.ThreeOrMoreUnion).STArea(), 0) as AreaAssessedThreeOrMore
From dbo.LandUseBlock l
	cross apply
	(
		Select case when l.LandUseBlockGeometry.STIsValid() = 1 then l.LandUseBlockGeometry else l.LandUseBlockGeometry.MakeValid() end as LandUseBlockGeometry
	) b
	outer apply
	(
		Select
			geometry::UnionAggregate(case when c.CompletedAssessmentCount >= 1 then c.OnlandVisualTrashAssessmentAreaGeometry end) as OnceOrMoreUnion,
			geometry::UnionAggregate(case when c.CompletedAssessmentCount >= 2 then c.OnlandVisualTrashAssessmentAreaGeometry end) as TwiceOrMoreUnion,
			geometry::UnionAggregate(case when c.CompletedAssessmentCount >= 3 then c.OnlandVisualTrashAssessmentAreaGeometry end) as ThreeOrMoreUnion
		From OnlandVisualTrashAssessmentAreaCompletedCount c
		Where c.StormwaterJurisdictionID = l.StormwaterJurisdictionID
			and c.OnlandVisualTrashAssessmentAreaGeometry.STIntersects(b.LandUseBlockGeometry) = 1
	) t
Go

/* select * from dbo.vLandUseBlockOVTAAssessmentCoverage where StormwaterJurisdictionID = 30 and PermitTypeID = 1 */
