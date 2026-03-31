Create View dbo.vWaterQualityManagementPlanDetailed
as

with wqmpv(WaterQualityManagementPlanID, WaterQualityManagementPlanVerifyID, LastEditedDate, RankNumber)
as
(
	select WaterQualityManagementPlanID, WaterQualityManagementPlanVerifyID, LastEditedDate, 
    rank() over (partition by WaterQualityManagementPlanID order by LastEditedDate desc, WaterQualityManagementPlanVerifyID desc) as RankNumber
	from dbo.WaterQualityManagementPlanVerify
)

select	wqmp.WaterQualityManagementPlanID, wqmp.WaterQualityManagementPlanName, 
        wqmp.StormwaterJurisdictionID, o.OrganizationName as StormwaterJurisdictionName, sj.StormwaterJurisdictionPublicWQMPVisibilityTypeID,
        wqmp.ApprovalDate, wqmp.DateOfConstruction, wqmp.RecordNumber,
        wqmplu.WaterQualityManagementPlanLandUseID, wqmplu.WaterQualityManagementPlanLandUseDisplayName,
        wqmppr.WaterQualityManagementPlanPriorityID, wqmppr.WaterQualityManagementPlanPriorityDisplayName,
        wqmps.WaterQualityManagementPlanStatusID, wqmps.WaterQualityManagementPlanStatusDisplayName,
        wqmpdt.WaterQualityManagementPlanDevelopmentTypeID, wqmpdt.WaterQualityManagementPlanDevelopmentTypeDisplayName,
        wqmp.MaintenanceContactName, wqmp.MaintenanceContactOrganization, wqmp.MaintenanceContactPhone, wqmp.MaintenanceContactAddress1, wqmp.MaintenanceContactAddress2, wqmp.MaintenanceContactCity, wqmp.MaintenanceContactState, wqmp.MaintenanceContactZip, 
        wqmppt.WaterQualityManagementPlanPermitTermID, wqmppt.WaterQualityManagementPlanPermitTermDisplayName,
        hat.HydromodificationAppliesTypeID, hat.HydromodificationAppliesTypeDisplayName, hs.HydrologicSubareaID, hs.HydrologicSubareaName,
        wqmp.RecordedWQMPAreaInAcres, isnull(wqmpb.GeometryNative.STArea(), 0) * 0.000247105 as CalculatedWQMPAcreage,
        tst.TrashCaptureStatusTypeID, tst.TrashCaptureStatusTypeDisplayName, wqmp.TrashCaptureEffectiveness, 
        wqmpma.WaterQualityManagementPlanModelingApproachID, wqmpma.WaterQualityManagementPlanModelingApproachDisplayName,
        isnull(tbmp.TreatmentBMPCount, 0) as TreatmentBMPCount, isnull(qbmp.QuickBMPCount, 0) as QuickBMPCount, isnull(scbmp.SourceControlBMPCount, 0) as SourceControlBMPCount, isnull(wqmpdoc.DocumentCount, 0) as DocumentCount,
        isnull(parcels.AssociatedAPNs, '') as AssociatedAPNs,
        cast(isnull(case when docTypeReq.RequiredDocumentCount = reqDocCount.RequiredDocumentCount then 1 else 0 end, 0) as bit) as HasRequiredDocuments,
        wqmpv.WaterQualityManagementPlanVerifyID, wqmpv.LastEditedDate as VerificationDate,
        cast(case when wqmpb.WaterQualityManagementPlanID is not null then 1 else 0 end as bit) as HasBoundary
from dbo.WaterQualityManagementPlan wqmp
join dbo.StormwaterJurisdiction sj on wqmp.StormwaterJurisdictionID = sj.StormwaterJurisdictionID
join dbo.Organization o on sj.OrganizationID = o.OrganizationID
left join dbo.WaterQualityManagementPlanDevelopmentType wqmpdt on wqmp.WaterQualityManagementPlanDevelopmentTypeID = wqmpdt.WaterQualityManagementPlanDevelopmentTypeID
left join dbo.WaterQualityManagementPlanLandUse wqmplu on wqmp.WaterQualityManagementPlanLandUseID = wqmplu.WaterQualityManagementPlanLandUseID
left join dbo.WaterQualityManagementPlanModelingApproach wqmpma on wqmp.WaterQualityManagementPlanModelingApproachID = wqmpma.WaterQualityManagementPlanModelingApproachID
left join dbo.WaterQualityManagementPlanPermitTerm wqmppt on wqmp.WaterQualityManagementPlanPermitTermID = wqmppt.WaterQualityManagementPlanPermitTermID
left join dbo.WaterQualityManagementPlanPriority wqmppr on wqmp.WaterQualityManagementPlanPriorityID = wqmppr.WaterQualityManagementPlanPriorityID
left join dbo.WaterQualityManagementPlanStatus wqmps on wqmp.WaterQualityManagementPlanStatusID = wqmps.WaterQualityManagementPlanStatusID
left join dbo.TrashCaptureStatusType tst on wqmp.TrashCaptureStatusTypeID = tst.TrashCaptureStatusTypeID
left join dbo.HydrologicSubarea hs on wqmp.HydrologicSubareaID = hs.HydrologicSubareaID
left join dbo.HydromodificationAppliesType hat on wqmp.HydromodificationAppliesTypeID = hat.HydromodificationAppliesTypeID
left join dbo.WaterQualityManagementPlanBoundary wqmpb on wqmp.WaterQualityManagementPlanID = wqmpb.WaterQualityManagementPlanID
left join
(
	select tb.WaterQualityManagementPlanID, count(*) as TreatmentBMPCount
	from dbo.TreatmentBMP tb
    where tb.WaterQualityManagementPlanID is not null
	group by tb.WaterQualityManagementPlanID
) tbmp on wqmp.WaterQualityManagementPlanID = tbmp.WaterQualityManagementPlanID
left join
(
	select WaterQualityManagementPlanID, sum(NumberOfIndividualBMPs) as QuickBMPCount
	from dbo.QuickBMP
    where WaterQualityManagementPlanID is not null
	group by WaterQualityManagementPlanID
) qbmp on wqmp.WaterQualityManagementPlanID = qbmp.WaterQualityManagementPlanID
left join
(
	select WaterQualityManagementPlanID, count(*) as SourceControlBMPCount
	from dbo.SourceControlBMP
    where WaterQualityManagementPlanID is not null
	group by WaterQualityManagementPlanID
) scbmp on wqmp.WaterQualityManagementPlanID = scbmp.WaterQualityManagementPlanID
left join
(
	select WaterQualityManagementPlanID, count(*) as DocumentCount
	from dbo.WaterQualityManagementPlanDocument
    where WaterQualityManagementPlanID is not null
	group by WaterQualityManagementPlanID
) wqmpdoc on wqmp.WaterQualityManagementPlanID = wqmpdoc.WaterQualityManagementPlanID
left join (
    select WaterQualityManagementPlanID, STRING_AGG(CAST(ParcelNumber as varchar(MAX)), ', ') WITHIN GROUP (ORDER BY ParcelNumber ASC) as AssociatedAPNs
    from (
        select distinct WaterQualityManagementPlanID, p.ParcelNumber 
        from dbo.WaterQualityManagementPlanParcel wqmpp 
        join dbo.Parcel p on wqmpp.ParcelID = p.ParcelID
    ) b 
    group by b.WaterQualityManagementPlanID
) parcels on wqmp.WaterQualityManagementPlanID = parcels.WaterQualityManagementPlanID
left join
(
    select WaterQualityManagementPlanID, count(distinct wqmpd.WaterQualityManagementPlanDocumentTypeID) as RequiredDocumentCount
    from dbo.WaterQualityManagementPlanDocument wqmpd
    join dbo.WaterQualityManagementPlanDocumentType wqmpdt on wqmpd.WaterQualityManagementPlanDocumentTypeID = wqmpdt.WaterQualityManagementPlanDocumentTypeID and wqmpdt.IsRequired = 1
    group by wqmpd.WaterQualityManagementPlanID
) reqDocCount on wqmp.WaterQualityManagementPlanID = reqDocCount.WaterQualityManagementPlanID
left join wqmpv on wqmp.WaterQualityManagementPlanID = wqmpv.WaterQualityManagementPlanID and wqmpv.RankNumber = 1
cross join 
(
    select count(IsRequired) as RequiredDocumentCount
    from dbo.WaterQualityManagementPlanDocumentType
    where IsRequired = 1
) docTypeReq
GO