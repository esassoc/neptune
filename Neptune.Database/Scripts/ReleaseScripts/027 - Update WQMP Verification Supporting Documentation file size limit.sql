DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '027 - Update WQMP Verification Supporting Documentation file size limit'
IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    -- The new endpoint + FileResources.ValidateFileUpload cap was raised to 500 MB to match the
    -- shared form-field component's auto-rendered hint. Bring the seeded help text in line.
    UPDATE dbo.NeptunePage
    SET NeptunePageContent = '<p><strong>This step is optional.</strong> Upload a single supporting document for this verification &mdash; a field checklist, self-certification form, O&amp;M record, or photo of conditions. Accepted file types: PDF, JPG, PNG, DOCX, XLSX, CSV, TXT. Max 500&nbsp;MB.</p>'
    WHERE NeptunePageTypeID = 98;

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-995 Round 5: bump help text from 200MB to 500MB to match the new endpoint cap'
END
