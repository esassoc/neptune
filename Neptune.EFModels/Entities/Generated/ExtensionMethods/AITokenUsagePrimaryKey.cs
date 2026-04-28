//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: dbo.AITokenUsage


namespace Neptune.EFModels.Entities
{
    public class AITokenUsagePrimaryKey : EntityPrimaryKey<AITokenUsage>
    {
        public AITokenUsagePrimaryKey() : base(){}
        public AITokenUsagePrimaryKey(int primaryKeyValue) : base(primaryKeyValue){}
        public AITokenUsagePrimaryKey(AITokenUsage aITokenUsage) : base(aITokenUsage){}

        public static implicit operator AITokenUsagePrimaryKey(int primaryKeyValue)
        {
            return new AITokenUsagePrimaryKey(primaryKeyValue);
        }

        public static implicit operator AITokenUsagePrimaryKey(AITokenUsage aITokenUsage)
        {
            return new AITokenUsagePrimaryKey(aITokenUsage);
        }
    }
}