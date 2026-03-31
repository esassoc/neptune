MERGE INTO dbo.WaterQualityManagementPlanStatus AS Target
USING (VALUES
(1, 'Active', 'Active'),
(2, 'Inactive', 'Inactive'),
(3, 'Draft', 'Draft')
)
AS Source (WaterQualityManagementPlanStatusID, WaterQualityManagementPlanStatusName, WaterQualityManagementPlanStatusDisplayName)
ON Target.WaterQualityManagementPlanStatusID = Source.WaterQualityManagementPlanStatusID
WHEN MATCHED THEN
UPDATE SET
	WaterQualityManagementPlanStatusName = Source.WaterQualityManagementPlanStatusName,
	WaterQualityManagementPlanStatusDisplayName = Source.WaterQualityManagementPlanStatusDisplayName
WHEN NOT MATCHED BY TARGET THEN
	INSERT (WaterQualityManagementPlanStatusID, WaterQualityManagementPlanStatusName, WaterQualityManagementPlanStatusDisplayName)
	VALUES (WaterQualityManagementPlanStatusID, WaterQualityManagementPlanStatusName, WaterQualityManagementPlanStatusDisplayName)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;
