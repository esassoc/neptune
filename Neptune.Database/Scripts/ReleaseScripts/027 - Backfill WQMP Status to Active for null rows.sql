DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '027 - Backfill WQMP Status to Active for null rows'

IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    -- NPT-1051: WQMP Status is becoming a legal-record gate that excludes non-Active WQMPs from
    -- modeling and trash result calculations. Per data-steward decision, every WQMP that exists is
    -- a binding agreement and therefore Active by default. Backfill any null Status rows to Active
    -- so the new filters in NereidService.GetWaterQualityManagementPlanNodes and
    -- vTrashGeneratingUnitLoadStatistic don't silently drop them.

    UPDATE dbo.WaterQualityManagementPlan
    SET WaterQualityManagementPlanStatusID = 1 -- Active
    WHERE WaterQualityManagementPlanStatusID IS NULL

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-1051: default any null WQMP Status to Active before adding Status==Active filters to modeling and trash pipelines'
END
