create view dbo.vGeoServerOnlandVisualTrashAssessmentArea as
	Select
		area.OnlandVisualTrashAssessmentAreaID,
		area.OnlandVisualTrashAssessmentAreaName,
		area.StormwaterJurisdictionID,
        o.OrganizationName as StormwaterJurisdictionName,
		area.OnlandVisualTrashAssessmentAreaGeometry4326 as OnlandVisualTrashAssessmentAreaGeometry,
		score.OnlandVisualTrashAssessmentScoreDisplayName as Score,
		ovta.OnlandVisualTrashAssessmentID,
		ovta.CompletedDate,
		ovta.IsProgressAssessment
	from dbo.OnlandVisualTrashAssessmentArea area
    join dbo.StormwaterJurisdiction sj on area.StormwaterJurisdictionID = sj.StormwaterJurisdictionID
    join dbo.Organization o on sj.OrganizationID = o.OrganizationID
	left join (
		Select OnlandVisualTrashAssessmentID, OnlandVisualTrashAssessmentAreaID, CompletedDate, IsProgressAssessment, OnlandVisualTrashAssessmentScoreID,
			Row_Number() over (partition by OnlandVisualTrashAssessmentAreaID order by CompletedDate desc) as RankByCompletedDate
		from dbo.OnlandVisualTrashAssessment
		where CompletedDate is not null
	) ovta on area.OnlandVisualTrashAssessmentAreaID = ovta.OnlandVisualTrashAssessmentAreaID
	left join dbo.OnlandVisualTrashAssessmentScore score on ovta.OnlandVisualTrashAssessmentScoreID = score.OnlandVisualTrashAssessmentScoreID
	where
		(
			RankByCompletedDate between 1 and 5
			or RankByCompletedDate is null -- have to account for this being null so we get the results of the left outer join
		)
		-- NPT-1099: exclude SQL-invalid geometry rows so GeoServer's WMS/WFS render never hits SQL error
		-- 24144 (a single invalid row blanks the whole layer). STIsValid() tests validity without throwing.
		-- Invalid rows are repaired at the source (save-path MakeValid, plus a one-time manual cleanup of
		-- existing rows) and reappear here once valid. Deliberately NOT MakeValid-ing on read (unlike
		-- vGeoServerDelineation).
		and area.OnlandVisualTrashAssessmentAreaGeometry4326.STIsValid() = 1
go