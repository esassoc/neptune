//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: dbo.OvtaAreaSourceType


namespace Neptune.EFModels.Entities
{
    public class OvtaAreaSourceTypePrimaryKey : EntityPrimaryKey<OvtaAreaSourceType>
    {
        public OvtaAreaSourceTypePrimaryKey() : base(){}
        public OvtaAreaSourceTypePrimaryKey(int primaryKeyValue) : base(primaryKeyValue){}
        public OvtaAreaSourceTypePrimaryKey(OvtaAreaSourceType ovtaAreaSourceType) : base(ovtaAreaSourceType){}

        public static implicit operator OvtaAreaSourceTypePrimaryKey(int primaryKeyValue)
        {
            return new OvtaAreaSourceTypePrimaryKey(primaryKeyValue);
        }

        public static implicit operator OvtaAreaSourceTypePrimaryKey(OvtaAreaSourceType ovtaAreaSourceType)
        {
            return new OvtaAreaSourceTypePrimaryKey(ovtaAreaSourceType);
        }
    }
}