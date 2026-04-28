CREATE TABLE [dbo].[WaterQualityManagementPlanDocument](
	[WaterQualityManagementPlanDocumentID] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WaterQualityManagementPlanDocument_WaterQualityManagementPlanDocumentID] PRIMARY KEY,
	[WaterQualityManagementPlanID] [int] NOT NULL CONSTRAINT [FK_WaterQualityManagementPlanDocument_WaterQualityManagementPlan_WaterQualityManagementPlanID] FOREIGN KEY REFERENCES [dbo].[WaterQualityManagementPlan] ([WaterQualityManagementPlanID]),
	[FileResourceID] [int] NOT NULL CONSTRAINT [FK_WaterQualityManagementPlanDocument_FileResource_FileResourceID] FOREIGN KEY REFERENCES [dbo].[FileResource] ([FileResourceID]),
	[DisplayName] [varchar](100),
	[Description] [varchar](1000) NULL,
	[UploadDate] [datetime] NOT NULL,
	[WaterQualityManagementPlanDocumentTypeID] [int] NOT NULL CONSTRAINT [FK_WaterQualityManagementPlanDocument_WaterQualityManagementPlanDocumentType_WaterQualityManagementPlanDocumentTypeID] FOREIGN KEY REFERENCES [dbo].[WaterQualityManagementPlanDocumentType] ([WaterQualityManagementPlanDocumentTypeID]),
	[AnthropicFileID] [nvarchar](64) NULL,
	[AnthropicFileUploadedDate] [datetime] NULL,
	CONSTRAINT [AK_WaterQualityManagementPlanDocument_DisplayName_WaterQualityManagementPlanID] UNIQUE ([DisplayName], [WaterQualityManagementPlanID])
)