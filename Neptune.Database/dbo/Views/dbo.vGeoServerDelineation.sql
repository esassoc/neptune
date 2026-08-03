Create View dbo.vGeoServerDelineation as
Select
	2 * DelineationID - 1 as PrimaryKey,
	d.DelineationID,
	Null as WaterQualityManagementPlanID,
	DelineationGeometry4326 as DelineationGeometry,
	DelineationTypeName as DelineationType,
	t.TreatmentBMPID,
	sj.StormwaterJurisdictionID,
	t.TreatmentBMPName,
	o.OrganizationName,
	Case
		when d.IsVerified = 1 then 'Verified'
		else 'Provisional'
	end as DelineationStatus,
    tbt.IsAnalyzedInModelingModule
from
	dbo.Delineation d join dbo.DelineationType dt on d.DelineationTypeID = dt.DelineationTypeID
	join dbo.TreatmentBMP t on d.TreatmentBMPID = t.TreatmentBMPID
    join dbo.TreatmentBMPType tbt on t.TreatmentBMPTypeID = tbt.TreatmentBMPTypeID
	left join dbo.StormwaterJurisdiction sj on t.StormwaterJurisdictionID = sj.StormwaterJurisdictionID
	left join dbo.Organization o on sj.OrganizationID = o.OrganizationID
	where t.ProjectID is null
		-- NPT-1115: exclude SQL-invalid geometry rows so GeoServer's WMS/WFS render never hits SQL error
		-- 24144 (a single invalid row blanks the whole layer). STIsValid() tests validity without throwing.
		-- Invalid rows are repaired at the source (save-path MakeValid, plus the nightly pDelineationMakeValid)
		-- and reappear here once valid. Deliberately NOT MakeValid-ing on read, matching
		-- vGeoServerOnlandVisualTrashAssessmentArea (Ray's preferred pattern).
		and DelineationGeometry4326.STIsValid() = 1