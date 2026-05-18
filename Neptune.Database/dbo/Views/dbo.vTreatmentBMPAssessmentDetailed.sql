Create View dbo.vTreatmentBMPAssessmentDetailed
as

--with tbopf(TreatmentBMPAssessmentID, TreatmentBMPObservationID,
--		TreatmentBMPAssessmentObservationTypeID, TreatmentBMPAssessmentObservationTypeName,
--        ObservationTypeSpecificationID, ObservationTypeSpecificationName, ObservationTypeSpecificationDisplayName, 
--        ObservationTypeCollectionMethodID, ObservationTypeCollectionMethodName, ObservationTypeCollectionMethodDisplayName, ObservationTypeCollectionMethodDescription,
--        PropertiesToObserve, AssessmentDescription, ObservationValues)
--as
--(
--    select TreatmentBMPAssessmentID, TreatmentBMPObservationID,
--		TreatmentBMPAssessmentObservationTypeID, TreatmentBMPAssessmentObservationTypeName,
--        ObservationTypeSpecificationID, ObservationTypeSpecificationName, ObservationTypeSpecificationDisplayName, 
--        ObservationTypeCollectionMethodID, ObservationTypeCollectionMethodName, ObservationTypeCollectionMethodDisplayName, ObservationTypeCollectionMethodDescription,
--        PropertiesToObserve, AssessmentDescription, STRING_AGG(ObservationValue, ', ') as ObservationValues
--    from dbo.vTreatmentBMPObservationPassFail
--    where datalength(ObservationValue) > 0
--    group by TreatmentBMPAssessmentID, TreatmentBMPObservationID, Notes,
--		TreatmentBMPAssessmentObservationTypeID, TreatmentBMPAssessmentObservationTypeName,
--        ObservationTypeSpecificationID, ObservationTypeSpecificationName, ObservationTypeSpecificationDisplayName, 
--        ObservationTypeCollectionMethodID, ObservationTypeCollectionMethodName, ObservationTypeCollectionMethodDisplayName, ObservationTypeCollectionMethodDescription,
--        PropertiesToObserve, AssessmentDescription
--),
--tbodv(TreatmentBMPAssessmentID, TreatmentBMPObservationID,
--		TreatmentBMPAssessmentObservationTypeID, TreatmentBMPAssessmentObservationTypeName,
--        ObservationTypeSpecificationID, ObservationTypeSpecificationName, ObservationTypeSpecificationDisplayName, 
--        ObservationTypeCollectionMethodID, ObservationTypeCollectionMethodName, ObservationTypeCollectionMethodDisplayName, ObservationTypeCollectionMethodDescription,
--        PropertiesToObserve, AssessmentDescription, ObservationValues)
--as
--(
--    select TreatmentBMPAssessmentID, TreatmentBMPObservationID,
--		TreatmentBMPAssessmentObservationTypeID, TreatmentBMPAssessmentObservationTypeName,
--        ObservationTypeSpecificationID, ObservationTypeSpecificationName, ObservationTypeSpecificationDisplayName, 
--        ObservationTypeCollectionMethodID, ObservationTypeCollectionMethodName, ObservationTypeCollectionMethodDisplayName, ObservationTypeCollectionMethodDescription,
--        PropertiesToObserve, AssessmentDescription, STRING_AGG(ObservationValue, ', ') as ObservationValues
--    from dbo.vTreatmentBMPObservationDiscreteValue
--    where datalength(ObservationValue) > 0
--    group by TreatmentBMPAssessmentID, TreatmentBMPObservationID, Notes,
--		TreatmentBMPAssessmentObservationTypeID, TreatmentBMPAssessmentObservationTypeName,
--        ObservationTypeSpecificationID, ObservationTypeSpecificationName, ObservationTypeSpecificationDisplayName, 
--        ObservationTypeCollectionMethodID, ObservationTypeCollectionMethodName, ObservationTypeCollectionMethodDisplayName, ObservationTypeCollectionMethodDescription,
--        PropertiesToObserve, AssessmentDescription
--)

select  tba.TreatmentBMPAssessmentID, tbat.TreatmentBMPAssessmentTypeDisplayName, tba.IsAssessmentComplete, tba.AssessmentScore,
        bmp.TreatmentBMPID, bmp.TreatmentBMPName, bmp.TreatmentBMPTypeID, bmpt.TreatmentBMPTypeName,
        fv.FieldVisitID, fvt.FieldVisitTypeDisplayName, fv.VisitDate, fv.IsFieldVisitVerified,
        fv.PerformedByPersonID, fvp.FirstName + ' ' + fvp.LastName as PerformedByPersonName,
        sj.StormwaterJurisdictionID, o.OrganizationName as StormwaterJurisdictionName, sj.StormwaterJurisdictionPublicWQMPVisibilityTypeID,
        wqmp.WaterQualityManagementPlanID, wqmp.WaterQualityManagementPlanName
        --,
        --isnull(tbo20.ObservationValues, 'n/a') as [Inlet Condition],
        --isnull(tbo23.ObservationValues, 'n/a') as [Outlet Condition],
        --isnull(tbo24.ObservationValues, 'n/a') as [Condition of Flow Spreader to BMP],
        --isnull(tbo25.ObservationValues, 'n/a') as [Mechanical Equipment Operability],
        --isnull(tbo26.ObservationValues, 'n/a') as [Harvested Water Equipment Operability],
        --isnull(tbo27.ObservationValues, 'n/a') as [Device Operability],
        --isnull(tbo28.ObservationValues, 'n/a') as [Significant Nuisance Conditions],
        --isnull(tbo29.ObservationValues, 'n/a') as [Leak in Storage or Piping],
        --isnull(tbo30.ObservationValues, 'n/a') as [Pavement Design Life Remaining],
        --isnull(tbo33.ObservationValues, 'n/a') as [Material Accumulation in Forebay/ Pretreatment],
        --isnull(tbo34.ObservationValues, 'n/a') as [Material Accumulation as Percent of Total System Volume],
        --isnull(tbo35.ObservationValues, 'n/a') as [Percent BMP Surface Area Clogged],
        --isnull(tbo36.ObservationValues, 'n/a') as [Mulch Thickness],
        --isnull(tbo37.ObservationValues, 'n/a') as [Desirable Vegetation Cover],
        --isnull(tbo38.ObservationValues, 'n/a') as [Undesirable Vegetation Cover],
        --isnull(tbo39.ObservationValues, 'n/a') as [Wetland Vegetative Cover],
        --isnull(tbo40.ObservationValues, 'n/a') as [Surface Erosion],
        --isnull(tbo41.ObservationValues, 'n/a') as [Inlet Scour/Erosion],
        --isnull(tbo42.ObservationValues, 'n/a') as [Current Diversion Capacity],
        --isnull(tbo43.ObservationValues, 'n/a') as [Device Operability2]
from dbo.TreatmentBMPAssessment tba
join dbo.TreatmentBMPAssessmentType tbat on tba.TreatmentBMPAssessmentTypeID = tbat.TreatmentBMPAssessmentTypeID
join dbo.TreatmentBMPType bmpt on tba.TreatmentBMPTypeID = bmpt.TreatmentBMPTypeID
join dbo.TreatmentBMP bmp on tba.TreatmentBMPID = bmp.TreatmentBMPID
join dbo.FieldVisit fv on tba.FieldVisitID = fv.FieldVisitID
join dbo.FieldVisitType fvt on fv.FieldVisitTypeID = fvt.FieldVisitTypeID
join dbo.Person fvp on fv.PerformedByPersonID = fvp.PersonID
join dbo.StormwaterJurisdiction sj on bmp.StormwaterJurisdictionID = sj.StormwaterJurisdictionID
join dbo.Organization o on sj.OrganizationID = o.OrganizationID
left join dbo.WaterQualityManagementPlan wqmp on bmp.WaterQualityManagementPlanID = wqmp.WaterQualityManagementPlanID
--left join tbopf tbo20 on tba.TreatmentBMPAssessmentID = tbo20.TreatmentBMPAssessmentID and tbo20.TreatmentBMPAssessmentObservationTypeID = 20
--left join tbopf tbo23 on tba.TreatmentBMPAssessmentID = tbo23.TreatmentBMPAssessmentID and tbo23.TreatmentBMPAssessmentObservationTypeID = 23
--left join tbopf tbo24 on tba.TreatmentBMPAssessmentID = tbo24.TreatmentBMPAssessmentID and tbo24.TreatmentBMPAssessmentObservationTypeID = 24
--left join tbopf tbo25 on tba.TreatmentBMPAssessmentID = tbo25.TreatmentBMPAssessmentID and tbo25.TreatmentBMPAssessmentObservationTypeID = 25
--left join tbopf tbo26 on tba.TreatmentBMPAssessmentID = tbo26.TreatmentBMPAssessmentID and tbo26.TreatmentBMPAssessmentObservationTypeID = 26
--left join tbopf tbo27 on tba.TreatmentBMPAssessmentID = tbo27.TreatmentBMPAssessmentID and tbo27.TreatmentBMPAssessmentObservationTypeID = 27
--left join tbopf tbo28 on tba.TreatmentBMPAssessmentID = tbo28.TreatmentBMPAssessmentID and tbo28.TreatmentBMPAssessmentObservationTypeID = 28
--left join tbopf tbo29 on tba.TreatmentBMPAssessmentID = tbo29.TreatmentBMPAssessmentID and tbo29.TreatmentBMPAssessmentObservationTypeID = 29 
--left join tbodv tbo30 on tba.TreatmentBMPAssessmentID = tbo30.TreatmentBMPAssessmentID and tbo30.TreatmentBMPAssessmentObservationTypeID = 30
--left join tbodv tbo33 on tba.TreatmentBMPAssessmentID = tbo33.TreatmentBMPAssessmentID and tbo33.TreatmentBMPAssessmentObservationTypeID = 33
--left join tbodv tbo34 on tba.TreatmentBMPAssessmentID = tbo34.TreatmentBMPAssessmentID and tbo34.TreatmentBMPAssessmentObservationTypeID = 34
--left join tbodv tbo35 on tba.TreatmentBMPAssessmentID = tbo35.TreatmentBMPAssessmentID and tbo35.TreatmentBMPAssessmentObservationTypeID = 35
--left join tbodv tbo36 on tba.TreatmentBMPAssessmentID = tbo36.TreatmentBMPAssessmentID and tbo36.TreatmentBMPAssessmentObservationTypeID = 36
--left join tbodv tbo37 on tba.TreatmentBMPAssessmentID = tbo37.TreatmentBMPAssessmentID and tbo37.TreatmentBMPAssessmentObservationTypeID = 37
--left join tbodv tbo38 on tba.TreatmentBMPAssessmentID = tbo38.TreatmentBMPAssessmentID and tbo38.TreatmentBMPAssessmentObservationTypeID = 38
--left join tbodv tbo39 on tba.TreatmentBMPAssessmentID = tbo39.TreatmentBMPAssessmentID and tbo39.TreatmentBMPAssessmentObservationTypeID = 39
--left join tbodv tbo40 on tba.TreatmentBMPAssessmentID = tbo40.TreatmentBMPAssessmentID and tbo40.TreatmentBMPAssessmentObservationTypeID = 40
--left join tbodv tbo41 on tba.TreatmentBMPAssessmentID = tbo41.TreatmentBMPAssessmentID and tbo41.TreatmentBMPAssessmentObservationTypeID = 41
--left join tbodv tbo42 on tba.TreatmentBMPAssessmentID = tbo42.TreatmentBMPAssessmentID and tbo42.TreatmentBMPAssessmentObservationTypeID = 42
--left join tbodv tbo43 on tba.TreatmentBMPAssessmentID = tbo43.TreatmentBMPAssessmentID and tbo43.TreatmentBMPAssessmentObservationTypeID = 43


GO