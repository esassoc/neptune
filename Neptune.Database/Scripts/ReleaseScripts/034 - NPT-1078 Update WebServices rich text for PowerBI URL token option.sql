DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '034 - NPT-1078 Update WebServices rich text for PowerBI URL token option'
IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    -- NPT-1078: WebServiceTokenAuthenticationHandler now also accepts the access token as a
    -- query string parameter (?x-api-key=…) so PowerBI users can paste a fully self-contained
    -- URL into their data source, mirroring the legacy MVC PowerBIController which took the
    -- token as a route segment. Update the Web Services tab help text so the option is
    -- documented in-product. Only updates the row if it still matches the prior seeded copy
    -- from migration 032 — admin edits via the in-page editor are preserved untouched.
    DECLARE @OldContent NVARCHAR(MAX) = N'<p>Neptune exposes a set of read-only Web Services for external data consumers (most commonly PowerBI dashboards). The endpoints publish Treatment Facility, Water Quality Management Plan, Land Use, and Model Results data for downstream reporting.</p><p><strong>API Documentation.</strong> The full endpoint catalog, request/response schemas, and a built-in &quot;try it out&quot; client are available at the API documentation page linked below. The documentation is the canonical reference &mdash; per-endpoint URLs are not duplicated here.</p><p><strong>Access Tokens.</strong> Each user has a personal access token below. Send it in the <code>x-api-key</code> HTTP header on every request. Keep your token private; if it leaks, regenerate it from this page to invalidate the old one.</p><p>NOTE: The legacy <code>/PowerBI/&hellip;</code> URLs hosted by the previous MVC application have been retired. All consumers must use the new API documented below.</p>';

    DECLARE @NewContent NVARCHAR(MAX) = N'<p>Neptune exposes a set of read-only Web Services for external data consumers (most commonly PowerBI dashboards). The endpoints publish Treatment Facility, Water Quality Management Plan, Land Use, and Model Results data for downstream reporting.</p><p><strong>API Documentation.</strong> The full endpoint catalog, request/response schemas, and a built-in &quot;try it out&quot; client are available at the API documentation page linked below. The documentation is the canonical reference &mdash; per-endpoint URLs are not duplicated here.</p><p><strong>Access Tokens.</strong> Each user has a personal access token below. Send it in the <code>x-api-key</code> HTTP header on every request. Keep your token private; if it leaks, regenerate it from this page to invalidate the old one.</p><p><strong>PowerBI users.</strong> PowerBI&rsquo;s data source dialog doesn&rsquo;t make custom HTTP headers easy. As a convenience, the token may also be passed as a query string parameter &mdash; append <code>?x-api-key=YOUR-TOKEN</code> to the endpoint URL. Prefer the header path whenever your client supports it; URLs with embedded tokens are logged in server access logs and browser history.</p><p>NOTE: The legacy <code>/PowerBI/&hellip;</code> URLs hosted by the previous MVC application have been retired. All consumers must use the new API documented below.</p>';

    UPDATE dbo.NeptunePage
    SET NeptunePageContent = @NewContent
    WHERE NeptunePageTypeID = 99
      AND NeptunePageContent = @OldContent

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-1078: document the ?x-api-key query string option added to WebServiceTokenAuthenticationHandler for PowerBI users'
END
