//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[WaterQualityManagementPlanDocumentType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class WaterQualityManagementPlanDocumentType : IHavePrimaryKey
    {
        public static readonly WaterQualityManagementPlanDocumentTypeFinalWQMP FinalWQMP = WaterQualityManagementPlanDocumentTypeFinalWQMP.Instance;
        public static readonly WaterQualityManagementPlanDocumentTypeAsBuiltDrawings AsBuiltDrawings = WaterQualityManagementPlanDocumentTypeAsBuiltDrawings.Instance;
        public static readonly WaterQualityManagementPlanDocumentTypeOMPlan OMPlan = WaterQualityManagementPlanDocumentTypeOMPlan.Instance;
        public static readonly WaterQualityManagementPlanDocumentTypeOther Other = WaterQualityManagementPlanDocumentTypeOther.Instance;

        public static readonly List<WaterQualityManagementPlanDocumentType> All;
        public static readonly ReadOnlyDictionary<int, WaterQualityManagementPlanDocumentType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static WaterQualityManagementPlanDocumentType()
        {
            All = new List<WaterQualityManagementPlanDocumentType> { FinalWQMP, AsBuiltDrawings, OMPlan, Other };
            AllLookupDictionary = new ReadOnlyDictionary<int, WaterQualityManagementPlanDocumentType>(All.ToDictionary(x => x.WaterQualityManagementPlanDocumentTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected WaterQualityManagementPlanDocumentType(int waterQualityManagementPlanDocumentTypeID, string waterQualityManagementPlanDocumentTypeName, string waterQualityManagementPlanDocumentTypeDisplayName, bool isRequired)
        {
            WaterQualityManagementPlanDocumentTypeID = waterQualityManagementPlanDocumentTypeID;
            WaterQualityManagementPlanDocumentTypeName = waterQualityManagementPlanDocumentTypeName;
            WaterQualityManagementPlanDocumentTypeDisplayName = waterQualityManagementPlanDocumentTypeDisplayName;
            IsRequired = isRequired;
        }

        [Key]
        public int WaterQualityManagementPlanDocumentTypeID { get; private set; }
        public string WaterQualityManagementPlanDocumentTypeName { get; private set; }
        public string WaterQualityManagementPlanDocumentTypeDisplayName { get; private set; }
        public bool IsRequired { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return WaterQualityManagementPlanDocumentTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(WaterQualityManagementPlanDocumentType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.WaterQualityManagementPlanDocumentTypeID == WaterQualityManagementPlanDocumentTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as WaterQualityManagementPlanDocumentType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return WaterQualityManagementPlanDocumentTypeID;
        }

        public static bool operator ==(WaterQualityManagementPlanDocumentType left, WaterQualityManagementPlanDocumentType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WaterQualityManagementPlanDocumentType left, WaterQualityManagementPlanDocumentType right)
        {
            return !Equals(left, right);
        }

        public WaterQualityManagementPlanDocumentTypeEnum ToEnum => (WaterQualityManagementPlanDocumentTypeEnum)GetHashCode();

        public static WaterQualityManagementPlanDocumentType ToType(int enumValue)
        {
            return ToType((WaterQualityManagementPlanDocumentTypeEnum)enumValue);
        }

        public static WaterQualityManagementPlanDocumentType ToType(WaterQualityManagementPlanDocumentTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case WaterQualityManagementPlanDocumentTypeEnum.AsBuiltDrawings:
                    return AsBuiltDrawings;
                case WaterQualityManagementPlanDocumentTypeEnum.FinalWQMP:
                    return FinalWQMP;
                case WaterQualityManagementPlanDocumentTypeEnum.OMPlan:
                    return OMPlan;
                case WaterQualityManagementPlanDocumentTypeEnum.Other:
                    return Other;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum WaterQualityManagementPlanDocumentTypeEnum
    {
        FinalWQMP = 1,
        AsBuiltDrawings = 2,
        OMPlan = 3,
        Other = 4
    }

    public partial class WaterQualityManagementPlanDocumentTypeFinalWQMP : WaterQualityManagementPlanDocumentType
    {
        private WaterQualityManagementPlanDocumentTypeFinalWQMP(int waterQualityManagementPlanDocumentTypeID, string waterQualityManagementPlanDocumentTypeName, string waterQualityManagementPlanDocumentTypeDisplayName, bool isRequired) : base(waterQualityManagementPlanDocumentTypeID, waterQualityManagementPlanDocumentTypeName, waterQualityManagementPlanDocumentTypeDisplayName, isRequired) {}
        public static readonly WaterQualityManagementPlanDocumentTypeFinalWQMP Instance = new WaterQualityManagementPlanDocumentTypeFinalWQMP(1, @"FinalWQMP", @"Final WQMP", true);
    }

    public partial class WaterQualityManagementPlanDocumentTypeAsBuiltDrawings : WaterQualityManagementPlanDocumentType
    {
        private WaterQualityManagementPlanDocumentTypeAsBuiltDrawings(int waterQualityManagementPlanDocumentTypeID, string waterQualityManagementPlanDocumentTypeName, string waterQualityManagementPlanDocumentTypeDisplayName, bool isRequired) : base(waterQualityManagementPlanDocumentTypeID, waterQualityManagementPlanDocumentTypeName, waterQualityManagementPlanDocumentTypeDisplayName, isRequired) {}
        public static readonly WaterQualityManagementPlanDocumentTypeAsBuiltDrawings Instance = new WaterQualityManagementPlanDocumentTypeAsBuiltDrawings(2, @"AsBuiltDrawings", @"As-built drawings", true);
    }

    public partial class WaterQualityManagementPlanDocumentTypeOMPlan : WaterQualityManagementPlanDocumentType
    {
        private WaterQualityManagementPlanDocumentTypeOMPlan(int waterQualityManagementPlanDocumentTypeID, string waterQualityManagementPlanDocumentTypeName, string waterQualityManagementPlanDocumentTypeDisplayName, bool isRequired) : base(waterQualityManagementPlanDocumentTypeID, waterQualityManagementPlanDocumentTypeName, waterQualityManagementPlanDocumentTypeDisplayName, isRequired) {}
        public static readonly WaterQualityManagementPlanDocumentTypeOMPlan Instance = new WaterQualityManagementPlanDocumentTypeOMPlan(3, @"OMPlan", @"O&M Plan", true);
    }

    public partial class WaterQualityManagementPlanDocumentTypeOther : WaterQualityManagementPlanDocumentType
    {
        private WaterQualityManagementPlanDocumentTypeOther(int waterQualityManagementPlanDocumentTypeID, string waterQualityManagementPlanDocumentTypeName, string waterQualityManagementPlanDocumentTypeDisplayName, bool isRequired) : base(waterQualityManagementPlanDocumentTypeID, waterQualityManagementPlanDocumentTypeName, waterQualityManagementPlanDocumentTypeDisplayName, isRequired) {}
        public static readonly WaterQualityManagementPlanDocumentTypeOther Instance = new WaterQualityManagementPlanDocumentTypeOther(4, @"Other", @"Other", false);
    }
}