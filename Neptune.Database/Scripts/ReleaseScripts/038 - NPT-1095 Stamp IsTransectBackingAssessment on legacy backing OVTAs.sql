DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '038 - NPT-1095 Stamp IsTransectBackingAssessment on legacy backing OVTAs'

IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    /*
        OVTA assessments finalized before the IsTransectBackingAssessment flag existed have it = 0, so areas
        with no flagged backing assessment never updated their stored TransectLine when the backing points were
        edited (NPT-1095). The code fix adds a runtime "no other backing exists" fallback, but for legacy
        multi-assessment areas that would anchor to whichever assessment is edited first. Stamp the correct
        backing up front: the earliest-completed assessment per area (Complete status + MIN(CompletedDate)) —
        the same selection as OnlandVisualTrashAssessmentAreas.RecomputeTransectLine, plus an
        OnlandVisualTrashAssessmentID tie-break for deterministic results (RecomputeTransectLine's
        MinBy(CompletedDate) does not itself tie-break).

        Touches only areas that currently have ZERO assessments flagged backing, so it's idempotent. Geometry
        is NOT recomputed here — existing TransectLines refresh on the next edit via the code fix.
    */
    ;WITH AreasMissingBacking AS
    (
        SELECT ovta.OnlandVisualTrashAssessmentAreaID
        FROM dbo.OnlandVisualTrashAssessment ovta
        WHERE ovta.OnlandVisualTrashAssessmentAreaID IS NOT NULL
        GROUP BY ovta.OnlandVisualTrashAssessmentAreaID
        HAVING SUM(CASE WHEN ovta.IsTransectBackingAssessment = 1 THEN 1 ELSE 0 END) = 0
    ),
    EarliestCompletedPerArea AS
    (
        SELECT ovta.OnlandVisualTrashAssessmentID,
               ROW_NUMBER() OVER (
                   PARTITION BY ovta.OnlandVisualTrashAssessmentAreaID
                   ORDER BY ovta.CompletedDate ASC, ovta.OnlandVisualTrashAssessmentID ASC
               ) AS RowNum
        FROM dbo.OnlandVisualTrashAssessment ovta
        INNER JOIN AreasMissingBacking a ON a.OnlandVisualTrashAssessmentAreaID = ovta.OnlandVisualTrashAssessmentAreaID
        WHERE ovta.OnlandVisualTrashAssessmentStatusID = 2 -- Complete
    )
    UPDATE ovta
    SET ovta.IsTransectBackingAssessment = 1
    FROM dbo.OnlandVisualTrashAssessment ovta
    INNER JOIN EarliestCompletedPerArea ec ON ec.OnlandVisualTrashAssessmentID = ovta.OnlandVisualTrashAssessmentID
    WHERE ec.RowNum = 1;

    PRINT '  Stamped backing assessment on ' + CAST(@@ROWCOUNT AS VARCHAR) + ' legacy OVTA area(s).';

    /*
        De-duplicate any area that has MORE THAN ONE assessment flagged backing. Keep only the
        earliest-completed Complete assessment flagged (matching RecomputeTransectLine) and demote the rest.
        Observed in QA/prod where an InProgress assessment (no CompletedDate) was erroneously left flagged
        alongside the real Complete backing. Only acts on areas that have a Complete keeper, and never demotes
        the keeper itself.
    */
    ;WITH AreasWithMultipleBacking AS
    (
        SELECT ovta.OnlandVisualTrashAssessmentAreaID
        FROM dbo.OnlandVisualTrashAssessment ovta
        WHERE ovta.OnlandVisualTrashAssessmentAreaID IS NOT NULL
        GROUP BY ovta.OnlandVisualTrashAssessmentAreaID
        HAVING SUM(CASE WHEN ovta.IsTransectBackingAssessment = 1 THEN 1 ELSE 0 END) > 1
    ),
    KeeperPerArea AS
    (
        SELECT ovta.OnlandVisualTrashAssessmentAreaID,
               ovta.OnlandVisualTrashAssessmentID,
               ROW_NUMBER() OVER (
                   PARTITION BY ovta.OnlandVisualTrashAssessmentAreaID
                   ORDER BY ovta.CompletedDate ASC, ovta.OnlandVisualTrashAssessmentID ASC
               ) AS RowNum
        FROM dbo.OnlandVisualTrashAssessment ovta
        INNER JOIN AreasWithMultipleBacking a ON a.OnlandVisualTrashAssessmentAreaID = ovta.OnlandVisualTrashAssessmentAreaID
        WHERE ovta.OnlandVisualTrashAssessmentStatusID = 2 -- Complete
    )
    UPDATE ovta
    SET ovta.IsTransectBackingAssessment = 0
    FROM dbo.OnlandVisualTrashAssessment ovta
    INNER JOIN AreasWithMultipleBacking a ON a.OnlandVisualTrashAssessmentAreaID = ovta.OnlandVisualTrashAssessmentAreaID
    WHERE ovta.IsTransectBackingAssessment = 1
      AND EXISTS (SELECT 1 FROM KeeperPerArea k WHERE k.OnlandVisualTrashAssessmentAreaID = ovta.OnlandVisualTrashAssessmentAreaID AND k.RowNum = 1)
      AND NOT EXISTS (SELECT 1 FROM KeeperPerArea k WHERE k.OnlandVisualTrashAssessmentID = ovta.OnlandVisualTrashAssessmentID AND k.RowNum = 1);

    PRINT '  Demoted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' duplicate backing flag(s) in multi-backing area(s).';

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'Normalize IsTransectBackingAssessment for legacy OVTA areas (NPT-1095): stamp the earliest-completed assessment where none is flagged, and demote duplicates where more than one is flagged, so each area resolves to exactly one (Complete) backing.'
END
