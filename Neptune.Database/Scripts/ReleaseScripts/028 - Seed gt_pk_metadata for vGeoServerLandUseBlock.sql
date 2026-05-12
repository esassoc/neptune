-- NPT-1032: Tell GeoServer to use the vGeoServerLandUseBlock view's synthetic `PrimaryKey`
-- column as the feature ID. Without this, GeoServer's JDBC store may auto-detect any of the
-- *ID columns as the FID and silently drop it from the queryable schema, breaking CQL filters
-- like `StormwaterJurisdictionID = N`. Same pattern already in place for vGeoServerDelineation.
MERGE INTO dbo.gt_pk_metadata AS Target
USING (VALUES
('dbo', 'vGeoServerLandUseBlock', 'PrimaryKey', 0, 'assigned')
)
AS Source (table_schema, table_name, pk_column, pk_column_idx, pk_policy)
ON Target.table_schema = Source.table_schema
   AND Target.table_name = Source.table_name
   AND Target.pk_column = Source.pk_column
WHEN MATCHED THEN
UPDATE SET
	pk_column_idx = Source.pk_column_idx,
	pk_policy = Source.pk_policy
WHEN NOT MATCHED BY TARGET THEN
	INSERT (table_schema, table_name, pk_column, pk_column_idx, pk_policy)
	VALUES (Source.table_schema, Source.table_name, Source.pk_column, Source.pk_column_idx, Source.pk_policy);
