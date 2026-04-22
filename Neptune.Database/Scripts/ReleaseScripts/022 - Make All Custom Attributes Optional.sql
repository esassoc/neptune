-- NPT-1030: Make all custom attributes optional (Modeling and Maintenance purposes; OtherDesignAttributes already handled by script 021)
UPDATE dbo.CustomAttributeType
SET IsRequired = 0
WHERE IsRequired = 1;
