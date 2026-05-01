CREATE TABLE [dbo].[WaterQualityManagementPlanVerify](
	[WaterQualityManagementPlanVerifyID] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_WaterQualityManagementPlanVerify_WaterQualityManagementPlanVerifyID] PRIMARY KEY,
	[WaterQualityManagementPlanID] [int] NOT NULL CONSTRAINT [FK_WaterQualityManagementPlanVerify_WaterQualityManagementPlan_WaterQualityManagementPlanID] FOREIGN KEY REFERENCES [dbo].[WaterQualityManagementPlan] ([WaterQualityManagementPlanID]),
	[WaterQualityManagementPlanVerifyTypeID] [int] NOT NULL CONSTRAINT [FK_WaterQualityManagementPlanVerify_WaterQualityManagementPlanVerifyType_WaterQualityManagementPlanVerifyTypeID] FOREIGN KEY REFERENCES [dbo].[WaterQualityManagementPlanVerifyType] ([WaterQualityManagementPlanVerifyTypeID]),
	[WaterQualityManagementPlanVisitStatusID] [int] NOT NULL CONSTRAINT [FK_WaterQualityManagementPlanVerify_WaterQualityManagementPlanVisitStatus_WaterQualityManagementPlanVisitStatusID] FOREIGN KEY REFERENCES [dbo].[WaterQualityManagementPlanVisitStatus] ([WaterQualityManagementPlanVisitStatusID]),
	[FileResourceID] [int] NULL CONSTRAINT [FK_WaterQualityManagementPlanVerify_FileResource_FileResourceID] FOREIGN KEY REFERENCES [dbo].[FileResource] ([FileResourceID]),
	[WaterQualityManagementPlanVerifyStatusID] [int] NULL CONSTRAINT [FK_WaterQualityManagementPlanVerify_WaterQualityManagementPlanVerifyStatus_WaterQualityManagementPlanVerifyStatusID] FOREIGN KEY REFERENCES [dbo].[WaterQualityManagementPlanVerifyStatus] ([WaterQualityManagementPlanVerifyStatusID]),
	[LastEditedByPersonID] [int] NOT NULL CONSTRAINT [FK_WaterQualityManagementPlanVerify_Person_LastEditedByPersonID_PersonID] FOREIGN KEY REFERENCES [dbo].[Person] ([PersonID]),
	[SourceControlCondition] [varchar](1000) NULL,
	[EnforcementOrFollowupActions] [varchar](1000) NULL,
	[LastEditedDate] [datetime] NOT NULL,
	[IsDraft] [bit] NOT NULL,
	[VerificationDate] [date] NOT NULL
)