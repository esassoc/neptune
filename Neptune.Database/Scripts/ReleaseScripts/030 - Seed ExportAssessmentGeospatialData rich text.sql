DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '030 - Seed ExportAssessmentGeospatialData rich text'
IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    -- NPT-998 rework: the SPA Download OVTA Areas page binds <custom-rich-text> to
    -- NeptunePageTypeID = 44 (ExportAssessmentGeospatialData), but the legacy MVC view rendered
    -- the same instructions as hard-coded prose. The corresponding NeptunePage row was never
    -- seeded, so the SPA renders a blank panel. Seed the content from the legacy MVC view.
    -- Only fill blank/null rows so we don't clobber any admin edit made via the in-page editor.
    DECLARE @Content NVARCHAR(MAX) = N'<p>Use this form to download OVTA Areas for a given jurisdiction:</p><ul><li>A single ArcGIS file geodatabase containing onland visual trash assessment area features will be produced for the selected jurisdiction.</li><li>It will contain attributes for Area ID, Area Name, Jurisdiction, and Description.</li><li>This file download is intended to support bulk editing in desktop GIS software and can be re-uploaded to the platform to replace the existing onland visual trash assessment areas utilized in trash results calculations.</li></ul>';

    IF EXISTS (SELECT 1 FROM dbo.NeptunePage WHERE NeptunePageTypeID = 44)
        UPDATE dbo.NeptunePage
        SET NeptunePageContent = @Content
        WHERE NeptunePageTypeID = 44
          AND (NeptunePageContent IS NULL OR LTRIM(RTRIM(NeptunePageContent)) = '')
    ELSE
        INSERT INTO dbo.NeptunePage (NeptunePageTypeID, NeptunePageContent)
        VALUES (44, @Content)

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-998 rework: seed help text for Download OVTA Areas (NeptunePageType 44) that was never populated when the SPA page replaced the MVC hard-coded prose with a rich-text panel'
END
