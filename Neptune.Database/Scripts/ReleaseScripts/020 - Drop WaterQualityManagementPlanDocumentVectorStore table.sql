-- NPT-1037: Drop the OpenAI vector store tracking table (migrating to Claude)
IF OBJECT_ID('dbo.WaterQualityManagementPlanDocumentVectorStore', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.WaterQualityManagementPlanDocumentVectorStore;
    PRINT 'Dropped WaterQualityManagementPlanDocumentVectorStore table.';
END
