CREATE TABLE [dbo].[LoadGeneratingUnitStaging](
	[LoadGeneratingUnitStagingID] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_LoadGeneratingUnitStaging_LoadGeneratingUnitStagingID] PRIMARY KEY,
	[ModelBasinID] [int] NULL,
	[RegionalSubbasinID] [int] NULL,
	[DelineationID] [int] NULL,
	[WaterQualityManagementPlanID] [int] NULL,
	[LoadGeneratingUnitGeometry] [geometry] NOT NULL,
	[LoadGeneratingUnitGeometry4326] [geometry] NOT NULL
)
GO
