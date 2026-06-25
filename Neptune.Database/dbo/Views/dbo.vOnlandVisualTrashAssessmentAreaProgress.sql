Create View dbo.vOnlandVisualTrashAssessmentAreaProgress
as
Select
	subq.OnlandVisualTrashAssessmentAreaID as PrimaryKey,
	subq.OnlandVisualTrashAssessmentAreaID,
	score.OnlandVisualTrashAssessmentScoreDisplayName,
	score.OnlandVisualTrashAssessmentScoreID
From (
	-- aggregate over areas
	Select
		OnlandVisualTrashAssessmentAreaID,
		Avg(NumericValue) as NumericValue,
		-- we only care about areas with one or more progress assessments within the last 5 years, so we need this additional agg
		Count(*) as NumberOfProgressAssessments
	From (
		-- get the progress assessments
		select
			OnlandVisualTrashAssessmentAreaID,
			NumericValue,
			-- we only care about the most recent assessments, so we need to rank them on date
			ROW_NUMBER() over (partition by OnlandVisualTrashAssessmentAreaID order by CompletedDate desc) as RowNumber
		from dbo.OnlandVisualTrashAssessment ovta
			inner join dbo.OnlandVisualTrashAssessmentScore score
				on ovta.OnlandVisualTrashAssessmentScoreID = score.OnlandVisualTrashAssessmentScoreID
		where IsProgressAssessment = 1 and CompletedDate >= DATEADD(year, -5, CAST(CURRENT_TIMESTAMP AS date))
		) subq
	where RoWNumber <=3
	group by (OnlandVisualTrashAssessmentAreaID)
	having count(*) >= 1
) subq inner join dbo.OnlandVisualTrashAssessmentScore score
	on subq.NumericValue = score.NumericValue
Go

/* select * from dbo.vOnlandVisualTrashAssessmentAreaProgress */