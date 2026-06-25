DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '033 - NPT-1068 Seed OCTAPrioritizationDataHub rich text'
IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

    PRINT @MigrationName;

    -- NPT-1068: the SPA Data Hub County GIS tab adds a sixth tile, OCTA Prioritization,
    -- alongside the existing Parcels / Regional Subbasins / HRU Characteristics /
    -- Model Basins / Precipitation Zones refresh tiles. Seed starter help text matching
    -- the sibling-tile style (short paragraph: what the layer is + how it refreshes).
    -- Only fill blank/null rows so we don't clobber any admin edit made via the in-page
    -- editor.
    DECLARE @Content NVARCHAR(MAX) = N'<p class="MsoNormal">OCTA Prioritization ranks catchments across Orange County by water quality benefit metrics and is consumed by the OCTA M2 Tier 2 grant program dashboard in the Planning module. Admins can refresh OCTA Prioritization from OC Survey. There is no automated job to refresh this layer because this dataset is very stable.<span style="mso-spacerun: yes;">&nbsp; </span></p>';

    IF EXISTS (SELECT 1 FROM dbo.NeptunePage WHERE NeptunePageTypeID = 100)
        UPDATE dbo.NeptunePage
        SET NeptunePageContent = @Content
        WHERE NeptunePageTypeID = 100
          AND (NeptunePageContent IS NULL OR LTRIM(RTRIM(NeptunePageContent)) = '')
    ELSE
        INSERT INTO dbo.NeptunePage (NeptunePageTypeID, NeptunePageContent)
        VALUES (100, @Content)

    INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
    SELECT 'Ray Lee', @MigrationName, 'NPT-1068: seed help text for the new SPA Data Hub County GIS tab OCTA Prioritization tile (NeptunePageType 100) replacing the legacy MVC TreatmentBMP/Index admin dropdown link'
END
