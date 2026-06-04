DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '032 - NPT-1078 Seed WebServices rich text'
IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    -- NPT-1078: the SPA Data Hub Web Services tab binds <custom-rich-text> to
    -- NeptunePageTypeID = 99 (WebServices). Seed the intro content describing the new
    -- Neptune.ExternalAPI surface, the Scalar UI documentation link, and how access tokens
    -- are used. Only fill blank/null rows so we don't clobber any admin edit made via the
    -- in-page editor.
    DECLARE @Content NVARCHAR(MAX) = N'<p>Neptune exposes a set of read-only Web Services for external data consumers (most commonly PowerBI dashboards). The endpoints publish Treatment Facility, Water Quality Management Plan, Land Use, and Model Results data for downstream reporting.</p><p><strong>API Documentation.</strong> The full endpoint catalog, request/response schemas, and a built-in &quot;try it out&quot; client are available at the API documentation page linked below. The documentation is the canonical reference &mdash; per-endpoint URLs are not duplicated here.</p><p><strong>Access Tokens.</strong> Each user has a personal access token below. Send it in the <code>x-api-key</code> HTTP header on every request. Keep your token private; if it leaks, regenerate it from this page to invalidate the old one.</p><p>NOTE: The legacy <code>/PowerBI/&hellip;</code> URLs hosted by the previous MVC application have been retired. All consumers must use the new API documented below.</p>';

    IF EXISTS (SELECT 1 FROM dbo.NeptunePage WHERE NeptunePageTypeID = 99)
        UPDATE dbo.NeptunePage
        SET NeptunePageContent = @Content
        WHERE NeptunePageTypeID = 99
          AND (NeptunePageContent IS NULL OR LTRIM(RTRIM(NeptunePageContent)) = '')
    ELSE
        INSERT INTO dbo.NeptunePage (NeptunePageTypeID, NeptunePageContent)
        VALUES (99, @Content)

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-1078: seed help text for the new SPA Data Hub Web Services tab (NeptunePageType 99) that replaces the legacy Razor view'
END
