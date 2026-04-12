CREATE TABLE [dbo].[WaterQualityManagementPlanExtractionResult]
(
    [WaterQualityManagementPlanExtractionResultID]  INT IDENTITY(1,1) NOT NULL,
    [WaterQualityManagementPlanID]                  INT NOT NULL,
    [WaterQualityManagementPlanDocumentID]           INT NOT NULL,
    [ExtractionResultJson]                          NVARCHAR(MAX) NOT NULL,
    [ExtractedAt]                                   DATETIME NOT NULL,

    CONSTRAINT [PK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanExtractionResultID] PRIMARY KEY ([WaterQualityManagementPlanExtractionResultID]),
    CONSTRAINT [FK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlan_WaterQualityManagementPlanID] FOREIGN KEY ([WaterQualityManagementPlanID]) REFERENCES dbo.WaterQualityManagementPlan([WaterQualityManagementPlanID]),
    CONSTRAINT [FK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanDocument_WaterQualityManagementPlanDocumentID] FOREIGN KEY ([WaterQualityManagementPlanDocumentID]) REFERENCES dbo.WaterQualityManagementPlanDocument([WaterQualityManagementPlanDocumentID]),
    CONSTRAINT [AK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanID] UNIQUE ([WaterQualityManagementPlanID]),
)
GO
