MERGE INTO dbo.OvtaAreaSourceType AS Target
USING (VALUES
(1, 'Parcel', 'Parcels'),
(2, 'LandUseBlock', 'Land Use Blocks')
)
AS Source (OvtaAreaSourceTypeID, OvtaAreaSourceTypeName, OvtaAreaSourceTypeDisplayName)
ON Target.OvtaAreaSourceTypeID = Source.OvtaAreaSourceTypeID
WHEN MATCHED THEN
UPDATE SET
	OvtaAreaSourceTypeName = Source.OvtaAreaSourceTypeName,
	OvtaAreaSourceTypeDisplayName = Source.OvtaAreaSourceTypeDisplayName
WHEN NOT MATCHED BY TARGET THEN
	INSERT (OvtaAreaSourceTypeID, OvtaAreaSourceTypeName, OvtaAreaSourceTypeDisplayName)
	VALUES (OvtaAreaSourceTypeID, OvtaAreaSourceTypeName, OvtaAreaSourceTypeDisplayName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;
