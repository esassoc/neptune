DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '026 - Add AnthropicFileID to WaterQualityManagementPlanDocument'
IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    -- NPT-1044: cache the Anthropic Files-API file_id on the document so re-extracts and
    -- chat sessions can reference the same uploaded PDF without re-uploading. Lifts the
    -- 32 MB document-block ceiling we currently enforce at upload + extract by switching
    -- to a Files-API source on the Beta Messages API.
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.WaterQualityManagementPlanDocument') AND name = 'AnthropicFileID')
    BEGIN
        ALTER TABLE dbo.WaterQualityManagementPlanDocument ADD AnthropicFileID NVARCHAR(64) NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.WaterQualityManagementPlanDocument') AND name = 'AnthropicFileUploadedDate')
    BEGIN
        ALTER TABLE dbo.WaterQualityManagementPlanDocument ADD AnthropicFileUploadedDate DATETIME NULL;
    END

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-1044: cache Anthropic Files-API file_id to support PDFs >32 MB on extraction + chat'
END
