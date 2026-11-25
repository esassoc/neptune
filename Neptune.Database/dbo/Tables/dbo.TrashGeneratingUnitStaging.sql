CREATE TABLE [dbo].[TrashGeneratingUnitStaging](
	[TrashGeneratingUnitStagingID] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_TrashGeneratingUnitStaging_TrashGeneratingUnitStagingID] PRIMARY KEY,
	[StormwaterJurisdictionID] [int] NOT NULL,
	[OnlandVisualTrashAssessmentAreaID] [int] NULL,
	[LandUseBlockID] [int] NULL,
	[DelineationID] [int] NULL,
	[WaterQualityManagementPlanID] [int] NULL,
	[TrashGeneratingUnitGeometry] [geometry] NOT NULL,
	[TrashGeneratingUnitGeometry4326] [geometry] NOT NULL,
	[LastUpdateDate] [datetime] NOT NULL
)
GO
