//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: dbo.LoadGeneratingUnitStaging


namespace Neptune.EFModels.Entities
{
    public class LoadGeneratingUnitStagingPrimaryKey : EntityPrimaryKey<LoadGeneratingUnitStaging>
    {
        public LoadGeneratingUnitStagingPrimaryKey() : base(){}
        public LoadGeneratingUnitStagingPrimaryKey(int primaryKeyValue) : base(primaryKeyValue){}
        public LoadGeneratingUnitStagingPrimaryKey(LoadGeneratingUnitStaging loadGeneratingUnitStaging) : base(loadGeneratingUnitStaging){}

        public static implicit operator LoadGeneratingUnitStagingPrimaryKey(int primaryKeyValue)
        {
            return new LoadGeneratingUnitStagingPrimaryKey(primaryKeyValue);
        }

        public static implicit operator LoadGeneratingUnitStagingPrimaryKey(LoadGeneratingUnitStaging loadGeneratingUnitStaging)
        {
            return new LoadGeneratingUnitStagingPrimaryKey(loadGeneratingUnitStaging);
        }
    }
}