-- NPT-1155: one row for each pair of WQMP boundaries that overlap, for the WQMP Boundary Overlap
-- Report. Only boundaries that are Load Generating Unit inputs are compared (Active, not Detailed;
-- see vPyQgisWaterQualityManagementPlanLGUInput), because those are the ones that put the same
-- ground into the model more than once. The lower WQMP ID is always the first of the pair.
-- The 200 square meter (0.05 acre) floor matches the delineation overlap check in
-- pDelineationMarkThoseThatHaveDiscrepancies.
create view dbo.vWaterQualityManagementPlanBoundaryOverlap
as
with lguInput (WaterQualityManagementPlanID, Boundary)
as
(
	select WQMPID, WaterQualityManagementPlanBoundary.MakeValid()
	from dbo.vPyQgisWaterQualityManagementPlanLGUInput
)
select
	row_number() over(order by overlap.WaterQualityManagementPlanID, overlap.OverlappingWaterQualityManagementPlanID) as PrimaryKey,
	overlap.WaterQualityManagementPlanID,
	wqmp.WaterQualityManagementPlanName,
	wqmp.StormwaterJurisdictionID,
	o.OrganizationName as StormwaterJurisdictionName,
	overlap.OverlappingWaterQualityManagementPlanID,
	overlappingWqmp.WaterQualityManagementPlanName as OverlappingWaterQualityManagementPlanName,
	overlappingWqmp.StormwaterJurisdictionID as OverlappingStormwaterJurisdictionID,
	overlappingO.OrganizationName as OverlappingStormwaterJurisdictionName,
	overlap.OverlapAreaInSquareMeters
from
(
	-- intentionally an inner query: the intersection is only computed for pairs that intersect
	select a.WaterQualityManagementPlanID, b.WaterQualityManagementPlanID as OverlappingWaterQualityManagementPlanID,
		a.Boundary.STIntersection(b.Boundary).STArea() as OverlapAreaInSquareMeters
	from lguInput a
	join lguInput b on a.WaterQualityManagementPlanID < b.WaterQualityManagementPlanID and a.Boundary.STIntersects(b.Boundary) = 1
) overlap
join dbo.WaterQualityManagementPlan wqmp on overlap.WaterQualityManagementPlanID = wqmp.WaterQualityManagementPlanID
join dbo.StormwaterJurisdiction sj on wqmp.StormwaterJurisdictionID = sj.StormwaterJurisdictionID
join dbo.Organization o on sj.OrganizationID = o.OrganizationID
join dbo.WaterQualityManagementPlan overlappingWqmp on overlap.OverlappingWaterQualityManagementPlanID = overlappingWqmp.WaterQualityManagementPlanID
join dbo.StormwaterJurisdiction overlappingSj on overlappingWqmp.StormwaterJurisdictionID = overlappingSj.StormwaterJurisdictionID
join dbo.Organization overlappingO on overlappingSj.OrganizationID = overlappingO.OrganizationID
where overlap.OverlapAreaInSquareMeters > 200
GO
