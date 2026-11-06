DECLARE @MigrationName VARCHAR(200);
SET @MigrationName = '044 - NPT-1137 Update Simple BMP area field definitions';

IF NOT EXISTS(SELECT * FROM dbo.DatabaseMigration DM WHERE DM.ReleaseScriptFileName = @MigrationName)
BEGIN

	PRINT @MigrationName;

	-- Calculated Boundary Acreage is now the GIS boundary area only, and Drainage Area Treated falls back to the
	-- recorded area only when there's no boundary (AP decision 11/4). Field definitions are admin-editable, so only
	-- replace text still matching what 043 inserted.
	update dbo.FieldDefinition
	set FieldDefinitionValue = '<p>The area of the WQMP boundary in acres, calculated from its mapped boundary.</p>'
	where FieldDefinitionTypeID = 148
		and FieldDefinitionValue = '<p>The recorded area of the WQMP in acres. When no recorded area is entered, the area of the WQMP boundary is used.</p>'

	update dbo.FieldDefinition
	set FieldDefinitionValue = '<p>The area treated by the simple BMP in acres: % of Site Treated multiplied by the WQMP''s Calculated Boundary Acreage, or by its Recorded WQMP Area when the WQMP has no boundary.</p>'
	where FieldDefinitionTypeID = 149
		and FieldDefinitionValue = '<p>The area treated by the simple BMP in acres, calculated as % of Site Treated multiplied by the WQMP''s Calculated Boundary Acreage.</p>'

	INSERT INTO dbo.DatabaseMigration(MigrationAuthorName, ReleaseScriptFileName, MigrationReason)
	SELECT 'Shannon Bulloch', @MigrationName, 'NPT-1137 rework: Simple BMP grid area field definitions'
END
