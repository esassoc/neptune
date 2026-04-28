-- NPT-1030: Make Other Design Attributes optional (they were marked required but never enforced)
UPDATE dbo.CustomAttributeType
SET IsRequired = 0
WHERE CustomAttributeTypePurposeID = 2; -- OtherDesignAttributes
