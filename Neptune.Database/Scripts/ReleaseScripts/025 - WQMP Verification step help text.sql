DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '025 - WQMP Verification step help text'
IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    INSERT INTO dbo.NeptunePage (NeptunePageTypeID, NeptunePageContent)
    VALUES
        (93, '<p>Record the basics of this verification visit: the verification type, visit status, date, overall verification status, source control condition, and any enforcement or follow-up actions.</p>'),
        (94, '<p><strong>This step is optional.</strong> Use it to record findings against the WQMP''s inventoried Treatment BMPs. Mark each as adequate or not, with a note describing what you observed. The same physical BMP may legitimately appear here as both a Structural (inventoried) record AND a Simplified record &mdash; both are kept.</p>'),
        (95, '<p><strong>This step is optional.</strong> Use it to record quick / simplified BMP findings. The same physical BMP may legitimately appear here as both a Simplified record AND a Structural (inventoried) record on the previous step &mdash; both are kept.</p>'),
        (96, '<p><strong>This step is optional.</strong> Use it to record source-control BMP attribute findings (e.g., presence of and condition of educational signage, stenciled storm drains, etc.). Add notes wherever the condition is non-standard.</p>'),
        (97, '<p>Review everything you''ve recorded and finalize the verification when ready. Once finalized, the verification becomes read-only.</p>')

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-995 rework: seed initial workflow help text for the 6 WQMP O&M Verification step types'
END
