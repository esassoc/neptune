Create view dbo.vPyQgisDelineationLGUInput
as
Select
	DelineationID as DelinID,
	DelineationGeometry
from
	dbo.Delineation d join dbo.TreatmentBMP t
		on d.TreatmentBMPID = t.TreatmentBMPID
	join dbo.TreatmentBMPType ttype
		on ttype.TreatmentBMPTypeID = t.TreatmentBMPTypeID
Where d.DelineationTypeID = 2  -- distributed
 and d.IsVerified = 1 -- provisional delineations are not modeled, so they do not take ground from the layers below (NPT-1155)
 and ttype.TreatmentBMPModelingTypeID is not null  --only include modeling BMPs
 and t.ProjectID is null --don't include project treatment bmps
GO