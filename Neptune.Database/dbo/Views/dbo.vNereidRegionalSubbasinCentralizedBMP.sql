create view dbo.vNereidRegionalSubbasinCentralizedBMP
As
Select
	rsb.RegionalSubbasinID as PrimaryKey,
	rsb.RegionalSubbasinID,
	rsb.OCSurveyCatchmentID,
	bmp.TreatmentBMPID,
	bmp.UpstreamBMPID,
	ROW_NUMBER() over (partition by rsb.RegionalSubbasinID order by d.DelineationID) as RowNumber
from dbo.Delineation d
	join dbo.TreatmentBMP bmp
		on d.TreatmentBMPID = bmp.TreatmentBMPID
	join dbo.RegionalSubbasin rsb
		on bmp.RegionalSubbasinID = rsb.RegionalSubbasinID
where
	DelineationTypeID = 1 
	and rsb.IsInModelBasin = 1 
	and bmp.ProjectID is null
	and d.IsVerified = 1 -- provisional delineations are not modeled (NPT-1155)
GO
