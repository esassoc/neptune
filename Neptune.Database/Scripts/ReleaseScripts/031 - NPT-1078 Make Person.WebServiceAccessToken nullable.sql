DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '031 - NPT-1078 Make Person.WebServiceAccessToken nullable'
IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    -- NPT-1078: Person.WebServiceAccessToken used to be auto-populated with Guid.NewGuid() at
    -- Person creation. The new Web Services tab gives users an explicit "Generate Token" button
    -- and treats a missing token as "not yet generated", so the column must be nullable.
    ALTER TABLE dbo.Person ALTER COLUMN WebServiceAccessToken UNIQUEIDENTIFIER NULL;

    -- Some legacy rows have WebServiceAccessToken = '00000000-0000-0000-0000-000000000000'
    -- (the result of the WebMvc UserContext anonymous-DTO path being persisted somewhere it
    -- shouldn't have been). Null those out so the new auth handler can't be tricked into
    -- authenticating any caller that sends an all-zeros GUID. Users in this state will see the
    -- "Generate Token" affordance in the SPA the next time they visit the Web Services tab.
    UPDATE dbo.Person
    SET WebServiceAccessToken = NULL
    WHERE WebServiceAccessToken = '00000000-0000-0000-0000-000000000000';

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-1078: make Person.WebServiceAccessToken nullable so the SPA Web Services tab can render a Generate Token affordance for users without one. Also nulls out legacy Guid.Empty values that would otherwise allow trivial impersonation.'
END
