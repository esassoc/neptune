//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[WaterQualityManagementPlanDevelopmentType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class WaterQualityManagementPlanDevelopmentType : IHavePrimaryKey
    {
        public static readonly WaterQualityManagementPlanDevelopmentTypeNewDevelopment NewDevelopment = WaterQualityManagementPlanDevelopmentTypeNewDevelopment.Instance;
        public static readonly WaterQualityManagementPlanDevelopmentTypeRedevelopment Redevelopment = WaterQualityManagementPlanDevelopmentTypeRedevelopment.Instance;

        public static readonly List<WaterQualityManagementPlanDevelopmentType> All;
        public static readonly ReadOnlyDictionary<int, WaterQualityManagementPlanDevelopmentType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static WaterQualityManagementPlanDevelopmentType()
        {
            All = new List<WaterQualityManagementPlanDevelopmentType> { NewDevelopment, Redevelopment };
            AllLookupDictionary = new ReadOnlyDictionary<int, WaterQualityManagementPlanDevelopmentType>(All.ToDictionary(x => x.WaterQualityManagementPlanDevelopmentTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected WaterQualityManagementPlanDevelopmentType(int waterQualityManagementPlanDevelopmentTypeID, string waterQualityManagementPlanDevelopmentTypeName, string waterQualityManagementPlanDevelopmentTypeDisplayName)
        {
            WaterQualityManagementPlanDevelopmentTypeID = waterQualityManagementPlanDevelopmentTypeID;
            WaterQualityManagementPlanDevelopmentTypeName = waterQualityManagementPlanDevelopmentTypeName;
            WaterQualityManagementPlanDevelopmentTypeDisplayName = waterQualityManagementPlanDevelopmentTypeDisplayName;
        }

        [Key]
        public int WaterQualityManagementPlanDevelopmentTypeID { get; private set; }
        public string WaterQualityManagementPlanDevelopmentTypeName { get; private set; }
        public string WaterQualityManagementPlanDevelopmentTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return WaterQualityManagementPlanDevelopmentTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(WaterQualityManagementPlanDevelopmentType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.WaterQualityManagementPlanDevelopmentTypeID == WaterQualityManagementPlanDevelopmentTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as WaterQualityManagementPlanDevelopmentType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return WaterQualityManagementPlanDevelopmentTypeID;
        }

        public static bool operator ==(WaterQualityManagementPlanDevelopmentType left, WaterQualityManagementPlanDevelopmentType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WaterQualityManagementPlanDevelopmentType left, WaterQualityManagementPlanDevelopmentType right)
        {
            return !Equals(left, right);
        }

        public WaterQualityManagementPlanDevelopmentTypeEnum ToEnum => (WaterQualityManagementPlanDevelopmentTypeEnum)GetHashCode();

        public static WaterQualityManagementPlanDevelopmentType ToType(int enumValue)
        {
            return ToType((WaterQualityManagementPlanDevelopmentTypeEnum)enumValue);
        }

        public static WaterQualityManagementPlanDevelopmentType ToType(WaterQualityManagementPlanDevelopmentTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case WaterQualityManagementPlanDevelopmentTypeEnum.NewDevelopment:
                    return NewDevelopment;
                case WaterQualityManagementPlanDevelopmentTypeEnum.Redevelopment:
                    return Redevelopment;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum WaterQualityManagementPlanDevelopmentTypeEnum
    {
        NewDevelopment = 1,
        Redevelopment = 2
    }

    public partial class WaterQualityManagementPlanDevelopmentTypeNewDevelopment : WaterQualityManagementPlanDevelopmentType
    {
        private WaterQualityManagementPlanDevelopmentTypeNewDevelopment(int waterQualityManagementPlanDevelopmentTypeID, string waterQualityManagementPlanDevelopmentTypeName, string waterQualityManagementPlanDevelopmentTypeDisplayName) : base(waterQualityManagementPlanDevelopmentTypeID, waterQualityManagementPlanDevelopmentTypeName, waterQualityManagementPlanDevelopmentTypeDisplayName) {}
        public static readonly WaterQualityManagementPlanDevelopmentTypeNewDevelopment Instance = new WaterQualityManagementPlanDevelopmentTypeNewDevelopment(1, @"NewDevelopment", @"New Development");
    }

    public partial class WaterQualityManagementPlanDevelopmentTypeRedevelopment : WaterQualityManagementPlanDevelopmentType
    {
        private WaterQualityManagementPlanDevelopmentTypeRedevelopment(int waterQualityManagementPlanDevelopmentTypeID, string waterQualityManagementPlanDevelopmentTypeName, string waterQualityManagementPlanDevelopmentTypeDisplayName) : base(waterQualityManagementPlanDevelopmentTypeID, waterQualityManagementPlanDevelopmentTypeName, waterQualityManagementPlanDevelopmentTypeDisplayName) {}
        public static readonly WaterQualityManagementPlanDevelopmentTypeRedevelopment Instance = new WaterQualityManagementPlanDevelopmentTypeRedevelopment(2, @"Redevelopment", @"Redevelopment");
    }
}