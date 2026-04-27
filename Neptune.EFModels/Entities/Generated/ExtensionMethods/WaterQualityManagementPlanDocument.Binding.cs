//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[WaterQualityManagementPlanDocument]
namespace Neptune.EFModels.Entities
{
    public partial class WaterQualityManagementPlanDocument : IHavePrimaryKey
    {
        public int PrimaryKey => WaterQualityManagementPlanDocumentID;
        public WaterQualityManagementPlanDocumentType WaterQualityManagementPlanDocumentType => WaterQualityManagementPlanDocumentType.AllLookupDictionary[WaterQualityManagementPlanDocumentTypeID];

        public static class FieldLengths
        {
            public const int DisplayName = 100;
            public const int Description = 1000;
            public const int AnthropicFileID = 64;
        }
    }
}