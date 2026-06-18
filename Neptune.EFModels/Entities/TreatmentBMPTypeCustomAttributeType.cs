using System.ComponentModel.DataAnnotations.Schema;

namespace Neptune.EFModels.Entities
{
    public partial class TreatmentBMPTypeCustomAttributeType : IHaveASortOrder
    {
        public string GetDisplayName()
        {
            return CustomAttributeType.CustomAttributeTypeName;
        }

        public int GetID()
        {
            return TreatmentBMPTypeCustomAttributeTypeID;
        }
    }
}