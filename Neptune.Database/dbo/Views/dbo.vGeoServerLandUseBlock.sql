create view  dbo.vGeoServerLandUseBlock
as
select
	-- Synthetic, stable, unique surrogate key for GeoServer's gt_pk_metadata entry. Multiplying
	-- avoids any collision with raw LandUseBlockIDs if GeoServer's auto-detection ever flips
	-- which column it treats as the FID.
	2 * LandUseBlockID - 1 as PrimaryKey,
	LandUseBlockID,
	PriorityLandUseTypeID,
	LandUseDescription,
	PermitTypeID,
	StormwaterJurisdictionID,
	LandUseBlockGeometry4326 as LandUseBlockGeometry
from dbo.LandUseBlock
GO
