MERGE INTO dbo.Role AS Target
USING (VALUES
(1, 'Admin', 'Administrator', ''),
(3, 'Unassigned', 'Unassigned', ''),
(4, 'SitkaAdmin', 'Sitka Administrator', ''),
(5, 'JurisdictionManager', 'Jurisdiction Manager', ''),
(6, 'JurisdictionEditor', 'Jurisdiction Editor', '')
)
AS Source (RoleID, RoleName, RoleDisplayName, RoleDescription)
ON Target.RoleID = Source.RoleID
WHEN MATCHED THEN
UPDATE SET
	RoleName = Source.RoleName,
	RoleDisplayName = Source.RoleDisplayName,
	RoleDescription = Source.RoleDescription
WHEN NOT MATCHED BY TARGET THEN
	INSERT (RoleID, RoleName, RoleDisplayName, RoleDescription)
	VALUES (RoleID, RoleName, RoleDisplayName, RoleDescription)
WHEN NOT MATCHED BY SOURCE THEN
	DELETE;