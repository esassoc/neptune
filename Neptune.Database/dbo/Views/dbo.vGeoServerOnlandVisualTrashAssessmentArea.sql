create view dbo.vGeoServerOnlandVisualTrashAssessmentArea as
	Select
		area.OnlandVisualTrashAssessmentAreaID,
		area.OnlandVisualTrashAssessmentAreaName,
		area.StormwaterJurisdictionID,
        o.OrganizationName as StormwaterJurisdictionName,
		area.OnlandVisualTrashAssessmentAreaGeometry4326 as OnlandVisualTrashAssessmentAreaGeometry,
		-- NPT-1126: one row per area. Score is the area's most recently completed OVTA (baseline or progress),
		-- so existing consumers that read Score keep working; BaselineScore/ProgressScore are the area's
		-- calculated scores, used by the ovta_baseline_score / ovta_progress_score styles.
		score.OnlandVisualTrashAssessmentScoreDisplayName as Score,
		baselineScore.OnlandVisualTrashAssessmentScoreDisplayName as BaselineScore,
		progressScore.OnlandVisualTrashAssessmentScoreDisplayName as ProgressScore,
		ovta.OnlandVisualTrashAssessmentID,
		ovta.CompletedDate,
		ovta.IsProgressAssessment
	from dbo.OnlandVisualTrashAssessmentArea area
    join dbo.StormwaterJurisdiction sj on area.StormwaterJurisdictionID = sj.StormwaterJurisdictionID
    join dbo.Organization o on sj.OrganizationID = o.OrganizationID
	left join (
		Select OnlandVisualTrashAssessmentID, OnlandVisualTrashAssessmentAreaID, CompletedDate, IsProgressAssessment, OnlandVisualTrashAssessmentScoreID,
			Row_Number() over (partition by OnlandVisualTrashAssessmentAreaID order by CompletedDate desc, OnlandVisualTrashAssessmentID desc) as RankByCompletedDate
		from dbo.OnlandVisualTrashAssessment
		where OnlandVisualTrashAssessmentStatusID = 2 -- Complete
	) ovta on area.OnlandVisualTrashAssessmentAreaID = ovta.OnlandVisualTrashAssessmentAreaID and ovta.RankByCompletedDate = 1
	left join dbo.OnlandVisualTrashAssessmentScore score on ovta.OnlandVisualTrashAssessmentScoreID = score.OnlandVisualTrashAssessmentScoreID
	left join dbo.OnlandVisualTrashAssessmentScore baselineScore on area.OnlandVisualTrashAssessmentBaselineScoreID = baselineScore.OnlandVisualTrashAssessmentScoreID
	left join dbo.OnlandVisualTrashAssessmentScore progressScore on area.OnlandVisualTrashAssessmentProgressScoreID = progressScore.OnlandVisualTrashAssessmentScoreID
	where
		-- NPT-1099: exclude SQL-invalid geometry rows so GeoServer's WMS/WFS render never hits SQL error
		-- 24144 (a single invalid row blanks the whole layer). STIsValid() tests validity without throwing.
		-- Invalid rows are repaired at the source (save-path MakeValid, plus a one-time manual cleanup of
		-- existing rows) and reappear here once valid. Deliberately NOT MakeValid-ing on read (unlike
		-- vGeoServerDelineation).
		area.OnlandVisualTrashAssessmentAreaGeometry4326.STIsValid() = 1
go