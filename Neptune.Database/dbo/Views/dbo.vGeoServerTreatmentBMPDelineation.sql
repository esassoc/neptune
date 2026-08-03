Create View dbo.vGeoServerTreatmentBMPDelineation as
Select
	d.DelineationID as PrimaryKey,
	-- NPT-1115: repair SQL-invalid geometry on read so GeoServer's WMS never throws error 24144
	-- (see vGeoServerDelineation). Valid polygons pass through untouched.
	case when DelineationGeometry4326.STIsValid() = 1 then DelineationGeometry4326 else DelineationGeometry4326.MakeValid() end as DelineationGeometry,
	t.TreatmentBMPID,
	t.TreatmentBMPName,
	p.ProjectName
from
	dbo.Delineation d join dbo.DelineationType dt on d.DelineationTypeID = dt.DelineationTypeID
	join dbo.TreatmentBMP t on d.TreatmentBMPID = t.TreatmentBMPID
	join dbo.Project p on t.ProjectID = p.ProjectID
where t.ProjectID is not null