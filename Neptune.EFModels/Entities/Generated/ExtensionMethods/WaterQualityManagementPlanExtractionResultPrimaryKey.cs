//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: dbo.WaterQualityManagementPlanExtractionResult


namespace Neptune.EFModels.Entities
{
    public class WaterQualityManagementPlanExtractionResultPrimaryKey : EntityPrimaryKey<WaterQualityManagementPlanExtractionResult>
    {
        public WaterQualityManagementPlanExtractionResultPrimaryKey() : base(){}
        public WaterQualityManagementPlanExtractionResultPrimaryKey(int primaryKeyValue) : base(primaryKeyValue){}
        public WaterQualityManagementPlanExtractionResultPrimaryKey(WaterQualityManagementPlanExtractionResult waterQualityManagementPlanExtractionResult) : base(waterQualityManagementPlanExtractionResult){}

        public static implicit operator WaterQualityManagementPlanExtractionResultPrimaryKey(int primaryKeyValue)
        {
            return new WaterQualityManagementPlanExtractionResultPrimaryKey(primaryKeyValue);
        }

        public static implicit operator WaterQualityManagementPlanExtractionResultPrimaryKey(WaterQualityManagementPlanExtractionResult waterQualityManagementPlanExtractionResult)
        {
            return new WaterQualityManagementPlanExtractionResultPrimaryKey(waterQualityManagementPlanExtractionResult);
        }
    }
}