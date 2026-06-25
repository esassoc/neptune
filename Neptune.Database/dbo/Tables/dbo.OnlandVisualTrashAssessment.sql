CREATE TABLE [dbo].[OnlandVisualTrashAssessment](
	[OnlandVisualTrashAssessmentID] [int] IDENTITY(1,1) NOT NULL CONSTRAINT [PK_OnlandVisualTrashAssessment_OnlandVisualTrashAssessmentID] PRIMARY KEY,
	[CreatedByPersonID] [int] NOT NULL CONSTRAINT [FK_OnlandVisualTrashAssessment_Person_CreatedByPersonID_PersonID] FOREIGN KEY REFERENCES [dbo].[Person] ([PersonID]),
	[CreatedDate] [datetime] NOT NULL,
	[OnlandVisualTrashAssessmentAreaID] [int] NULL CONSTRAINT [FK_OnlandVisualTrashAssessment_OnlandVisualTrashAssessmentArea_OnlandVisualTrashAssessmentAreaID] FOREIGN KEY REFERENCES [dbo].[OnlandVisualTrashAssessmentArea] ([OnlandVisualTrashAssessmentAreaID]),
	[Notes] [varchar](500) NULL,
	[StormwaterJurisdictionID] [int] NOT NULL CONSTRAINT [FK_OnlandVisualTrashAssessment_StormwaterJurisdiction_StormwaterJurisdictionID] FOREIGN KEY REFERENCES [dbo].[StormwaterJurisdiction] ([StormwaterJurisdictionID]),
	[AssessingNewArea] [bit] NULL,
	[OnlandVisualTrashAssessmentStatusID] [int] NOT NULL CONSTRAINT [FK_OnlandVisualTrashAssessment_OnlandVisualTrashAssessmentStatus_OnlandVisualTrashAssessmentStatusID] FOREIGN KEY REFERENCES [dbo].[OnlandVisualTrashAssessmentStatus] ([OnlandVisualTrashAssessmentStatusID]),
	[DraftGeometry] [geometry] NULL,
	[IsDraftGeometryManuallyRefined] [bit] NULL,
	[OnlandVisualTrashAssessmentScoreID] [int] NULL CONSTRAINT [FK_OnlandVisualTrashAssessment_OnlandVisualTrashAssessmentScore_OnlandVisualTrashAssessmentScoreID] FOREIGN KEY REFERENCES [dbo].[OnlandVisualTrashAssessmentScore] ([OnlandVisualTrashAssessmentScoreID]),
	[CompletedDate] [date] NULL,
	[DraftAreaName] [varchar](100) NULL,
	[DraftAreaDescription] [varchar](500) NULL,
	[IsTransectBackingAssessment] [bit] NOT NULL,
	[IsProgressAssessment] [bit] NOT NULL,
	[SecondAssessorName] [varchar](200) NULL,
	[OvtaAreaSourceTypeID] [int] NULL CONSTRAINT [FK_OnlandVisualTrashAssessment_OvtaAreaSourceType_OvtaAreaSourceTypeID] FOREIGN KEY REFERENCES [dbo].[OvtaAreaSourceType] ([OvtaAreaSourceTypeID]),
	--CONSTRAINT [FK_OnlandVisualTrashAssessment_OnlandVisualTrashAssessmentArea_OnlandVisualTrashAssessmentAreaID_StormwaterJurisdictionID] FOREIGN KEY([OnlandVisualTrashAssessmentAreaID], [StormwaterJurisdictionID]) REFERENCES [dbo].[OnlandVisualTrashAssessmentArea] ([OnlandVisualTrashAssessmentAreaID], [StormwaterJurisdictionID]),
	CONSTRAINT [CK_OnlandVisualTrashAssessment_AssessmentCannotHaveDraftGeometryAndOfficialArea] CHECK  ((NOT ([DraftGeometry] IS NOT NULL AND [OnlandVisualTrashAssessmentAreaID] IS NOT NULL))),
	CONSTRAINT [CK_OnlandVisualTrashAssessment_AssessmentCannotHaveDraftGeometryWhenComplete] CHECK  ((NOT ([DraftGeometry] IS NOT NULL AND [OnlandVisualTrashAssessmentStatusID]=(2)))),
	CONSTRAINT [CK_OnlandVisualTrashAssessment_CompletedAssessmentMustHaveCompletedDate] CHECK  ((NOT ([CompletedDate] IS NULL AND [OnlandVisualTrashAssessmentStatusID]=(2)))),
	CONSTRAINT [CK_OnlandVisualTrashAssessment_CompletedAssessmentMustHaveScore] CHECK  ((NOT ([OnlandVisualTrashAssessmentScoreID] IS NULL AND [OnlandVisualTrashAssessmentStatusID]=(2))))
)
GO


CREATE SPATIAL INDEX [SPATIAL_OnlandVisualTrashAssessment_DraftGeometry] ON [dbo].[OnlandVisualTrashAssessment]
(
	[DraftGeometry]
)USING  GEOMETRY_AUTO_GRID
WITH (BOUNDING_BOX =(-118, 33, -117, 34),
CELLS_PER_OBJECT = 8, PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON)
GO

-- Supports the per-area "most recent assessment" (ovtaad) and progress-score
-- (vOnlandVisualTrashAssessmentAreaProgress) subqueries used by vTrashGeneratingUnitLoadStatistic.
-- Both do ROW_NUMBER() OVER (PARTITION BY OnlandVisualTrashAssessmentAreaID ORDER BY CompletedDate DESC);
-- without this index the trash-analysis-areas grid re-scanned + re-sorted this table ~1,680x (4.4M rows, ~2s).
-- Key order + DESC matches the window so the seek is already ordered; includes cover the projected columns.
CREATE NONCLUSTERED INDEX [IX_OnlandVisualTrashAssessment_OnlandVisualTrashAssessmentAreaID_CompletedDate] ON [dbo].[OnlandVisualTrashAssessment]
(
	[OnlandVisualTrashAssessmentAreaID] ASC,
	[CompletedDate] DESC
)
INCLUDE ([OnlandVisualTrashAssessmentScoreID], [IsProgressAssessment])