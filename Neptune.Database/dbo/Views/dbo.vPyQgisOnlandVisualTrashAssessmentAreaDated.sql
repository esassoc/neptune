-- The OVTA Areas that go into the Trash Generating Unit overlay, with the date the overlay uses to
-- decide which of two overlapping areas wins (the later one).
--
-- NPT-1144: an area is an input when it has a score that can change a loading rate:
--   * a Baseline Score (2 or more completed baseline assessments; stored on the area, the same
--     column vTrashGeneratingUnitLoadStatistic reads), or
--   * a Progress Score (1 or more completed progress assessments in the last 5 years;
--     vOnlandVisualTrashAssessmentAreaProgress).
-- It used to be any area with 2 or more assessments that had a CompletedDate, which left out areas
-- scored from a single progress assessment.
--
-- "Completed" is status Complete, as it is for both scores. An assessment returned to edit keeps its
-- CompletedDate but does not count.
Create View dbo.vPyQgisOnlandVisualTrashAssessmentAreaDated as

Select
	a.OnlandVisualTrashAssessmentAreaID as OVTAID,
	a.OnlandVisualTrashAssessmentAreaGeometry,
	CAST(q.CompletedDate AS datetime) as AssessDate
from dbo.OnlandVisualTrashAssessmentArea a
	inner join (
		Select
			ovta.OnlandVisualTrashAssessmentAreaID,
			max(ovta.CompletedDate) as CompletedDate
		from dbo.OnlandVisualTrashAssessment ovta
		where ovta.OnlandVisualTrashAssessmentStatusID = 2 -- Complete
			and ovta.CompletedDate is not null
		group by ovta.OnlandVisualTrashAssessmentAreaID
	) q
		on a.OnlandVisualTrashAssessmentAreaID = q.OnlandVisualTrashAssessmentAreaID
	left join dbo.vOnlandVisualTrashAssessmentAreaProgress areaProgress
		on a.OnlandVisualTrashAssessmentAreaID = areaProgress.OnlandVisualTrashAssessmentAreaID
where a.OnlandVisualTrashAssessmentBaselineScoreID is not null
	or areaProgress.OnlandVisualTrashAssessmentAreaID is not null
GO
