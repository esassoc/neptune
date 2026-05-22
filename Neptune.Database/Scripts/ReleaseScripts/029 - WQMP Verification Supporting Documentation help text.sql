DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '029 - WQMP Verification Supporting Documentation help text'
IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    -- Upsert: seed the row on fresh databases, or update the existing content on databases
    -- where the older 026 / 027 split scripts (during NPT-995 Round 5 dev) already inserted
    -- a row for NeptunePageTypeID = 98.
    IF EXISTS (SELECT 1 FROM dbo.NeptunePage WHERE NeptunePageTypeID = 98)
        UPDATE dbo.NeptunePage
        SET NeptunePageContent = '<p><strong>This step is optional.</strong> Upload a single supporting document for this verification &mdash; a field checklist, self-certification form, O&amp;M record, or photo of conditions. Accepted file types: PDF, JPG, PNG, DOCX, XLSX, CSV, TXT. Max 500&nbsp;MB.</p>'
        WHERE NeptunePageTypeID = 98
    ELSE
        INSERT INTO dbo.NeptunePage (NeptunePageTypeID, NeptunePageContent)
        VALUES
            (98, '<p><strong>This step is optional.</strong> Upload a single supporting document for this verification &mdash; a field checklist, self-certification form, O&amp;M record, or photo of conditions. Accepted file types: PDF, JPG, PNG, DOCX, XLSX, CSV, TXT. Max 500&nbsp;MB.</p>')

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-995 Round 5: seed help text for the new Supporting Documentation step (NeptunePageType 98)'
END
