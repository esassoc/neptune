CREATE TABLE [dbo].[SourceControlBMP](
	[SourceControlBMPID] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_SourceControlBMP_SourceControlBMPID] PRIMARY KEY,
	[WaterQualityManagementPlanID] [int] NOT NULL CONSTRAINT [FK_SourceControlBMP_WaterQualityManagementPlan_WaterQualityManagementPlanID] FOREIGN KEY REFERENCES [dbo].[WaterQualityManagementPlan] ([WaterQualityManagementPlanID]),
	[SourceControlBMPAttributeID] [int] NOT NULL CONSTRAINT [FK_SourceControlBMP_SourceControlBMPAttribute_SourceControlBMPAttributeID] FOREIGN KEY REFERENCES [dbo].[SourceControlBMPAttribute] ([SourceControlBMPAttributeID]),
	[IsPresent] [bit] NULL,
	[SourceControlBMPNote] [varchar](500) NULL
)