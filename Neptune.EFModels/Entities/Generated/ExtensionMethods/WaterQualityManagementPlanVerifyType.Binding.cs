//  IMPORTANT:
//  This file is generated. Your changes will be lost.
//  Use the corresponding partial class for customizations.
//  Source Table: [dbo].[WaterQualityManagementPlanVerifyType]
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Neptune.EFModels.Entities
{
    public abstract partial class WaterQualityManagementPlanVerifyType : IHavePrimaryKey
    {
        public static readonly WaterQualityManagementPlanVerifyTypeJurisdictionPerformed JurisdictionPerformed = WaterQualityManagementPlanVerifyTypeJurisdictionPerformed.Instance;
        public static readonly WaterQualityManagementPlanVerifyTypeSelfCertification SelfCertification = WaterQualityManagementPlanVerifyTypeSelfCertification.Instance;

        public static readonly List<WaterQualityManagementPlanVerifyType> All;
        public static readonly ReadOnlyDictionary<int, WaterQualityManagementPlanVerifyType> AllLookupDictionary;

        /// <summary>
        /// Static type constructor to coordinate static initialization order
        /// </summary>
        static WaterQualityManagementPlanVerifyType()
        {
            All = new List<WaterQualityManagementPlanVerifyType> { JurisdictionPerformed, SelfCertification };
            AllLookupDictionary = new ReadOnlyDictionary<int, WaterQualityManagementPlanVerifyType>(All.ToDictionary(x => x.WaterQualityManagementPlanVerifyTypeID));
        }

        /// <summary>
        /// Protected constructor only for use in instantiating the set of static lookup values that match database
        /// </summary>
        protected WaterQualityManagementPlanVerifyType(int waterQualityManagementPlanVerifyTypeID, string waterQualityManagementPlanVerifyTypeName, string waterQualityManagementPlanVerifyTypeDisplayName)
        {
            WaterQualityManagementPlanVerifyTypeID = waterQualityManagementPlanVerifyTypeID;
            WaterQualityManagementPlanVerifyTypeName = waterQualityManagementPlanVerifyTypeName;
            WaterQualityManagementPlanVerifyTypeDisplayName = waterQualityManagementPlanVerifyTypeDisplayName;
        }

        [Key]
        public int WaterQualityManagementPlanVerifyTypeID { get; private set; }
        public string WaterQualityManagementPlanVerifyTypeName { get; private set; }
        public string WaterQualityManagementPlanVerifyTypeDisplayName { get; private set; }
        [NotMapped]
        public int PrimaryKey { get { return WaterQualityManagementPlanVerifyTypeID; } }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public bool Equals(WaterQualityManagementPlanVerifyType other)
        {
            if (other == null)
            {
                return false;
            }
            return other.WaterQualityManagementPlanVerifyTypeID == WaterQualityManagementPlanVerifyTypeID;
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as WaterQualityManagementPlanVerifyType);
        }

        /// <summary>
        /// Enum types are equal by primary key
        /// </summary>
        public override int GetHashCode()
        {
            return WaterQualityManagementPlanVerifyTypeID;
        }

        public static bool operator ==(WaterQualityManagementPlanVerifyType left, WaterQualityManagementPlanVerifyType right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WaterQualityManagementPlanVerifyType left, WaterQualityManagementPlanVerifyType right)
        {
            return !Equals(left, right);
        }

        public WaterQualityManagementPlanVerifyTypeEnum ToEnum => (WaterQualityManagementPlanVerifyTypeEnum)GetHashCode();

        public static WaterQualityManagementPlanVerifyType ToType(int enumValue)
        {
            return ToType((WaterQualityManagementPlanVerifyTypeEnum)enumValue);
        }

        public static WaterQualityManagementPlanVerifyType ToType(WaterQualityManagementPlanVerifyTypeEnum enumValue)
        {
            switch (enumValue)
            {
                case WaterQualityManagementPlanVerifyTypeEnum.JurisdictionPerformed:
                    return JurisdictionPerformed;
                case WaterQualityManagementPlanVerifyTypeEnum.SelfCertification:
                    return SelfCertification;
                default:
                    throw new ArgumentException("Unable to map Enum: {enumValue}");
            }
        }
    }

    public enum WaterQualityManagementPlanVerifyTypeEnum
    {
        JurisdictionPerformed = 1,
        SelfCertification = 2
    }

    public partial class WaterQualityManagementPlanVerifyTypeJurisdictionPerformed : WaterQualityManagementPlanVerifyType
    {
        private WaterQualityManagementPlanVerifyTypeJurisdictionPerformed(int waterQualityManagementPlanVerifyTypeID, string waterQualityManagementPlanVerifyTypeName, string waterQualityManagementPlanVerifyTypeDisplayName) : base(waterQualityManagementPlanVerifyTypeID, waterQualityManagementPlanVerifyTypeName, waterQualityManagementPlanVerifyTypeDisplayName) {}
        public static readonly WaterQualityManagementPlanVerifyTypeJurisdictionPerformed Instance = new WaterQualityManagementPlanVerifyTypeJurisdictionPerformed(1, @"Jurisdiction Performed", @"Jurisdiction Performed");
    }

    public partial class WaterQualityManagementPlanVerifyTypeSelfCertification : WaterQualityManagementPlanVerifyType
    {
        private WaterQualityManagementPlanVerifyTypeSelfCertification(int waterQualityManagementPlanVerifyTypeID, string waterQualityManagementPlanVerifyTypeName, string waterQualityManagementPlanVerifyTypeDisplayName) : base(waterQualityManagementPlanVerifyTypeID, waterQualityManagementPlanVerifyTypeName, waterQualityManagementPlanVerifyTypeDisplayName) {}
        public static readonly WaterQualityManagementPlanVerifyTypeSelfCertification Instance = new WaterQualityManagementPlanVerifyTypeSelfCertification(2, @"Self Certification", @"Self Certification");
    }
}