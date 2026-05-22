Create View dbo.vMaintenanceRecordDetailed
as

with mrovs(MaintenanceRecordID, CustomAttributeTypeID, CustomAttributeTypeName, ObservationValues)
as
(
    select mr.MaintenanceRecordID, mr.CustomAttributeTypeID, cat.CustomAttributeTypeName, STRING_AGG(mrv.ObservationValue, ', ') as ObservationValues
    from dbo.MaintenanceRecordObservation mr
    join dbo.MaintenanceRecordObservationValue mrv on mr.MaintenanceRecordObservationID = mrv.MaintenanceRecordObservationID
    join dbo.CustomAttributeType cat on mr.CustomAttributeTypeID = cat.CustomAttributeTypeID
    where datalength(mrv.ObservationValue) > 0
    group by mr.MaintenanceRecordID, mr.CustomAttributeTypeID, cat.CustomAttributeTypeName
)

select  mr.MaintenanceRecordID, mr.MaintenanceRecordDescription, mr.MaintenanceRecordTypeID, mrt.MaintenanceRecordTypeDisplayName,
        bmp.TreatmentBMPID, bmp.TreatmentBMPTypeID, bmp.TreatmentBMPName, fv.VisitDate, fv.IsFieldVisitVerified,
        fv.FieldVisitID, fv.PerformedByPersonID, fvp.FirstName + ' ' + fvp.LastName as PerformedByPersonName,
        sj.StormwaterJurisdictionID, o.OrganizationName as StormwaterJurisdictionName, sj.StormwaterJurisdictionPublicWQMPVisibilityTypeID,
        wqmp.WaterQualityManagementPlanID, wqmp.WaterQualityManagementPlanName,
        isnull(cat78.ObservationValues, 'n/a') as [Structural Repair Conducted],
        isnull(cat79.ObservationValues, 'n/a') as [Mechanical Repair Conducted],
        isnull(cat80.ObservationValues, 'n/a') as [Infiltration Surface Restored],
        isnull(cat81.ObservationValues, 'n/a') as [Filtration Surface Restored],
        isnull(cat82.ObservationValues, 'n/a') as [Media Replaced],
        isnull(cat83.ObservationValues, 'n/a') as [Mulch Added],
        isnull(cat84.ObservationValues, 'n/a') as [Percent Trash],
        isnull(cat85.ObservationValues, 'n/a') as [Percent Green Waste],
        isnull(cat86.ObservationValues, 'n/a') as [Percent Sediment],
        isnull(cat87.ObservationValues, 'n/a') as [Area Reseeded],
        isnull(cat88.ObservationValues, 'n/a') as [Vegetation Planted],
        isnull(cat89.ObservationValues, 'n/a') as [Surface and Bank Erosion Repaired],
        isnull(cat107.ObservationValues, 'n/a') as [Total Material Volume Removed (cu-ft)],
        isnull(cat108.ObservationValues, 'n/a') as [Total Material Volume Removed (gal)]
        
from dbo.MaintenanceRecord mr
left join dbo.MaintenanceRecordType mrt on mr.MaintenanceRecordTypeID = mrt.MaintenanceRecordTypeID
join dbo.TreatmentBMP bmp on mr.TreatmentBMPID = bmp.TreatmentBMPID
join dbo.FieldVisit fv on mr.FieldVisitID = fv.FieldVisitID
join dbo.Person fvp on fv.PerformedByPersonID = fvp.PersonID
join dbo.StormwaterJurisdiction sj on bmp.StormwaterJurisdictionID = sj.StormwaterJurisdictionID
join dbo.Organization o on sj.OrganizationID = o.OrganizationID
left join dbo.WaterQualityManagementPlan wqmp on bmp.WaterQualityManagementPlanID = wqmp.WaterQualityManagementPlanID
left join mrovs cat78 on mr.MaintenanceRecordID = cat78.MaintenanceRecordID and cat78.CustomAttributeTypeID = 78
left join mrovs cat79 on mr.MaintenanceRecordID = cat79.MaintenanceRecordID and cat79.CustomAttributeTypeID = 79
left join mrovs cat80 on mr.MaintenanceRecordID = cat80.MaintenanceRecordID and cat80.CustomAttributeTypeID = 80
left join mrovs cat81 on mr.MaintenanceRecordID = cat81.MaintenanceRecordID and cat81.CustomAttributeTypeID = 81
left join mrovs cat82 on mr.MaintenanceRecordID = cat82.MaintenanceRecordID and cat82.CustomAttributeTypeID = 82
left join mrovs cat83 on mr.MaintenanceRecordID = cat83.MaintenanceRecordID and cat83.CustomAttributeTypeID = 83
left join mrovs cat84 on mr.MaintenanceRecordID = cat84.MaintenanceRecordID and cat84.CustomAttributeTypeID = 84
left join mrovs cat85 on mr.MaintenanceRecordID = cat85.MaintenanceRecordID and cat85.CustomAttributeTypeID = 85
left join mrovs cat86 on mr.MaintenanceRecordID = cat86.MaintenanceRecordID and cat86.CustomAttributeTypeID = 86
left join mrovs cat87 on mr.MaintenanceRecordID = cat87.MaintenanceRecordID and cat87.CustomAttributeTypeID = 87
left join mrovs cat88 on mr.MaintenanceRecordID = cat88.MaintenanceRecordID and cat88.CustomAttributeTypeID = 88
left join mrovs cat89 on mr.MaintenanceRecordID = cat89.MaintenanceRecordID and cat89.CustomAttributeTypeID = 89
left join mrovs cat107 on mr.MaintenanceRecordID = cat107.MaintenanceRecordID and cat107.CustomAttributeTypeID = 107
left join mrovs cat108 on mr.MaintenanceRecordID = cat108.MaintenanceRecordID and cat108.CustomAttributeTypeID = 108

GO