DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '037 - NPT-1068 Rewrite legacy FileResource URLs in NeptunePage content'

IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    /*
        NeptunePage rich text (<img src> / <a href>) contains absolute links to the retired WebMvc
        FileResource route, e.g. https://www.ocstormwatertools.org/FileResource/DisplayResource/{guid}.
        The MVC app and that route are gone; the live endpoint is /file-resources/{guid} on the API.

        Strip the scheme + host so the stored value becomes the RELATIVE /file-resources/{guid}. The SPA's
        custom-rich-text component rewrites that relative path to the absolute API URL at render time, so we
        avoid baking an environment-specific host (which moved under NPT-1078) into the database. Replacing
        only the host+legacy-path leaves the trailing {guid} intact.

        This is naturally idempotent (the pattern no longer matches after the first run), but it's guarded by
        DatabaseMigration per the ReleaseScripts convention.
    */
    -- Cover www/non-www x http/https so every row the WHERE selects is actually rewritten (no recording the
    -- migration while silently leaving a host variant un-rewritten). Any other residual host form is still
    -- repaired for display by the SPA's custom-rich-text render-time rewrite (which matches any host).
    UPDATE dbo.NeptunePage
    SET NeptunePageContent =
        REPLACE(
        REPLACE(
        REPLACE(
        REPLACE(NeptunePageContent, 'https://www.ocstormwatertools.org/FileResource/DisplayResource/', '/file-resources/'),
                                    'http://www.ocstormwatertools.org/FileResource/DisplayResource/',  '/file-resources/'),
                                    'https://ocstormwatertools.org/FileResource/DisplayResource/',      '/file-resources/'),
                                    'http://ocstormwatertools.org/FileResource/DisplayResource/',       '/file-resources/')
    WHERE NeptunePageContent LIKE '%ocstormwatertools.org/FileResource/DisplayResource/%';

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'Retire legacy MVC FileResource URLs in NeptunePage rich text content (WebMvc retirement / NPT-1068); convert to the relative /file-resources/{guid} the SPA resolves at render.'
END
