CREATE TABLE [dbo].[OvtaAreaSourceType](
	[OvtaAreaSourceTypeID] [int] NOT NULL CONSTRAINT [PK_OvtaAreaSourceType_OvtaAreaSourceTypeID] PRIMARY KEY,
	[OvtaAreaSourceTypeName] [varchar](100) CONSTRAINT [AK_OvtaAreaSourceType_OvtaAreaSourceTypeName] UNIQUE,
	[OvtaAreaSourceTypeDisplayName] [varchar](100) CONSTRAINT [AK_OvtaAreaSourceType_OvtaAreaSourceTypeDisplayName] UNIQUE
)
