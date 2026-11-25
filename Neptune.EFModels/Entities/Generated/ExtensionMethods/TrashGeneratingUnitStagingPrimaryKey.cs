//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: dbo.TrashGeneratingUnitStaging


namespace Neptune.EFModels.Entities
{
    public class TrashGeneratingUnitStagingPrimaryKey : EntityPrimaryKey<TrashGeneratingUnitStaging>
    {
        public TrashGeneratingUnitStagingPrimaryKey() : base(){}
        public TrashGeneratingUnitStagingPrimaryKey(int primaryKeyValue) : base(primaryKeyValue){}
        public TrashGeneratingUnitStagingPrimaryKey(TrashGeneratingUnitStaging trashGeneratingUnitStaging) : base(trashGeneratingUnitStaging){}

        public static implicit operator TrashGeneratingUnitStagingPrimaryKey(int primaryKeyValue)
        {
            return new TrashGeneratingUnitStagingPrimaryKey(primaryKeyValue);
        }

        public static implicit operator TrashGeneratingUnitStagingPrimaryKey(TrashGeneratingUnitStaging trashGeneratingUnitStaging)
        {
            return new TrashGeneratingUnitStagingPrimaryKey(trashGeneratingUnitStaging);
        }
    }
}