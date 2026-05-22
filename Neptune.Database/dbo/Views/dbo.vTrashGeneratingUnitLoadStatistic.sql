/*
Everything needed to compute load-based results, including the choropleth symbology for the load-based maps
*/
create view dbo.vTrashGeneratingUnitLoadStatistic
as
Select
	*,
	case
		when CurrentLoadingRate < ProgressLoadingRate then CurrentLoadingRate - BaselineLoadingRate
		else ProgressLoadingRate - BaselineLoadingRate
	end as LoadingRateDelta
from
(
Select
	TrashGeneratingUnitID,
	TreatmentBMPID,
	TreatmentBMPName,
	TreatmentBMPTypeID,
	TreatmentBMPTypeName,
    TrashGeneratingUnitGeometry,
	Area,
	StormwaterJurisdictionID,
	OrganizationID,
	OrganizationName,
	BaselineLoadingRate,
	IsNull(Cast(IsFullTrashCapture as bit), 0) as IsFullTrashCapture,
	IsNull(Cast(IsPartialTrashCapture as bit), 0) as IsPartialTrashCapture,
	PartialTrashCaptureEffectivenessPercentage,
	PriorityLandUseTypeDisplayName as LandUseType,
	PriorityLandUseTypeID,
	Cast(HasBaselineScore as bit) as HasBaselineScore,
	Cast(HasProgressScore as bit) as HasProgressScore,
	Case
		When IsFullTrashCapture = 1 then 0
		When PartialTrashCaptureEffectivenessPercentage > 0 then BaselineLoadingRate * (1 - PartialTrashCaptureEffectivenessPercentage/100.0)
		When OnlandVisualTrashAssessmentAreaProgressScore = 'A' then 2.5
		When OnlandVisualTrashAssessmentAreaBaselineScore = 'A' then 2.5
		When HasProgressScore = 1 then ProgressLoadingRate
		When HasBaselineScore = 1 then BaselineLoadingRate
		Else BaselineLoadingRate
	end as CurrentLoadingRate,
	ProgressLoadingRate,
	DelineationIsVerified,
	PriorityLandUseTypeDisplayName,
	OnlandVisualTrashAssessmentAreaID,
	WaterQualityManagementPlanID,
	WaterQualityManagementPlanName,
	LandUseBlockID,
	LastUpdateDate,
    OnlandVisualTrashAssessmentAreaName,
    OnlandVisualTrashAssessmentAreaBaselineScore,
	OnlandVisualTrashAssessmentAreaProgressScore,
    MedianHouseholdIncomeResidential,
    MedianHouseholdIncomeRetail,
    PermitClass,
    LandUseForTGR,
    TrashCaptureEffectivenessBMP,
    TrashCaptureStatusBMP,
    TrashCaptureStatusWQMP,
    TrashCaptureEffectivenessWQMP,
    TrashGenerationRate,
    TrashCaptureStatus,
	TrashCaptureStatusSortOrder,
	AssessmentScore,
	MostRecentAssessmentDate,
	CompletedBaselineAssessmentCount,
	CompletedProgressAssessmentCount,
	IsPriorityLandUse -- ALUs are not PLUs

From (
	Select
		tgu.TrashGeneratingUnitID,
		tbmp.TreatmentBMPID,
		tbmp.TreatmentBMPName,
		tbmpt.TreatmentBMPTypeID,
		tbmpt.TreatmentBMPTypeName,
        tgu4326.TrashGeneratingUnit4326Geometry as TrashGeneratingUnitGeometry,
        IsNull(tgu.TrashGeneratingUnitGeometry.STArea(), 0) as Area,
		tgu.StormwaterJurisdictionID,
		o.OrganizationID,
		o.OrganizationName,
		plut.PriorityLandUseTypeDisplayName,
		plut.PriorityLandUseTypeID,
		tgu.OnlandVisualTrashAssessmentAreaID,
		wqmp.WaterQualityManagementPlanID,
		wqmp.WaterQualityManagementPlanName,
		lub.LandUseBlockID,
		Case
			when area.OnlandVisualTrashAssessmentBaselineScoreID is null then 0
			else 1
		end as HasBaselineScore,
		Case
			when area.OnlandVisualTrashAssessmentProgressScoreID is null then 0
			else 1
		end as HasProgressScore,
		Case
			when scoreBaseline.TrashGenerationRate is null then lub.TrashGenerationRate
			else scoreBaseline.TrashGenerationRate
		end	as BaselineLoadingRate,
		-- NPT-1051: WQMP-sourced trash capture status only counts when the WQMP is Active.
		-- Draft / Inactive WQMPs (legal-record gate) drop out of result calculations regardless
		-- of their TrashCaptureStatusTypeID. WQMP identity columns (WaterQualityManagementPlanID,
		-- WaterQualityManagementPlanName) are *not* gated — the TGU detail UI still surfaces
		-- the associated WQMP for context.
		case
			when (tbmp.TrashCaptureStatusTypeID = 1 and d.IsVerified = 1) or (wqmp.TrashCaptureStatusTypeID = 1 and wqmp.WaterQualityManagementPlanStatusID = 1) then 1
			else 0
		end as IsFullTrashCapture,
		case
			when (tbmp.TrashCaptureStatusTypeID = 2 and d.IsVerified = 1) or (wqmp.TrashCaptureStatusTypeID = 2 and wqmp.WaterQualityManagementPlanStatusID = 1) then 1
			else 0
		end as IsPartialTrashCapture,
		IsNull(
			case
				when wqmp.TrashCaptureEffectiveness is not null and wqmp.WaterQualityManagementPlanStatusID = 1 then wqmp.TrashCaptureEffectiveness
				else tbmp.TrashCaptureEffectiveness
			end
		, 0) as PartialTrashCaptureEffectivenessPercentage,
		IsNull(d.IsVerified, 0) as DelineationIsVerified,
		IsNull(
			Case
				when scoreProgress.TrashGenerationRate is null then 
					case
						when scoreBaseline.TrashGenerationRate is null then lub.TrashGenerationRate
						else scoreBaseline.TrashGenerationRate
					end
				else scoreProgress.TrashGenerationRate
			end, 0
		) as ProgressLoadingRate,
		tgu.LastUpdateDate,
		tbmp.TrashCaptureEffectiveness as TrashCaptureEffectivenessBMP,
        tcs.TrashCaptureStatusTypeDisplayName as TrashCaptureStatusBMP,
        area.OnlandVisualTrashAssessmentAreaName,
        scoreBaseline.OnlandVisualTrashAssessmentScoreDisplayName as OnlandVisualTrashAssessmentAreaBaselineScore,
        scoreProgress.OnlandVisualTrashAssessmentScoreDisplayName as OnlandVisualTrashAssessmentAreaProgressScore,
        case when wqmp.WaterQualityManagementPlanStatusID = 1 then wqmptcs.TrashCaptureStatusTypeDisplayName end as TrashCaptureStatusWQMP,
        case when wqmp.WaterQualityManagementPlanStatusID = 1 then wqmp.TrashCaptureEffectiveness end as TrashCaptureEffectivenessWQMP,
        lub.MedianHouseholdIncomeResidential,
        lub.MedianHouseholdIncomeRetail,
        pt.PermitTypeDisplayName as PermitClass,
        lub.LandUseForTGR,
        lub.TrashGenerationRate,
        case
		    when tbmp.TrashCaptureStatusTypeID = 1 or (wqmp.TrashCaptureStatusTypeID = 1 and wqmp.WaterQualityManagementPlanStatusID = 1) then 'Full'
		    when tbmp.TrashCaptureStatusTypeID = 2 or (wqmp.TrashCaptureStatusTypeID = 2 and wqmp.WaterQualityManagementPlanStatusID = 1) then 'Partial'
		    when tbmp.TrashCaptureStatusTypeID = 3 or (wqmp.TrashCaptureStatusTypeID = 3 and wqmp.WaterQualityManagementPlanStatusID = 1) then 'None'
		    else 'NotProvided'
	    end as TrashCaptureStatus,
		case
		    when tbmp.TrashCaptureStatusTypeID = 1 or (wqmp.TrashCaptureStatusTypeID = 1 and wqmp.WaterQualityManagementPlanStatusID = 1) then 1
		    when tbmp.TrashCaptureStatusTypeID = 2 or (wqmp.TrashCaptureStatusTypeID = 2 and wqmp.WaterQualityManagementPlanStatusID = 1) then 2
		    when tbmp.TrashCaptureStatusTypeID = 3 or (wqmp.TrashCaptureStatusTypeID = 3 and wqmp.WaterQualityManagementPlanStatusID = 1) then 3
		    else 4
	    end as TrashCaptureStatusSortOrder,
	    case when ovtaad.MostRecentAssessmentScore is null then 'NotProvided' else ovtaad.MostRecentAssessmentScore end as AssessmentScore,
	    ovtaad.MostRecentAssessmentDate,
		ovtacb.CompletedBaselineAssessmentCount,
		ovtacp.CompletedProgressAssessmentCount,
	    Case when tgu.LandUseBlockID is null then 0 when plut.PriorityLandUseTypeName = 'ALU' then 0 else 1 end as IsPriorityLandUse -- ALUs are not PLUs

	From
		dbo.TrashGeneratingUnit tgu
        join dbo.TrashGeneratingUnit4326 tgu4326 on 
            tgu.TrashGeneratingUnitID = tgu4326.TrashGeneratingUnitID
		join dbo.StormwaterJurisdiction sj on tgu.StormwaterJurisdictionID = sj.StormwaterJurisdictionID
		join dbo.Organization o on sj.OrganizationID = o.OrganizationID
		left join dbo.LandUseBlock lub on tgu.LandUseBlockID = lub.LandUseBlockID
		left join dbo.Delineation d on tgu.DelineationID = d.DelineationID
		left join dbo.TreatmentBMP tbmp on d.TreatmentBMPID = tbmp.TreatmentBMPID
		left join dbo.TreatmentBMPType tbmpt on tbmp.TreatmentBMPTypeID = tbmpt.TreatmentBMPTypeID
		left join dbo.WaterQualityManagementPlan wqmp on tgu.WaterQualityManagementPlanID = wqmp.WaterQualityManagementPlanID
		left join dbo.TrashCaptureStatusType tcs on tbmp.TrashCaptureStatusTypeID = tcs.TrashCaptureStatusTypeID
		left join dbo.PriorityLandUseType plut on lub.PriorityLandUseTypeID = plut.PriorityLandUseTypeID
        left join dbo.PermitType pt on lub.PermitTypeID = pt.PermitTypeID
		left join dbo.TrashCaptureStatusType wqmptcs on wqmp.TrashCaptureStatusTypeID = wqmptcs.TrashCaptureStatusTypeID
		left join dbo.OnlandVisualTrashAssessmentArea area on tgu.OnlandVisualTrashAssessmentAreaID = area.OnlandVisualTrashAssessmentAreaID
		left join dbo.OnlandVisualTrashAssessmentScore scoreBaseline on area.OnlandVisualTrashAssessmentBaselineScoreID = scoreBaseline.OnlandVisualTrashAssessmentScoreID
		left join dbo.vOnlandVisualTrashAssessmentAreaProgress areaProgress on tgu.OnlandVisualTrashAssessmentAreaID = areaProgress.OnlandVisualTrashAssessmentAreaID
		left join dbo.OnlandVisualTrashAssessmentScore scoreProgress on areaProgress.OnlandVisualTrashAssessmentScoreID = scoreProgress.OnlandVisualTrashAssessmentScoreID
	    left join 
        (
            Select
	            a.OnlandVisualTrashAssessmentAreaID,
	            q.CompletedDate as MostRecentAssessmentDate,
	            Score.OnlandVisualTrashAssessmentScoreDisplayName as MostRecentAssessmentScore
            from dbo.OnlandVisualTrashAssessmentArea a
	        join (
		            Select
			            ovta.OnlandVisualTrashAssessmentID,
			            ovta.OnlandVisualTrashAssessmentAreaID,
			            ovta.OnlandVisualTrashAssessmentScoreID,
			            ovta.CompletedDate,
			            rownumber = Row_Number() over (partition by ovta.OnlandVisualTrashAssessmentAreaID order by ovta.CompletedDate desc)
		            from dbo.OnlandVisualTrashAssessment ovta
		            where CompletedDate is not null
	            ) q on a.OnlandVisualTrashAssessmentAreaID = q.OnlandVisualTrashAssessmentAreaID
	            join dbo.OnlandVisualTrashAssessmentScore score on score.OnlandVisualTrashAssessmentScoreID = q.OnlandVisualTrashAssessmentScoreID
	            where rownumber = 1
        ) ovtaad on tgu.OnlandVisualTrashAssessmentAreaID = ovtaad.OnlandVisualTrashAssessmentAreaID

		left join 
		(
			Select
				OnlandVisualTrashAssessmentAreaID,
				count (*) as CompletedBaselineAssessmentCount
			from dbo.OnlandVisualTrashAssessment ovta
			where ovta.IsProgressAssessment = 0
			group by OnlandVisualTrashAssessmentAreaID
		) ovtacb on tgu.OnlandVisualTrashAssessmentAreaID = ovtacb.OnlandVisualTrashAssessmentAreaID
		left join 
		(
			Select
				OnlandVisualTrashAssessmentAreaID,
				count (*) as CompletedProgressAssessmentCount
			from dbo.OnlandVisualTrashAssessment ovta
			where ovta.IsProgressAssessment = 1
			group by OnlandVisualTrashAssessmentAreaID
		) ovtacp on tgu.OnlandVisualTrashAssessmentAreaID = ovtacp.OnlandVisualTrashAssessmentAreaID
	Where 
		tgu.LandUseBlockID is not null
		and lub.PermitTypeID = 1
		and tgu4326.TrashGeneratingUnit4326Geometry.STGeometryType() in ('POLYGON', 'MULTIPOLYGON')
) subq
) subq2
GO