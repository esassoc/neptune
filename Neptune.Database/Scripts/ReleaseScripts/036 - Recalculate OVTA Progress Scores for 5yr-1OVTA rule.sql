DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '036 - Recalculate OVTA Progress Scores for 5yr-1OVTA rule';

IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

	PRINT @MigrationName;

	-- NPT-1065: The progress-score rule was loosened from "2 completed progress OVTAs within 4 years"
	-- to "1 completed progress OVTA within 5 years" so Track 2 permittees (who may revisit sites only
	-- every 5 years) can update Trash Generation Rates. The stored OnlandVisualTrashAssessmentProgressScoreID
	-- is only recalculated when an OVTA for an area is created/edited/deleted, so this one-time backfill
	-- applies the new rule retroactively to all existing areas. The score is the average (rounded) of the
	-- top 3 most recent qualifying progress assessments, matching CalculateProgressScore() and
	-- dbo.vOnlandVisualTrashAssessmentAreaProgress.
	UPDATE ovta
	SET ovta.OnlandVisualTrashAssessmentProgressScoreID = ovtas.OnlandVisualTrashAssessmentScoreID
		FROM dbo.OnlandVisualTrashAssessmentArea ovta
	LEFT JOIN (
		SELECT
			OnlandVisualTrashAssessmentAreaID,
			AVG(NumericValue) AS Top3AverageNumericValue
		FROM (
			SELECT
				ovta2.OnlandVisualTrashAssessmentAreaID,
				ovtas2.NumericValue,
				ROW_NUMBER() OVER (
					PARTITION BY ovta2.OnlandVisualTrashAssessmentAreaID
					ORDER BY ovta2.CompletedDate DESC
				) AS rn
			FROM dbo.OnlandVisualTrashAssessment ovta2
			JOIN dbo.OnlandVisualTrashAssessmentScore ovtas2
				ON ovta2.OnlandVisualTrashAssessmentScoreID = ovtas2.OnlandVisualTrashAssessmentScoreID
				--OnlandVisualTrashAssessmentStatusName = Complete
			WHERE ovta2.OnlandVisualTrashAssessmentStatusID = 2
			  AND ovta2.IsProgressAssessment = 1
			  AND ovta2.CompletedDate >= DATEADD(year, -5, GETDATE())
		) ranked
		WHERE rn <= 3
		GROUP BY OnlandVisualTrashAssessmentAreaID
		HAVING COUNT(*) >= 1
	) ovtaa ON ovta.OnlandVisualTrashAssessmentAreaID = ovtaa.OnlandVisualTrashAssessmentAreaID
		LEFT JOIN dbo.OnlandVisualTrashAssessmentScore ovtas
		ON ROUND(ovtaa.Top3AverageNumericValue, 0) = ovtas.NumericValue

	INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
	SELECT 'Shannon Bulloch', @MigrationName, 'NPT-1065 - Recalculate OVTA Progress Scores under 1-OVTA-within-5-years rule'
END
