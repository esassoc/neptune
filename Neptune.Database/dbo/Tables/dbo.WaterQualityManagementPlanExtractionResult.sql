CREATE TABLE [dbo].[WaterQualityManagementPlanExtractionResult]
(
    [WaterQualityManagementPlanExtractionResultID]  INT IDENTITY(1,1) NOT NULL,
    [WaterQualityManagementPlanID]                  INT NOT NULL,
    [WaterQualityManagementPlanDocumentID]           INT NOT NULL,
    [ExtractionResultJson]                          NVARCHAR(MAX) NOT NULL,
    [ExtractedAt]                                   DATETIME NOT NULL,
    [DraftOverlayJson]                              NVARCHAR(MAX) NULL,
    [DraftUpdatedByPersonID]                        INT NULL,
    [DraftUpdatedDate]                              DATETIME NULL,
    [ApprovedByPersonID]                            INT NULL,
    [ApprovedDate]                                  DATETIME NULL,

    CONSTRAINT [PK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanExtractionResultID] PRIMARY KEY ([WaterQualityManagementPlanExtractionResultID]),
    CONSTRAINT [FK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlan_WaterQualityManagementPlanID] FOREIGN KEY ([WaterQualityManagementPlanID]) REFERENCES dbo.WaterQualityManagementPlan([WaterQualityManagementPlanID]),
    CONSTRAINT [FK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanDocument_WaterQualityManagementPlanDocumentID] FOREIGN KEY ([WaterQualityManagementPlanDocumentID]) REFERENCES dbo.WaterQualityManagementPlanDocument([WaterQualityManagementPlanDocumentID]),
    CONSTRAINT [FK_WaterQualityManagementPlanExtractionResult_Person_DraftUpdatedByPersonID_PersonID] FOREIGN KEY ([DraftUpdatedByPersonID]) REFERENCES dbo.Person([PersonID]),
    CONSTRAINT [FK_WaterQualityManagementPlanExtractionResult_Person_ApprovedByPersonID_PersonID] FOREIGN KEY ([ApprovedByPersonID]) REFERENCES dbo.Person([PersonID]),
    CONSTRAINT [AK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanID] UNIQUE ([WaterQualityManagementPlanID]),
)
GO
