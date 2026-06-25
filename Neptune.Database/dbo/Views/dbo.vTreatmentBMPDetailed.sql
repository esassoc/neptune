Create View dbo.vTreatmentBMPDetailed
as

with tbas(TreatmentBMPID, TreatmentBMPAssessmentID, FieldVisitID, VisitDate, AssessmentScore, RankNumber)
as
(
	select tba.TreatmentBMPID, tba.TreatmentBMPAssessmentID, fv.FieldVisitID, fv.VisitDate, tba.AssessmentScore, rank() over (partition by tba.TreatmentBMPID order by fv.VisitDate desc, tba.TreatmentBMPAssessmentID desc) as RankNumber
	from dbo.TreatmentBMPAssessment tba
	join dbo.FieldVisit fv on tba.FieldVisitID = fv.FieldVisitID
),
mrs(TreatmentBMPID, MaintenanceRecordID, FieldVisitID, VisitDate, RankNumber)
as
(
	select mr.TreatmentBMPID, mr.MaintenanceRecordID, fv.FieldVisitID, fv.VisitDate, rank() over (partition by mr.TreatmentBMPID order by fv.VisitDate desc, mr.MaintenanceRecordID desc) as RankNumber
	from dbo.MaintenanceRecord mr
	join dbo.FieldVisit fv on mr.FieldVisitID = fv.FieldVisitID
)

select	tb.TreatmentBMPID as PrimaryKey,
		tb.TreatmentBMPID, tb.TreatmentBMPName, tbt.TreatmentBMPTypeID, tbt.TreatmentBMPTypeName, sj.StormwaterJurisdictionID, o.OrganizationName
		, wqmp.WaterQualityManagementPlanID, wqmp.WaterQualityManagementPlanName
		, tb.RequiredFieldVisitsPerYear, tb.RequiredPostStormFieldVisitsPerYear, tb.TreatmentBMPLifespanEndDate, tb.YearBuilt, tb.Notes, tb.InventoryIsVerified
		, tb.OwnerOrganizationID, oo.OrganizationName as OwnerOrganizationName
		, tblt.TreatmentBMPLifespanTypeID, tblt.TreatmentBMPLifespanTypeDisplayName
		, tcst.TrashCaptureStatusTypeID, tcst.TrashCaptureStatusTypeDisplayName
		, sbt.SizingBasisTypeID, sbt.SizingBasisTypeDisplayName
		, dt.DelineationTypeID, dt.DelineationTypeDisplayName
		, isnull((select max(RankNumber) from tbas where tbas.TreatmentBMPID = tb.TreatmentBMPID), 0) as NumberOfAssessments
		, tbasLatest.TreatmentBMPAssessmentID as LatestTreatmentBMPAssessmentID, tbasLatest.VisitDate as LatestAssessmentDate, tbasLatest.AssessmentScore as LatestAssessmentScore
		, isnull((select max(RankNumber) from mrs where mrs.TreatmentBMPID = tb.TreatmentBMPID), 0) as NumberOfMaintenanceRecords
		, mrsLatest.MaintenanceRecordID as LatestMaintenanceRecordID, mrsLatest.VisitDate as LatestMaintenanceDate
		, possibleBandT.NumberOfBenchmarkAndThresholds, isnull(enteredBandT.NumberOfBenchmarkAndThresholdsEntered, 0) as NumberOfBenchmarkAndThresholdsEntered
		, tb.TrashCaptureEffectiveness
        , tb.LocationPoint4326.STY as Latitude
        , tb.LocationPoint4326.STX as Longitude
from dbo.TreatmentBMP tb
join dbo.TreatmentBMPType tbt on tb.TreatmentBMPTypeID = tbt.TreatmentBMPTypeID
join dbo.StormwaterJurisdiction sj on tb.StormwaterJurisdictionID = sj.StormwaterJurisdictionID
join dbo.Organization o on sj.OrganizationID = o.OrganizationID
join dbo.Organization oo on tb.OwnerOrganizationID = oo.OrganizationID
left join dbo.WaterQualityManagementPlan wqmp on tb.WaterQualityManagementPlanID = wqmp.WaterQualityManagementPlanID
join dbo.TrashCaptureStatusType tcst on tb.TrashCaptureStatusTypeID = tcst.TrashCaptureStatusTypeID
join dbo.SizingBasisType sbt on tb.SizingBasisTypeID = sbt.SizingBasisTypeID
left join dbo.TreatmentBMPLifespanType tblt on tb.TreatmentBMPLifespanTypeID = tblt.TreatmentBMPLifespanTypeID
left join dbo.Delineation d on tb.TreatmentBMPID = d.TreatmentBMPID
left join dbo.DelineationType dt on d.DelineationTypeID = dt.DelineationTypeID
left join tbas tbasLatest on tb.TreatmentBMPID = tbasLatest.TreatmentBMPID and tbasLatest.RankNumber = 1
left join mrs mrsLatest on tb.TreatmentBMPID = mrsLatest.TreatmentBMPID and mrsLatest.RankNumber = 1
left join 
(
	select tbtaot.TreatmentBMPTypeID, count(tbaot.TreatmentBMPAssessmentObservationTypeID) as NumberOfBenchmarkAndThresholds
	from dbo.TreatmentBMPAssessmentObservationType tbaot
	join dbo.ObservationTypeSpecification ots on tbaot.ObservationTypeSpecificationID = ots.ObservationTypeSpecificationID
	join dbo.TreatmentBMPTypeAssessmentObservationType tbtaot on tbaot.TreatmentBMPAssessmentObservationTypeID = tbtaot.TreatmentBMPAssessmentObservationTypeID and ots.ObservationThresholdTypeID != 3
	group by tbtaot.TreatmentBMPTypeID
) possibleBandT on tbt.TreatmentBMPTypeID = possibleBandT.TreatmentBMPTypeID
left join 
(
	select tbbt.TreatmentBMPID, tbbt.TreatmentBMPTypeID, count(tbbt.TreatmentBMPAssessmentObservationTypeID) as NumberOfBenchmarkAndThresholdsEntered
	from dbo.TreatmentBMPBenchmarkAndThreshold tbbt
	group by tbbt.TreatmentBMPID, tbbt.TreatmentBMPTypeID
) enteredBandT on tb.TreatmentBMPID = enteredBandT.TreatmentBMPID
where tb.ProjectID is null

GO