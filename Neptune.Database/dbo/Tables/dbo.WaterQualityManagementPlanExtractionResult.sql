CREATE TABLE [dbo].[WaterQualityManagementPlanExtractionResult]
(
    [WaterQualityManagementPlanExtractionResultID]  INT IDENTITY(1,1) NOT NULL,
    [WaterQualityManagementPlanID]                  INT NOT NULL,
    [WaterQualityManagementPlanDocumentID]           INT NOT NULL,
    [ExtractionResultJson]                          NVARCHAR(MAX) NOT NULL,
    [ExtractedAt]                                   DATETIME NOT NULL,
    -- NPT-1051 dropped DraftOverlayJson, DraftUpdatedByPersonID, DraftUpdatedDate,
    -- ApprovedByPersonID, ApprovedDate — the AI wizard was reframed as another data-entry
    -- method peer to the modal CRUD editors. Per-field draft state and the propose+approve
    -- cycle were removed. The DACPAC drops the columns on publish; data loss is bounded
    -- because the AI flow shipped recently in NPT-1020 with a small audit trail.

    CONSTRAINT [PK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanExtractionResultID] PRIMARY KEY ([WaterQualityManagementPlanExtractionResultID]),
    CONSTRAINT [FK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlan_WaterQualityManagementPlanID] FOREIGN KEY ([WaterQualityManagementPlanID]) REFERENCES dbo.WaterQualityManagementPlan([WaterQualityManagementPlanID]),
    CONSTRAINT [FK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanDocument_WaterQualityManagementPlanDocumentID] FOREIGN KEY ([WaterQualityManagementPlanDocumentID]) REFERENCES dbo.WaterQualityManagementPlanDocument([WaterQualityManagementPlanDocumentID]),
    CONSTRAINT [AK_WaterQualityManagementPlanExtractionResult_WaterQualityManagementPlanID] UNIQUE ([WaterQualityManagementPlanID]),
)
GO
