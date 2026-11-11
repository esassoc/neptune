DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '043 - NPT-1137 Add Field Definitions for Simple BMP grid';

IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

	PRINT @MigrationName;

	insert into dbo.FieldDefinition(FieldDefinitionTypeID, FieldDefinitionValue)
	values
		(148, '<p>The recorded area of the WQMP in acres. When no recorded area is entered, the area of the WQMP boundary is used.</p>'),
		(149, '<p>The area treated by the simple BMP in acres, calculated as % of Site Treated multiplied by the WQMP''s Calculated Boundary Acreage.</p>')

	INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
	SELECT 'Shannon Bulloch', @MigrationName, 'NPT-1137: Field definitions for the Simple BMP grid on the WQMP list page'
END
